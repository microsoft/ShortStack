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
    //------------------------------------------------------------------------------
    async new(args: string[] = []) {
        this._logLine("New Stack")
    }
}