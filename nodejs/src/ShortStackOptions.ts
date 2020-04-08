
export enum ShortStackAction
{
    Help,
    Test,
    New
}

//------------------------------------------------------------------------------
// Simple Class for thinking about options
//------------------------------------------------------------------------------
export class ShortStackOptions
{
    action = ShortStackAction.Help;
    helpOption: string | undefined = undefined;
    stackName: string | undefined = undefined;
    stackOrigin: string | undefined = undefined;
    badArgs = new Array<string>();

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(argv: string[])
    {
        if(argv.length == 0) return;

        // commandline is -name=value  (or /name=value), value is optional
        const argument = this.getArgParts(argv[0]);
        switch(argument.name)
        {
            case "h": 
            case "help": 
            case "?": this.processHelp(argv.slice(1)); break;
            case "t":
            case "test": this.action = ShortStackAction.Test; break;
            default: this.badArgs.push(argv[0]); break;
        }
    }

    //------------------------------------------------------------------------------
    // break -FOO=Bar into {name:"foo", value: "Bar"}
    //------------------------------------------------------------------------------
    getArgParts(argument: string)
    {
        const trimmed = argument.replace(/^([-/]*)/, "");
        const parts = trimmed.split('=',2);
        return {
            name: parts[0].toLowerCase(), 
            value: parts.length == 2 ? parts[1] : undefined
        }
    }

    //------------------------------------------------------------------------------
    // process help command
    //------------------------------------------------------------------------------
    processHelp(argv: string[])
    {
        this.action = ShortStackAction.Help;
        if(argv.length > 0)
        {
            this.helpOption = this.getArgParts(argv[0]).name;
        }
    }

    //------------------------------------------------------------------------------
    // process 'new' command
    //------------------------------------------------------------------------------
    processNew(argv: string[])
    {
        this.action = ShortStackAction.Help;
        if(argv.length > 0) this.stackName = this.getArgParts(argv[0]).name;
        if(argv.length > 1) this.stackOrigin = this.getArgParts(argv[1]).name;       
    }
}