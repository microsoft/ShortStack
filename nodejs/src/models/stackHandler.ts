
import { IGitHelper } from "../helpers/gitHelper";

//------------------------------------------------------------------------------
// The Stack handling class.   This is where the magic happens
//------------------------------------------------------------------------------
export class StackHandler
{
    private _git: IGitHelper;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(git: IGitHelper)
    {
        this._git = git;
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

        //     var status = GetDanglingWorkStatus();
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

        //     var newStack = new StackInfo(stackName, _git.RemoteUrl, _git.RepositoryRootPath)
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

}