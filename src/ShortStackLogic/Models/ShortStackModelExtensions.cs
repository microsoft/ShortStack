using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Extensions for the models
    /// </summary>
    //---------------------------------------------------------------------------------
    public static class ShortStackModelExtensions
    {
        //---------------------------------------------------------------------------------
        /// <summary>
        /// Current level in the stack
        /// </summary>
        //---------------------------------------------------------------------------------
        public static void SetCurrentLevel(this StackInfo stack, StackLevel newLevel)
        {
            stack.CurrentLevelNumber = -1;
            foreach(var level in stack.Levels)
            {
                level.IsCurrent = (newLevel != null && level.Number == newLevel.Number);
                if (level.IsCurrent) stack.CurrentLevelNumber = level.Number;
            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Current level in the stack
        /// </summary>
        //---------------------------------------------------------------------------------
        public static StackLevel CurrentLevel(this StackInfo stack)
        {
            return stack.Levels.Where(l => l.Number == stack.CurrentLevelNumber).FirstOrDefault();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Number of the last level
        /// </summary>
        //---------------------------------------------------------------------------------
        public static int LastLevelNumber(this StackInfo stack)
        {
            if (stack.Levels.Count == 0) return -1;
            return stack.Levels.Select(l => l.Number).Max();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Topmost level in the stack
        /// </summary>
        //---------------------------------------------------------------------------------
        public static StackLevel LastLevel(this StackInfo stack)
        {
            var lastLevelNumber = stack.LastLevelNumber();
            if (lastLevelNumber == -1) return null;
            return stack.Levels.Where(l => l.Number == lastLevelNumber).FirstOrDefault();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get branch name with stack suffix
        /// </summary>
        //---------------------------------------------------------------------------------
        public static string CreateBranchLevelName(this StackInfo stack, int levelNumber)
        {
            return stack.StackName + "/ss" + levelNumber.ToString("000");
        }
    }
}
