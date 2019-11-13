using System;
using System.Diagnostics;
using System.IO;
using log4net;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetNuke.MSBuild.Tasks
{
    public class GetVersion : Task
    {
        private const string CsVersionStart = "[assembly: AssemblyVersion";
        private const string CsVersionEnd = ")]";

        public string SolutionFile { get; set; }

        [Output]
        public string BuildVersion { get; set; }

        [Output]
        public string FormattedBuildVersion { get; set; }

        [Output]
        public string Revision { get; set; }

        [Output]
        public string ErrorCode { get; set; }

        public override bool Execute()
        {
            LogFormat("Message", "Start");

            try
            {
                    LogFormat("Message", SolutionFile);
                    if (SolutionFile == null)
                    {
                        return false;
                    }

                    var content = File.ReadAllText(SolutionFile);
                    var indexStart = content.IndexOf(CsVersionStart) + 28;
                    var indexEnd = content.IndexOf(CsVersionEnd, indexStart);
                    var originalVersionNumber = content.Substring(indexStart, indexEnd - indexStart - 1);
                    var versionNumber = originalVersionNumber;

                    var values = versionNumber.Split(".".ToCharArray());
                    ErrorCode = versionNumber;
                    int main;
                    int minor;
                    int version;
                    int revision;
                    int.TryParse(values[0], out main);
                    int.TryParse(values[1], out minor);
                    int.TryParse(values[2], out version);
                    int.TryParse(values[3], out revision);

                    BuildVersion = String.Format("{0}.{1}.{2}.{3}", main, minor, version, revision);
                    FormattedBuildVersion = String.Format("{0}.{1}.{2}", main.ToString("00"), minor.ToString("00"), version.ToString("00"));
                    Revision = revision.ToString();

                return true;

            }
            catch (Exception ex)
            {
                LogFormat("Error", ex.StackTrace);
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
        }
    }
}
