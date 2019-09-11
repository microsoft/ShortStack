// <copyright file="PushStackLevelRequestParams.cs" company="Microsoft">
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
    public class PushStackLevelRequestParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PushStackLevelRequestParams"/> class.
        /// </summary>
        /// <param name="stackInfo">The stack to push.</param>
        /// <param name="stackLevel">The stack level to switch to.</param>
        public PushStackLevelRequestParams(StackInfo stackInfo, StackLevel stackLevel)
        {
            this.StackInfo = stackInfo;
            this.StackLevel = stackLevel;
        }

        /// <summary>
        /// Gets the stack to push.
        /// </summary>
        [DataMember(Name = "stackInfo")]
        public StackInfo StackInfo { get; }

        /// <summary>
        /// Gets the stack level to push.
        /// </summary>
        [DataMember(Name = "stackLevel")]
        public StackLevel StackLevel { get; }
    }
}
