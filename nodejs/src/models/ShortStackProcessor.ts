
import { IGitHelper, GitBranchInfo } from "../helpers/gitHelper";

const BranchNameMatcher = /(.*)\/ss(\\d{3})$/i;

class StackCommit
{
    Id: string | undefined
    ShortMessage: string | undefined
}

class StackReviewer
{
    vote: number | undefined
    displayName: string | undefined
    imageUrl: string | undefined
}

class StackPullRequest
{
    pullRequestId: number | undefined
    codeReviewId: number | undefined
    status: string | undefined
    creationDate: Date | undefined
    title: string | undefined
    sourceRefName: string | undefined
    targetRefName: string | undefined
    mergeStatus: string | undefined
    reviewers: StackReviewer[] | undefined
    url: string | undefined
    description: string | undefined
}

//---------------------------------------------------------------------------------
/// Model for handling information about a level in a stack
//---------------------------------------------------------------------------------
class StackLevel
{
    /// The number of this level
    Number = 0;

    /// Text from the most recent commit
    RecentCommitDescription: string | undefined;

    /// The number of this level
    IsCurrent = false;

    /// Name of the stack
    StackName = "";

    /// The repository that has this level
    RepositoryUrl = "";

    /// The branch to pull from
    OriginBranch = "";

    /// The branch to push to
    TargetOriginBranch = "";

    /// The branch on this PC
    LocalBranch = "";

    /// Commits that have not been pushed to TargetOriginBranch
    /// (This can take time to discover)
    UnpushedCommits: StackCommit[] = [];

    /// Commits that have not been pulled from OriginBranch 
    /// (This can take time to discover)
    UnpulledCommits: StackCommit[] = [];

    /// All commits made on the current level
    /// (This can take time to discover)
    AllCommits: StackCommit[] = [];

    /// Pull request for this stack level.  (Created by ss-push)
    PullRequest: StackPullRequest | undefined;

    /// The information about the stack that contains this level
    Stack: StackInfo | undefined;

    //---------------------------------------------------------------------------------
    /// ctor
    //---------------------------------------------------------------------------------
    static TryCreate(branch: GitBranchInfo)
    {
        const match = BranchNameMatcher.exec(branch.FriendlyName);
        if (!match) return undefined;

        const stackName = match[1];
        const number = parseInt(match[2]);

        const recentCommit = branch.GetMostRecentCommit();

        const newLevel = new StackLevel();
        newLevel.StackName= stackName;
        newLevel.RecentCommitDescription = recentCommit?.Message;
        newLevel.Number = number;
        newLevel.OriginBranch = branch.RemoteName + "/" + branch.UpstreamBranchCanonicalName.Replace("refs/heads/", "");
        newLevel.TargetOriginBranch = branch.RemoteName + "/" + branch.FriendlyName;
        newLevel.LocalBranch = branch.FriendlyName;
        newLevel.IsCurrent = branch.IsCurrentRepositoryHead;

        return newLevel;
    }

    // //---------------------------------------------------------------------------------
    // /// Fill in the details of this object from the other object
    // //---------------------------------------------------------------------------------
    // public void FillDetails(IShortStackHandler handler)
    // {
    //     const source = handler.GetLevelDetails(Stack, this);
    //     this.PullRequest = source.PullRequest;
    //     this.UnpulledCommits = source.UnpulledCommits;
    //     this.UnpushedCommits = source.UnpushedCommits;
    //     this.AllCommits = source.AllCommits;
    // }
}

//---------------------------------------------------------------------------------
/// Model for handling information about a Short Stack collection of branches
//---------------------------------------------------------------------------------
class StackInfo
{
        // User-Chosen Stack name
        StackName: string;

        /// All available levels in this stack
        Levels: StackLevel[] = [];

        /// The number of the current level (not necessarily the index in levels)
        CurrentLevelNumber: number | undefined;

        /// Url to the source repository
        RepositoryUrl: string;

        /// What the origin is for this stack.
        Origin : string | undefined;

        /// The root of the repository.
        RepositoryRootPath : string;

        /// Number of the last level
        get LastLevelNumber()
        {
            if (this.Levels.length == 0) return -1;
            return this.Levels.reduce((max, l)  => {return  l.Number > max.Number ? l : max}).Number;
        }

        /// Topmost level in the stack
        get LastLevel()
        {
            var lastLevelNumber = this.LastLevelNumber;
            if (lastLevelNumber == -1) return undefined;
            return this.Levels.find(l => {return l.Number == lastLevelNumber});
        }


        // private int _hashCode;

        //---------------------------------------------------------------------------------
        /// ctor
        //---------------------------------------------------------------------------------
        constructor(stackName: string, repositoryUrl: string, repositoryRootPath: string)
        {
            this.StackName = stackName;
            this.RepositoryRootPath = repositoryRootPath;
            this.RepositoryUrl = repositoryUrl;
            //_hashCode = ($"{StackName.ToLower()}~{RepositoryRootPath.ToLower()}").GetHashCode();
        }

        //---------------------------------------------------------------------------------
        /// Add a level
        //---------------------------------------------------------------------------------
        AddLevel(level:StackLevel)
        {
            this.Levels.push(level);
            level.Stack = this;
        }

        // #region Equality
        // //---------------------------------------------------------------------------------
        // /// == operator
        // //---------------------------------------------------------------------------------
        // public static bool operator ==(StackInfo x, StackInfo y)
        // {

        //     if ((object)x == null) return (object)y == null;
        //     return x.Equals(y);
        // }

        // //---------------------------------------------------------------------------------
        // /// != operator
        // //---------------------------------------------------------------------------------
        // public static bool operator !=(StackInfo x, StackInfo y)
        // {
        //     if ((object)x == null) return (object)y != null;
        //     return !x.Equals(y);
        // }

        // //---------------------------------------------------------------------------------
        // /// See if two stacks are the same
        // //---------------------------------------------------------------------------------
        // public bool Equals(StackInfo x, StackInfo y)
        // {
        //     if (y == null) return false;
        //     return x.StackName.Equals(y.StackName, StringComparison.OrdinalIgnoreCase)
        //         && x.RepositoryUrl.Equals(y.RepositoryUrl, StringComparison.OrdinalIgnoreCase);
        // }

        // //---------------------------------------------------------------------------------
        // /// See if two stacks are the same
        // //---------------------------------------------------------------------------------
        // public override bool Equals(object obj)
        // {
        //     if (obj == null) return false;
        //     return Equals(this, obj as StackInfo);
        // }

        // //---------------------------------------------------------------------------------
        // /// GetHashCode for use in dictionaries
        // //---------------------------------------------------------------------------------
        // public int GetHashCode(StackInfo obj)
        // {
        //     const stackInfo = obj as StackInfo;
        //     if (stackInfo != null) return stackInfo._hashCode;
        //     return obj.GetHashCode();
        // }

        // //---------------------------------------------------------------------------------
        // /// GetHashCode for use in dictionaries
        // //---------------------------------------------------------------------------------
        // public override int GetHashCode()
        // {
        //     return _hashCode;
        // }
        // #endregion
}



//------------------------------------------------------------------------------
// The Stack handling class.   This is where the magic happens
//------------------------------------------------------------------------------
export class ShortStackProcessor
{
        // /// This event is trigged when something happens that is probably noteworthy
        // /// to an end user.
        // public event ShortStackClientNotification OnNotify;

    #stacksCache = new Map<string, StackInfo>();
    /// Get stacks for the current repository
    get Stacks() { return this.#stacksCache ?? (this.#stacksCache = this.GetStacks())};

    /// The currently active stack (if any)
    get CurrentStack() { 
        const stackToFind = this.GetStackName(this._git.CurrentBranch);
        for(const stack of this.Stacks.values()) 
        {
            if(stack.StackName == stackToFind) return stack;
        }
        return undefined;
    }

    /// Root path of the handler's repository
    public RootPath(){return this._git.RepositoryRootPath};


    private _git: IGitHelper;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(git: IGitHelper)
    {
        this._git = git;
    }

    //---------------------------------------------------------------------------------
    /// Get all the stacks in the current repository (these are not cached)
    //---------------------------------------------------------------------------------
    private GetStacks()
    {
        const stacks = new Map<string, StackInfo>();
        for (const branch of this._git.LocalBranches)
        {
            const newLevel = StackLevel.TryCreate(branch);
            if (newLevel == null) continue;

            if (!stacks.ContainsKey(newLevel.StackName))
            {
                const newStack = new StackInfo(newLevel.StackName, this._git.RemoteUrl, this._git.RepositoryRootPath);
                stacks.Add(newLevel.StackName, newStack);
            }

            const thisStack = stacks.get(newLevel.StackName);
            if(thisStack) {
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
        }

        return stacks;
    }


    //---------------------------------------------------------------------------------
    /// Get the stack name from a branch name
    //---------------------------------------------------------------------------------
    private GetStackName(branchName: string)
    {
        const matches = BranchNameMatcher.exec(branchName);
        if(matches)
        {
            const parts = matches[1]?.split('/');
            return parts[parts.length - 1];
        }
        return null;
    }

    //---------------------------------------------------------------------------------
    /// Find a stack by name
    //---------------------------------------------------------------------------------
    private GetStack(stackName:string) 
    {
        for(const stack of this.Stacks.values())
        {
            if(stack.StackName.toLowerCase() == stackName.toLowerCase()) return stack;
        }
        return undefined;
    }

    //------------------------------------------------------------------------------
    // Create a new stack or stack level
    //------------------------------------------------------------------------------
    createNewStack(stackName: string | undefined, originBranch: string | undefined)
    {
        if (this._git.HasUncommittedChanges)
        {
            throw new Error("There are uncommitted changes.");
        }

        // StackInfo targetStack = null; 
        // if (stackName == null)
        // {
        //     if (CurrentStack == null)
        //     {
        //         throw new ShortStackException("You are not in a stacked branch.  Pass in Name (with an optional Origin) to create a stacked branch.");
        //     }
        //     stackName = CurrentStack.StackName;

        //     const status = GetDanglingWorkStatus();
        //     //we are in the correct stack level but nothing has happenned on this level, so prevent creating a new level
        //     if (status == DanglingWorkStatus.Clean)
        //     {
        //         throw new ShortStackException("You have not pushed any commits to the current branch. Create some commits before moving to the next level in the stack");
        //     }
        // }

        // targetStack = GetStack(stackName);

        // // Create the stack if it is not there
        // if (targetStack == null)
        // {
        //     LogInformation($"Create new stack '{stackName}' tracking {originBranch}");
        //     if (_git.GetBranch(originBranch) == null)
        //     {
        //         throw new ShortStackException($"Origin branch '{originBranch}' does not exist at {_git.RemoteUrl}");
        //     }

        //     const newStack = new StackInfo(stackName, _git.RemoteUrl, _git.RepositoryRootPath)
        //     {
        //         Origin = originBranch,
        //     };

        //     Stacks.Add(stackName, newStack);
        //     CreateNextStackLevel(stackName, originBranch); // Create Level 0, but this will not get used
        //     // Make sure it is up to date
        //     _git.Pull();
        //     LogVerbose($"Pulled from {newStack.CurrentLevel().OriginBranch} to { newStack.CurrentLevel().TargetOriginBranch }");
        // }

        // CreateNextStackLevel(stackName); // Create level 1, this is the effective starting stack level
    }

    
    //---------------------------------------------------------------------------------
    /// Create the next level in the specified stack
    //---------------------------------------------------------------------------------
    private CreateNextStackLevel(currentStackName: string)//, origin: string = null)
    {
        const currentStack = this.GetStack(currentStackName);
        if (currentStack == null)
        {
            throw Error(`There is no stack information for the stack named '${currentStackName}'`);
        }

        let currentLevelNumber = -1;

        const lastLevel = currentStack.LastLevel;
    //     if (lastLevel != null)
    //     {
    //         currentLevelNumber = lastLevel.Number;
    //         if (origin != null)
    //         {
    //             throw new ShortStackException("Only the zero-level of a stack can have an origin override.");
    //         }

    //         // Make sure we are at the top of the stack
    //         if (!lastLevel.IsCurrent)
    //         {
    //             _git.Checkout(lastLevel.LocalBranch);
    //             currentStack.SetCurrentLevel(lastLevel);
    //             LogVerbose($"Change branch to {lastLevel.LocalBranch}");
    //         }
    //     }

    //     // Set the origin to point to the lastLevel if there is one
    //     if (origin == null) origin = lastLevel?.TargetOriginBranch;

    //     // If we don't have an origin, something is wrong
    //     if (origin == null)
    //     {
    //         throw new ShortStackException("The origin must be specified for zero-level branches!");
    //     }

    //     // Fix up the origin branch name
    //     if (!origin.StartsWith("origin/"))
    //     {
    //         origin = "origin/" + origin;
    //     }

    //     // Create the new stack branch
    //     const newLevelNumber = currentLevelNumber + 1;
    //     string newBranchName = currentStack.CreateBranchLevelName(newLevelNumber);
    //     _git.CreateBranch(newBranchName, origin);
    //     LogVerbose($"Creating branch {newBranchName} to track {origin}");
    //     _git.Checkout(newBranchName, origin);
    //     LogVerbose($"Change branch to {newBranchName}");

    //     // Push up to the server to enforce branch creation there
    //     _git.Push(newBranchName);
    //     LogVerbose($"Pushed from {newBranchName} to origin/{newBranchName} ");

    //     // Put it on the current stack record
    //     const newLevel = new StackLevel()
    //     {
    //         IsCurrent = true,
    //         RecentCommitDescription = "[new]",
    //         LocalBranch = newBranchName,
    //         Number = newLevelNumber,
    //         OriginBranch = origin,
    //         TargetOriginBranch = "origin/" + newBranchName,
    //         RepositoryUrl = _git.RemoteUrl,
    //         StackName = currentStack.StackName,
    //     };

    //     currentStack.AddLevel(newLevel);
    //     currentStack.SetCurrentLevel(newLevel);
    //     LogInformation($"Now on stack '{currentStack.StackName}', Level {newLevelNumber}");
    // }

}