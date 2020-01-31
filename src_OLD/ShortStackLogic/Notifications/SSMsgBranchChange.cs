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
    /// Notification for Branch changes
    /// </summary>
    //---------------------------------------------------------------------------------
    public class SSMsgBranchChange
    {
        public string OldBranchName { get; set; }
        public string NewBranchName { get; set; }
    }
}
