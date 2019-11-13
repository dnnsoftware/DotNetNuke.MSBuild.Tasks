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

    public class CheckForOptimize : Task
    {
        [Required]
        public string[] FileNames { get; set; }

        [Output]
        public string Output { get; set; }

        public override bool Execute()
        {
            if (FileNames == null)
            {
                Output += "false";
                return false;
            }
            foreach (var fileName in FileNames)
            {
                try
                {
                    var docNav = new XPathDocument(fileName);
                    var nav = docNav.CreateNavigator();
                    var strExpression = "//*[contains(@Condition,'Release|AnyCPU')]";

                    var xmlNode = nav.SelectSingleNode(strExpression);

                    if (!xmlNode.OuterXml.Contains("Optimize>true<"))
                    {
                        Output += fileName + ":false ";
                    }
                    else
                    {
                        Output += fileName + ":true ";
                    }
                }
                catch (Exception ex)
                {
                    var file = new System.IO.StreamWriter("c:\\Builds\\log.txt");
                    file.WriteLine(ex.Message);
                    file.Close();
                    Output += fileName + ":false ";
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
