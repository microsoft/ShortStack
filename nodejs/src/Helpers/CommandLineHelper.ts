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
    // Example of how to use in a script:
    const options = new MyCommandLineOptions(); 
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
    Positional,
    NameValue,
    Flag,
    Environment
}


//------------------------------------------------------------------------------
// Properties for a parameter
//------------------------------------------------------------------------------
export interface ParameterProperties
{
    description?: string
    alternateNames?: string[]
    required?: boolean
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

    constructor(propertyName: string, type: ParameterType){
        this.propertyName = propertyName;
        this.type = type;
    }
}

const objectParameterMap = new Map<string, Map<string, ArgumentDetails>>();

//------------------------------------------------------------------------------
// helper to safely get the parameter map from an object
//------------------------------------------------------------------------------
function getParameterMap(target: any, constructIfMissing: boolean = false)
{
    if(!objectParameterMap.get(target.constructor.name) && constructIfMissing)
    {
        objectParameterMap.set(target.constructor.name, new Map<string, ArgumentDetails>());
    }

    const properties = objectParameterMap.get(target.constructor.name);
    if(!properties) throw Error("Could not find properties definitions for " + target.constructor.name);
    return properties;
}

//------------------------------------------------------------------------------
// general decorator for parameters
//------------------------------------------------------------------------------
function genericParameter(type: ParameterType, args: {description?: string, alternateNames?: string[], required?: boolean}) {
    return function decorator(this: any, target: any, key: string) {
        const properties = getParameterMap(target, true);
        if(properties.get(key)) return;

        const details = new ArgumentDetails(key, type);
        details.alternateNames = args.alternateNames;
        details.description = args.description;
        if(args.required) details.required = args.required;
        details.orderValue = properties.size;
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
    // Show Usage
    //------------------------------------------------------------------------------
    processCommandLine(argv: string[] | null = null)
    {
        if(!argv) argv = process.argv.slice(2);
        try {
            fillOptions(this, argv, this.addError)

            if(argv.length == 0) this.showHelp = true;
            if(this.showHelp) return;

            this.validate(this.addError);            
        }
        catch(err)
        {
            this.addError("GENERAL ERROR", err.message);
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
    let parameters = objectParameterMap.get(CommandLineOptionsClass.name)
    if(!parameters) parameters = new Map<string, ArgumentDetails>();
    else {
        parameters.forEach(p => p.orderValue += 10000);
    }
    
    const localParameters = objectParameterMap.get(options.constructor.name);
    if(localParameters) {
        for(const parameter of localParameters.values())
        {
            parameters?.set(parameter.propertyName, parameter);
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
// Process a decorated class with the argument list
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

//------------------------------------------------------------------------------
// Process a decorated class with the argument list
//------------------------------------------------------------------------------
function fillOptions(options: any, argv: string[], reportError: (paramaterName: string, message: string) => void)
{
    const parameters = getParameters(options);

    const positionalHandlers = new Array<(v: string) => void>();
    const positionalNames = new Array<string>();
    // const argHandlers = new Map<string, (v: string) => void>();
    const flagHandlers = new Map<string, () => void>();
    
    for(const parameter of parameters.values())
    {
        switch(parameter.type)
        {
            case  ParameterType.Positional: 
                positionalHandlers.push((v: string) => {options[parameter.propertyName] = v});
                positionalNames.push(parameter.propertyName);
                break;
            case  ParameterType.Flag: 
                if(parameter.alternateNames)
                {
                    for(const name of parameter.alternateNames)
                    {
                        flagHandlers.set(name.toLowerCase(), () => {options[parameter.propertyName] = true});
                    }
                }
                else 
                {
                    flagHandlers.set(parameter.propertyName.toLowerCase(), () => {options[parameter.propertyName] = true});
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
        }
    }

    let positionIndex = 0;
    for(let i = 0; i < argv.length; i++)
    {

        const trimmed = argv[i].replace(/^([-/]*)/, "");
        const parts = trimmed.split('=');

        // Maybe this is a flag
        const parameterName= parts[0].toLowerCase();
        const flagHandler = flagHandlers.get(parameterName);
        if(flagHandler) {
            flagHandler();
            continue;
        }
        else 
        {
            // if it looks like a plain argument, try to process it
            // as a positional paraments
            if(positionIndex < positionalHandlers.length)
            {
                try {
                    positionalHandlers[positionIndex](argv[i]);
                }
                catch(err)
                {
                    reportError(positionalNames[positionIndex], err.message)
                }

                positionIndex++;
                continue;
            }
            else {
                reportError(parameterName, "Unknown parameter")
            }
        }

    }
}