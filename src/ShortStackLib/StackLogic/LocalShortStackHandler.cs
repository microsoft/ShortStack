using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Call shortstack logic directly (as opposed to using an RPC server)
    /// </summary>
    //---------------------------------------------------------------------------------
    public class LocalShortStackHandler : IShortStackHandler
    {
#pragma warning disable CS1591
        ShortStackProcessor _processor;

        public LocalShortStackHandler(string path)
        {
            _processor = new ShortStackProcessor(path);
            _processor.OnNotify += _processor_OnNotify;
        }

        private void _processor_OnNotify(ShortStackNotification notification)
        {
            var outputColor = ConsoleColor.Gray;
            switch(notification.Status)
            {
                case ShortStackNotification.NotificationStatus.Detail: outputColor = ConsoleColor.DarkGray; break;
                case ShortStackNotification.NotificationStatus.Warning: outputColor = ConsoleColor.Yellow; break;
                case ShortStackNotification.NotificationStatus.Error: outputColor = ConsoleColor.Red; break;
            }

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = outputColor;
            Console.WriteLine(notification.DisplayText);
            Console.ForegroundColor = originalColor;
        }

        public DanglingWorkStatus CheckServerForDanglingWork() => _processor.GetDanglingWorkStatus();
        public void CreatePullRequest(string commitDescription) => _processor.CreatePullRequest(commitDescription);
        public StackLevel GetLevelDetails(StackInfo stack, StackLevel level) => _processor.GetLevelDetails(level);
        public StackInfo[] GetStacks() => _processor.Stacks.Values.ToArray();
        public void GoToStack(string stackName, int stackLevel) => _processor.GoToStackLevel(stackName, stackLevel);
        public void NewStack(string name, string desiredOrigin) => _processor.CreateNewStack(name, desiredOrigin);
        public StackCommit[] PushStackLevel() => _processor.PushStackLevel();
        public string[] GetBranchNames(string stackName, bool includeOrigin) => _processor.GetBranchNames(stackName, includeOrigin);
        public void PurgeStack(string stackName, bool includeOrigin) => _processor.PurgeStack(stackName, includeOrigin);
#pragma warning restore CS1591
    }
}
