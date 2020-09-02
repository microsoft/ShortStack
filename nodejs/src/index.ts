import { ShortStackOptions } from "./ShortStackOptions";
import chalk from "chalk"
import { StackHandler } from "./models/StackHandler";

//------------------------------------------------------------------------------
// main
//------------------------------------------------------------------------------
async function main() {

    process.on("SIGINT", async () => {
        console.log("*** Process was interrupted! ***")
        process.exit(1);
    });

    try {
        const options = new ShortStackOptions(); 
      
        if(options.showHelp)
        {
            options.showUsage();
            return 0;
        }

        if(options.validationErrors) {
            console.log("Validation errors: ");
            for(const error of options.validationErrors){
                console.log(`    ${error.paramaterName}:  ${error.message}`);
            }
        }

        if(!options.action)  throw Error("No action specified."); 

        const handler = new StackHandler(console.log);
        switch(options.action)
        {
            case "new":  await handler.new(options.actionArguments || []); break;
            case "go":  console.log("GO"); break;
            default: throw Error(`Unknown action: ${options.action}`)
        }
        return 0;
    } catch (error) {
        console.error(error.stack);
        return 1;
    } 
}

// ------------------------------------------------------------------------------
// ------------------------------------------------------------------------------
// ------------------------------------------------------------------------------

console.log(chalk.whiteBright(`SHORTSTACK v${require("../package.json").version}`))

main()
    .then(status => {
        //console.log(`Exiting with status: ${status}`)
        process.exit(status);
    });

