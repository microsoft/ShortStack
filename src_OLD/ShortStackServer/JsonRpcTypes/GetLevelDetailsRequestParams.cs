// <copyright file="GetLevelDetailsRequestParams.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Runtime.Serialization;
    using Microsoft.Tools.Productivity.ShortStack;

    /// <summary>
    /// Contains the information necessary to get details on a stack level.
    /// </summary>
    [DataContract]
    public class GetLevelDetailsRequestParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetLevelDetailsRequestParams"/> class.
        /// </summary>
        /// <param name="stackLevel">The stack level to switch to.</param>
        public GetLevelDetailsRequestParams(StackLevel stackLevel)
        {
            this.StackLevel = stackLevel;
        }

        /// <summary>
        /// Gets the stack level to switch to.
        /// </summary>
        [DataMember(Name = "stackLevel")]
        public StackLevel StackLevel { get; }
    }
}
