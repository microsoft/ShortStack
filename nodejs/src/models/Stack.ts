import { SimpleGit } from "simple-git";
import { ShortStackError } from "./CommandHandler";

//------------------------------------------------------------------------------
// Helper to generate branch names
//------------------------------------------------------------------------------
function constructStackLevelBranchName(stackName: string, levelNumber: number) 
{
    return `${stackName}/_ss/${`${levelNumber}`.padStart(3,"0")}`
}

//------------------------------------------------------------------------------
// A single level in the stack
//------------------------------------------------------------------------------
export class StackItem {
    parent: Stack;
    levelNumber: number;
    get branchName() { return constructStackLevelBranchName(this.parent.name, this.levelNumber)}

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(parent: Stack, levelNumber: number) {
        this.parent = parent;
        this.levelNumber = levelNumber;
    }
}

//------------------------------------------------------------------------------
// A full set of stacked changes
//------------------------------------------------------------------------------
export class Stack {
    name: string;
    parentBranch: string;
    remoteName: string;
    levels = new Array<StackItem>();
    currentLevel?: StackItem;
    private _git: SimpleGit;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(git: SimpleGit, name: string, parentBranch: string, remoteName: string) {
        this._git = git;
        this.name = name;
        this.parentBranch = parentBranch;
        this.remoteName = remoteName;
    }

    //------------------------------------------------------------------------------
    // Add a level to the current stack
    //------------------------------------------------------------------------------
    async AddLevel()
    {
        let trackingBranch = this.parentBranch;
        let newLevelNumber = 0;

        if(this.levels.length > 0)
        {
            const lastLevel = this.levels[this.levels.length-1];
            newLevelNumber = lastLevel.levelNumber+1;
            trackingBranch = lastLevel.branchName;
        }
        const newBranchName = constructStackLevelBranchName(this.name, newLevelNumber); 
        await this._git.branch([newBranchName, "--track", trackingBranch])
        await this._git.checkout(newBranchName);
        await this._git.pull();
        await this._git.push(this.remoteName, newBranchName);

        const newItem = new StackItem(this, newLevelNumber);
        this.levels.push(newItem);
        this.currentLevel = newItem;
    }

}

//------------------------------------------------------------------------------
// The current state of all local stacks
//------------------------------------------------------------------------------
export class StackInfo {
    current?: StackItem;

    private _git: SimpleGit;
    private _stacks = new Map<string, Stack>()

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(git: SimpleGit) {
        this._git = git;
    }

    //------------------------------------------------------------------------------
    // discover all the local stacks
    //------------------------------------------------------------------------------
    static async Create(git: SimpleGit, currentBranch: string)
    {
        const output = new StackInfo(git);
        const branchSummary = await git.branchLocal()
        for(const localBranchName in branchSummary.branches)
        {
            console.log("Loading branch: " + localBranchName);
            // TODO: If the current branch matches the localBranchName, then set this.current
            //console.log(`Data: ${JSON.stringify(branchSummary.branches[localBranchName],null,2)}`)
        }

        return output;
    }

    //------------------------------------------------------------------------------
    // Create a brand new stack
    //------------------------------------------------------------------------------
    async CreateStack(name: string, parentBranch?: string )
    {
        name = name.toLowerCase();
        let remoteName = "origin";  // Discover like this: await this._git.remote([])
        if(!parentBranch)
        {
            // Get default branch
            const remoteInfo = await this._git.remote(["show", remoteName]);
            if(!remoteInfo) throw new ShortStackError("Could not find remote info.  Please specify a tracking branch explicitly. ");
            const match = (/HEAD branch: (\S+)/i).exec(remoteInfo);
            if(!match) parentBranch = "master";
            else parentBranch = match[1].valueOf();
        }
        const newStack = new Stack(this._git, name, parentBranch, remoteName );

        this._stacks.set(name, newStack)
        await newStack.AddLevel();

        this.current = newStack.currentLevel;
        return newStack;
    }
}