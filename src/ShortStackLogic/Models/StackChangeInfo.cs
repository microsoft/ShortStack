using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// A commit to a stack level
    /// </summary>
    //---------------------------------------------------------------------------------
    [DataContract]
    public class StackChangeInfo
    {
        /// <summary>
        /// Description from the commit
        /// </summary>
        public string Description { get; set; }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        public StackChangeInfo(string description)
        {
            Description = description;
        }
    }
}
