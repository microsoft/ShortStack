//------------------------------------------------------------------------------
// Simple Class for thinking about options
//------------------------------------------------------------------------------
export class ShortStackOptions
{
    shouldMoo = true;
    brainSize: string | undefined = undefined;
    badArgs = new Array<string>();

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(argv: string[])
    {
        for(let i =2; i < argv.length; i++)
        {
            // commandline is -name=value  (or /name=value), value is optional
            let trimmed = argv[i].replace(/^([-/]*)/, "").toLowerCase();
            let parts = trimmed.split('=');
            switch(parts[0])
            {
                case "brainsize": this.brainSize = parts[1]; break;
                case "moo": this.shouldMoo = true; break;
                default: this.badArgs.push(argv[i]); break;
            }
        }
    }
}