// <copyright file="NonStackRequestParams.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Parameters that are needed for all RPC calls.
    /// </summary>
    [DataContract]
    public class NonStackRequestParams
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="startPath">The starting path to locate origin VSTS branches.</param>
        public NonStackRequestParams(string startPath)
        {
            this.StartPath = startPath;
        }

        /// <summary>
        /// Gets the starting path to locate origin branches.
        /// </summary>
        [DataMember(Name ="startPath")]
        public string StartPath { get; }
    }
}
