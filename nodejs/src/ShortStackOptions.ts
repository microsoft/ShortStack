import { CommandLineOptionsClass, positionalParameter, subCommand, flagParameter } from "./Helpers/CommandLineHelper";

abstract class SubOptions extends CommandLineOptionsClass
{
    longDescription= ``
    validate(reportError: (paramaterName: string, message: string) => void){}
}
//------------------------------------------------------------------------------
// New command options
//------------------------------------------------------------------------------
export class ShortStackNewOptions extends SubOptions { 
    commandName= "new"
    shortDescription= `Create a new stack or new level on an existing stack`

    @positionalParameter({description: "Name of the stack to create"})
    stackName: string | null = null;

    @positionalParameter({description: "Desired root branch for this stack"})
    root: string | null = null;
}

//------------------------------------------------------------------------------
// go command options
//------------------------------------------------------------------------------
export class ShortStackGoOptions extends SubOptions { 
    commandName= "go"
    shortDescription= `Go to a particular stack and/or stack level`

    @positionalParameter({description: "Name or level of the stack to go to (default is current stack)"})
    nameOrLevel: string | null = null;

    @positionalParameter({description: "Level of the stack to go to"})
    level: string | null = null;
}

//------------------------------------------------------------------------------
// list command options
//------------------------------------------------------------------------------
export class ShortStackListOptions extends SubOptions { 
    commandName= "list"
    shortDescription= `List available stacks`
}


//------------------------------------------------------------------------------
// main program options
//------------------------------------------------------------------------------
export class ShortStackOptions extends CommandLineOptionsClass { 
    commandName= "shortstack"
    shortDescription= `(Version ${require("../package.json").version}) A tool for stacking pull requests in a git repo`
    longDescription= `Great for isolating smaller changes of a much larger big change`

    @subCommand({
        description: "A Shortstack action.  Use 'shortstack help actions' to see available actions.",
        commands: [ShortStackNewOptions, ShortStackGoOptions, ShortStackListOptions]
    })
    action?: CommandLineOptionsClass;

    //------------------------------------------------------------------------------
    // validate
    //------------------------------------------------------------------------------
    validate(reportError: (paramaterName: string, message: string) => void)
    {
        // TODO: add code here to validate the full parameter set.
        // if there are any problems, call reportError() so that they will
        // all be reported to the user. 
    }
}