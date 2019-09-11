using LibGit2Sharp;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Credential help
    /// </summary>
    //---------------------------------------------------------------------------------
    class Credentials
    {
        private static readonly string VisualStudioOnlineResourceId = "499b84ac-1321-427f-aa17-267ca6975798";
        private static readonly string TfsClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";

        static AuthenticationResult _cachedResult;
        static DateTime _cacheTime = DateTime.MinValue;
        static object _credentialLock = new object();

        /// <summary>
        /// The visual studio toekn
        /// </summary>
        public static AuthenticationResult VisualStudioToken
        {
            get
            {
                lock(_credentialLock)
                {
                    if(_cachedResult == null || (DateTime.Now - _cacheTime).TotalMinutes > 30)
                    {
                        var context = new AuthenticationContext("https://login.microsoftonline.com/common");
                        var userCred = new UserCredential();
                        _cacheTime = DateTime.Now;
                        _cachedResult = context.AcquireTokenAsync(VisualStudioOnlineResourceId, TfsClientId, userCred).Result;

                    }
                    return _cachedResult;
                }
            }
        }
    }
}
