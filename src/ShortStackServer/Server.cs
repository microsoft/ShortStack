// <copyright file="Server.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Tools.Productivity.ShortStack;
    using Microsoft.Tools.Productivity.ShortStack.Utilities;
    using Microsoft.Tools.Productivity.ShortStackLogic.Constants;
    using Newtonsoft.Json.Linq;
    using ShortStackServer.JsonRpcTypes;
    using StreamJsonRpc;

    /// <summary>
    /// Encapsulates the Short-Stack server.
    /// </summary>
    internal class Server : IDisposable
    {
        /// <summary>
        /// This event is signaled when the JSON-RPC server is asked to exit. Frees up the main thread this is waiting.
        /// </summary>
        private readonly ManualResetEvent exitEvent = new ManualResetEvent(false);

        /// <summary>
        /// And interface used to broadcast notifications to the clients.
        /// </summary>
        private readonly IBroadcastClientNotify broadcastClientNotify;

        /// <summary>
        /// Holds the RPC channel created from the pipe stream passed in.
        /// </summary>
        private JsonRpc mainRpcChannel;

        /// <summary>
        /// Used to implement the dispose pattern and prevents multiple disposals.
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Protects <see cref="exitAlreadyRequested"/> from multiple threads.
        /// </summary>
        private object exitRequestedLock = new object();

        /// <summary>
        /// Set to true if an exit has already been requested.
        /// </summary>
        private bool exitAlreadyRequested = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="pipeStream">The named pipe stream for the incoming RPC messages.</param>
        /// <param name="broadcastClientNotify">And interface used to broadcast notifications to the clients.</param>
        public Server(Stream pipeStream, IBroadcastClientNotify broadcastClientNotify)
        {
            this.broadcastClientNotify = broadcastClientNotify;
            this.mainRpcChannel = new JsonRpc(pipeStream, pipeStream, this);
            this.mainRpcChannel.Disconnected += this.OnRpcChannelDisconnected;
            this.mainRpcChannel.StartListening();
        }

        /// <summary>
        /// Implements <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }

        /// <summary>
        /// Blocks the calling thread until <see cref="exitEvent"/> is set.
        /// </summary>
        /// <remarks>
        /// This is called after the constructor runs and the JSON-RPC server has been set up.
        /// </remarks>
        public void WaitForExit()
        {
            this.exitEvent.WaitOne();
        }

        /// <summary>
        /// This is an implementation of the "sayHello"  JSON-RPC method.
        /// </summary>
        /// <param name="token">The incoming parameters as a JSON token.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        /// <returns>Returns a greeting after waiting 1 second.</returns>
        [JsonRpcMethod("getLevelDetails")]
        public Task<GetLevelDetailsResponse> GetLevelDetails(JToken token, CancellationToken cancellationToken)
        {
            return Tasks.PerformLongRunningOperation(
               () =>
               {
                   var parameters = token.ToObject<StackedRequestParams>();
                   var processor = new ShortStackProcessor(parameters.StackInfo.RepositoryRootPath);
                   var outputLevel = processor.GetLevelDetails(parameters.StackLevel);
                   return new GetLevelDetailsResponse(outputLevel);
               },
               cancellationToken);

        }

        /// <summary>
        /// Implements retrieving the stack-information from a file path.
        /// </summary>
        /// <param name="token">The incoming parameters as a JSON token.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        /// <returns>Returns the stack information for the given file path.</returns>
        [JsonRpcMethod("getStackInfoFromFilePath")]
        public Task<StackInfoRequestResponse> GetStackInfoAsync(JToken token, CancellationToken cancellationToken)
        {
            return Tasks.PerformLongRunningOperation(
                () =>
                {
                    var parameters = token.ToObject<NonStackRequestParams>();
                    var processor = new ShortStackProcessor(parameters.StartPath);
                    return new StackInfoRequestResponse(processor.Stacks.Values);
                },
                cancellationToken);
        }

        /// <summary>
        /// Implements creating a new stack.
        /// </summary>
        /// <param name="token">The incoming parameters as a JSON token.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        [JsonRpcMethod("createNewStack")]
        public void NewStack(JToken token, CancellationToken cancellationToken)
        {
            var parameters = token.ToObject<NewStackRequestParams>();
            var processor = new ShortStackProcessor(parameters.StartPath);
            processor.CreateNewStack(parameters.StackName, parameters.DesiredOriginBranch);

            this.broadcastClientNotify.NewStackCreated(parameters.StackName);
        }

        /// <summary>
        /// Implements creating a new stack level.
        /// </summary>
        /// <param name="token">The incoming parameters as a JSON token.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [JsonRpcMethod("createNewStackLevel")]
        public Task CreateNewStackLevelAsync(JToken token, CancellationToken cancellationToken)
        {
            return Tasks.PerformLongRunningOperation(
                () =>
                {
                    var parameters = token.ToObject<StackedRequestParams>();
                    var processor = new ShortStackProcessor(parameters.StackInfo.RepositoryRootPath);
                    processor.CreateNextStackLevel(parameters.StackInfo.StackName);
                }, cancellationToken);
        }

        /// <summary>
        /// Implements creating a new stack level.
        /// </summary>
        /// <param name="token">The incoming parameters as a JSON token.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [JsonRpcMethod("pushStackLevel")]
        public Task PushStackLevel(JToken token, CancellationToken cancellationToken)
        {
            return Tasks.PerformLongRunningOperation(
                () =>
                {
                    var parameters = token.ToObject<PushStackLevelRequestParams>();
                    var processor = new ShortStackProcessor(parameters.StackInfo.RepositoryRootPath);
                    processor.PushStackLevel();
                }, cancellationToken);
        }

        /// <summary>
        /// Switches to an existing stack.
        /// </summary>
        /// <param name="token">The stack information to use for the switch.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        [JsonRpcMethod("switchToStack")]
        public void SwitchToStack(JToken token, CancellationToken cancellationToken)
        {
            var parameters = token.ToObject<SwitchToStackRequestParams>();
            return;
        }

        /// <summary>
        /// Normalizes an existing stack.
        /// </summary>
        /// <param name="token">The stack information to use for the switch.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        [JsonRpcMethod("normalizeStack")]
        public void NormalizeStack(JToken token, CancellationToken cancellationToken)
        {
            var parameters = token.ToObject<NormalizeStackRequestParams>();
            return;
        }

        /// <summary>
        /// Switches to an existing stack level.
        /// </summary>
        /// <param name="token">The stack information to use for the switch.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        [JsonRpcMethod(StringConstants.GoToStackLevel)]
        public void GoToStackLevel(JToken token, CancellationToken cancellationToken)
        {
            Tasks.PerformLongRunningOperation(
                () =>
                {
                    var parameters = token.ToObject<StackedRequestParams>();
                    var processor = new ShortStackProcessor(parameters.StackInfo.RepositoryRootPath);
                    processor.GoToStackLevel(parameters.StackInfo.StackName, parameters.StackLevel.Number);
                    return;
                }, cancellationToken);
        }

        /// <summary>
        /// Pushes a level of the stack.
        /// </summary>
        /// <param name="token">The stack information to use for the switch.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        [JsonRpcMethod("goToPullRequest")]
        public void GoToPullRequest(JToken token, CancellationToken cancellationToken)
        {
            var parameters = token.ToObject<StackedRequestParams>();
            return;
        }

        /// <summary>
        /// Retrieves a list of origin branches from VSTS.
        /// </summary>
        /// <param name="token">The stack information to use for the switch.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        /// <returns>Returns a list of origin branches.</returns>
        [JsonRpcMethod("getOriginBranches")]
        public Task<GetOriginBranchesResponse> GetOriginBranchesAsync(JToken token, CancellationToken cancellationToken)
        {
            return Tasks.PerformLongRunningOperation(
                () =>
                {
                    var parameters = token.ToObject<NonStackRequestParams>();
                    var processor = new ShortStackProcessor(parameters.StartPath);
                    var originBranches = processor.OriginBranches;

                    var branches = new List<OriginBranchInformation>();

                    foreach (var originBranch in originBranches)
                    {
                        branches.Add(new OriginBranchInformation(originBranch.FriendlyName, originBranch.RemoteName, originBranch.CanonicalName));
                    }

                    return new GetOriginBranchesResponse(branches);
                },
                cancellationToken);
        }

        /// <summary>
        /// Checks for Dangling Work.
        /// </summary>
        /// <param name="token">The stack information to use for the switch.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        /// <returns>Result from the Dangling work check.</returns>
        [JsonRpcMethod(StringConstants.CheckForDanglingWork)]
        public Task<DanglingWorkStatus> CheckForDanglingWork(JToken token, CancellationToken cancellationToken)
        {
            return Tasks.PerformLongRunningOperation<DanglingWorkStatus>(
                () =>
                {
                    var parameters = token.ToObject<NonStackRequestParams>();
                    var processor = new ShortStackProcessor(parameters.StartPath);
                    return processor.GetDanglingWorkStatus();
                }, cancellationToken);
        }

        /// <summary>
        /// This is an implementation of the "shutdown" JSON-RPC method.
        /// </summary>
        [JsonRpcMethod("shutdown")]
        public void Shutdown()
        {
            this.TryExit();
        }

        /// <summary>
        /// Sends a notification to the client.
        /// </summary>
        /// <typeparam name="T">The type parameters to the notification.</typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <param name="notificationName">The name of the notification. (Defaults to caller name).</param>
        /// <returns>Returns a task representing the async notification.</returns>
        internal Task SendNotification<T>(T parameters, [CallerMemberName] string notificationName = null)
        {
            string typeScriptNotificationName = string.Format("{0}{1}", notificationName[0].ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture), notificationName.Substring(1));
            return this.mainRpcChannel.NotifyWithParameterObjectAsync(typeScriptNotificationName, parameters);
        }

        /// <summary>
        /// Sends a request to the client.
        /// </summary>
        /// <typeparam name="T">The type parameters to the request.</typeparam>
        /// <typeparam name="TR">The type of the response to the request.</typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <param name="cancellationToken">The cancellation token passed by the client.</param>
        /// <param name="requestName">The name of the notification. (Defaults to caller name).</param>
        /// <returns>Returns a task representing the async notification.</returns>
        internal Task<TR> SendRequestAsync<T, TR>(T parameters, CancellationToken cancellationToken, [CallerMemberName] string requestName = null)
        {
            string typeScriptRequestName = string.Format("{0}{1}", requestName[0].ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture), requestName.Substring(1));
            return this.mainRpcChannel.InvokeWithParameterObjectAsync<TR>(typeScriptRequestName, parameters, cancellationToken);
        }

        /// <summary>
        /// Attempts to exit the JSON-RPC server.
        /// </summary>
        /// <returns>Returns true if an exit request was performed.</returns>
        internal bool TryExit()
        {
            bool performExit = false;

            lock (this.exitRequestedLock)
            {
                if (!this.exitAlreadyRequested)
                {
                    this.exitAlreadyRequested = true;
                    performExit = true;
                }
            }

            if (performExit)
            {
                this.exitEvent.Set();
            }

            return performExit;
        }

        /// <summary>
        /// Disposes the resources (the JSON_RPC channel).
        /// </summary>
        /// <param name="disposing">Indicates whether we are disposing or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.mainRpcChannel?.Dispose();
                    this.mainRpcChannel = null;
                }

                this.disposedValue = true;
            }
        }

        // Raised when the client process closes the named pipe.
        private void OnRpcChannelDisconnected(object sender, JsonRpcDisconnectedEventArgs disconnectedEventArgs)
        {
            this.TryExit();
        }
    }
}
