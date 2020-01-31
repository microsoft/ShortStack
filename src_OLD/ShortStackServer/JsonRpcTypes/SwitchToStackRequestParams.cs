// <copyright file="SwitchToStackRequestParams.cs" company="Microsoft">
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
    public class SwitchToStackRequestParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchToStackRequestParams"/> class.
        /// </summary>
        /// <param name="stackInfo">The stack witch contains the level to switch to.</param>
        /// <param name="stackLevel">The stack level to switch to.</param>
        public SwitchToStackRequestParams(StackInfo stackInfo, StackLevel stackLevel)
        {
            this.StackInfo = stackInfo;
            this.StackLevel = stackLevel;
        }

        /// <summary>
        /// Gets the stack witch contains the to switch to.
        /// </summary>
        [DataMember(Name = "stackInfo")]
        public StackInfo StackInfo { get; }

        /// <summary>
        /// Gets the stack level to switch to.
        /// </summary>
        [DataMember(Name = "stackLevel")]
        public StackLevel StackLevel { get; }
    }
}
