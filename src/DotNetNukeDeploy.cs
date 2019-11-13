using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using log4net;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Xml;

namespace DotNetNuke.MSBuild.Tasks
{
    using System.Xml.XPath;
    using System.IO;
    using System.Xml.Linq;

    public class DotNetNukeDeploy : Task
    {
        private readonly IISManager iisMgr = new IISManager();
        public string PhysicalPath { get; set; }
        public string WebsiteName { get; set; }
        public string AppPool { get; set; }
        public string Install { get; set; }
        bool autoFailed;
        [Output]
        public string Error { get; set; }

        public override bool Execute()
        {
            try
            {
                if (iisMgr.VDirExists(WebsiteName))
                {
                    iisMgr.DeleteVirtualDirectory("localhost", WebsiteName);
                }

                iisMgr.CreateVirtualDirectory(WebsiteName, PhysicalPath, AppPool);

                String username = Environment.UserDomainName + "\\" + Environment.UserName;
                LogFormat("Message", "-----------------------------");
                LogFormat("Message", "Setting Permission to: " + username);
                LogFormat("Message", "-----------------------------");
                DirectoryManager.SetFolderPermissions(username, new DirectoryInfo(PhysicalPath));

                if (Install == "true")
                {
                    autoFailed = false;
                    var url = string.Format("http://localhost/{0}/Install/Install.aspx?mode=install", WebsiteName);
                    LogFormat("Message", "Install URL: - " + url + "\r\n");
                    var wc = new DotNetNukeDeployWebClient();

                    string data = TestUrl(wc, url);

                    autoFailed = (data.Contains("Error") || data.Contains("error") || data.Contains("bypasses"));

                    if (!autoFailed)
                    {
                        var homePageUrl = string.Format("http://localhost/{0}/default.aspx", WebsiteName);
                        string homePage = TestUrl(wc, homePageUrl);
                        LogFormat("Message", "Install URL: - " + homePageUrl + "\r\n");
                    }

                    LogFormat("Message", "-----------------------------");
                    LogFormat("Message", "DNN INSTALL LOGGING INFO");
                    LogFormat("Message", "-----------------------------");

                    LogFormat("Message", "log output to:" + Directory.GetCurrentDirectory());
                    using (var file = new StreamWriter(Directory.GetCurrentDirectory() + "\\installLog.html"))
                    {
                        file.WriteLine(data);
                    }

                    if (autoFailed)
                    {
                        LogFormat("Error ", data);
                        return false;
                    }

                    LogFormat("Message", "-------------------------------------------\r\n");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                Error = "ERROR OCCURRED DURING AUTO-INSTALL " + ex.Message + "-- Stack: " + ex.StackTrace;

                LogFormat("Error", Error);
                return false;
            }
        }

        private string TestUrl(DotNetNukeDeployWebClient wc, string url)
        {
            var retrys = 5;
            var data = "";

            while (retrys > 0)
            {
                try
                {
                    data = wc.DownloadString(url);
                    break;
                }
                catch (Exception)
                {
                    LogFormat("Message", "Testing URL: " + url + " Failed, Tries Left " + retrys);
                    Thread.Sleep(5000);
                    retrys--;

                    if (retrys == 0)
                    {
                        throw;
                    }
                }
            }
            return data;
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
