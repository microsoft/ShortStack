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
    /// Model for handling information about a Short Stack collection of branches
    /// </summary>
    //---------------------------------------------------------------------------------
    [DataContract]
    public class StackInfo: IEqualityComparer<StackInfo>
    {
        /// <summary>
        /// User-Chosen Stack name
        /// </summary>
        [DataMember(Name = "stackName")]
        public string StackName { get;  }

        /// <summary>
        /// All available levels in this stack
        /// </summary>
        private List<StackLevel> _levels = new List<StackLevel>();
        [DataMember(Name = "levels")]
        public IList<StackLevel> Levels => _levels;

        /// <summary>
        /// The number of the current level (not necessarily the index in levels)
        /// </summary>
        [DataMember(Name = "currentLevelNumber")]
        public int? CurrentLevelNumber { get; set; }

        /// <summary>
        /// Url to the source repository
        /// </summary>
        [DataMember(Name = "repositoryUrl")]
        public string RepositoryUrl { get; }

        /// <summary>
        /// What the origin is for this stack.
        /// </summary>
        [DataMember(Name = "origin")]
        public object Origin { get; internal set; }

        /// <summary>
        /// The root of the repository.
        /// </summary>
        [DataMember(Name = "repositoryRootPath")]
        public string RepositoryRootPath { get;  }

        private int _hashCode;

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        public StackInfo(string stackName, string repositoryUrl, string repositoryRootPath)
        {
            StackName = stackName;
            CurrentLevelNumber = null;
            RepositoryRootPath = repositoryRootPath;
            RepositoryUrl = repositoryUrl;
            _hashCode = ($"{StackName.ToLower()}~{RepositoryRootPath.ToLower()}").GetHashCode();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Add a level
        /// </summary>
        //---------------------------------------------------------------------------------
        public void AddLevel(StackLevel level)
        {
            _levels.Add(level);
            level.Stack = this;
        }

        #region Equality
        //---------------------------------------------------------------------------------
        /// <summary>
        /// == operator
        /// </summary>
        //---------------------------------------------------------------------------------
        public static bool operator ==(StackInfo x, StackInfo y)
        {

            if ((object)x == null) return (object)y == null;
            return x.Equals(y);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// != operator
        /// </summary>
        //---------------------------------------------------------------------------------
        public static bool operator !=(StackInfo x, StackInfo y)
        {
            if ((object)x == null) return (object)y != null;
            return !x.Equals(y);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// See if two stacks are the same
        /// </summary>
        //---------------------------------------------------------------------------------
        public bool Equals(StackInfo x, StackInfo y)
        {
            if (y == null) return false;
            return x.StackName.Equals(y.StackName, StringComparison.OrdinalIgnoreCase)
                && x.RepositoryUrl.Equals(y.RepositoryUrl, StringComparison.OrdinalIgnoreCase);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// See if two stacks are the same
        /// </summary>
        //---------------------------------------------------------------------------------
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return Equals(this, obj as StackInfo);
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// GetHashCode for use in dictionaries
        /// </summary>
        //---------------------------------------------------------------------------------
        public int GetHashCode(StackInfo obj)
        {
            var stackInfo = obj as StackInfo;
            if (stackInfo != null) return stackInfo._hashCode;
            return obj.GetHashCode();
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// GetHashCode for use in dictionaries
        /// </summary>
        //---------------------------------------------------------------------------------
        public override int GetHashCode()
        {
            return _hashCode;
        }
        #endregion
    }
}
