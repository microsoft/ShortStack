using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Goes to a target level of a given stack
    /// </summary>
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsCommon.Select, "StackLevel")] 
    [OutputType(typeof(StackInfo))]
    public class SelectStackLevel : ShortStackCmdletBase
    {
        /// <summary>
        /// Desired stack name
        /// </summary>
        [Parameter(Position = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Desired stack position
        /// </summary>
        [Parameter(Position = 2)]
        public string Level { get; set; }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ProcessRecord - main processing goes here
        /// </summary>
        //---------------------------------------------------------------------------------
        protected override void ProcessRecord()
        {
            ProcessSafely(() =>
            {
                int stackLevel = (int)StackLevelAlias.Top;
                var stackName = (string)null;

                if(Name == null)
                {

                }
                else if(Level != null)
                {
                    stackName = Name;
                    if(TryParseLevelNumber(Level, out var parsedLevel))
                    {
                        stackLevel = parsedLevel;
                    }
                    else
                    {
                        throw new ShortStackException("Stack level must be a number or [Top|Bottom|Root]");
                    }
                }
                else if(TryParseLevelNumber(Name, out var parsedLevel))
                {
                    stackLevel = parsedLevel;
                }
                else
                {
                    stackName = Name;
                    stackLevel = (int)StackLevelAlias.Top;
                }

                Handler.GoToStack(stackName, stackLevel);
                Print(ConsoleColor.White, $"Checked out to {GetCurrentStackLevel().LocalBranch}");
                if(ObjectOutput.IsPresent) WriteObject(CurrentStack.CurrentLevel());
            });
        }
    }
}
