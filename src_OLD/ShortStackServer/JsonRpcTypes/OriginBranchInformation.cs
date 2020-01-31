// <copyright file="OriginBranchInformation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Encapsulates information about a remote GIT branch.
    /// </summary>
    [DataContract]
    public class OriginBranchInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OriginBranchInformation"/> class.
        /// </summary>
        /// <param name="friendlyName">The friendly name of the branch.</param>
        /// <param name="remoteName">The remote name of the branch.</param>
        /// <param name="canonicalName">The canonical name of the branch.</param>
        public OriginBranchInformation(string friendlyName, string remoteName, string canonicalName)
        {
            this.FriendlyName = friendlyName;
            this.RemoteName = remoteName;
            this.CanonicalName = canonicalName;
        }

        /// <summary>
        /// Gets the friendly name of the branch.
        /// </summary>
        [DataMember(Name = "friendlyName")]
        public string FriendlyName { get;  }

        /// <summary>
        /// Gets the remote name of the branch.
        /// </summary>
        [DataMember(Name = "remoteName")]
        public string RemoteName { get; }

        /// <summary>
        /// Gets the canonical name of the branch.
        /// </summary>
        [DataMember(Name = "CanonicalName")]
        public string CanonicalName { get; }
    }
}
