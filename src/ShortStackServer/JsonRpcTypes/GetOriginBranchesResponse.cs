// <copyright file="GetOriginBranchesResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Encapsulates the response from a <see cref="OriginBranchInformation"/> to the JSON-RPC server.
    /// </summary>
    [DataContract]
    public class GetOriginBranchesResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetOriginBranchesResponse"/> class.
        /// </summary>
        /// <param name="branches">The origin branches.</param>
        public GetOriginBranchesResponse(IEnumerable<OriginBranchInformation> branches)
        {
            this.Branches = branches;
        }

        /// <summary>
        /// Gets the origin branches.
        /// </summary>
        [DataMember(Name = "branches")]
        public IEnumerable<OriginBranchInformation> Branches { get; }
    }
}
