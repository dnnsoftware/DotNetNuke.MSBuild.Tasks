using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using DotNetNuke.Integrity.DatabaseBuilder;
using System.IO;

namespace DotNetNuke.MSBuild.Tasks
{
    public class BuildSignatureDatabase : Task
    {
        private string _outputFile;
        private string _inputDirectory;
        private string _certificateFile;
        private string _certificatePassword;
        private string _excludePatterns;
        private string _includePatterns;
        private bool _embedCertificate;

        [Required]
        public string OutputFile
        {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        [Required]
        public string InputDirectory
        {
            get { return _inputDirectory; }
            set { _inputDirectory = value; }
        }

        [Required]
        public string CertificateFile
        {
            get { return _certificateFile; }
            set { _certificateFile = value; }
        }

        [Required]
        public string CertificatePassword
        {
            get { return _certificatePassword; }
            set { _certificatePassword = value; }
        }

        public string ExcludePatterns
        {
            get { return _excludePatterns; }
            set { _excludePatterns = value; }
        }

        public string IncludePatterns
        {
            get { return _includePatterns; }
            set { _includePatterns = value; }
        }

        public bool EmbedCertificate
        {
            get { return _embedCertificate; }
            set { _embedCertificate = value; }
        }

        public override bool Execute()
        {
            // Load the certificate
            X509Certificate2 certificate = new X509Certificate2(CertificateFile, CertificatePassword);
            if (!certificate.HasPrivateKey)
            {
                Log.LogError("Input certificate must have a private key");
                return false;
            }

            // Build regex filters
            List<FileFilter> excludeFilters = BuildFileFiltersList(ExcludePatterns);
            List<FileFilter> includeFilters = BuildFileFiltersList(IncludePatterns);

            // Build reporter
            ProgressReporter reporter = new MsBuildLogReporter(Log);

            // Build the database
            DatabaseBuildResult result = DatabaseBuilder.BuildDatabase(OutputFile,
                                                                                       EmbedCertificate,
                                                                                       InputDirectory,
                                                                                       excludeFilters,
                                                                                       includeFilters,
                                                                                       certificate,
                                                                                       reporter);
            return result.Succeeded;

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

        private static List<FileFilter> BuildFileFiltersList(string inputStrings)
        {
            List<FileFilter> excludeFilters = new List<FileFilter>();
            if (inputStrings != null)
            {
                foreach (string exclude in inputStrings.Split(';'))
                {
                    excludeFilters.Add(new RegexFileFilter(new Regex(exclude)));
                }
            }
            return excludeFilters;
        }

        private class MsBuildLogReporter : ProgressReporter
        {
            private TaskLoggingHelper _log;

            public MsBuildLogReporter(TaskLoggingHelper log)
            {
                _log = log;
            }

            public override void NotifyEnterDirectory(DirectoryInfo directory)
            {
                base.NotifyEnterDirectory(directory);
                _log.LogMessage(MessageImportance.Low, BuildDefaultEnterDirectoryMessage(directory));
            }

            public override void NotifyError(DatabaseBuildError error, FileInfo file)
            {
                base.NotifyError(error, file);
                if (error.Warning)
                {
                    _log.LogWarning(BuildDefaultErrorMessage(error, file));
                }
                else
                {
                    _log.LogError(BuildDefaultErrorMessage(error, file));
                }
            }

            public override void NotifyExitDirectory(DirectoryInfo directory)
            {
                base.NotifyExitDirectory(directory);
                _log.LogMessage(MessageImportance.Low, BuildDefaultExitDirectoryMessage(directory));
            }

            public override void NotifyFileExcluded(FileInfo file)
            {
                base.NotifyFileExcluded(file);
                _log.LogMessage(MessageImportance.Low, BuildDefaultFileExcludedMessage(file));
            }

            public override void NotifyFinishedBuilding(DatabaseBuildResult result)
            {
                base.NotifyFinishedBuilding(result);
                _log.LogMessage(MessageImportance.Normal, BuildDefaultFinishedBuildingMessage(result));
            }

            public override void NotifyStartBuilding(DirectoryInfo rootDirectory)
            {
                base.NotifyStartBuilding(rootDirectory);
                _log.LogMessage(MessageImportance.Normal, BuildDefaultStartBuildingMessage(rootDirectory));
            }

            public override void NotifyProcessingFile(FileInfo file)
            {
                base.NotifyProcessingFile(file);
                _log.LogMessage(MessageImportance.Normal, BuildDefaultProcessingFileMessage(file));
            }
        }
    }
}
