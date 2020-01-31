using System.Management.Automation;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Finish the stack and push all the changes back to origin in one big PR
    /// </summary>
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsLifecycle.Complete, "Stack")]
    public class CompleteStack : ShortStackCmdletBase
    {
        //---------------------------------------------------------------------------------
        /// <summary>
        /// ProcessRecord - main processing goes here
        /// </summary>
        //---------------------------------------------------------------------------------
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            CheckForDanglingWork();

            //write - host - ForegroundColor Yellow "WARNING: This command will delete local stacked branches and mark all pull requests as complete."
            //write - host "It is assumed you have run the following:"
            //write - host - NoNewLine - ForegroundColor White "SS UPDATE 0"
            //write - host "   - to make sure everything has been pushed"
            //write - host - NoNewLine - ForegroundColor White "SS STATUS"
            //write - host "   - to make sure you have pull requests in place."
            //write - host - ForegroundColor DarkYellow "Do you wish to continue?"

            // Ask the user to type yes to continue

            // Get a filled out stackinfo to see if we need to resolve any problems
            //      If needed, Run an update on branch 0 to bring in all external changes and normalize the stack

            // Create a new branch:  users/username/name/FINISH
            //      Delete the exiting finish branch if it already exists
            //      git merge with the last stacked branch
            //      git push to origin/[same-branch-name]
            
            // Find all related pull requests (free from a filled out stack info)
            //      Mark as completed
            //      Remember the titles and descriptions for one big master description
            //      Remember the URLs to the PRs
            //      Build a large description that has a big summary comment on the top and a list of links to PRs on the bottom


            // Create a pull request for the final branch with the big description you built earlier

            // If we can successfully create the pull request
            //      Delete all the local and remote stacked branches, but keep the finish branch just in case





        }
    }
}