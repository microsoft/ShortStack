import { CommandLineOptionsClass, positionalParameter, subCommand, flagParameter } from "./Helpers/CommandLineHelper";

//------------------------------------------------------------------------------
// New command options
//------------------------------------------------------------------------------
export class ShortStackNewOptions extends CommandLineOptionsClass { 
    commandName= "new"
    shortDescription= `Create a new stack or new level on an existing stack`
    longDescription= ``

    @positionalParameter({description: "Name of the stack to create"})
    stackName: string | null = null;

    @positionalParameter({description: "Desired root branch for this stack"})
    root: string | null = null;

    validate(reportError: (paramaterName: string, message: string) => void){}
}

//------------------------------------------------------------------------------
// go command options
//------------------------------------------------------------------------------
export class ShortStackGoOptions extends CommandLineOptionsClass { 
    commandName= "go"
    shortDescription= `Go to a particular stack and/or stack level`
    longDescription= ``

    @positionalParameter({description: "Name or level of the stack to go to (default is current stack)"})
    nameOrLevel: string | null = null;

    @positionalParameter({description: "Level of the stack to go to"})
    level: string | null = null;

    validate(reportError: (paramaterName: string, message: string) => void){}
}

//------------------------------------------------------------------------------
// main program options
//------------------------------------------------------------------------------
export class ShortStackOptions extends CommandLineOptionsClass { 
    commandName= "shortstack"
    shortDescription= `(Version ${require("../package.json").version}) A tool for stacking pull requests in a git repo`
    longDescription= `Great for isolating smaller changes of a much larger big change`

    @flagParameter({description: "Specify this if you want to launch missiles"})
    launchMissiles = false;

    @subCommand({
        description: "A Shortstack action.  Use 'shortstack help actions' to see available actions.",
        commands: [ShortStackNewOptions, ShortStackGoOptions]
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