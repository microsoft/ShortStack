using System.Threading;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Interface for working with the stack server
    /// </summary>
    //---------------------------------------------------------------------------------
    public interface IShortStackHandler
    {
        StackInfo[] GetStacks();
        void NewStack(string name, string desiredOrigin);
        void GoToStack(string stackName, int stackLevel);
        void CreatePullRequest(string commitDescription);
        DanglingWorkStatus CheckServerForDanglingWork();
        StackLevel GetLevelDetails(StackInfo stack, StackLevel level);
        string[] GetBranchNames(string stackName, bool includeOrigin);
        void PurgeStack(string stackName, bool includeOrigin);
        StackCommit[] PushStackLevel();
    }
}
