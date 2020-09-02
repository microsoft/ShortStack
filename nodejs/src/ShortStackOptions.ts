import { CommandLineOptionsClass, positionalParameter, remainingParameters as remainingParameters } from "./Helpers/CommandLineHelper";

export class ShortStackNewOptions extends CommandLineOptionsClass { 
    commandName= "new"
    shortDescription= `Create a new stack or new level on an existing stack`
    longDescription= ``

    @positionalParameter({description: "Name of the stack to create"})
    name?: string;

    @positionalParameter({description: "Desired root branch for this stack"})
    root?: string;

    constructor(argv: string[]) { super(); this.processCommandLine(argv); }

    validate(reportError: (paramaterName: string, message: string) => void){}
}

export class ShortStackGoOptions extends CommandLineOptionsClass { 
    commandName= "go"
    shortDescription= `Go to a particular stack and/or stack level`
    longDescription= ``

    @positionalParameter({description: "Name or level of the stack to go to (default is current stack)"})
    name?: string;

    @positionalParameter({description: "Level of the stack to go to"})
    level?: string;

    constructor(argv: string[]) { super(); this.processCommandLine(argv); }

    validate(reportError: (paramaterName: string, message: string) => void){}
}


export class ShortStackOptions extends CommandLineOptionsClass { 
    commandName= "shortstack"
    shortDescription= `(Version ${require("../package.json").version}) A tool for stacking pull requests in a git repo`
    longDescription= `Great for isolating smaller changes of a much larger big change`

    // Action shortstack should take
    @positionalParameter({ description: "Shortstack action.  Use 'shortstack help actions' to see available actions."})
    action?: string;

    @remainingParameters({ description: "Arguments to go with the action"})
    actionArguments?: string[];

    // // Flag parameters - these are True/False
    // // Normally a parameter follow property name, but you can specify any number of alternate names
    // @flagParameter({description: "blah blah"}, alternateNames: [ "c", "choc", "fudge"]})
    // addChocolate = false;
    // // Environment Parameters - these are pulled from the environment
    // @environmentParameter({description: "Buzz buzz", required: true})
    // USERNAME: string | undefined = undefined;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor()
    {
        super();
        this.processCommandLine();
    }

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