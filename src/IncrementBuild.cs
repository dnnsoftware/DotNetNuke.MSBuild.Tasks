using System;
using System.Diagnostics;
using System.IO;
using log4net;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetNuke.MSBuild.Tasks
{
    public class IncrementBuild : Task
    {
        private static readonly ILog log = LogManager.GetLogger("IncrementBuild");
        private const string VbVersionStart = "<Assembly: AssemblyVersion";
        private const string VbVersionEnd = ")>";
        private const string CsVersionStart = "[assembly: AssemblyVersion";
        private const string CsVersionEnd = ")]";
        private string _versionStart = CsVersionStart;
        private string _versionEnd = CsVersionEnd;
        public string[] AssemblyFiles { get; set; }

        public bool AutoIncrementVersion { get; set; }
        [Output]
        public string DefaultVersion { get; set; }

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
                foreach(var assemblyFile in AssemblyFiles)
                {
                    LogFormat("Message", assemblyFile);
                    if (assemblyFile == null)
                    {
                        return false;
                    }

                    if (assemblyFile.EndsWith("cs"))
                    {
                        _versionStart = CsVersionStart;
                        _versionEnd = CsVersionEnd;
                    }
                    else
                    {
                        _versionStart = VbVersionStart;
                        _versionEnd = VbVersionEnd;
                    }

                    var content = File.ReadAllText(assemblyFile);
                    var indexStart = content.IndexOf(_versionStart) + 28;
                    var indexEnd = content.IndexOf(_versionEnd, indexStart);
                    var originalVersionNumber = content.Substring(indexStart, indexEnd - indexStart - 1);
                    var versionNumber = originalVersionNumber;
                    if (DefaultVersion != default(string))
                    {
                        versionNumber = DefaultVersion;
                    }
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
                    if (AutoIncrementVersion)
                    {
                        revision = revision + 1;
                    }

                    BuildVersion = String.Format("{0}.{1}.{2}.{3}", main, minor, version, revision);
                    FormattedBuildVersion = String.Format("{0}.{1}.{2}", main.ToString("00"), minor.ToString("00"), version.ToString("00"));
                    Revision = revision.ToString();
                    if (AutoIncrementVersion || DefaultVersion != default(string))
                    {
                        var projectFileInfo = new FileInfo(assemblyFile) { IsReadOnly = false };
                        content = content.Replace(originalVersionNumber, BuildVersion);
                        var newProjectFile = new StreamWriter(assemblyFile);
                        newProjectFile.Write(content);
                        newProjectFile.Close();
                        var projectFileInfo2 = new FileInfo(assemblyFile) { IsReadOnly = true };
                    }

                }

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
            else
            {
                Debug.Print(message, args);
            }
        }
    }
}
