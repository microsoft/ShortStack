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
    public class ExampleCode
    {
        ///[TestMethod]
        public async Task StartServerAndGetStackInfo()
        {
            await Task.Run(() =>
            {
                var serverPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shortStackServer.exe"));
                ProcessStartInfo info = new ProcessStartInfo(serverPath);
                info.UseShellExecute = false;
                info.CreateNoWindow = true;

                // Create the named pipe used for the JSON-RPC communication
                var pipeName = "powerShell-pipe-" + Guid.NewGuid().ToString("D");
                var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // Pass the pipe name to the language server
                info.Arguments = @"--pipe \\.\pipe\" + pipeName + " --debugOnStart";

                Process process = new Process();
                process.StartInfo = info;

                process.Start();
                pipe.WaitForConnection();

                //var requestParams = new StackInfoRequestParams("c:\\foo");

                //var jsonRpc = StreamJsonRpc.JsonRpc.Attach(pipe);
                //var result = await jsonRpc.InvokeWithParameterObjectAsync<StackInfoRequestResponse>("getStackInfoFromFilePath", requestParams);
                //await jsonRpc.InvokeAsync("shutdown");
            });
        }
    }
}
