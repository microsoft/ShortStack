// <copyright file="NormalizeStackRequestParams.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Runtime.Serialization;
    using Microsoft.Tools.Productivity.ShortStack;

    /// <summary>
    /// Contains the information used to request to normalize a stack.
    /// </summary>
    [DataContract]
    public class NormalizeStackRequestParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NormalizeStackRequestParams"/> class.
        /// </summary>
        /// <param name="stackInfo">The stack to normalize.</param>
        /// <param name="normalizeWithOrigin">True to normalize against origin.</param>
        public NormalizeStackRequestParams(StackInfo stackInfo, bool normalizeWithOrigin)
        {
            this.StackInfo = stackInfo;
            this.NoramlizeWithOrigin = normalizeWithOrigin;
        }

        /// <summary>
        /// Gets the stack information.
        /// </summary>
        [DataMember(Name = "stackInfo")]
        public StackInfo StackInfo { get; }

        /// <summary>
        /// Gets a value indicating whether to normalize (update) with the origin.
        /// </summary>
        [DataMember(Name = "noramlizeWithOrigin")]
        public bool NoramlizeWithOrigin { get; }
    }
}
