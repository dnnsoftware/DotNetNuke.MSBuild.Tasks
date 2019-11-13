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

    public class UpdateNode : Task
    {
        public string FileName { get; set; }
        public string NewFileName { get; set; }
        public string Root { get; set; }
        public string Descendant { get; set; }
        public string Attribute { get; set; }
        public string AttributeValue { get; set; }
        public string ReplacementAttributeValue { get; set; }

        public override bool Execute()
        {
            XmlDocument projectFile;
            if (FileName == string.Empty)
            {
                return false;
            }

            if (Descendant == string.Empty)
            {
                return false;
            }

            try
            {
                XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003"; 
                var projectFileInfo = new FileInfo(FileName) { IsReadOnly = false };
                projectFile = new XmlDocument();
                projectFile.Load(FileName);
                StringReader sr = new StringReader(projectFile.OuterXml);
                XDocument xRoot = XDocument.Load(sr);

                xRoot.Element(msbuild + Root)
                        .Elements(msbuild + Descendant)
                        .Where(x => x.Attribute(Attribute).Value == AttributeValue)
                        .Single()
                        .SetAttributeValue(Attribute, ReplacementAttributeValue);
                projectFile.LoadXml(xRoot.ToString());
                projectFile.Save(NewFileName);
                return true;
            }
            catch (Exception ex)
            {
                var a = ex.Message;
                return false;
            }
        }
    }
}
