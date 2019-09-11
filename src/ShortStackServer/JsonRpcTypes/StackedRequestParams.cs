// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Runtime.Serialization;
    using Microsoft.Tools.Productivity.ShortStack;

    /// <summary>
    /// Contains the information necessary to go to a stack level.
    /// </summary>
    [DataContract]
    public class StackedRequestParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackedRequestParams"/> class.
        /// </summary>
        /// <param name="stackInfo">The stack witch contains the level to switch to.</param>
        /// <param name="stackLevel">The stack level to switch to.</param>
        public StackedRequestParams(StackInfo stackInfo, StackLevel stackLevel)
        {
            this.StackInfo = stackInfo;
            this.StackLevel = stackLevel;
        }

        /// <summary>
        /// Gets the stack describing current environment.
        /// </summary>
        [DataMember(Name = "stackInfo")]
        public StackInfo StackInfo { get; }

        /// <summary>
        /// Gets the specific stack level we are concerned about (optional).
        /// </summary>
        [DataMember(Name = "stackLevel")]
        public StackLevel StackLevel { get; }
    }
}
