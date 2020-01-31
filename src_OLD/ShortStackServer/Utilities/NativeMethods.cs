// <copyright file="NativeMethods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.Utilities
{
    using System;
    using System.IO.Pipes;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Encapsulates native methods used by the NMake server because every .Net app needs PInvoke
    /// to be useful.
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>
        /// Returns the server responsible for setting up the named pipe.
        /// </summary>
        /// <param name="pipeServer">The named pipe client stream (i.e. the server pipe the client connected to).</param>
        /// <param name="serverProcessId">On success, the server process id.</param>
        /// <returns>Returns true on success.</returns>
        public static bool TryGetNamedPipeServerProcessId(NamedPipeClientStream pipeServer, out uint serverProcessId)
        {
            serverProcessId = 0;

            IntPtr hPipe = pipeServer.SafePipeHandle.DangerousGetHandle();
            if (GetNamedPipeServerProcessId(hPipe, out var nProcID))
            {
                serverProcessId = nProcID;
                return true;
            }

            return false;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNamedPipeServerProcessId(IntPtr Pipe, out uint serverProcecessId);
    }
}
