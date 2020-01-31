using System;
using System.Management.Automation;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// <para type="synopsis">Start a new set of stacked pull requests.</para>
    /// <para type="description">This is the first command to run.</para>
    /// </summary>    
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsCommon.Remove, "Stack")]
    [OutputType(typeof(StackInfo))]
    public class RomoveStack : ShortStackCmdletBase
    {
        /// <summary>
        /// <para type="description">The name for this new stack.  Choose a short, descriptive name
        /// with letters, numbers, and underscores.</para>
        /// </summary>
        [Parameter(Position = 1)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">Use this flag to also delete the stack branches on the server (origin).</para>
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeOrigin { get; set; }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ProcessRecord
        /// </summary>
        //---------------------------------------------------------------------------------
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            if(Name == null && CurrentStack == null)
            {
                WriteWarning("No stack specified or implied.  Nothing to do.");
                return;
            }

            var stackName = string.IsNullOrEmpty(Name) ? CurrentStack.StackName : Name;
            var branchNames = Handler.GetBranchNames(stackName, IncludeOrigin.ToBool());

            if (branchNames == null || branchNames.Length == 0)
            {
                WriteWarning("Could not find any branches to remove.");
                return;
            }

            Print(ConsoleColor.Yellow, "This command will delete the following branches:");
            foreach(var branchName in branchNames)
            {
                Print(ConsoleColor.Yellow, $"    {branchName}");
            }
            Print(ConsoleColor.Yellow, "Type 'YES' to continue:");
            var userinput = Console.ReadLine().ToUpper();
            if(userinput == "YES")
            {
                Print("OK!  Farewell, fine branches.  We hardly knew you...");
                Handler.PurgeStack(stackName, IncludeOrigin.ToBool());
            }
            else
            {
                Print("No branches deleted.");
            }
        }
    }
}
