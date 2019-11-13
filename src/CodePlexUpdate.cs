using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using log4net;
using Microsoft.Build.Utilities;

namespace DotNetNuke.MSBuild.Tasks
{
    public class CodePlexUpdate : Task
    {
        public string TFSServer { get; set; }
        public string TFSRoot { get; set; }
        public string TFSMappedFolder { get; set; }
        public string TFSCommitter { get; set; }

        public string CPServer { get; set; }
        public string CPRoot { get; set; }
        public string TempFolder { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(@"c:\lastrun.txt"))
            {
                var sw = File.CreateText(@"c:\lastrun.txt");
                sw.Write(DateTime.Today.AddDays(-1).Ticks);
                sw.Close();
            }
            long ticks;
            var sw2 = File.OpenText(@"c:\lastrun.txt");
            long.TryParse(sw2.ReadToEnd(), out ticks);
            sw2.Close();
            var fromDate = new DateTime(ticks);

#pragma warning disable 612,618
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(TFSServer);
#pragma warning restore 612,618
            var vcs = (VersionControlServer)tfs.GetService(typeof(VersionControlServer));

            string path = TFSRoot;
            VersionSpec version = VersionSpec.Latest;
            const int deletionId = 0;
            const RecursionType recursion = RecursionType.Full;
            string user = null;
            VersionSpec versionFrom = new DateVersionSpec(fromDate);
            VersionSpec versionTo = null;
            const int maxCount = 100;
            const bool includeChanges = true;
            const bool slotMode = true;
            const bool includeDownloadInfo = true;

            IEnumerable allChangeSets =
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


            var changes = new List<Change>();
            var comments = new StringBuilder();
            foreach (var changeSet in allChangeSets.Cast<Changeset>().Where(changeSet => changeSet != null && changeSet.Committer != TFSCommitter))
            {
                comments.Append(string.Format("{0}", changeSet.Comment));
                changes.AddRange(changeSet.Changes.Where(change => change.Item.ServerItem.Contains("Community") || change.Item.ServerItem.Contains("Website") || change.Item.ServerItem.Contains("packages")));
            }
            if (Directory.Exists(TempFolder))
            {
                Directory.Delete(TempFolder,true);
            }
            Directory.CreateDirectory(TempFolder);
            var networkCredential = new NetworkCredential("", "", "snd"); //TODO: need fill the credentials
            using (var tfsCodePlex = new TeamFoundationServer(CPServer, networkCredential))
            {
                var vcsCodePlex = tfsCodePlex.GetService(typeof(VersionControlServer)) as VersionControlServer;
                    //vcsCodePlex.DeleteWorkspace("CodePlex Temporary Workspace", vcsCodePlex.AuthenticatedUser);
                var workspace = vcsCodePlex.CreateWorkspace("CodePlex Temporary Workspace", vcsCodePlex.AuthenticatedUser);
                try
                {
                    var workingFolder = new WorkingFolder(CPRoot, TempFolder);
                    workspace.CreateMapping(workingFolder);
                    var listDistinctChanges = new List<Change>();

                    foreach (Change change in changes.OrderByDescending(s => s.Item.CheckinDate))
                    {
                        if (listDistinctChanges.Count(s => s.Item.ServerItem == change.Item.ServerItem) == 0)
                        {
                            listDistinctChanges.Add(change);
                        }
                    }
                    foreach (Change change in listDistinctChanges)
                    {
                        var changedFileName = change.Item.ServerItem.Replace(TFSRoot, TFSMappedFolder);
                        var destFileName = change.Item.ServerItem.Replace(TFSRoot, TempFolder);
                        var destSccName = change.Item.ServerItem.Replace(TFSRoot, CPRoot);
                        var changeSwitch = change.ChangeType.ToString().Trim();
                        switch (changeSwitch)
                        {
                            case "Add, Edit, Encoding":
                                Directory.CreateDirectory(destFileName.Remove(destFileName.LastIndexOf("/", System.StringComparison.InvariantCulture)));
                                if (File.Exists(changedFileName))
                                {
                                    File.Copy(changedFileName, destFileName, true);
                                    workspace.PendAdd(destFileName);
                                }
                                break;
                            case "Edit":
                                workspace.Get(new GetRequest(destSccName, RecursionType.None, VersionSpec.Latest), GetOptions.Overwrite);
                                if (File.Exists(changedFileName))
                                {
                                    workspace.PendEdit(destSccName);
                                    File.Copy(changedFileName, destFileName, true);
                                }
                                break;
                            case "Delete":
                                workspace.Get(new GetRequest(destSccName, RecursionType.None, VersionSpec.Latest), GetOptions.Overwrite);
                                workspace.PendDelete(destSccName);
                                File.Delete(destFileName);
                                break;
                        }
                        Debug.Print(changedFileName);
                        Debug.Print(destFileName);
                    }
                    var pendingChanges = workspace.GetPendingChanges();
                    if (pendingChanges.Any())
                    {
                        workspace.CheckIn(pendingChanges, comments.ToString());
                    }
                }
                catch (Exception ex)
                {
                    LogFormat("Error", ex.Message);
                }
                finally
                {
                    var sw = File.CreateText(@"c:\lastrun.txt");
                    sw.Write(DateTime.Now.Ticks);
                    sw.Close();
                    workspace.Delete();
                }
            }
            return true;
        }

        private void LogFormat(string level, string message, params object[] args)
        {
            if (BuildEngine != null)
            {
                switch (level)
                {
                    case "Message":
                        Log.LogMessage(message, args);
                        break;
                    case "Error":
                        Log.LogError(message, args);
                        break;
                }
            }
            else
            {
                Debug.Print(message, args);
            }
        }

    }
}
