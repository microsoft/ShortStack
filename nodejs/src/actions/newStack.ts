import { ShortStackOptions } from "../ShortStackOptions";
import { ShortStackProcessor } from "../models/ShortStackProcessor";
import chalk from "chalk";
import { GitHelper } from "../helpers/gitHelper";

//------------------------------------------------------------------------------
// Start a new stack or extend the current stack 1 level
//------------------------------------------------------------------------------
export function newStack(options: ShortStackOptions)
{
    const handler = new ShortStackProcessor(new GitHelper());
    handler.createNewStack(options.stackName, options.stackOrigin);

    console.log(chalk.greenBright("==================================="));
    console.log(chalk.greenBright("---  Your new stack  is ready!  ---"));
    console.log(chalk.greenBright("==================================="));
    console.log("Next steps:");
    console.log("    1) Commit changes. Keep your changes simple becaue making new stack levels is easy");
    console.log("    2) Push your change:  Push-Changes");
    console.log("That's it!  Shortstack will auto-create a pull request and a new stack level.");

    // if (ObjectOutput.IsPresent)
    // {
    //     WriteObject(GetCurrentStackLevel());
    // }

    

}