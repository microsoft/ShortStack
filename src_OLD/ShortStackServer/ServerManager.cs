// <copyright file="ServerManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.MemoryMappedFiles;
    using System.IO.Pipes;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Tools.Productivity.ShortStack.Utilities;
    using ShortStackServer.Utilities;

    /// <summary>
    /// The main entry point for the ShortStack JSON-RPC Server.
    /// </summary>
    public class ServerManager
    {
        /// <summary>
        /// How long (in milliseconds) a JSON-RPC thread will wait for a its pipe to get connected.
        /// </summary>
        private const int JsonRpcPipeWaitTime = 5000;

        /// <summary>
        /// In the unlikely event that there are multiple secondary instances trying to start
        /// all at once and connect to the primary instance, this is the wait time (in milliseconds)
        /// that the secondary instance will wait trying to create a new memory mapped file.
        /// </summary>
        private const int MemoryMappedFileCreateRetryWaitTime = 1000;

        /// <summary>
        /// In the unlikely event that there are multiple secondary instances trying to start
        /// all at once and connect to the primary instance, this the number of times a retry
        /// will be attempted to create a new memory mapped file.
        /// </summary>
        private const int NumberOfMemoryMappedFileCreationAttempts = 3;

        /// <summary>
        /// Error code that is thrown when the memory mapped file already exists (which can happen if you have
        /// many instances firing up and trying to signal the first instance).
        /// </summary>
        private const int AlreadyExistsErrorCode = unchecked((int)0x800700b7);

        /// <summary>
        /// This is the name of the event that is used to determine if another instance of this
        /// process is already running.
        /// </summary>
        private static string singleInstanceMutexName;

        /// <summary>
        /// This is the name of the event that is used to determine if a secondary instance has launched
        /// and set its command line information in the memory mapped file created by the first instance.
        /// </summary>
        private static string memoryMappedFileDataReadyEventName;

        /// <summary>
        /// Used for the name of a memory mapped file to re-use this instance if we have multiple consumers running.
        /// The secondary instance(s) write data to this event.
        /// </summary>
        private static string fileMappingName;

        /// <summary>
        /// The mutex that is checked to see if an instance is already running.
        /// </summary>
        private static Mutex alreadyRunningMutex;

        /// <summary>
        /// Used to block the main thread until all RPC threads have completed.
        /// </summary>
        private static int rpcThreads;

        /// <summary>
        /// A lock that will protect the list of RPC servers.
        /// </summary>
        private static ReaderWriterLockSlim rpcServersLock = new ReaderWriterLockSlim();

        /// <summary>
        /// A list of RPC servers.
        /// </summary>
        private static List<Server> rpcServers = new List<Server>();

        /// <summary>
        /// An interface passed to all the JSON-RPC threads that they can use to broadcast notifications to all clients.
        /// </summary>
        private static IBroadcastClientNotify broadcastClientNotify;

        /// <summary>
        /// The server entry point.
        /// </summary>
        /// <remarks>
        /// The server can only handle two command line options.
        /// The first is the local pipe name (--pipe) used for the JSON-RPC messages.D:\git\ShortStack\src\ShortStackServer\Program.cs
        /// The second can be used to break into the server on launch (--debugOnStart).
        /// </remarks>
        /// <param name="startParameters">Startup parameters.</param>
        /// <returns>A task for the server.</returns>
        public static Task KickOff(ServerStartParameters startParameters)
        {
            return Tasks.PerformLongRunningOperation(
                () =>
                {
                    broadcastClientNotify = new BroadClassClientNotify(rpcServersLock, rpcServers);

                    // The server will attempt to connect to an already running instance unless explicitly asked by the client to not do so.
                    if (startParameters.ForceNewInstance)
                    {
                        using (var threadsDone = new EventWaitHandle(false, EventResetMode.ManualReset))
                        {
                            CreateRPCServerThread(threadsDone, startParameters);

                            threadsDone.WaitOne();
                        }
                    }
                    else
                    {
                        // This section of code handles making sure there is only a single instance of this server.

                        // The client has asked us to attempt to use a single instance. So set up the
                        // names used for the mutex, semaphore and memory mapped file.
                        SetupSingleInstanceNames();

                        // Create the named mutex that ensures that the process only runs once.

                        // If the mutex was already created, this signifies that there is already an instance
                        // of the application running.
                        alreadyRunningMutex = new Mutex(true, singleInstanceMutexName, out var createdNew);

                        if (createdNew)
                        {
                            StartupPrimaryInstance(startParameters);
                        }
                        else
                        {
                            HandleSecondaryInstance(startParameters);
                        }
                    }
                }, CancellationToken.None);
        }

        /// <summary>
        /// Handles startup of a secondary instance of the NMake JSON-RPC server.
        /// </summary>
        /// <param name="commandLineInformation">The command line information (debug on start, pipe name, etc.).</param>
        private static void StartupPrimaryInstance(ServerStartParameters commandLineInformation)
        {
            using (var threadsDone = new EventWaitHandle(false, EventResetMode.ManualReset))
            {
                using (var mappedDataReadySemaphore = new Semaphore(0, 1, memoryMappedFileDataReadyEventName))
                {
                    // In this case, we are the primary instance. Create a thread-pool wait for the memory mapped
                    // file to be written and signaled to be ready.
                    var mappedFileDataReadythreadPoolWait = ThreadPool.RegisterWaitForSingleObject(
                        mappedDataReadySemaphore,
                        (state, timedout) => // This starts the callback that occurs when the semaphore is released by another instance beginning.
                        {
                            if (timedout)
                            {
                                return;
                            }

                            // Open up a memory mapped file and read the command line data
                            // from the secondary instance.
                            using (var fileMapping = MemoryMappedFile.OpenExisting(fileMappingName))
                            {
                                using (var fileMapStream = fileMapping.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                                {
                                    var bf = new BinaryFormatter();
                                    var secondaryInstanceCommandLineInformation = (ServerStartParameters)bf.Deserialize(fileMapStream);

                                    // Let the first instance know that we are done. We no longer need to block
                                    // it while creating the thread.
                                    using (var dataReadEvent = new EventWaitHandle(false, EventResetMode.ManualReset, secondaryInstanceCommandLineInformation.DataReadEventName))
                                    {
                                        dataReadEvent.Set();
                                    }

                                    // Now, go ahead and start up a thread to handle the JSON-RPC threads on behalf of the secondary instance.
                                    // Don't confuse this with starting the primary instance JSON-RPC thread. That happens below.
                                    CreateRPCServerThread(threadsDone, secondaryInstanceCommandLineInformation);
                                }
                            }
                        },
                        state: null,
                        millisecondsTimeOutInterval: -1,
                        executeOnlyOnce: false);

                    // This is the primary instance, so go ahead and create our JSON-RPC thread.
                    CreateRPCServerThread(threadsDone, commandLineInformation);

                    // Now wait for all the JSON-RPC instances to be complete.
                    // This blocks the main thread of the primary instance.
                    threadsDone.WaitOne();

                    // Now, shutdown the thread pool wait.
                    mappedFileDataReadythreadPoolWait.Unregister(null);
                }
            }
        }

        /// <summary>
        /// Handles startup of a secondary instance of the JSON-RPC server.
        /// </summary>
        /// <param name="commandLineInformation">The command line information (debug on start, pipe name, etc.).</param>
        private static void HandleSecondaryInstance(ServerStartParameters commandLineInformation)
        {
            // Create the named semaphore that lets the primary (first) instance know that there is data ready (command line arguments)
            // to read from a memory mapped file.
            using (var mappedDataReadySemaphore = new Semaphore(0, 1, memoryMappedFileDataReadyEventName))
            {
                var dataReadEventName = Guid.NewGuid().ToString();
                commandLineInformation.DataReadEventName = dataReadEventName;

                int retries = 0;
                bool shouldRetry = true;

                // This loop will only retry if the memory mapped file already
                // exists, indicating multiple instances are starting up and attempting to start and signal
                // the primary instance at the same time. This will be extremely rare (if it ever happens).
                // Which is the reason it is only done on specifically an already exists exception.
                while (shouldRetry && (retries++) < NumberOfMemoryMappedFileCreationAttempts)
                {
                    shouldRetry = false;
                    try
                    {
                        // This instance is a secondary instance. Signal the primary instance to start a JSON-RPC thread for us.
                        // There is a slim possibility that another instance is trying to do the same thing.
                        // So, we catch the already exist exception (and only that exception, and only that error code)
                        // and retry up to three times.
                        using (var fileMapping = MemoryMappedFile.CreateNew(fileMappingName, 1024, MemoryMappedFileAccess.ReadWrite))
                        {
                            // Write the data into the memory mapped file for the primary instance.
                            using (var fileMapStream = fileMapping.CreateViewStream(0, 0, MemoryMappedFileAccess.Write))
                            {
                                var bf = new BinaryFormatter();
                                bf.Serialize(fileMapStream, commandLineInformation);
                            }

                            using (var dataReadEvent = new EventWaitHandle(false, EventResetMode.ManualReset, dataReadEventName))
                            {
                                // Signal (release) the primary instance.
                                mappedDataReadySemaphore.Release(1);

                                // Wait for the primary instance to be complete.
                                dataReadEvent.WaitOne();
                            }
                        }
                    }
                    catch (System.IO.IOException e) when (e.HResult == AlreadyExistsErrorCode)
                    {
                        // If the memory mapped file already exists, then let's retry
                        // again after 1 second.
                        shouldRetry = true;
                        Thread.Sleep(MemoryMappedFileCreateRetryWaitTime);
                    }
                }
            }
        }

        /// <summary>
        /// Starts a JSON-RPC server to handle client calls.
        /// </summary>
        /// <param name="commandLineInformation">The command line information (debug on start, pipe name, etc.).</param>
        /// <remarks>
        /// This function does not return until the JSON-RPC server exits.</remarks>
        private static void StartJsonRpcServer(ServerStartParameters commandLineInformation)
        {
            Debug.WriteLine("Starting server connection on " + commandLineInformation.PipeName);

            var jsonRpcStream = new NamedPipeClientStream(".", commandLineInformation.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                // The connect call will throw if the connection cannot be established within 5 seconds.
                // If a debugger is attached, wait indefinitely to aid debugging.
                if (commandLineInformation.DebugOnStart && Debugger.IsAttached)
                {
                    jsonRpcStream.Connect();
                }
                else
                {
                    jsonRpcStream.Connect(JsonRpcPipeWaitTime);
                }

                using (var server = new Server(jsonRpcStream, broadcastClientNotify))
                {
                    rpcServersLock.EnterWriteLock();
                    rpcServers.Add(server);
                    rpcServersLock.ExitWriteLock();

                    // Now for a bit of fun. Since VS doesn't like to shutdown properly (or it crashes a lot)
                    // we watch the pipe owner process, and if it goes away, we exit ourselves, so we don't stay around forever
                    if (NativeMethods.TryGetNamedPipeServerProcessId(jsonRpcStream, out var serverProcessId))
                    {
                        try
                        {
                            var serverProcess = Process.GetProcessById((int)serverProcessId);
                            ThreadPool.RegisterWaitForSingleObject(
                                waitObject: new ProcessWaitHandle(serverProcess),
                                callBack: (state, timedOut) =>
                                {
                                    server.TryExit();
                                },
                                state: null,
                                millisecondsTimeOutInterval: -1,
                                executeOnlyOnce: true);
                        }
                        catch (ArgumentException)
                        {
                            // This will get thrown if for some reason our server that created
                            // the named pipe exits before we get a chance to watch it :)
                        }
                    }

                    // So if the server starts, we don't want to do Dispose twice.
                    jsonRpcStream = null;

                    server.WaitForExit();

                    rpcServersLock.EnterWriteLock();
                    rpcServers.Add(server);
                    rpcServersLock.ExitWriteLock();
                }
            }
            catch (TimeoutException)
            {
            }

            if (jsonRpcStream != null)
            {
                jsonRpcStream.Dispose();
            }
        }

        /// <summary>
        /// Starts a JSON-RPC thread to handle client calls.
        /// </summary>
        /// <param name="threadsDoneEvent">The event to set when all RPC threads are finished.</param>
        /// <param name="commandLineInformation">The command line information (debug on start, pipe name, etc.).</param>
        /// <remarks>
        /// This code only catches exceptions it expects to encounter. If there is some other
        /// type of exception, can't start thread, can't create pipe, etc., then
        /// we want the process to crash so we can figure out the conditions under which
        /// it occurs and address it.
        /// The event source is owned by the new thread, and it must dispose of it when finished.
        /// </remarks>
        private static void CreateRPCServerThread(EventWaitHandle threadsDoneEvent, ServerStartParameters commandLineInformation)
        {
            if (commandLineInformation.DebugOnStart)
            {
                Debugger.Launch();
            }

            new Thread(new ThreadStart(() =>
            {
                Interlocked.Increment(ref rpcThreads);

                StartJsonRpcServer(commandLineInformation);

                var numberOfThreadsRemaining = Interlocked.Decrement(ref rpcThreads);

                // Release our mutex is trying to reduce the chance that while we are exiting
                // another process is trying to send command line information to us.
                if (numberOfThreadsRemaining == 0)
                {
                    alreadyRunningMutex?.Dispose();
                }

                if (numberOfThreadsRemaining == 0)
                {
                    // We need to send our events and dispose our event source
                    // before signaling all threads done. Otherwise
                    // the main thread exits and we lose events.
                    threadsDoneEvent.Set();
                }
            })).Start();
        }

        /// <summary>
        /// Sets up the local session named objects that are used to connect secondary
        /// instances to primary instances.
        /// </summary>
        private static void SetupSingleInstanceNames()
        {
            // Hash the assembly's running location into the single instance name so that we
            // don't collide when we auto update a VSCode extension (which occurs to a new directory because)
            // it has the version path in it. So if there are multiple VSCode clients running, the one
            // that actually auto-updates the VSCode extension and reloads will get the new fresh version of the server
            // instead of trying to connect to the old one.
            var hashAssemblyLocation = Assembly.GetExecutingAssembly().Location.GetHashCode().ToString();
            singleInstanceMutexName = "{AA446B3C-FFA8-4085-8179-AC952D69E65F}" + $"_{hashAssemblyLocation}";
            memoryMappedFileDataReadyEventName = $"DataReady-{singleInstanceMutexName}";
            fileMappingName = $"DataRead-{singleInstanceMutexName}";
        }
    }
}
