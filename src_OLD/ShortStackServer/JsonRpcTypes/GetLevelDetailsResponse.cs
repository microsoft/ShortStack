// <copyright file="GetLevelDetailsResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ShortStackServer.JsonRpcTypes
{
    using System.Runtime.Serialization;
    using Microsoft.Tools.Productivity.ShortStack;

    /// <summary>
    /// Contains the response from getting stack level details.
    /// </summary>
    [DataContract]
    public class GetLevelDetailsResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetLevelDetailsResponse"/> class.
        /// </summary>
        /// <param name="stackLevel">An instance of <see cref="StackLevel"/> sent in the response.</param>
        public GetLevelDetailsResponse(StackLevel stackLevel)
        {
            this.StackLevel = stackLevel;
        }

        /// <summary>
        /// Gets a description of a stack level.
        /// </summary>
        [DataMember(Name = "stackLevel")]
        public StackLevel StackLevel { get; }
    }
}
