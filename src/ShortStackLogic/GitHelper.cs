using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Access Git through this
    /// </summary>
    //---------------------------------------------------------------------------------
    internal class GitHelper
    {
        private const string REFS_HEAD = @"refs/heads/";
        private const string REFS_REMOTES = @"refs/remotes/";
        /// <summary>
        /// Handle to the local repository
        /// </summary>
        public Repository LocalRepository { get; private set; }

        /// <summary>
        /// Url for the local repository
        /// </summary>
        public string RemoteUrl => (LocalRepository.Config.Get<string>("remote", "origin", "url")).Value;

        /// <summary>
        /// Check if the local repository has dangling work
        /// </summary>
        public bool HasUncommittedChanges => LocalRepository.RetrieveStatus().IsDirty;

        /// <summary>
        /// Check if the local repository has uncommited changes
        /// </summary>
        public bool HasUnpushedCommits => LocalRepository.Head.TrackingDetails.AheadBy > 0;

        /// <summary>
        /// Url for the local repository
        /// </summary>
        public string RepositoryRootPath { get; }

        /// <summary>
        /// Currently checked out branch
        /// </summary>
        public string CurrentBranch => LocalRepository.Head.CanonicalName;

        CredentialsHandler SupplyUserCredentials => (_url, _user, _cred) =>
        {
            return new UsernamePasswordCredentials
            {
                Password = Credentials.VisualStudioToken.AccessToken,
                Username = Credentials.VisualStudioToken.UserInfo.UniqueId,
            };
        };

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        public GitHelper(string path)
        {
            RepositoryRootPath = Repository.Discover(path);
            LocalRepository = new Repository(RepositoryRootPath);

        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get all the stacks in the current repository (these are not cached)
        /// </summary>
        //---------------------------------------------------------------------------------
        public Dictionary<string, StackInfo> GetStacks()
        {
            var stacks = new Dictionary<string, StackInfo>();
            foreach (Branch branch in LocalRepository.Branches.Where(b => !b.IsRemote))
            {
                var newLevel = StackLevel.TryCreate(branch);
                if (newLevel == null) continue;

                if (!stacks.ContainsKey(newLevel.StackName))
                {
                    var newStack = new StackInfo(newLevel.StackName, RemoteUrl, RepositoryRootPath);
                    stacks.Add(newLevel.StackName, newStack);
                }

                var thisStack = stacks[newLevel.StackName];
                thisStack.AddLevel(newLevel);
                if (newLevel.IsCurrent)
                {
                    thisStack.CurrentLevelNumber = newLevel.Number;
                }

                if (newLevel.Number == 0)
                {
                    thisStack.Origin = newLevel.OriginBranch;
                }
            }

            return stacks;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get branch information from local repository
        /// </summary>
        //---------------------------------------------------------------------------------
        internal Branch GetBranch(string branchName)
        {
            branchName = branchName.ToLower();
            return LocalRepository.Branches.Where(b =>
                b.FriendlyName.ToLower() == branchName
                || b.CanonicalName.ToLower() == branchName).FirstOrDefault();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// IsBranchAheadOrBehind
        /// </summary>
        //---------------------------------------------------------------------------------
        public bool IsBranchAheadOrBehind(string branchName)
        {
            var branch = GetBranch(branchName);
            return ((branch.TrackingDetails.AheadBy.HasValue &&
                     branch.TrackingDetails.AheadBy.Value != 0) ||
                     (branch.TrackingDetails.BehindBy.HasValue &&
                     branch.TrackingDetails.BehindBy.Value != 0));
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Runs the equivalent of git push
        /// </summary>
        //---------------------------------------------------------------------------------
        public void Push(string branchName)
        {
            var output = new List<StackChangeInfo>();
            var pushOptions = new PushOptions() { CredentialsProvider = SupplyUserCredentials };
            var branch = GetBranch(branchName);
            // we always push to the same remote counterpart in the remote origin and branch always tracks the previous stack branch if present else master
            LocalRepository.Network.Push(LocalRepository.Network.Remotes["origin"], $"{REFS_HEAD}{branchName}", pushOptions);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Runs the equivalent of git pull
        /// </summary>
        //---------------------------------------------------------------------------------
        public void Pull()
        {
            Commands.Pull(
                LocalRepository,
                new Signature(Credentials.VisualStudioToken.UserInfo.FamilyName
                , Credentials.VisualStudioToken.UserInfo.DisplayableId
                , DateTime.Now),
                new PullOptions() { FetchOptions = new FetchOptions() { CredentialsProvider = SupplyUserCredentials } });
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Runs the equivalent of git branch newBranch origin
        /// </summary>
        //---------------------------------------------------------------------------------
        public void CreateBranch(string newBranchName, string trackThisBranch)
        {
            if (GetBranch(newBranchName) != null)
            {
                throw new ShortStackException("Branch already exists: " + newBranchName);
            }
            var newBranch = LocalRepository.CreateBranch(newBranchName);
            var remote = LocalRepository.Network.Remotes["origin"]; // we always use origin remote
            string upstreamBranch = String.IsNullOrEmpty(trackThisBranch) ?
                                        newBranch.CanonicalName :  // track the branch being created
                                        GetCanonicalBranchName(trackThisBranch);
            LocalRepository.Branches.Update(
                newBranch,
                b => b.Remote = remote.Name,
                b => b.UpstreamBranch = upstreamBranch);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Runs the equivalent of git checkout branch
        /// </summary>
        //---------------------------------------------------------------------------------
        public void Checkout(string branchName, string origin = null)
        {
            var branch = GetBranch(branchName);

            //if this is the zero branch, this is failing becuase there is no remote branch tracking the current
            if (branch == null)
            {
                if (branchName.EndsWith("/ss000"))
                {
                    throw new ShortStackException($"Local branch does have a remote {origin} tracking it");
                }
                else
                {
                    throw new ShortStackException("Cannot find branch: " + branchName);
                }
            }

            Commands.Checkout(LocalRepository, branch);
        }

        public class CommitDifference
        {
            /// <summary>
            /// ParentBranch
            /// </summary>
            public Branch ParentBranch { get; set; }

            /// <summary>
            /// ChildBranch
            /// </summary>
            public Branch ChildBranch { get; set; }

            /// <summary>
            /// UnpulledCommits - will be null if we couldn't find 
            /// a common ancestor
            /// </summary>
            public Commit[] UnpulledCommits { get; set; }

            /// <summary>
            /// CommonAncestor - null if we can't find one in relatively recent commit list
            /// </summary>
            public Commit CommonAncestor { get; set; }

            /// <summary>
            /// AllCommits - All Commits unique to Child
            /// </summary>
            public Commit[] AllChildCommits { get; set; }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// get commit difference information between two branchs
        /// </summary>
        //---------------------------------------------------------------------------------
        public CommitDifference GetCommitDifference(string parentBranchName, string childBranchName)
        {
            var output = new CommitDifference();
            output.ParentBranch = GetBranch(parentBranchName);
            output.ChildBranch = GetBranch(childBranchName);

            var parentWork = new List<Commit>();
            var childWork = new List<Commit>();

            parentWork.Add(output.ParentBranch.Tip);
            childWork.Add(output.ChildBranch.Tip);
            var parentCommits = new Dictionary<ObjectId, Commit>();
            var childCommits = new Dictionary<ObjectId, Commit>();

            int count = 0;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                while (count < 50)
                {
                    var childSet = childWork.ToArray();
                    childWork.Clear();
                    var parentSet = parentWork.ToArray();
                    parentWork.Clear();

                    foreach (var commit in childSet)
                    {
                        if (parentCommits.ContainsKey(commit.Id))
                        {
                            output.CommonAncestor = commit;
                            output.UnpulledCommits = parentCommits.Values.ToArray();
                            output.AllChildCommits = childCommits.Values.ToArray();
                            return output;
                        }
                        if (!childCommits.ContainsKey(commit.Id))
                        {
                            childCommits.Add(commit.Id, commit);
                            childWork.AddRange(commit.Parents);
                        }
                    }

                    foreach (var commit in parentSet)
                    {
                        if (childCommits.ContainsKey(commit.Id))
                        {
                            childCommits.Remove(commit.Id);
                            output.CommonAncestor = commit;
                            output.UnpulledCommits = parentCommits.Values.ToArray();
                            output.AllChildCommits = childCommits.Values.ToArray();
                            return output;
                        }
                        if (!parentCommits.ContainsKey(commit.Id))
                        {
                            parentCommits.Add(commit.Id, commit);
                            parentWork.AddRange(commit.Parents);
                        }
                    }

                    count++;
                }

            }
            finally
            {
                Debug.WriteLine($"Common ancestor search perf:   Count was {count} and elapsed time was {stopwatch.Elapsed.TotalSeconds.ToString(".000")} s");
            }

            // At this point, we didn't find a common ancestor soon enough,
            // so we are giving up with null values for the wanted data
            return output;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Returns the origin branches associated with the repository.
        /// </summary>
        //---------------------------------------------------------------------------------
        public IEnumerable<Branch> OriginBranches
        {
            get
            {
                var branches = new List<Branch>();
                foreach (var branch in LocalRepository.Branches)
                {
                    if (branch.IsRemote)
                    {
                        branches.Add(branch);
                    }
                }

                return branches;
            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Returns the local branches associated with the repository.
        /// </summary>
        //---------------------------------------------------------------------------------
        public IEnumerable<Branch> LocalBranches
        {
            get
            {
                var branches = new List<Branch>();
                foreach (var branch in LocalRepository.Branches)
                {
                    if (!branch.IsRemote)
                    {
                        branches.Add(branch);
                    }
                }

                return branches;
            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Deletes the specified Branch
        /// </summary>
        //---------------------------------------------------------------------------------
        internal void DeleteBranch(string branchName)
        {
            LocalRepository.Branches.Remove(branchName);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get the long branch name from any branch name
        /// </summary>
        //---------------------------------------------------------------------------------
        private string GetCanonicalBranchName(string branchName)
        {
            if (branchName.StartsWith(REFS_HEAD)) return branchName;

            if (branchName.StartsWith(REFS_REMOTES)) return branchName;

            if (branchName.StartsWith("origin/"))
            {
                // we track all our branches from remote origin.
                return $"{REFS_HEAD}{branchName.Replace("origin/", "")}";
            }

            throw new ArgumentException($"{nameof(branchName)} should be prefixed by origin remote or should be a canonical name");
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Commits all uncommitted files to the local branch
        /// </summary>
        //---------------------------------------------------------------------------------
        internal void CommitDanglingWork(string commitDescription)
        {
            var userName = LocalRepository.Config.Get<string>("user.name");
            var email = LocalRepository.Config.Get<string>("user.email");
            if (userName == null)
            {
                throw new ShortStackException("Could not commit because no user.name in git config.");
            }

            if (email == null)
            {
                throw new ShortStackException("Could not commit because no user.email in git config.");
            }

            Commands.Stage(LocalRepository, "*");
            Signature author = new Signature(userName.Value, email.Value, DateTime.Now);
            Signature committer = author;

            // Commit to the repository
            Commit commit = LocalRepository.Commit(commitDescription, author, committer);
        }
    }
}
