
// COMMENTED OUT FOR NOW
//  This was part of an early attempt to integrate ShortStack with VS Code
//  Removing for now while we get the logic sorted out and working properly from the 
//  command line.
//          -eric jorgensen



//using Microsoft.Tools.Productivity.ShortStackLogic.Constants;
//using ShortStackServer;
//using ShortStackServer.JsonRpcTypes;
//using StreamJsonRpc;
//using System;
//using System.Diagnostics;
//using System.IO;
//using System.IO.Pipes;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Microsoft.Tools.Productivity.ShortStack
//{
//    //---------------------------------------------------------------------------------
//    /// <summary>
//    /// Rpc version of shortstack functionality
//    /// </summary>
//    //---------------------------------------------------------------------------------
//    class RpcShortStackHandler : IShortStackHandler, IDisposable
//    {
//        JsonRpc _server;
//        Task _serverTask;
//        string _rootPath;

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// ctor - get connected to the server
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public RpcShortStackHandler(string rootPath)
//        {
//            _rootPath = rootPath;
//            Debug.WriteLine("Starting Server Handler");
//            var stopwatch = Stopwatch.StartNew();
//            // Create the named pipe used for the JSON-RPC communication
//            var pipeName = @"\\.\pipe\powerShell-pipe-" + Guid.NewGuid().ToString("D");
//            var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
//            var startParams = new ServerStartParameters()
//            {
//                DebugOnStart = false,
//                PipeName = pipeName,  
//            };
//            _serverTask = ServerManager.KickOff(startParams);

//            Debug.WriteLine("Client waiting to connect on " + pipeName);
//            pipe.WaitForConnection();
//            _server = JsonRpc.Attach(pipe, this);
//            stopwatch.Stop();
//            Debug.WriteLine($"Acquired Server ({stopwatch.Elapsed.TotalSeconds.ToString("0.00")}s)");
//        }

//        T DoRpcCall<T>(Func<T> callme)
//        {
//            try
//            {
//                return callme();
//            }
//            catch(Exception e)
//            {
//                if (e.InnerException != null && e.InnerException is ShortStackException)
//                {
//                    throw new ShortStackException(e.InnerException.Message);
//                }
//                else
//                {
//                    Debug.WriteLine(e.ToString());
//                    throw e.InnerException;
//                }
//            }
//        }

//        void DoRpcCall(Action callme)
//        {
//            try
//            {
//                callme();
//            }
//            catch (Exception e)
//            {
//                if (e.InnerException != null && e.InnerException is ShortStackException)
//                {
//                    throw new ShortStackException(e.InnerException.Message);
//                }
//                else
//                {
//                    Debug.WriteLine(e.ToString());
//                    throw e.InnerException;
//                }
//            }
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Get all of the stacks available in the current location
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public StackInfo[] GetStacks()
//        {
//            return DoRpcCall(() =>
//            {
//                var requestParams = new NonStackRequestParams(Directory.GetCurrentDirectory());
//                var result = _server.InvokeWithParameterObjectAsync<StackInfoRequestResponse>("getStackInfoFromFilePath", requestParams).Result;
//                return result.StackInfo.ToArray();
//            });
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Create a new stack
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public void NewStack(string name, string desiredOrigin)
//        {
//            DoRpcCall(() =>
//            {
//                var requestParams = new NewStackRequestParams(Directory.GetCurrentDirectory(), name, desiredOrigin);
//                var result = _server.InvokeWithParameterObjectAsync<StackInfoRequestResponse>("createNewStack", requestParams).Result;
//            });
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Call the server to check for dangling work
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public DanglingWorkStatus CheckServerForDanglingWork()
//        {
//            return DoRpcCall(() =>
//            {
//                var requestParams = new NonStackRequestParams(Directory.GetCurrentDirectory()) { };
//                return _server.InvokeWithParameterObjectAsync<DanglingWorkStatus>(StringConstants.CheckForDanglingWork, requestParams).Result;
//            });
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Call the server to retrieve all uncomitted work
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public DanglingWork GetDanglingWork()
//        {
//            return DoRpcCall(() =>
//            {
//                var requestParams = new NonStackRequestParams(Directory.GetCurrentDirectory()) { };
//                return _server.InvokeWithParameterObjectAsync<DanglingWork>(StringConstants.CheckForDanglingWork, requestParams).Result;
//            });
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Translated vesion of GotoStack
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public void GoToStack(StackInfo stack, StackLevel stackLevel)
//        {
//            DoRpcCall(() =>
//            {
//                var requestParams = new StackedRequestParams(stack, stackLevel);
//                var result = _server.InvokeWithParameterObjectAsync<StackInfoRequestResponse>(StringConstants.GoToStackLevel, requestParams).Result;
//            });
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Call the server to go to another branch in the stack
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public void GoToStack(string stackName, int stackLevel)
//        {
//            throw new ShortStackException("Bad Service call");
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Fill in the details of a level
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public StackLevel GetLevelDetails(StackInfo stack, StackLevel level)
//        {
//            return DoRpcCall(() =>
//            {
//                var requestParams = new StackedRequestParams(stack, level);
//                var result = _server.InvokeWithParameterObjectAsync<GetLevelDetailsResponse>(StringConstants.GetLevelDetails, requestParams).Result;
//                return result.StackLevel;
//            });
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Create a new stack level
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public void NewStackLevel(StackInfo stackInfo)
//        {
//            DoRpcCall(() =>
//            {
//                var requestParams = new StackedRequestParams(stackInfo, null);
//                var result = _server.InvokeWithParameterObjectAsync<StackInfoRequestResponse>("createNewStackLevel", requestParams).Result;
//            });
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// PushStackLevel
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public StackCommit[] PushStackLevel()
//        {
//            throw new NotImplementedException();
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// PurgeStack
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public void PurgeStack(string stackName, bool includeOrigin)
//        {
//            throw new NotImplementedException();
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// GetBranchNamesForStackName
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public string[] GetBranchNames(string stackName, bool includeOrigin)
//        {
//            throw new NotImplementedException();
//        }

//        //---------------------------------------------------------------------------------
//        /// <summary>
//        /// Clean up the rpc when we are done
//        /// </summary>
//        //---------------------------------------------------------------------------------
//        public void Dispose()
//        {
//        }
//        /// <summary>
//        /// Create a pull request
//        /// </summary>
//        public void CreatePullRequest(string commitDescription)
//        {
//            throw new NotImplementedException();
//        }
//    }

//}
