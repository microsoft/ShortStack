using LibGit2Sharp;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Model for handling information about a level in a stack
    /// </summary>
    //---------------------------------------------------------------------------------
    [DataContract]
    public class StackLevel
    {
        /// <summary>
        /// The number of this level
        /// </summary>
        [DataMember(Name = "number")]
        public int Number { get; set; }

        /// <summary>
        /// Text from the most recent commit
        /// </summary>
        [DataMember(Name = "recentCommitDescription")]
        public string RecentCommitDescription { get; set; }

        /// <summary>
        /// The number of this level
        /// </summary>
        [DataMember(Name = "isCurrent")]
        public bool IsCurrent { get; set; }

        /// <summary>
        /// Name of the stack
        /// </summary>
        [DataMember(Name = "stackName")]
        public string StackName { get; set; }

        /// <summary>
        /// The repository that has this level
        /// </summary>
        [DataMember(Name = "repositoryUrl")]
        public string RepositoryUrl { get; set; }

        /// <summary>
        /// The branch to pull from
        /// </summary>
        [DataMember(Name = "originBranch")]
        public string OriginBranch { get; set; }

        /// <summary>
        /// The branch to push to
        /// </summary>
        [DataMember(Name = "targetOriginBranch")]
        public string TargetOriginBranch { get; set; }

        /// <summary>
        /// The branch on this PC
        /// </summary>
        [DataMember(Name = "localbranch")]
        public string LocalBranch { get; set; }

        /// <summary>
        /// Commits that have not been pushed to TargetOriginBranch
        /// (This can take time to discover)
        /// </summary>
        [DataMember(Name = "allCommits")]
        public StackCommit[] UnpushedCommits { get; set; }

        /// <summary>
        /// Commits that have not been pulled from OriginBranch 
        /// (This can take time to discover)
        /// </summary>
        [DataMember(Name = "allCommits")]
        public StackCommit[] UnpulledCommits { get; set; }

        /// <summary>
        /// All commits made on the current level
        /// (This can take time to discover)
        /// </summary>
        [DataMember(Name = "allCommits")]
        public StackCommit[] AllCommits { get; set; }

        /// <summary>
        /// Pull request for this stack level.  (Created by ss-push)
        /// </summary>
        [DataMember(Name = "pullRequest")]
        public StackPullRequest PullRequest { get; set; }


        /// <summary>
        /// The information about the stack that contains this level
        /// </summary>
        [DataMember(Name = "stack")]
        public StackInfo Stack { get; internal set; }

        /// <summary>
        /// This is how we identify that a branch is part of a stack
        /// </summary>
        public static Regex BranchNameMatcher = new Regex("(.*)/ss(\\d{3})$", RegexOptions.IgnoreCase);

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        internal static StackLevel TryCreate(Branch branch)
        {
            var match = BranchNameMatcher.Match(branch.FriendlyName);
            if (!match.Success) return null;

            var stackName = match.Groups[1].Value;
            var number = int.Parse(match.Groups[2].Value);

            var recentCommit = branch.Commits.FirstOrDefault();

            var newLevel = new StackLevel()
            {
                StackName = stackName,
                RecentCommitDescription = recentCommit?.Message,
                Number = number,
                OriginBranch = branch.RemoteName + "/" + branch.UpstreamBranchCanonicalName.Replace("refs/heads/", ""),
                TargetOriginBranch = branch.RemoteName + "/" + branch.FriendlyName,
                LocalBranch = branch.FriendlyName,
                IsCurrent = branch.IsCurrentRepositoryHead,
            };
            return newLevel;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Fill in the details of this object from the other object
        /// </summary>
        //---------------------------------------------------------------------------------
        public void FillDetails(IShortStackHandler handler)
        {
            var source = handler.GetLevelDetails(Stack, this);
            this.PullRequest = source.PullRequest;
            this.UnpulledCommits = source.UnpulledCommits;
            this.UnpushedCommits = source.UnpushedCommits;
            this.AllCommits = source.AllCommits;
        }
    }
}
