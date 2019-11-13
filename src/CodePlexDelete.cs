using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using log4net;
using Microsoft.Build.Utilities;

namespace DotNetNuke.MSBuild.Tasks
{
    public class CodePlexDelete : Task
    {
        public string FileName { get; set; }
        public string SvnLocation { get; set; }
        public string FileRootPath { get; set; }

        public override bool Execute()
        {
            var tr = new StreamReader(FileName);
            var content = tr.ReadToEnd();
            tr.Close();

            var tableIndexStart = content.IndexOf("<table");
            var tableIndexEnd = content.IndexOf("</table>");
            var table = content.Substring(tableIndexStart, tableIndexEnd - tableIndexStart + 8).Replace("&nbsp;", string.Empty);
            if (table.StartsWith("<table") && table.EndsWith("</table>"))
            {
                XDocument xRoot = XDocument.Parse(table);
                var start = false;
                foreach (var row in xRoot.Root.Nodes())
                {
                    XDocument xRow = XDocument.Parse(row.ToString());
                    var deleteFileRow = xRow.Root.Nodes().ToList()[0].ToString();
                    if (start)
                    {
                        var deleteFile = deleteFileRow.Split(new string[] {"</td>"}, StringSplitOptions.RemoveEmptyEntries)[0];
                        deleteFile = deleteFile.Substring(deleteFile.IndexOf(">")+1, deleteFile.Length - deleteFile.IndexOf(">")-1).Replace("\\", @"\");
                        var startInfo = new ProcessStartInfo
                                            {
                                                CreateNoWindow = false,
                                                UseShellExecute = false,
                                                FileName = SvnLocation + @"\svn.exe",
                                                WindowStyle = ProcessWindowStyle.Hidden,
                                                Arguments = @"delete --force " + FileRootPath + deleteFile
                                            };
                        using (var exeProcess = Process.Start(startInfo))
                        {
                            exeProcess.WaitForExit();
                        }
                    }
                    if (deleteFileRow.Contains("Right Orphan"))
                    {
                        start = true;
                    }
                }
                return true;
            }
            else
            {
                LogFormat("Error", "No tables in HTML");
                return false;
            }
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
