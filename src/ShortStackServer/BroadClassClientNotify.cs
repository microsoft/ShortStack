// <copyright file="BroadClassClientNotify.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Implements the functionality to notify all clients.
    /// </summary>
    internal class BroadClassClientNotify : IBroadcastClientNotify
    {
        /// <summary>
        /// A lock that will protect the list of RPC servers.
        /// </summary>
        private readonly ReaderWriterLockSlim rpcServersLock;

        /// <summary>
        /// A list of RPC servers.
        /// </summary>
        private readonly List<Server> rpcServers;

        /// <summary>
        /// Initializes a new instance of the <see cref="BroadClassClientNotify"/> class.
        /// </summary>
        /// <param name="rpcServersLock">A lock used to protect access to the RPC server list. It is passed in by the main server program.</param>
        /// <param name="rpcServers">The list of the RPC servers. It is passed in by the main server program.</param>
        public BroadClassClientNotify(ReaderWriterLockSlim rpcServersLock, List<Server> rpcServers)
        {
            this.rpcServers = rpcServers;
            this.rpcServersLock = rpcServersLock;
        }

        private IEnumerable<Server> RunningServers
        {
            get
            {
                this.rpcServersLock.EnterReadLock();
                var copiedServers = new Server[this.rpcServers.Count];
                this.rpcServers.CopyTo(copiedServers);
                this.rpcServersLock.ExitReadLock();

                return copiedServers;
            }
        }

        /// <summary>
        /// Sends a new stack created notification.
        /// </summary>
        /// <param name="stackName">The name of the newly created stack.</param>
        public void NewStackCreated(string stackName)
        {
            foreach (var server in this.RunningServers)
            {
                try
                {
                    server.SendNotification(stackName);
                }
                catch
                {
                }
            }
        }
    }
}
