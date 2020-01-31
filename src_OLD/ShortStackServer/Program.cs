// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer
{
    using System;

    /// <summary>
    /// The main entry point for the ShortStack JSON-RPC Server.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// This is the prefix used for local named pipes.
        /// </summary>
        private static string localPipeNamePrefix = @"\\.\pipe\";

        /// <summary>
        /// The server entry point.
        /// </summary>
        /// <param name="args">The arguments passed to the server.</param>
        /// <remarks>
        /// The server can only handle two command line options.
        /// The first is the local pipe name (--pipe) used for the JSON-RPC messages.
        /// The second can be used to break into the server on launch (--debugOnStart).
        /// </remarks>
        public static void Main(string[] args)
        {
            var argsEnumerator = args.GetEnumerator();
            var commandLineInformation = new ServerStartParameters
            {
                DebugOnStart = false,
                PipeName = null,
                DataReadEventName = null,
                ForceNewInstance = false,
            };

            while (argsEnumerator.MoveNext())
            {
                if (((string)argsEnumerator.Current).Equals("--pipe", StringComparison.OrdinalIgnoreCase))
                {
                    if (argsEnumerator.MoveNext())
                    {
                        if (((string)argsEnumerator.Current).StartsWith(localPipeNamePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            commandLineInformation.PipeName = ((string)argsEnumerator.Current).Substring(localPipeNamePrefix.Length);
                        }
                    }
                }
                else if (((string)argsEnumerator.Current).Equals("--debugOnStart", StringComparison.OrdinalIgnoreCase))
                {
                    commandLineInformation.DebugOnStart = true;
                }
                else if (((string)argsEnumerator.Current).Equals("--forceNewInstance", StringComparison.OrdinalIgnoreCase))
                {
                    commandLineInformation.ForceNewInstance = true;
                }
            }

            if (string.IsNullOrEmpty(commandLineInformation.PipeName))
            {
                throw new ArgumentException("The pipe name was not passed on the command line.");
            }

            ServerManager.KickOff(commandLineInformation);
        }
    }
}
