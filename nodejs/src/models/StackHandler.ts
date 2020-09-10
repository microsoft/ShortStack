import { ShortStackNewOptions } from "../ShortStackOptions";
import simpleGit, {SimpleGit, SimpleGitOptions, StatusResult} from 'simple-git';

export class ShortStackError extends Error{ }
export class StackHandler 
{
    private _logLine = (text:string) => {};
    private _git: SimpleGit; 
    private _status?: StatusResult;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(logLine: (text: string) => void)
    {
        this._logLine = logLine;

        const options: SimpleGitOptions = {
            baseDir: process.cwd(),
            binary: 'git',
            maxConcurrentProcesses: 6,
        };
        this._git = simpleGit(options);

    }

    //------------------------------------------------------------------------------
    // Initialize git access
    //------------------------------------------------------------------------------
    async init()
    {
        this._git.remote
        try {
            this._status = await this._git.status();
        }
        catch(error) {
            throw new ShortStackError(`Git error: ${error.message}`)    
        }
    }

    //------------------------------------------------------------------------------
    // Find any uncommitted work
    //------------------------------------------------------------------------------
    private async checkForDanglingWork()
    {
        await this._git.fetch();
        const status = this._status!;
        if(status.not_added.length > 0
            || status.conflicted.length > 0
            || status.created.length > 0
            || status.deleted.length > 0
            || status.modified.length > 0
            || status.renamed.length > 0
            || status.files.length > 0
            || status.staged.length > 0) {
                throw new ShortStackError(`The current branch (${status.current}) has uncommitted changes.`)
            }
    }

    //------------------------------------------------------------------------------
    // new - create a new stack or new level in the stack
    // usage:  ss new [stackname]
    //------------------------------------------------------------------------------
    async new(options: ShortStackNewOptions) 
    {
        await this.checkForDanglingWork();
        // TODO:  if(warn_on_dangling_work)  return  
            // git fetch
            // git status
        
        
        // Get a list of all the stacks
        // if options.name is null, then figure out the current stack and level
            // if not stacked: Error "Current branch is not a stacked branch.  To start a stack:  ss new (name) (origin branch to track)"

        // if there is a name
            // Error if the stack exists
            // Create level 0 to track the origin (default to main or master) and push to origin/0  
                // Branch name is name/__stack/0
                // tracking branch is origin/main(or other)
            // Push 0 to server
            // Set current stack object to 0
            //     git branch $newBranch --track $origin *> $null
            //     git checkout $newBranch   *> $null
            //     $pullResult = git_pull $origin
            //     [void](check_for_good_git_result $pullResult)
            //     $pushResult = git_push origin $newBranch
            //     [void](check_for_good_git_result $pushResult)
        
        
        // Create next stack level
            // Create branch n+1 to track n and push to origin n+1
    
        
        // write-host -ForegroundColor Green "==================================="
        // write-host -ForegroundColor Green "---  Your new branch is ready!  ---"
        // write-host -ForegroundColor Green "==================================="
        // write-host "Next steps:"
        // write-host "    1) Keep to a 'single-thesis' change for this branch"
        // write-host "    2) make as many commits as you want"
        // write-host "    3) When your change is finished:"
        // write-host "       ss push   <== pushes your changes up and creates a pull request."
        // write-host "       ss new    <== creates the next branch for a new change.`n`n"


        this._logLine(`NEW: ${options.stackName}, ${options.root}`);
    }
}