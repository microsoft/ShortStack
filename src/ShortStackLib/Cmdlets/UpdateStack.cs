using System;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Ensures that each level of the stack has all changes from the previous stack
    /// </summary>
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsData.Update, "Stack")] 
    [OutputType(typeof(StackInfo))]
    public class UpdateStack : ShortStackCmdletBase
    {

        /// <summary>
        /// <para type="description">The branch to start pulling from</para>
        /// </summary>
        [Parameter(Position = 1, Mandatory = false)]
        public string Start { get; set; }

        /// <summary>
        /// <para type="description">The branch to pull to</para>
        /// </summary>
        [Parameter(Position = 1, Mandatory = false)]
        public string Stop { get; set; }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ProcessRecord - main processing goes here
        /// </summary>
        //---------------------------------------------------------------------------------
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            if (CurrentStack == null)
            {
                Print(ConsoleColor.Red, "You are not in a stacked branch.  Use Get-Stacks to see available stacks.");
                return;
            }

            var danglingStatus = Handler.CheckServerForDanglingWork();

            if (danglingStatus == DanglingWorkStatus.UncommittedChanges)
            {
                Print(ConsoleColor.Red, "There are uncommitted changes. Please commit your changes first.");
                return;
            }

            int startLevel, stopLevel;
            if (!this.TryParseLevelNumber(Start, out startLevel))
            {
                if (String.IsNullOrEmpty(Start))
                {
                    startLevel = 1;
                }
                else
                {
                    Print(ConsoleColor.Red, "Start is an invalid level number");
                    return;
                }
            }

            if (!this.TryParseLevelNumber(Stop, out stopLevel))
            {
                if (String.IsNullOrEmpty(Stop))
                {
                    stopLevel = CurrentStack.CurrentLevel().Number;
                }
                else
                {
                    Print(ConsoleColor.Red, "Stop is an invalid level number");
                    return;
                }
            }

            if (startLevel > stopLevel)
            {
                Print(ConsoleColor.Red, "Start must be an earlier level than Stop");
                return;
            }


            StackLevel originalLevel = this.GetCurrentStackLevel();

            for (int i = startLevel; i <= stopLevel; i++)
            {
                StackLevel level = this.CurrentStack.Levels[i];
                this.Handler.GoToStack(this.CurrentStack.StackName, level.Number);
                level.FillDetails(this.Handler);
                if (level.UnpulledCommits.Any())
                {
                    //Pull and do a merge conflict
                }
                if (level.UnpushedCommits.Any() || level.UnpushedCommits.Any())
                {
                    this.Handler.PushStackLevel();
                }
            }

            this.Handler.GoToStack(this.CurrentStack.StackName, originalLevel.Number);
        }
    }
}
