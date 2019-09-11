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
    /// Notification for stack level createion
    /// </summary>
    //---------------------------------------------------------------------------------
    public class SSMsgNewStackLevel
    {
        public string StackName { get; set; }
        public int LevelNumber { get; set; }
    }
}
