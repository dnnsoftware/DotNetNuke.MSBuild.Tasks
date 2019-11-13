using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using System.Xml;

namespace DotNetNuke.MSBuild.Tasks
{
    using System.IO;
    using System.Xml.Linq;

    public class ProcessTemplates : Task
    {
        public string FileName { get; set; }
        public string OutputCe { get; set; }
        public string OutputPe { get; set; }
        public string OutputEe { get; set; }

        public override bool Execute()
        {
            if (FileName == null || OutputCe == null || OutputPe == null || OutputEe == null)
            {
                return false;
            }

            var projectFileBase = new XmlDocument();

            XDocument xRootCe;
            XDocument xRootPe;
            XDocument xRootEe;

            projectFileBase.Load(FileName);

            xRootCe = XDocument.Load(new StringReader(projectFileBase.OuterXml));
            xRootPe = XDocument.Load(new StringReader(projectFileBase.OuterXml));
            xRootEe = XDocument.Load(new StringReader(projectFileBase.OuterXml));

            //Remove all Settings which dont have the correct SKU
            xRootCe.Element("portal")
                .Elements("settings")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("CE")))
                .Remove();
            xRootPe.Element("portal")
                .Elements("settings")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("PE")))
                .Remove();
            xRootEe.Element("portal")
                .Elements("settings")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("EE")))
                .Remove();

            //Remove all tabs which dont have the correct SKU
            xRootCe.Element("portal")
                .Element("tabs")
                .Elements("tab")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("CE")))
                .Remove();
            xRootPe.Element("portal")
                .Element("tabs")
                .Elements("tab")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("PE")))
                .Remove();
            xRootEe.Element("portal")
                .Element("tabs")
                .Elements("tab")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("EE")))
                .Remove();

            //Remove all Pages which dont have the correct SKU
            xRootCe.Element("portal")
                .Element("portalDesktopModules")
                .Elements("portalDesktopModule")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("CE")))
                .Remove();
            xRootPe.Element("portal")
                .Element("portalDesktopModules")
                .Elements("portalDesktopModule")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("PE")))
                .Remove();
            xRootEe.Element("portal")
                .Element("portalDesktopModules")
                .Elements("portalDesktopModule")
                .Where(y => y.HasAttributes)
                .Where(x => !(x.Attribute("sku").Value.Contains("EE")))
                .Remove();

            var projectFileCE = new XmlDocument();
            var projectFilePE = new XmlDocument();
            var projectFileEE = new XmlDocument();

            projectFileCE.LoadXml(xRootCe.ToString());
            projectFilePE.LoadXml(xRootPe.ToString());
            projectFileEE.LoadXml(xRootEe.ToString());

            using (TextWriter sw = new StreamWriter(OutputCe, false, Encoding.UTF8))
                projectFileCE.Save(sw);

            using (TextWriter sw = new StreamWriter(OutputPe, false, Encoding.UTF8))
                projectFilePE.Save(sw);

            using (TextWriter sw = new StreamWriter(OutputEe, false, Encoding.UTF8))
                projectFileEE.Save(sw);
            return true;
        }

        public static void SetFileReadAccess(string fileName, bool setReadOnly)
        {
            var fInfo = new FileInfo(fileName) { IsReadOnly = setReadOnly };
        }

    }
}
