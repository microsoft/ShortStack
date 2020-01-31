using System.Management.Automation;
using System.Threading;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Creates a pull request for the current stack.
    /// </summary>
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsCommon.New, "PullRequest")] 
    [OutputType(typeof(StackInfo))]
    public class NewPullRequest : ShortStackCmdletBase
    {
        /// <summary>
        /// If this is specified, any dangling work will be committed
        /// with this description
        /// </summary>
        [Parameter(Position = 1)]
        public string CommitDescription { get; set; }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ProcessRecord - main processing goes here
        /// </summary>
        //---------------------------------------------------------------------------------
        protected override void ProcessRecord()
        {
            ProcessSafely(() =>
            {
                Handler.CreatePullRequest(CommitDescription);
            });

        }
    }
}