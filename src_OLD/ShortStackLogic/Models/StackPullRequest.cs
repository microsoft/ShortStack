using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{

    [DataContract]
    public class StackReviewer
    {
        public int vote { get; set; }
        public string displayName { get; set; }
        public string imageUrl { get; set; }
    }

    [DataContract]
    public class StackPullRequest
    {
        public long pullRequestId { get; set; }
        public long codeReviewId { get; set; }
        public string status { get; set; }
        public DateTime creationDate { get; set; }
        public string title { get; set; }
        public string sourceRefName { get; set; }
        public string targetRefName { get; set; }
        public string mergeStatus { get; set; }
        public StackReviewer[] reviewers { get; set; }
        public string url { get; set; }
        public string description { get; set; }
    }
}