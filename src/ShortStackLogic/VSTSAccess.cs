using LibGit2Sharp;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// Helper for VSTS calls
    /// </summary>
    //---------------------------------------------------------------------------------
    class VSTSAccess
    {
        private string _apiRoot;

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        public VSTSAccess(string webUrl)
        {
            var partMatch = Regex.Match(webUrl, "^https://(.+?).visualstudio.com(/.+?)?(/.+?)?/_git/(.+?)$");
            if (partMatch.Success)
            {
                var server = partMatch.Groups[1].Value;
                var collection = partMatch.Groups[2].Value.Trim('/');
                var project = partMatch.Groups[3].Value.Trim('/');
                var repository = partMatch.Groups[4].Value.Trim('/');
                if (collection == "")
                {
                    // 'server/_git/Name' is a repository with the same name as the project
                    collection = "DefaultCollection";
                    project = repository;
                }
                else if (project == "")
                {
                    // 'server/Project/_git/Name' is a repository in a single-project collection
                    project = collection;
                    collection = "DefaultCollection";
                }

                _apiRoot = $"https://{server}.visualstudio.com/{collection}/{project}/_apis/git/repositories/{repository}";
            }
            else
            {
                throw new ShortStackException($"ERROR: the web url is in an unrecognized format: {webUrl}");
            }


            //  How to create the header:  'Authorization' = UserCredential.CreateAuthorizationHeader
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Construct the url to access the VSTS rest api
        /// </summary>
        //---------------------------------------------------------------------------------
        string GetApiUrl(string api, Dictionary<string, string> query = null)
        {
            var queryString = new StringBuilder();
            if(query != null)
            {
                foreach(var pair in query)
                {
                    queryString.Append($"&{pair.Key}={HttpUtility.UrlEncode(pair.Value)}");
                }
            }
            return $"{_apiRoot}/{api}?api-version=3.0{queryString}";
        }

        class PullRequestReponse
        {
            public StackPullRequest[] value { get; set; }
            public int count { get; set; }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get the pull request for the specified branch
        /// </summary>
        //---------------------------------------------------------------------------------
        internal StackPullRequest GetPullRequestByBranch(string originBranchName)
        {
            var query = new Dictionary<string, string>(){ { "sourceRefName", "refs/heads/" + originBranchName } };
            var response = RestGet<PullRequestReponse>(VstsApi.pullRequests, query);
            if (response.count == 0) return null;
            return response.value[0];
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Get the pull request for the specified branch
        /// </summary>
        //---------------------------------------------------------------------------------
        internal StackPullRequest CreatePullRequestByBranch(string branchName, string targetBranchName, string title, string description)
        {
            var query = new Dictionary<string, string>() {
                { "sourceRefName", "refs/heads/" + branchName },
                { "targetRefName", "refs/heads/" + targetBranchName },
                { "title", title },
                { "description", description },
            };
            var response = RestPost<PullRequestReponse>(VstsApi.pullRequests, new FormUrlEncodedContent(query));
            if (response.count == 0) return null;
            return response.value[0];
        }

        class JsonContent : StringContent
        {
            public JsonContent(object serializeMe)
                : base(JsonConvert.SerializeObject(serializeMe, Formatting.Indented)
                      , Encoding.UTF8
                      , "application/json")
            { }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Update an existing pull request with new information
        /// </summary>
        //---------------------------------------------------------------------------------
        internal void AmmendPullRequest(StackPullRequest existingPullRequest, Dictionary<string, string> patchDictionary)
        {
            var patchJson = JsonConvert.SerializeObject(patchDictionary);
            var query = new Dictionary<string, string>();
            var response = RestPatch<PullRequestReponse>(
                VstsApi.pullRequests, existingPullRequest.pullRequestId.ToString(), new JsonContent(patchJson));
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Make a PATCH rest call
        /// </summary>
        //---------------------------------------------------------------------------------
        T RestPatch<T>(VstsApi api, string id, HttpContent content, Dictionary<string, string> query = null)
            => RestCall<T>((HttpClient client)
                => client.PatchAsync(new Uri(GetApiUrl($"{api}/{id}", query)), content));


        /// <summary>
        /// Apis available for VSTS
        /// </summary>
        enum VstsApi
        {
            pullRequests
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Make a GET rest call
        /// </summary>
        //---------------------------------------------------------------------------------
        T RestGet<T>(VstsApi api, Dictionary<string, string> query = null)
            => RestCall<T>((HttpClient client) => client.GetAsync(new Uri(GetApiUrl(api.ToString(), query))));

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Make a POST rest call
        /// </summary>
        //---------------------------------------------------------------------------------
        T RestPost<T>(VstsApi api, HttpContent content, Dictionary<string, string> query = null)
            => RestCall<T>((HttpClient client) => client.PostAsync(new Uri(GetApiUrl(api.ToString(), query)), content));

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Make a rest call
        /// </summary>
        //---------------------------------------------------------------------------------
        T RestCall<T>(Func<HttpClient, Task<HttpResponseMessage>> action)
        {
            using (var client = new HttpClient())
            {
                var authHeaderParts = Credentials.VisualStudioToken.CreateAuthorizationHeader().Split(' ');
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authHeaderParts[0], authHeaderParts[1]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                var response = action.Invoke(client).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Parse the response body.
                    var jsonText = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine("JSON:" + jsonText);
                    return JsonConvert.DeserializeObject<T>(jsonText);
                }
                else
                {
                    throw new ShortStackException($"VSTS error: {response.StatusCode}: {response.ReasonPhrase}");
                }

            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Abandon a pull request
        /// </summary>
        //---------------------------------------------------------------------------------
        internal void AbandonPullRequest(object id)
        {
           // throw new NotImplementedException();
        }
    }
}
