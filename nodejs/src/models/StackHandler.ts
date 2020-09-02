import { ShortStackNewOptions } from "../ShortStackOptions";

export class StackHandler 
{
    private _logLine = (text:string) => {};

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(logLine: (text: string) => void)
    {
        this._logLine = logLine;
    }

    //------------------------------------------------------------------------------
    // new - create a new stack or new level in the stack
    // usage:  ss new [stackname]
    //------------------------------------------------------------------------------
    async new(args: string[]) 
    {
        const newOption = new ShortStackNewOptions(args);
        // Look at local branches and find all known stacks
        // look and see if the current branch is a stack
        // if there is a stackname arg,  

        this._logLine(`NEW: ${newOption.name}, ${newOption.root}`);
    }
}