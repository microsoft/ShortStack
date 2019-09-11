// <copyright file="StackInfoRequestResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.Tools.Productivity.ShortStack;

    /// <summary>
    /// Contains the information used to request stack information.
    /// </summary>
    [DataContract]
    public class StackInfoRequestResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackInfoRequestResponse"/> class.
        /// </summary>
        /// <param name="stackInfo">An instance of <see cref="StackInfo"/> sent in the response.</param>
        public StackInfoRequestResponse(IEnumerable<StackInfo> stackInfo)
        {
            this.StackInfo = stackInfo;
        }

        /// <summary>
        /// Gets the name of the file opened in the editor.
        /// </summary>
        [DataMember(Name = "stackInfo")]
        public IEnumerable<StackInfo> StackInfo { get; }
    }
}
