using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Tools.Productivity.ShortStack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShortStackServer.JsonRpcTypes;

namespace ShortStackLibTests
{
    [TestClass]
    public class ShortStackLogicTests
    {
        [TestMethod]
        public void StackInfoEqualityTests()
        {
            var stack1 = new StackInfo("foo", "barurl", "d:\\");
            var stack2 = new StackInfo("FOO", "BARURL", "d:\\");
            var stack3 = new StackInfo("FOO2", "BARURL", "d:\\");
            var stack4 = new StackInfo("FOO", "BARURL2", "d:\\");

            Assert.AreEqual(true, stack1.Equals(stack2));
            Assert.AreEqual(true, stack1 == stack2);
            Assert.AreEqual(true, stack1 != stack3);
            Assert.AreEqual(true, stack1 != stack4);
            Assert.AreEqual(false, stack1.Equals(stack3));
            Assert.AreEqual(false, stack1 == stack3);
            Assert.AreEqual(false, stack1.Equals(stack4));
            Assert.AreEqual(false, stack1 == stack4);
            Assert.AreEqual(true, (StackInfo)null == null);
            Assert.AreEqual(false, (StackInfo)null != null);

            Assert.AreEqual(stack1.GetHashCode(), stack2.GetHashCode());
            Assert.AreNotEqual(stack1.GetHashCode(), stack3.GetHashCode());
        }
    }
}
