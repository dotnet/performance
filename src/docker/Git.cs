using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DockerHarness
{
    /// <summary>
    ///   A class to identify and clone git repositories
    ///   It identifies a git repo as the combination of Url, Branch, and Commit
    ///   It also contains functions to clone and checkout git repos
    /// </summary>
    public class Git : IDisposable
    {
        /// <summary>
        ///   The URL where this repository lives
        ///   It may be any string acceptable to `git clone`
        ///   Generally this is a GitHub HTTPS URL
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        ///   The branch within the repo this object refers to
        ///   Setting this will ensure a branch is fetched for a cloned repo
        ///   If left null, it is assumed to be the deafault branch for the repo
        /// </summary>
        public string Branch
        {
            get => branch;
            set
            {
                if (branch != value)
                {
                    branch = value;
                    if (Location != null) {
                        Fetch();
                        if (Commit == null)
                        {
                            Checkout();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   The specific commit in the repo thiw object refers to
        ///   If null, it is assumed to be the branch HEAD
        /// </summary>
        public string Commit
        {
            get => commit;
            set
            {
                if (commit != value)
                {
                    commit = value;
                    if (Location != null) {
                        Checkout();
                    }
                }
            }
        }

        /// <summary>
        ///   DirectoryInfo pointing to the location on disk this repo is cloned
        ///   Will be null if the repo has not been cloned
        /// </summary>
        public DirectoryInfo Location { get; private set; }

        /// <summary>
        ///   A set of branches which have been fetched
        /// </summary>
        private ISet<string> fetched = new HashSet<string>();

        public Git(string url, string branch=null, string commit=null)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
            Url = url;
            Branch = branch;
            Commit = commit;
        }

        /// <summary>
        ///   Clones the repository, fetching and checking out the appropriate
        ///   commit as needed
        ///   Returns the DirectoryInfo of pointing to the directory where it landed
        /// </summary>
        public DirectoryInfo Clone()
        {
            if (Location == null)
            {
                Location = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "git-repos", Util.RandomString(16)));
                Util.Command("git", $"clone {Url} {Location}");

                if (Branch != null)
                {
                    Fetch();
                }

                if (Commit != null)
                {
                    Util.Command("git", $"checkout {Commit}", Location);
                }
            }

            return Location;
        }

        /// <summary>
        ///   Ensures the current Branch has been fetched
        /// </summary>
        private void Fetch()
        {
            if (Branch != null)
            {
                if (!fetched.Contains(Branch))
                {
                    Util.Command("git", $"fetch origin {Branch}", Location);
                    fetched.Add(Branch);
                }
            }
        }

        /// <summary>
        ///   Ensures the correct commit or branch is checked out
        ///   If Commit and Branch are null, this assumes "master"
        /// </summary>
        private void Checkout()
        {
            if (Commit != null)
            {
                Util.Command("git", $"checkout {Commit}", Location);
            }
            else if (Branch != null)
            {
                Util.Command("git", $"checkout {Branch}", Location);
            }
            else
            {
                Util.Command("git", $"checkout master", Location);
            }
        }

        public void Dispose()
        {
           Dispose(true);
           GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Cleans up unmanaged resources
        ///   This Git object may have cloned a repo which should be deleted
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                if (Location == null)
                {
                    Util.DeleteDirectory(Location);
                    Location = null;
                }
            }
        }

        private string branch;
        private string commit;
    }
}
