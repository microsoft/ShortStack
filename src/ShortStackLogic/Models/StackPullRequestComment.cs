using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    [DataContract]
    public class StackPullRequestComment
    {
        /// <summary>
        /// Content of this comment
        /// </summary>
        [DataMember(Name = "content")]
        public string Content { get; set; }

        /// <summary>
        /// Author name of this comment
        /// </summary>
        [DataMember(Name = "author")]
        public string Author { get; set; }

        /// <summary>
        /// Date of the comment
        /// </summary>
        [DataMember(Name = "date")]
        public DateTime Date { get; set; }
    }
}
