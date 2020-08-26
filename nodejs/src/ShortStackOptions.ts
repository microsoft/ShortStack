import { CommandLineOptionsClass } from "./Helpers/CommandLineHelper";

export class ShortStackOptions extends CommandLineOptionsClass { 
    commandName= "shortstack"
    shortDescription= `(Version ${require("../package.json").version}) A tool for stacking pull requests in a git repo`
    longDescription= `Great for isolating smaller changes of a much larger big change`

    // @positionalParameter({description: "My ID description", required: true})
    // myId: number = -1;  // Could also be a string
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