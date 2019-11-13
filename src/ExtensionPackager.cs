using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using log4net;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetNuke.MSBuild.Tasks
{
    public class ExtensionPackager : Task
    {
        public string Manifest { get; set; }
        public string WorkingFolder { get; set; }
        [Output]
        public TaskItem[] PackageFiles { get; set; }

        public override bool Execute()
        {
            var packageFiles = new List<TaskItem>();
            var manifest = XDocument.Load(string.Concat(WorkingFolder, Manifest));
            var packages = from package in manifest.Descendants("package")
                           select new
                                      {
                                          Name = package.Attribute("name").Value,
                                          PackageType = package.Attribute("type").Value,
                                          Version = package.Attribute("version").Value,
                                          Components = package.Element("components")
                                      };
            foreach (var package in packages)
            {
                var components = from component in package.Components.Descendants("component")
                                 select new
                                            {
                                                ComponentType = component.Attribute("type").Value,
                                                BasePath = component.Descendants().Descendants("basePath"),
                                                Files = component.Descendants().Descendants(component.Attribute("type").Value.ToLower())
                                            };
                foreach (var component in components)
                {
                    var files = from file in component.Files
                                select new
                                           {
                                               Path = file.Element("path"),
                                               Name = file.Element("name").Value,
                                               SourceFileName = file.Element("sourceFileName")
                                           };
                    foreach (var file in files)
                    {
                        string filename;
                        if (file.SourceFileName != null)
                        {
                            filename = file.SourceFileName.Value;
                        }
                        else
                        {
                            filename = file.Path != null ? string.Format("{0}\\{1}", file.Path.Value, file.Name) : file.Name;
                        }
                        var item = new TaskItem(filename);
                        packageFiles.Add(item);
                    }
                }
            }

            PackageFiles = packageFiles.ToArray();
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

