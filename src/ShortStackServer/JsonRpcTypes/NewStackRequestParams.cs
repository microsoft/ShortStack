// <copyright file="NewStackRequestParams.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Parameters for New Stack Request.
    /// </summary>
    [DataContract]
    public class NewStackRequestParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewStackRequestParams"/> class.
        /// </summary>
        /// <param name="startPath">Path for locating the repository.</param>
        /// <param name="stackName">Name of the new stack.</param>
        /// <param name="desiredOriginBranch">Origin branch to track.</param>
        public NewStackRequestParams(string startPath, string stackName, string desiredOriginBranch)
        {
            this.StartPath = startPath;
            this.StackName = stackName;
            this.DesiredOriginBranch = desiredOriginBranch;
        }

        /// <summary>
        /// Gets Path to start from.
        /// </summary>
        [DataMember(Name = "startPath")]
        public string StartPath { get; }

        /// <summary>
        /// Gets Name of the new stack.
        /// </summary>
        [DataMember(Name = "stackName")]
        public string StackName { get; }

        /// <summary>
        /// Gets Origin branch to track.
        /// </summary>
        [DataMember(Name = "desiredOriginBranch")]
        public string DesiredOriginBranch { get; }
    }
}
