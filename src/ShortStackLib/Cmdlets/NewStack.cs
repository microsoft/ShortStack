using System;
using System.Management.Automation;

namespace Microsoft.Tools.Productivity.ShortStack
{
    /// <summary>
    /// <para type="synopsis">Start a new set of stacked pull requests.</para>
    /// <para type="description">This is the first command to run.</para>
    /// </summary>    
    [Cmdlet(VerbsCommon.New, "Stack")]
    [OutputType(typeof(StackInfo))]
    public class NewStack : ShortStackCmdletBase
    {
        /// <summary>
        /// <para type="description">The name for this new stack.  Choose a short, descriptive name
        /// with letters, numbers, and underscores.</para>
        /// </summary>
        [Parameter(Position = 1, Mandatory = false)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The origin for this stack.  The default is 'master'.</para>
        /// </summary>
        [Parameter(Position = 2)]
        public string DesiredOrigin { get; set; } = "master"; // This is the default value

        /// <summary>
        /// ProcessRecord
        /// </summary>
        protected override void ProcessRecord()
        {
            ProcessSafely(() =>
            {
                Handler.NewStack(Name, DesiredOrigin);

                Print(ConsoleColor.Green, "===================================");
                Print(ConsoleColor.Green, "---  Your new stack  is ready!  ---");
                Print(ConsoleColor.Green, "===================================");
                Print("Next steps:");
                Print("    1) Commit changes. Keep your changes simple becaue making new stack levels is easy");
                Print("    2) Push your change:  Push-Changes");
                Print("That's it!  Shortstack will auto-create a pull request and a new stack level.");

                if (ObjectOutput.IsPresent)
                {
                    WriteObject(GetCurrentStackLevel());
                }

            });
        }
    }
}
