using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Xml;

namespace DotNetNuke.MSBuild.Tasks
{
    using System.Xml.XPath;
    using System.IO;
    using System.Xml.Linq;

    public class ProcessProjectFile : Task
    {
        public string[] FileNames { get; set; }

        public override bool Execute()
        {
            if (FileNames == null)
            {
                return false;
            }
            XmlDocument projectFile;
            foreach (var fileName in FileNames)
            {
                try
                {
                    SetFileReadAccess(fileName, false);

                    XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
                    projectFile = new XmlDocument();
                    projectFile.Load(fileName);
                    StringReader sr = new StringReader(projectFile.OuterXml);
                    XDocument xRoot = XDocument.Load(sr);

                    xRoot.Element(msbuild + "Project")
                        .Elements(msbuild + "PropertyGroup")
                        .Elements()
                        .Where(e => e.Name.LocalName.StartsWith("Scc")).Remove();

                    xRoot.Element(msbuild + "Project")
                        .Elements(msbuild + "PropertyGroup")
                        .Elements()
                        .Where(e => e.Name.LocalName.StartsWith("LangVersion")).Remove();

                    //xRoot.Element(msbuild + "Project")
                    //    .Elements(msbuild + "Import")
                    //    .Where(x => x.Attribute("Project").Value.Contains("BuildScripts"))
                    //    .Remove();

                    //try
                    //{
                    //    xRoot.Element(msbuild + "Project")
                    //        .Elements(msbuild + "Target")
                    //        .Where(x => x.Attribute("Name").Value == "AfterBuild")
                    //        .Single()
                    //        .SetAttributeValue("DependsOnTargets", "DebugProject");
                    //}
                    //catch (Exception)
                    //{

                    //    //do nothing element not used in project file.
                    //}

                    projectFile.LoadXml(xRoot.ToString());
                    projectFile.Save(fileName);
                }
                catch (Exception ex)
                {
                    var file = new System.IO.StreamWriter("F:\\Builds\\log.txt");
                    file.WriteLine(ex.Message);
                    file.Close();
                    return false;
                }
            }
            return true;
        }

        public static void SetFileReadAccess(string fileName, bool setReadOnly)
        {
            var fInfo = new FileInfo(fileName) { IsReadOnly = setReadOnly };
        }

    }
}
