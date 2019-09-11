// <copyright file="ProcessWaitHandle.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.Utilities
{
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Encapsulates a process into a wait handle using <see cref="SafeWaitHandle"/>.
    /// </summary>
    internal class ProcessWaitHandle : WaitHandle
    {
        private readonly Process process;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessWaitHandle"/> class.
        /// </summary>
        /// <param name="process">The process to convert to a WaitHandle.</param>
        public ProcessWaitHandle(Process process)
        {
            this.process = process;
            this.SafeWaitHandle = new SafeWaitHandle(process.Handle, false);
        }
    }
}
