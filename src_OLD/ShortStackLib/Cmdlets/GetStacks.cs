using System;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Shows the available stacks in the current repository
    /// </summary>
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsCommon.Get, "Stacks")]
    [OutputType(typeof(StackInfo))]
    public class GetStacks : ShortStackCmdletBase
    {
        //---------------------------------------------------------------------------------
        /// <summary>
        /// ProcessRecord - main processing goes here
        /// </summary>
        //---------------------------------------------------------------------------------
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            if (StackData.Length == 0)
            {
                Print(ConsoleColor.Yellow, "There are no stacks in this repository");
            }
            else
            {
                Print(text: "Found these stacks:");
                foreach (var stack in StackData)
                {
                    Print(text: $"    {stack.StackName} ({stack.Levels.Count()})");
                    if(ObjectOutput.IsPresent)
                    {
                        WriteObject(stack);
                    }
                }
            }
        }
    }
}
