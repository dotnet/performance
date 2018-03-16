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
    public class Git : IDisposable
    {
        public string Url { get; private set; }
        public string Branch
        {
            get => branch;
            set {
                if (branch != value)
                {
                    branch = value;
                    if (Location != null) {
                        Fetch();
                    }
                }
            }
        }
        public string Commit
        {
            get => commit;
            set {
                if (commit != value)
                {
                    commit = value;
                    if (Location != null) {
                        Checkout();
                    }
                }
            }
        }
        public DirectoryInfo Location { get; private set; }

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
