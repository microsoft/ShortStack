/* -----------------------------------------------------------------------------
A decorator approach to command-line parsing.
E.g.: 
    export class MyCommandLineOptions extends CommandLineOptionsClass { 
        commandName= "jenkit"
        shortDescription= `(Version ${require("../package.json").version}) A tool for mincing rectified bean sprouts`
        longDescription= `Yadda yadda yadda`
        // EXAMPLES OF PARAMETERS

        // positional parameter - can be a string or number
        // positional parameter order is controlled by the order in the code.  
        // Positional parameters and named parameters can be intermixed
        // parameters are not required by default
        @positionalParameter({description: "My ID description", required: true})
        myId: number = -1;  // Could also be a string

        // Flag parameters - these are True/False
        // Normally a parameter follow property name, but you can specify any number of alternate names
        @flagParameter({description: "blah blah"}, alternateNames: [ "c", "choc", "fudge"]})
        addChocolate = false;

        // Environment Parameters - these are pulled from the environment
        @environmentParameter({description: "Buzz buzz", required: true})
        USERNAME: string | undefined = undefined;

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

    // Example of how to use in a script:
    const options = CreateOptions(MyCommandLineOptions); 
    if(options.showHelp) options.showUsage();
    if(options.validationErrors) {
        console.log("Validation errors: ");
        for(const error of options.validationErrors){
            console.log(`    ${error.paramaterName}:  ${error.message}`);
        }
    }
----------------------------------------------------------------------------- */

export const HelpRightMargin = 120;

//------------------------------------------------------------------------------
// Types of parameters
//------------------------------------------------------------------------------
enum ParameterType 
{
    Positional = "Positional",              // positional parameter 
    NameValue = "NameValue",                // -Name=Value 
    Flag = "Flag",                          // -Flag
    Environment = "PositiEnvironmentonal",  // Pull this from the environment
    Remaining = "Remaining",                // Suck all the remaining parameters into a string array
    SubCommand = "SubCommand"               // A string subcommand with separate parameters
}


//------------------------------------------------------------------------------
// Properties for a parameter
//------------------------------------------------------------------------------
export interface ParameterProperties
{
    description?: string
    alternateNames?: string[]
    required?: boolean
    commands?: any[] // TODO: need to type this to a class that has a static Create(typearg: CommandLineOptionsClass, args: string[]) 
}

//------------------------------------------------------------------------------
// Properties for the command-line options in general
//------------------------------------------------------------------------------
export interface OptionsClassProperties
{
    commandName?: string 
    shortDescription?: string
    longDescription?: string
}

//------------------------------------------------------------------------------
// details for a particular argument
//------------------------------------------------------------------------------
class ArgumentDetails
{
    description?: string;
    alternateNames?: string[];
    propertyName: string;
    type: ParameterType;
    required = false;
    orderValue = 0;
    contentClassName: string;
    contentConstructor: any;
    decoratorConfig?: ParameterProperties;

    constructor(propertyName: string, type: ParameterType, classTarget: Object){
        this.propertyName = propertyName;
        this.type = type;
        this.contentClassName = classTarget.constructor.name;
        this.contentConstructor = classTarget.constructor;
    }

    public clone(): any 
    {
        var cloneObj = new ArgumentDetails("",ParameterType.Environment,"") as any;
        var me = this as any;
        for (var attribut in this) {
           cloneObj[attribut] = me[attribut];
        }
        return cloneObj;
    }
}

const objectParameterMap = new Map<string, Map<string, ArgumentDetails>>();

//------------------------------------------------------------------------------
// helper to safely get the parameter map from an object
//------------------------------------------------------------------------------
function getParameterMap(className: string, constructIfMissing: boolean = false)
{
    if(!objectParameterMap.get(className) && constructIfMissing)
    {
        objectParameterMap.set(className, new Map<string, ArgumentDetails>());
    }

    const properties = objectParameterMap.get(className);
    if(!properties) throw Error("Could not find properties definitions for " + className);
    return properties;
}

//------------------------------------------------------------------------------
// general decorator for parameters
//------------------------------------------------------------------------------
function genericParameter(type: ParameterType, args: ParameterProperties) {
    return function decorator(this: any, target: any, key: string) {
        const properties = getParameterMap(target.constructor.name, true);
        if(properties.get(key)) return;

        const details = new ArgumentDetails(key, type, target);
        details.decoratorConfig = args;
        details.alternateNames = args.alternateNames;
        details.description = args.description;
        if(args.required) details.required = args.required;
        details.orderValue = properties.size; // order is just the index in the array
        properties.set(key, details);    
    }
}

//------------------------------------------------------------------------------
// positionalParameter decorator
//------------------------------------------------------------------------------
export function positionalParameter(args: ParameterProperties) {
    return genericParameter(ParameterType.Positional, args);
}
  
//------------------------------------------------------------------------------
// flagParameter decorator
//------------------------------------------------------------------------------
export function flagParameter(args: ParameterProperties) {
    return genericParameter(ParameterType.Flag, args);
}
  
//------------------------------------------------------------------------------
// evironmentParameter decorator
//------------------------------------------------------------------------------
export function environmentParameter(args: ParameterProperties) {
    return genericParameter(ParameterType.Environment, args);
}
  
//------------------------------------------------------------------------------
// remainingParameters decorator
//------------------------------------------------------------------------------
export function remainingParameters(args: ParameterProperties) {
    return genericParameter(ParameterType.Remaining, args);
}
  
//------------------------------------------------------------------------------
// parameterSet decorator
//------------------------------------------------------------------------------
export function subCommand(args: ParameterProperties) {
    if(!args.commands) throw Error("@subCOmmand decorators must set the commands property")
    return genericParameter(ParameterType.SubCommand, args);
}
  
//------------------------------------------------------------------------------
// CreateOptions - use this to instantiate your options class
//------------------------------------------------------------------------------
export function CreateOptions(classType: any, args?: string[])
{
    const newOptions = new classType();
    newOptions.processCommandLine(args);
    return newOptions;
}

//------------------------------------------------------------------------------
// Abstract base class for command line options
//------------------------------------------------------------------------------
export abstract class CommandLineOptionsClass {
    abstract commandName: string;
    abstract shortDescription: string;
    abstract longDescription: string;

    @flagParameter({description: "Show Help", alternateNames: ["?", "h", "help"]})
    showHelp = false;  

    validationErrors: {paramaterName: string, message: string}[] | undefined = undefined;

    //------------------------------------------------------------------------------
    // Process command line
    //------------------------------------------------------------------------------
    processCommandLine(argv: string[] | null = null)
    {
        if(this.positionIndex > -1) throw Error("Called processCommandLine Twice");
        this.positionIndex = 0;
        if(!argv) argv = process.argv.slice(2);
        try {

            this.prepareHandlers(this.addError);
            argv.forEach(arg => this.processNextParameter(arg, true, this.addError));
    
            if(argv.length == 0) this.showHelp = true;
            if(this.showHelp) return;

            this.validate(this.addError);            
        }
        catch(err)
        {
            this.addError("GENERAL ERROR", err.message);
        }
    }

    positionalHandlers?: Array<(v: string) => boolean>;
    positionalNames?: Array<string>;
    flagHandlers?: Map<string, () => void>;
    private positionIndex = -1;

    //------------------------------------------------------------------------------
    // Process an options object with decorated members using the argument list
    //------------------------------------------------------------------------------
    private prepareHandlers(reportError: (paramaterName: string, message: string) => void)
    {
        if(this.positionalHandlers) return;
        const parameters = getParameters(this);

        this.positionalHandlers = new Array<(v: string) => boolean>();
        this.positionalNames = new Array<string>();
        this.flagHandlers = new Map<string, () => void>();
        
        const options = this as any;

        for(const parameter of parameters.values())
        {
            switch(parameter.type)
            {
                case  ParameterType.Positional: 
                    this.positionalHandlers.push((v: string) => {
                        options[parameter.propertyName] = v; 
                        return false;});
                    this.positionalNames.push(parameter.propertyName);
                    break;
                case  ParameterType.Flag: 
                    if(parameter.alternateNames)
                    {
                        for(const name of parameter.alternateNames)
                        {
                            this.flagHandlers.set(name.toLowerCase(), () => {options[parameter.propertyName] = true});
                        }
                    }
                    else 
                    {
                        this.flagHandlers.set(parameter.propertyName.toLowerCase(), () => {options[parameter.propertyName] = true});
                    }
                    break;
                case ParameterType.Environment:
                    let envNames = [parameter.propertyName];
                    if(parameter.alternateNames && parameter.alternateNames.length > 0)
                    {
                        envNames = parameter.alternateNames
                    }
                    // Try to get this value now from the environment
                    for(const name of envNames)
                    {
                        const foundValue = process.env[name];
                        if(foundValue) {
                            options[parameter.propertyName] = foundValue;
                            break;
                        }
                    }
                    if(parameter.required && !options[parameter.propertyName])
                    {
                        reportError(envNames.join("|"), "Could not find value from environment. " + parameter.description)
                    }
                    break;
                case  ParameterType.Remaining: 
                    // Since we are getting a set, the return on the handler is "true" to signal that we eat the rest of the parameters
                    options[parameter.propertyName] = new Array<string>();
                    this.positionalHandlers.push((v: string) => {options[parameter.propertyName].push(v); return true;});
                    this.positionalNames.push(parameter.propertyName);
                    break;
                case ParameterType.SubCommand:
                    // A sub command functions like a special positional parameter.  The first
                    // word is the name of the sub command, then the command "eats" the next few parameters 
                    // that belong to the sub command before releasing parameters back to the main command

                    // First, track empty versions of the subcommand options classes so that we know the subcommand
                    // Names.  
                    const subCommands = parameter.decoratorConfig!.commands!
                        .map((commandType: any) => CreateOptions(commandType,[]));
                    
                    let chosenSubCommand: CommandLineOptionsClass | undefined;

                    // The handler will feed arguments to the subcommand
                    this.positionalHandlers.push((arg: string) => {
                        if(!chosenSubCommand) {
                            chosenSubCommand = subCommands.find(c => c.commandName.toLowerCase() == arg.toLowerCase());
                            if(!chosenSubCommand) {
                                 reportError(parameter.propertyName, `Unknown option: ${arg}`);
                                 return false;
                            }
                            options[parameter.propertyName] = chosenSubCommand; 
                            return true; // We could process this, so keep parsing options
                        }
                        return chosenSubCommand.processNextParameter(arg, false, reportError);});
                    this.positionalNames.push(parameter.propertyName);
                    break;
                default: 
                    throw Error(`Bad parameter type: ${parameter.type}`)
            }
        }
    }

    //------------------------------------------------------------------------------
    // Process an options object with decorated members using the argument list
    // Returns true if there was a known way to process the argument
    //------------------------------------------------------------------------------
    processNextParameter(arg: string, reportUnknownParameter: boolean,  reportError: (paramaterName: string, message: string) => void)
    {
        const trimmed = arg.replace(/^([-/]*)/, "");
        const hasPrefix = trimmed.length < arg.length;
        const parts = trimmed.split('=');
        // Maybe this is a flag
        const parameterName= parts[0].toLowerCase();
        const flagHandler = this.flagHandlers!.get(parameterName);

        if(flagHandler && hasPrefix) {
            flagHandler();
            return true;
        }
        else 
        {
            // if it looks like a plain argument, try to process it
            // as a positional paraments
            if(this.positionIndex < this.positionalHandlers!.length)
            {
                try {
                    if(!this.positionalHandlers![this.positionIndex](arg)) 
                    {
                        this.positionIndex++;
                    };
                }
                catch(err)
                {
                    reportError(this.positionalNames![this.positionIndex], err.message)
                    this.positionIndex++;
                }

                return true;
            }
            else {
                if(reportUnknownParameter) reportError(parameterName, "Unknown parameter");
                return false;
            }
        }
    }

    //------------------------------------------------------------------------------
    // Add an error to the validation errors
    //------------------------------------------------------------------------------
    addError = (paramaterName: string, message: string) => {
        if(!this.validationErrors) this.validationErrors = new Array<{paramaterName: string, message: string}>();
        this.validationErrors.push({paramaterName, message});
    };   

    //------------------------------------------------------------------------------
    // validate
    //------------------------------------------------------------------------------
    abstract validate(reportError: (paramaterName: string, message: string) => void): void;
 
    //------------------------------------------------------------------------------
    // Show Usage
    //------------------------------------------------------------------------------
    showUsage(printLine: (text: string) => void = console.log)
    {
        showUsage(this, printLine);
    }
}

//------------------------------------------------------------------------------
// Class communicating problems with the command line
//------------------------------------------------------------------------------
export class OptionsError {
    message = "No Error";
    badArgs = new Array<string>();
}

//------------------------------------------------------------------------------
// Helper to build a parameter list from the base class and derived class
//------------------------------------------------------------------------------
function getParameters(options: any)
{
    const baseParameters = objectParameterMap.get(CommandLineOptionsClass.name)?.values();
    
    let parameters = new Map<string, ArgumentDetails>();
    for(let parameter of baseParameters!)
    {
        const parameterCopy = parameter.clone();
        parameterCopy.orderValue += 10000; // Help parameters should show up last
        parameters.set(parameter.propertyName, parameterCopy)
    }
    
    const localParameters = objectParameterMap.get(options.constructor.name);
    if(localParameters) {
        for(const parameter of localParameters.values())
        {
            parameters?.set(parameter.propertyName, parameter.clone());
        }
    }
    return parameters;
}

//------------------------------------------------------------------------------
// Break up a string so that it overflows cleanly
//------------------------------------------------------------------------------
function formatOverflow(leftMargin: number, rightMargin: number, text: string)
{
    text += " ";
    const output = new Array<string>();
    let currentLine ="";
    let currentWord = "";
    let currentLineHasText = true;

    const flushLine = () =>
    {
        output.push(currentLine);
        currentLine = " ".repeat(leftMargin) + currentWord;
        currentLineHasText = currentWord != "";
        currentWord = "";
    }

    for(let i = 0; i < text.length; i++)
    {
        // skip space at the start of the line
        if(text[i] == ' ' && !currentLineHasText) continue;
        if(text[i].match(/\s/)) {
            if(currentWord !== "") {
                if((currentLine.length + currentWord.length) >= rightMargin) {
                    flushLine()
                }
                else {
                    currentLine += currentWord;
                    currentLineHasText = true;
                    currentWord = "";
                }
            }
            if(text[i] == '\n') flushLine();
            else currentLine += text[i];
        }
        else {
            currentWord += text[i];
        }
    }

    if(currentLine != "") flushLine();

    return output.join("\n");
}

//------------------------------------------------------------------------------
// Show usage text for the options object
//------------------------------------------------------------------------------
function showUsage(options: any, printLine: (text: string) => void)
{ 
    let usageLine = "";
    usageLine += `USAGE: ${options.commandName} `;

    const details = new Array<string>();
    const environmentDetails = new Array<string>();

    const parameters = getParameters(options);
    if(parameters) {
        const quickLine = (name: string, description?: string) =>
        {
            let output =  name;
            output += " ".repeat(40 - output.length);
            if(description) output += description;
            return output;
        }

        const braceIfRequired = (required: boolean, text: string)=>  required ? text : `(${text})`;

        const sortedParameters = Array.from(parameters.values()).sort((v1,v2) => {
            if(v1.propertyName === "showHelp") return 1;
            if(v2.propertyName === "showHelp") return -1;
            if(v1.type != v2.type)
            {
                if(v1.type == ParameterType.Positional) return -1;
                if(v2.type == ParameterType.Positional) return 1;
            }
            if(v1.required != v2.required)
            {
                if(v1.required) return -1;
                if(v2.required) return 1;
            }
            return v1.orderValue - v2.orderValue;
        });

        for(const parameter of sortedParameters)
        {
            switch(parameter.type)
            {
                case  ParameterType.Positional: 
                    usageLine += " " + braceIfRequired(parameter.required, `[${parameter.propertyName}]`);
                    details.push(quickLine(parameter.propertyName, parameter.description));
                    break;
                case ParameterType.Flag:
                    let flags = "-" + parameter.propertyName;

                    if(parameter.alternateNames)
                    {
                        flags = "-" + parameter.alternateNames.join("|-");
                    }

                    usageLine += " " + braceIfRequired(parameter.required, flags);
                    details.push(quickLine(flags, parameter.description));
                    break;
                case ParameterType.Environment:
                    let envNames = [parameter.propertyName];
                    if(parameter.alternateNames && parameter.alternateNames.length > 0)
                    {
                        envNames = parameter.alternateNames
                    }
                    environmentDetails.push(quickLine (envNames.join("|"), parameter.description));
                    break;
                case ParameterType.Remaining:
                    usageLine += " " + braceIfRequired(parameter.required, `[${parameter.propertyName}...]`);
                    details.push(quickLine(parameter.propertyName, parameter.description));
                    break;
                case ParameterType.SubCommand:
                    const subCommandNames = new Array<string>();
                    for(const commandType of parameter.decoratorConfig!.commands!) {
                        const subCommandOptions = CreateOptions(commandType,[]);
                        details.push(quickLine(subCommandOptions.commandName, subCommandOptions.shortDescription))
                        subCommandNames.push(subCommandOptions.commandName)
                    }
                    usageLine += " " + braceIfRequired(parameter.required, `(${subCommandNames.join("|")}) (options)`);
                    break;
                default:
                    console.log(`ERROR: unknown parameter type: ${parameter.type}`);
                    break;
                }
        }
    }

    printLine(`${options.commandName}:  ${formatOverflow(options.commandName.length + 3, HelpRightMargin, options.shortDescription)}`);
    printLine("");
    printLine(formatOverflow(options.commandName.length + 9, HelpRightMargin, usageLine));
    details.forEach(d => printLine("   " + formatOverflow(43, HelpRightMargin, d)));
    if(environmentDetails.length > 0)
    {
        printLine("");
        printLine("Values from environment:");
        environmentDetails.forEach(d => printLine("   " + formatOverflow(43, HelpRightMargin, d)));
    }
    if(options.longDescription)
    {
        printLine("");
        printLine("DETAILED INFORMATION");
        printLine("   " + formatOverflow(3, HelpRightMargin, options.longDescription));
    }
}

