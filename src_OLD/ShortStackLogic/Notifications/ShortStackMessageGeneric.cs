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
    /// General notification to bubble up to users
    /// </summary>
    //---------------------------------------------------------------------------------
    public class ShortStackMessageGeneric
    {
        public ShortStackNotification.NotificationStatus Status { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        private string _message;

        public ShortStackMessageGeneric(string message)
        {
            _message = message;
        }

        public override string ToString()
        {
            return _message;
        }
    }
}
