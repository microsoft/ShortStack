// <copyright file="IBroadcastClientNotify.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer
{
    /// <summary>
    /// Encapsulates the notifications that are sent to all clients.
    /// </summary>
    public interface IBroadcastClientNotify
    {
        /// <summary>
        /// Sends a new stack created notification.
        /// </summary>
        /// <param name="stackName">The name of the new stack.</param>
        void NewStackCreated(string stackName);
    }
}
