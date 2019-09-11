using System.Management.Automation;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Push current changes to the server.
    /// </summary>
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsCommon.Push,"StackLevel")] 
    [OutputType(typeof(StackChangeInfo))]
    public class PushStackLevel : ShortStackCmdletBase
    {
        //---------------------------------------------------------------------------------
        /// <summary>
        /// ProcessRecord - main processing goes here
        /// </summary>
        //---------------------------------------------------------------------------------
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            try
            {
                var pushedChanges = Handler.PushStackLevel();
                if (pushedChanges == null || pushedChanges.Length == 0)
                {
                    WriteWarning("There are (probably) no changes to push.");
                }
                else 
                {
                    foreach(var commit in pushedChanges)
                    {
                        WriteVerbose($"Pushed: {commit.Id} {commit.ShortMessage}");
                    }
                }

                WriteObject(pushedChanges, enumerateCollection: true);
            }
            catch (ShortStackException e)
            {
                WriteError(new ErrorRecord(e, "Push Error", ErrorCategory.InvalidOperation, null));
            }
        }
    }
}
