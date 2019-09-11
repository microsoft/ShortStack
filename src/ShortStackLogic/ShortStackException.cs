using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    public class ShortStackException : Exception
    {
        public ShortStackException(string message) : base(message) { }
    }
}
