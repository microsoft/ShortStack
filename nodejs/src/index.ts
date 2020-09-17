import { ShortStackOptions, ShortStackNewOptions, ShortStackListOptions } from "./ShortStackOptions";
import chalk from "chalk"
import { CommandHandler, ShortStackError } from "./models/CommandHandler";
import { CreateOptions } from "./Helpers/CommandLineHelper";

//------------------------------------------------------------------------------
// main
//------------------------------------------------------------------------------
async function main() {

    process.on("SIGINT", async () => {
        console.log("*** Process was interrupted! ***")
        process.exit(1);
    });

    try {
        const options = CreateOptions(ShortStackOptions);
      
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
            return 1;
        }

        if(!options.action)  throw Error("No action specified."); 

        const handler = new CommandHandler(console.log);
        await handler.init();
        switch(options.action.commandName)
        {
            case "new":  await handler.new(options.action! as ShortStackNewOptions); break;
            case "go":  console.log("GO"); break;
            case "list":  await handler.list(options.action! as ShortStackListOptions); break;
            default: throw Error(`Unknown action: ${options.action}`)
        }
        return 0;
    } catch (error) {
        if(error instanceof ShortStackError) {
            console.error(chalk.redBright(error.message));
        }
        else {
            console.error(chalk.redBright(error.stack));
        }
        return 1;
    } 
}

// ------------------------------------------------------------------------------
// ------------------------------------------------------------------------------
// ------------------------------------------------------------------------------

console.log(chalk.whiteBright(`SHORTSTACK v${require("../package.json").version} -------------------------------------------------------`))

main()
    .then(status => {
        //console.log(`Exiting with status: ${status}`)
        process.exit(status);
    });
