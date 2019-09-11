using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Prints out the current status of the stack
    /// </summary>
    //---------------------------------------------------------------------------------
    [Cmdlet(VerbsCommon.Get, "StackStatus")]
    [OutputType(typeof(StackInfo))]
    public class GetStackStatus : ShortStackCmdletBase
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
                int stackLevel = -1;
                var stackName = (string)null;

                if (Name == null)
                {

                }
                else if (Level != null)
                {
                    stackName = Name;
                    if (TryParseLevelNumber(Level, out var parsedLevel))
                    {
                        stackLevel = parsedLevel;
                    }
                    else
                    {
                        throw new ShortStackException("Stack level must be a number or [Top|Bottom|Root]");
                    }
                }
                else if (TryParseLevelNumber(Name, out var parsedLevel))
                {
                    stackLevel = parsedLevel;
                }
                else
                {
                    stackName = Name;
                }

                if(stackName == null)
                {
                    if(CurrentStack == null)
                    {
                        throw new ShortStackException("Not currently on a stack.");
                    }

                    stackName = CurrentStack.StackName;
                }

                var stack = GetStack(stackName);
                if(stack == null)
                {
                    throw new ShortStackException($"Unknown stack: {stackName}");
                }

                var levelList = new List<StackLevel>();
                if(stackLevel > -1)
                {
                    if(stackLevel >= stack.Levels.Count)
                    {
                        stackLevel = stack.Levels.Count - 1;
                    }
                    levelList.Add(stack.Levels[stackLevel]);
                }
                else
                {
                    levelList.AddRange(stack.Levels);
                }

                Print(ConsoleColor.White, "============== STACK STATUS ================== ");
                var updateTasks = new List<Action>();
                var warningColor = ConsoleColor.Yellow;
                var normalColor = ConsoleColor.Gray;

                // Print what we can get, which is the number of levels we have.
                foreach (var level in levelList)
                {

                    var consoleRow = Console.CursorTop;
                    var currentCue = level.IsCurrent ? "* " : "  ";
                    Print($"{currentCue}{level.Number} ");

                    // This is a task to individually fill in this level
                    Action fillInDetails = () =>
                    {
                        level.FillDetails(Handler);
                        lock (updateTasks)
                        {
                            if (level.Number == 0)
                            {
                                if (level.UnpulledCommits.Length > 0)
                                {
                                    Print(warningColor, 10, consoleRow, $"{level.UnpulledCommits.Length} commits to pull from {stack.Origin} (Use 'Update-Stack 0' to bring these in)");
                                }
                                else
                                {
                                    Print(normalColor, 10, consoleRow, $"Up to date with {stack.Origin}");
                                }
                            }
                            else
                            {
                                Print(level.UnpulledCommits.Length > 0 ? warningColor : normalColor, 10, consoleRow, $"[{level.UnpulledCommits.Length}]");
                                Print(level.UnpulledCommits.Length > 0 ? warningColor : normalColor, 15, consoleRow, $"[{level.UnpushedCommits.Length}]");
                                var title = "No pull request";
                                var titleColor = warningColor;
                                if (level.PullRequest != null)
                                {
                                    title = level.PullRequest.title;
                                    titleColor = normalColor;
                                }
                                Print(titleColor, 20, consoleRow, $" {title}");
                            }
                        }
                    };

                    updateTasks.Add(fillInDetails);
                }

                var lastLine = Console.CursorTop;

                Parallel.Invoke(updateTasks.ToArray());
                Console.CursorTop = lastLine;
            });
        }
    }
}
