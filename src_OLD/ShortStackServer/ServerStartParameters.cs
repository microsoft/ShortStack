// <copyright file="ServerStartParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Data structure used to propagate information from secondary instances to the primary instance.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ServerStartParameters
    {
        /// <summary>
        /// True to signal a debugger launch on the primary instance.
        /// </summary>
        public bool DebugOnStart;

        /// <summary>
        /// Forces the NMake language server to use a new instance of connecting to an existing one.
        /// </summary>
        public bool ForceNewInstance;

        /// <summary>
        /// The pipe name passed to the secondary instance that the primary instance needs to create
        /// and connect to.
        /// </summary>
        public string PipeName;

        /// <summary>
        /// The event name that the secondary instance is waiting on the primary instance to signal.
        /// </summary>
        public string DataReadEventName;
    }
}
