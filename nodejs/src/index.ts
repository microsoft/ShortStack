import { ShortStackOptions, ShortStackAction } from "./ShortStackOptions";
import chalk from "chalk"
import { runTest } from "./actions/test";

//------------------------------------------------------------------------------
// main
//------------------------------------------------------------------------------
async function main(argv: string[]) {

    process.on("SIGINT", async () => {
        console.log("*** Process was interrupted! ***")
        process.exit(1);
    });

    try {
        const options = new ShortStackOptions(argv);
       
        if(options.action == ShortStackAction.Help)
        {
            showHelp(options);
            return 0;
        }

        if(options.badArgs.length > 0)
        {
            console.log("ERROR: Bad arguments: ");
            options.badArgs.forEach(arg => console.log(`  ${arg}`));
            return 1;
        }   

        switch(options.action)
        {
            case ShortStackAction.Test: runTest(); break;
        }

        // TODO: Run your application code here with your options
        // await myObject.doSomething();
        return 0;
    } catch (error) {
        console.error(error.stack);
        return 1;
    } 
}

//------------------------------------------------------------------------------
// show help
//------------------------------------------------------------------------------
function showHelp(options: ShortStackOptions)
{
    if(!options.helpOption)   
    {
        console.log("ShortStack is a tool for handling a stacked pull request workflow.")
        console.log("")
        console.log("for more information:")
        console.log(chalk.whiteBright("    ss help commands      ") + "Show available commands");
        console.log(chalk.whiteBright("    ss help workflow      ") + "Describe the ss workflow");
        console.log(chalk.whiteBright("    ss help setup         ") + "Instructions on how to set up your environment for stacked PRs");
    }

    switch(options.helpOption)
    {
        case "commands":
            console.log("Shortstack commands:");
            console.log(chalk.whiteBright("    test"));
            console.log("        Launch integration tests")
            break;
        case "workflow":
            break;
        case "setup":
            break;
    }
}

//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------

console.log(chalk.whiteBright('SHORTSTACK v0.01.00'))

main(process.argv.slice(2))
    .then(status => {
        //console.log(`Exiting with status: ${status}`)
        process.exit(status);
    });

