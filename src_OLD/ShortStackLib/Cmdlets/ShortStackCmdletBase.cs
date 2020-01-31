using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Common names for special levels
    /// </summary>
    //---------------------------------------------------------------------------------
    public enum LevelConstants
    {
        /// <summary>
        /// The most recently created level of the stack
        /// </summary>
        Top,

        /// <summary>
        /// Level 1, the first level created on the stack
        /// </summary>
        Bottom,

        /// <summary>
        /// Level 0
        /// </summary>
        Root
    }

    //---------------------------------------------------------------------------------
    /// <summary>
    /// Common behavior for SS cmdlets
    /// </summary>
    //---------------------------------------------------------------------------------
    public class ShortStackCmdletBase : Cmdlet
    {
        /// <summary>
        /// Indicates that we want the output to go into objects 
        /// and not readable text output
        /// </summary>
        [Parameter(HelpMessage = "Tells Short Stack to output to the object pipeline instead of readable text.")]     
        public SwitchParameter ObjectOutput { get; set; }

        private StackInfo[] _stackData;
        /// <summary>
        /// Stack info for the whole local repository
        /// </summary>
        protected StackInfo[] StackData => _stackData ?? (_stackData = Handler.GetStacks().ToArray());

        private StackInfo _currentStack;
        /// <summary>
        /// The stack we are at right now
        /// </summary>
        protected StackInfo CurrentStack => _currentStack ?? (_currentStack = StackData.Where(s => s.CurrentLevelNumber != null).FirstOrDefault());

        static ConsoleColor _defaultForegroundColor;

        /// <summary>
        /// use this to execute short stack logic
        /// </summary>
        protected IShortStackHandler Handler = new LocalShortStackHandler(new SessionState().Path.CurrentFileSystemLocation.ToString());

        //---------------------------------------------------------------------------------
        /// <summary>
        /// static ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        static ShortStackCmdletBase()
        {
            _defaultForegroundColor = Console.ForegroundColor;
        }
        
        //---------------------------------------------------------------------------------
        /// <summary>
        /// Return a stack by name
        /// </summary>
        //---------------------------------------------------------------------------------
        protected StackInfo GetStack(string name) => StackData.Where(s => s.StackName.ToLower() == name.ToLower()).FirstOrDefault();

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Return a stack by name
        /// </summary>
        //---------------------------------------------------------------------------------
        protected bool TryParseLevelNumber(string text, out int level)
        {
            switch(text.ToLowerInvariant())
            {
                case "root": level = (int)StackLevelAlias.Root; return true;
                case "top": level = (int)StackLevelAlias.Top; return true;
                case "bottom": level = (int)StackLevelAlias.Bottom; return true;
                default:
                    if(int.TryParse(text, out var result))
                    {
                        level = result;
                        return true;
                    }
                    level = 0;
                    return false;
            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Standard processing for all of the cmdlets
        /// </summary>
        //---------------------------------------------------------------------------------
        protected void ProcessSafely(Action processingAction)
        {
            try
            {
                base.ProcessRecord();
                processingAction();
            }
            catch(ShortStackException e)
            {
                WriteError(new ErrorRecord(e, "ShortStack Error", ErrorCategory.InvalidOperation, this));
            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Warn user if uncomitted/unadded files
        /// </summary>
        //---------------------------------------------------------------------------------
        protected DanglingWorkStatus CheckForDanglingWork()
        {
            var status = Handler.CheckServerForDanglingWork();
            
            if (status == DanglingWorkStatus.UncommittedChanges)
            {
                Print(ConsoleColor.Yellow, "WARNING: The current environment has uncommitted changes.");
                Print(ConsoleColor.Yellow, $"(Current branch is {CurrentStack?.CurrentLevel().LocalBranch}");
                Print(ConsoleColor.Yellow, $"Please 'git stash' or commit changes before running the command.");
            }
            if (status == DanglingWorkStatus.UnpushedCommits)
            {
                Print(ConsoleColor.Yellow, "WARNING: There are unpushed commits.");
                Print(ConsoleColor.Yellow, $"(Current branch is {CurrentStack?.CurrentLevel().LocalBranch}");
            }
            return status;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Print with no carriage return
        /// </summary>
        //---------------------------------------------------------------------------------
        protected void Print(ConsoleColor color, int x, int y, string text)
        {
            if (ObjectOutput.IsPresent) return;
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.SetCursorPosition(x, y);
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Print with carriage return
        /// </summary>
        //---------------------------------------------------------------------------------
        protected void Print(ConsoleColor color, string text)
        {
            if (ObjectOutput.IsPresent) return;
            Print(color, Console.CursorLeft, Console.CursorTop, text);
            Console.WriteLine();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Print with carriage return
        /// </summary>
        //---------------------------------------------------------------------------------
        protected void Print(string text)
        {
            if (ObjectOutput.IsPresent) return;
            Print(_defaultForegroundColor, Console.CursorLeft, Console.CursorTop, text);
            Console.WriteLine();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get the current Stack level
        /// </summary>
        //---------------------------------------------------------------------------------
        protected StackLevel GetCurrentStackLevel()
        {
            return CurrentStack.Levels.FirstOrDefault(l => l.Number == CurrentStack.CurrentLevelNumber);
        }
    }
}
