using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Tools.Productivity.ShortStack
{
    public delegate void ShortStackClientNotification(ShortStackNotification notification);

    //---------------------------------------------------------------------------------
    /// <summary>
    /// Abandon PRs for this stack and delete the branches, put us back on master branch
    /// </summary>
    //---------------------------------------------------------------------------------
    public class ShortStackProcessor
    {

        /// <summary>
        /// This event is trigged when something happens that is probably noteworthy
        /// to an end user.
        /// </summary>
        public event ShortStackClientNotification OnNotify;

        Dictionary<string, StackInfo> _stacksCache;
        /// <summary>
        /// Get stacks for the current repository
        /// </summary>
        public Dictionary<string, StackInfo> Stacks => _stacksCache ?? (_stacksCache = _git.GetStacks());

        /// <summary>
        /// The currently active stack (if any)
        /// </summary>
        public StackInfo CurrentStack => Stacks.Values.Where(s => s.StackName == GetStackName( _git.CurrentBranch)).FirstOrDefault();

        /// <summary>
        /// Root path of the handler's repository
        /// </summary>
        public string RootPath => _git?.RepositoryRootPath;

        GitHelper _git;

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        public ShortStackProcessor(string startingPath)
        {
            if (string.IsNullOrEmpty(startingPath))
            {
                throw new ShortStackException("Starting path must be specified.");
            }
            _git = new GitHelper(startingPath);
        }

        void LogVerbose(string message) => Notify(new ShortStackMessageGeneric(message) { Status = ShortStackNotification.NotificationStatus.Detail });
        void LogInformation(string message) => Notify(new ShortStackMessageGeneric(message) { Status = ShortStackNotification.NotificationStatus.Information });
        void LogWarning(string message) => Notify(new ShortStackMessageGeneric(message) { Status = ShortStackNotification.NotificationStatus.Warning });
        void LogError(string message) => Notify(new ShortStackMessageGeneric(message) { Status = ShortStackNotification.NotificationStatus.Error });

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Push any commits in the current stack level up to the server
        /// </summary>
        //---------------------------------------------------------------------------------
        public StackCommit[] PushStackLevel()
        {
            if (CurrentStack == null)
            {
                throw new ShortStackException("You are not in a stacked branch.  Use Show-Stacks to see available stacks.");
            }

            if (_git.HasUncommittedChanges)
            {
                throw new ShortStackException("There are uncommitted edits.  Please resolve or stash these before attempting this operation.");
            }

            var level = GetLevelDetails(CurrentStack.CurrentLevel());
            var vsts = new VSTSAccess(_git.RemoteUrl);
            var existingPullRequest = vsts.GetPullRequestByBranch(level.OriginBranch);

            if (existingPullRequest != null)
            {
                var newDescription = new StringBuilder();
                newDescription.Append(existingPullRequest.description);
                foreach (var commit in level.UnpushedCommits)
                {
                    newDescription.AppendLine(commit.ShortMessage);
                    LogVerbose($"Appending to PR description: {commit.ShortMessage}");
                }

                var patch = new Dictionary<string, string>();
                patch["description"] = newDescription.ToString();

                vsts.AmmendPullRequest(existingPullRequest, patch);
            }

            var output = level.UnpushedCommits;

            LogInformation($"Pushing {output.Length} commits to {level.OriginBranch}");
            _git.Push(CurrentStack.CurrentLevel().LocalBranch);

            return output;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get the stack name from a branch name
        /// </summary>
        //---------------------------------------------------------------------------------
        private string GetStackName(string branchName)
        {
            var match = StackLevel.BranchNameMatcher.Match(branchName);
            if(match.Success)
            {
                var parts = match.Groups[1].Value.Split('/');
                return parts[parts.Length - 1];
            }
            return null;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Find a stack by name
        /// </summary>
        //---------------------------------------------------------------------------------
        private StackInfo GetStack(string stackName) => Stacks.Values.Where(s => s.StackName.ToLower() == stackName.ToLower()).FirstOrDefault();

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get stacks for the current repository
        /// </summary>
        //---------------------------------------------------------------------------------
        private void Notify(object notification)
        {
            OnNotify?.Invoke(new ShortStackNotification(notification) { DisplayText = notification.ToString() });
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Check if there is any dangling work
        /// </summary>
        //---------------------------------------------------------------------------------
        public DanglingWorkStatus GetDanglingWorkStatus()
        {
            if (_git.HasUncommittedChanges)
            {
                return DanglingWorkStatus.UncommittedChanges;
            }
            else if (_git.HasUnpushedCommits)
            {
                return DanglingWorkStatus.UnpushedCommits;
            }
            else
            {
                return DanglingWorkStatus.Clean;
            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get stacks for the current repository
        /// </summary>
        //---------------------------------------------------------------------------------
        public void CreateNewStack(string stackName, string originBranch)
        {
            if (_git.HasUncommittedChanges)
            {
                throw new ShortStackException("There are uncommitted changes.");
            }

            StackInfo targetStack = null; 
            if (stackName == null)
            {
                if (CurrentStack == null)
                {
                    throw new ShortStackException("You are not in a stacked branch.  Pass in Name (with an optional Origin) to create a stacked branch.");
                }
                stackName = CurrentStack.StackName;

                var status = GetDanglingWorkStatus();
                //we are in the correct stack level but nothing has happenned on this level, so prevent creating a new level
                if (status == DanglingWorkStatus.Clean)
                {
                    throw new ShortStackException("You have not pushed any commits to the current branch. Create some commits before moving to the next level in the stack");
                }
            }

            targetStack = GetStack(stackName);

            // Create the stack if it is not there
            if (targetStack == null)
            {
                LogInformation($"Create new stack '{stackName}' tracking {originBranch}");
                if (_git.GetBranch(originBranch) == null)
                {
                    throw new ShortStackException($"Origin branch '{originBranch}' does not exist at {_git.RemoteUrl}");
                }

                var newStack = new StackInfo(stackName, _git.RemoteUrl, _git.RepositoryRootPath)
                {
                    Origin = originBranch,
                };

                Stacks.Add(stackName, newStack);
                CreateNextStackLevel(stackName, originBranch); // Create Level 0, but this will not get used
                // Make sure it is up to date
                _git.Pull();
                LogVerbose($"Pulled from {newStack.CurrentLevel().OriginBranch} to { newStack.CurrentLevel().TargetOriginBranch }");
            }

            CreateNextStackLevel(stackName); // Create level 1, this is the effective starting stack level
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Create the next level in the specified stack
        /// </summary>
        //---------------------------------------------------------------------------------
        public void CreateNextStackLevel(string currentStackName, string origin = null)
        {
            if (_git.HasUncommittedChanges)
            {
                throw new ShortStackException("There are uncommitted edits.  Please resolve or stash these before attempting this operation.");
            }

            var currentStack = GetStack(currentStackName);
            if (currentStack == null)
            {
                throw new ShortStackException($"There is no stack information for the stack named '{currentStackName}'");
            }

            int currentLevelNumber = -1;

            var lastLevel = currentStack.LastLevel();
            if (lastLevel != null)
            {
                currentLevelNumber = lastLevel.Number;
                if (origin != null)
                {
                    throw new ShortStackException("Only the zero-level of a stack can have an origin override.");
                }

                // Make sure we are at the top of the stack
                if (!lastLevel.IsCurrent)
                {
                    _git.Checkout(lastLevel.LocalBranch);
                    currentStack.SetCurrentLevel(lastLevel);
                    LogVerbose($"Change branch to {lastLevel.LocalBranch}");
                }
            }

            // Set the origin to point to the lastLevel if there is one
            if (origin == null) origin = lastLevel?.TargetOriginBranch;

            // If we don't have an origin, something is wrong
            if (origin == null)
            {
                throw new ShortStackException("The origin must be specified for zero-level branches!");
            }

            // Fix up the origin branch name
            if (!origin.StartsWith("origin/"))
            {
                origin = "origin/" + origin;
            }

            // Create the new stack branch
            var newLevelNumber = currentLevelNumber + 1;
            string newBranchName = currentStack.CreateBranchLevelName(newLevelNumber);
            _git.CreateBranch(newBranchName, origin);
            LogVerbose($"Creating branch {newBranchName} to track {origin}");
            _git.Checkout(newBranchName, origin);
            LogVerbose($"Change branch to {newBranchName}");

            // Push up to the server to enforce branch creation there
            _git.Push(newBranchName);
            LogVerbose($"Pushed from {newBranchName} to origin/{newBranchName} ");

            // Put it on the current stack record
            var newLevel = new StackLevel()
            {
                IsCurrent = true,
                RecentCommitDescription = "[new]",
                LocalBranch = newBranchName,
                Number = newLevelNumber,
                OriginBranch = origin,
                TargetOriginBranch = "origin/" + newBranchName,
                RepositoryUrl = _git.RemoteUrl,
                StackName = currentStack.StackName,
            };

            currentStack.AddLevel(newLevel);
            currentStack.SetCurrentLevel(newLevel);
            LogInformation($"Now on stack '{currentStack.StackName}', Level {newLevelNumber}");
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Go to another level of a stack
        /// </summary>
        //---------------------------------------------------------------------------------
        public void GoToStackLevel(string stackName, int stackLevel)
        {
            if (_git.HasUncommittedChanges)
            {
                throw new ShortStackException("There are uncommitted changes.");
            }

            var selectedStack = CurrentStack;
            if(stackName != null)
            {
                selectedStack = _stacksCache.Values
                    .Where(s => s.StackName.ToLowerInvariant() == stackName.ToLowerInvariant())
                    .FirstOrDefault();
            }

            if(selectedStack == null)
            {
                if(stackName == null)
                {
                    throw new ShortStackException("Not currently on a stack.");
                }
                else
                {
                    throw new ShortStackException($"Could not find stack named '{stackName}'");
                }
            }

            if(stackLevel < 0)
            {
                throw new ShortStackException($"Invalid stack level {stackLevel}");
            }

            if(stackLevel >= selectedStack.Levels.Count)
            {
                stackLevel = selectedStack.Levels.Count - 1;
            }

            if(CurrentStack != null)
            {
                CurrentStack.SetCurrentLevel(null);
            }

            var targetStackLevel = selectedStack.Levels[stackLevel];
            _git.Checkout(targetStackLevel.LocalBranch);
            selectedStack.SetCurrentLevel(targetStackLevel);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Fills in difficult to obtains details for a stacklevel
        /// </summary>
        //---------------------------------------------------------------------------------
        public StackLevel GetLevelDetails(StackLevel level)
        {
            Branch localBranch = _git.GetBranch(level.LocalBranch);
            Branch targetBranch = _git.GetBranch(level.TargetOriginBranch);
            Branch sourceBranch = _git.GetBranch(level.OriginBranch);

            StackCommit[] GetCommitData(IEnumerable<Commit> commits)
            {
                return commits?.Select(c => new StackCommit() { Id = c.Id.ToString(), ShortMessage = c.MessageShort }).ToArray();
            }

            try
            {
                var lookingBack = _git.GetCommitDifference(sourceBranch.CanonicalName, localBranch.CanonicalName);
                var lookingForward = _git.GetCommitDifference(localBranch.CanonicalName, targetBranch.CanonicalName);

                if (lookingBack.CommonAncestor != null)
                {
                    level.UnpulledCommits = GetCommitData(lookingBack.UnpulledCommits);
                    level.AllCommits = GetCommitData(lookingBack.AllChildCommits);
                }

                if (lookingForward.CommonAncestor != null)
                {
                    level.UnpushedCommits = GetCommitData(lookingForward.UnpulledCommits);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                throw new ShortStackException("Fill error: " + e.Message);
            }

            level.PullRequest = new VSTSAccess(_git.RemoteUrl).GetPullRequestByBranch(level.TargetOriginBranch);

            return level;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Automagically create a pull request
        /// </summary>
        //---------------------------------------------------------------------------------
        public void CreatePullRequest(string commitDescription)
        {
            if (CurrentStack == null)
            {
                throw new ShortStackException("You are not in a stacked branch.  Use Get-Stacks to see available stacks.");
            }

            var currentLevel = CurrentStack.CurrentLevel();
            GetLevelDetails(currentLevel);

            if (currentLevel.PullRequest != null)
            {
                LogInformation($"There is already a pull request sourced from {currentLevel.LocalBranch}");
                LogInformation($"    {currentLevel.PullRequest.url}");
                LogInformation($"    Use Push-StackLevel to add additional commits to this PR.");
                return;
            }

            StringBuilder description = new StringBuilder();
            var prefix = $"{CurrentStack.StackName}-{currentLevel.Number.ToString("000")}";
            string title = null;

            var danglingStatus = GetDanglingWorkStatus();
            if(danglingStatus == DanglingWorkStatus.Clean && commitDescription != null)
            {
                throw new ShortStackException("A commit description was given, but there is no dangling work to commit.");
            }

            if(currentLevel.AllCommits == null)
            {
                title = $"{prefix} [Please Edit This Useless Title]";
                description.AppendLine("Could not determine a commit list because, my gosh, you should have pulled a lot sooner.");
                LogWarning("I had trouble working out what commits are part of this pull request.  You will need to edit it by hand.");
            }
            else
            {
                var commitCount = currentLevel.AllCommits.Length - currentLevel.UnpushedCommits.Length;
                if (danglingStatus == DanglingWorkStatus.UncommittedChanges && commitDescription != null)
                {
                    LogInformation($"Committing dangling work ...");
                    commitCount++;
                    _git.CommitDanglingWork(commitDescription);
                    PushStackLevel();
                    GetLevelDetails(currentLevel);
                }

                if(commitCount == 0)
                {
                    LogError("There is no work on the remote to include in this pull request.");
                    LogError("Use Push-StackLevel to first push locally committed changes to the remote branch.");
                    return;
                }

                if (danglingStatus == DanglingWorkStatus.UncommittedChanges && commitDescription == null)
                {
                    LogWarning("There are uncommitted changes.  Use Push-StackLevel '[description]' to ammend this pull request.");
                }

                var unpushedIds = currentLevel.UnpushedCommits.Select(c => c.Id).ToArray();
                foreach (StackCommit commit in currentLevel.AllCommits.Where(c => !unpushedIds.Contains(c.Id)))
                {
                    if (title == null)
                    {
                        title = $"{prefix} {commit.ShortMessage}";
                    }
                    description.AppendLine(commit.ShortMessage);
                }
            }

            new VSTSAccess(_git.RemoteUrl).CreatePullRequestByBranch(currentLevel.LocalBranch, currentLevel.TargetOriginBranch, title, description.ToString());
            GetLevelDetails(currentLevel);
            LogInformation($"Pull request created: {currentLevel.PullRequest.url}");
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Returns branches on the remote server
        /// </summary>
        //---------------------------------------------------------------------------------
        public IEnumerable<Branch> OriginBranches
        {
            get => _git.OriginBranches;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Delete branches associated with a stack
        /// </summary>
        //---------------------------------------------------------------------------------
        public string[] GetBranchNames(string stackName, bool includeOrigin)
        {
            var output = new List<string>();
            foreach (var branch in _git.LocalBranches)
            {
                if (StackLevel.BranchNameMatcher.IsMatch(branch.CanonicalName)
                    && branch.CanonicalName.ToUpper().Contains($"/{stackName}/ss".ToUpper()))
                {
                    output.Add(branch.CanonicalName);
                }
            }

            if (includeOrigin)
            {
                foreach (var branch in _git.OriginBranches)
                {
                    if (StackLevel.BranchNameMatcher.IsMatch(branch.CanonicalName)
                        && branch.CanonicalName.ToUpper().Contains($"/{stackName}/ss".ToUpper()))
                    {
                        output.Add(branch.CanonicalName);
                    }
                }
            }

            output.Sort();
            return output.ToArray();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Delete branches and abandon pull requests associated with a stack
        /// </summary>
        //---------------------------------------------------------------------------------
        public void PurgeStack(string stackName, bool includeOrigin)
        {
            var stack = GetStack(stackName);
            if(stack != null)
            {
                if(stack.StackName == CurrentStack?.StackName)
                {
                    var safeBranch = "master";
                    if(stack.Levels[0].Number == 0)
                    {
                        safeBranch = stack.Levels[0].OriginBranch.Replace("origin/","");
                    }

                    try
                    {
                        LogVerbose($"Checking out to {safeBranch}");
                        _git.Checkout(safeBranch);

                    }
                    catch(Exception e)
                    {
                        LogError($"Problem: {e.Message}");
                    }
                }
                var vsts = new VSTSAccess(_git.RemoteUrl);
                foreach(var level in stack.Levels)
                {
                    if(level.PullRequest != null && level.PullRequest.status != "fppp")
                    {
                        LogVerbose($"Abandoning pull request: {level.PullRequest.title} ({level.PullRequest.url})");
                        vsts.AbandonPullRequest(level.PullRequest.pullRequestId);
                    }
                }
            }
          
            foreach (var branchName in GetBranchNames(stackName, includeOrigin))
            {
                try
                {
                    LogVerbose($"Deleting {branchName}");
                    _git.DeleteBranch(branchName);
                }
                catch (Exception e)
                {
                    LogError($"Problem: {e.Message}");
                }
            }
        }
    }
}
