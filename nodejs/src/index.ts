import { ShortStackOptions } from "./ShortStackOptions";
import chalk from "chalk"

//------------------------------------------------------------------------------
// main
//------------------------------------------------------------------------------
async function main(options: ShortStackOptions) {
    if(options.badArgs.length > 0)
    {
        console.log("ERROR: Bad arguments: ");
        options.badArgs.forEach(arg => console.log(`  ${arg}`));
        process.exit();
    }
    
    process.on("SIGINT", async () => {
        console.log("*** Process was interrupted! ***")
        process.exit(1);
    });

    try {
        // TODO: Run your application code here with your options
        // await myObject.doSomething();
        return 0;
    } catch (error) {
        console.error(error.stack);
        return 1;
    } 
}

//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------

console.log(chalk.whiteBright('SHORTSTACK v0.01.00'))

main(new ShortStackOptions(process.argv))
    .then(status => {
        //console.log(`Exiting with status: ${status}`)
        process.exit(status);
    });

