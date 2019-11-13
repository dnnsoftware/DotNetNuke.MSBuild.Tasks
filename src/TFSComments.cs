using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Build.Utilities;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace DotNetNuke.MSBuild.Tasks
{
    public class TfsComments : Task
    {
        public string Server { get; set; }
        public string Root { get; set; }
        public string Committer { get; set; }

        public override bool Execute()
        {
            Server = ""; //TODO: Fill the TFS Server
            Root = @"$/DotNetNuke/src/DotNetNuke_CS/";
            Committer = ""; //TODO: Fill the committer username.

#pragma warning disable 612,618
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(Server);
#pragma warning restore 612,618
            var vcs = (VersionControlServer)tfs.GetService(typeof(VersionControlServer));

            string path = Root;
            VersionSpec version = VersionSpec.Latest;
            const int deletionId = 0;
            const RecursionType recursion = RecursionType.Full;
            string user = null;
            VersionSpec versionFrom = null;
            VersionSpec versionTo = null;
            const int maxCount = 100;
            const bool includeChanges = true;
            const bool slotMode = true;
            const bool includeDownloadInfo = true;

            IEnumerable enumerable =
              vcs.QueryHistory(path,
                              version,
                              deletionId,
                              recursion,
                              user,
                              versionFrom,
                              versionTo,
                              maxCount,
                              includeChanges,
                              slotMode,
                              includeDownloadInfo);


            var c = new List<Changeset>();

            foreach (var i in enumerable)
            {
                var cs = (i as Changeset);
                if (cs != null && cs.Committer != Committer)
                {
                    foreach(var change in cs.Changes)
                    {
                     if(!change.Item.ServerItem.Contains("Professional"))
                        {
                         c.Add(cs);
                       }    
                    }
                }
            }
            return true;
        }
    }
}
