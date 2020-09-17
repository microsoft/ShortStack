import { SimpleGit, BranchSummary } from "simple-git";
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
    trackingBranch: string;
    label?: string;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(parent: Stack, levelNumber: number, trackingBranch: string) {
        this.parent = parent;
        this.levelNumber = levelNumber;
        this.trackingBranch = trackingBranch;
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

        const newItem = new StackItem(this, newLevelNumber, trackingBranch);
        this.levels.push(newItem);
        this.currentLevel = newItem;
    }

}

//------------------------------------------------------------------------------
// The current state of all local stacks
//------------------------------------------------------------------------------
export class StackInfo {
    current?: StackItem;

    get stacks() {return Array.from(this._stacks.values()) }

    private _git: SimpleGit;
    private _stacks = new Map<string, Stack>()
    private remoteName = "origin";



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
        const branchSummary = await git.branchLocal();
        output.remoteName = "origin";
        //TODO: maybe discover remote name like this:  await git.remote([])

        for(const branchKey in branchSummary.branches)
        {
            const branchInfo = branchSummary.branches[branchKey];
            const match = /(.*?)\/_ss\/(\d\d\d)$/.exec(branchKey);
            if(match) {
                const stackName = match[1];
                const levelNumber = parseInt(match[2]);
                const config = await git.listConfig();
                let trackingBranch = config.values[".git/config"][`branch.${branchKey}.merge`] as string;
                trackingBranch = trackingBranch.replace("refs/heads/", "");
                let remoteName = await output.remoteName;
                if(!output._stacks.get(stackName)) {
                    output._stacks.set(stackName, new Stack(git, stackName, trackingBranch, remoteName ))
                }

                const myStack = output._stacks.get(stackName)!;
                const newLevel = new StackItem(myStack, levelNumber, trackingBranch);
                newLevel.label = branchInfo.label;
                myStack.levels[levelNumber] = newLevel;

                if(branchKey == currentBranch) {
                    output.current = newLevel;
                    myStack.currentLevel = newLevel;
                }
            }
        }

        return output;
    }

    //------------------------------------------------------------------------------
    // Create a brand new stack
    //------------------------------------------------------------------------------
    async CreateStack(name: string, parentBranch?: string )
    {
        name = name.toLowerCase();
        if(!parentBranch)
        {
            // Get default branch
            const remoteInfo = await this._git.remote(["show", this.remoteName]);
            if(!remoteInfo) throw new ShortStackError("Could not find remote info.  Please specify a tracking branch explicitly. ");
            const match = (/HEAD branch: (\S+)/i).exec(remoteInfo);
            if(!match) parentBranch = "master";
            else parentBranch = match[1].valueOf();
        }
        const newStack = new Stack(this._git, name, parentBranch, this.remoteName );

        this._stacks.set(name, newStack)
        await newStack.AddLevel();

        this.current = newStack.currentLevel;
        return newStack;
    }
}