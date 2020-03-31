//------------------------------------------------------------------------------
// Simple Class for thinking about options
//------------------------------------------------------------------------------
export class ShortStackOptions
{
    showHelp = false;
    helpOption: string | undefined = undefined;
    badArgs = new Array<string>();

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(argv: string[])
    {
        if(argv.length == 0)
        {
            this.showHelp = true;
            return;
        }

        // commandline is -name=value  (or /name=value), value is optional
        const argument = this.getArgParts(argv[0]);
        switch(argument.name)
        {
            case "h": 
            case "help": 
            case "?": this.processHelp(argv.slice(1)); break;
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
        if(parts.length == 2) parts[0] = parts[0].toLowerCase();
        return {name: parts[0], value: parts.length == 2 ? parts[1] : undefined}
    }

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    processHelp(argv: string[])
    {
        this.showHelp = true;
        if(argv.length > 0)
        {
            this.helpOption = this.getArgParts(argv[0]).name;
        }
    }
}