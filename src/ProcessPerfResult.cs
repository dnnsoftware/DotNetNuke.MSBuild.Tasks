using System.IO;
using System.Text;
using Microsoft.Build.Utilities;
using System.Xml;

namespace DotNetNuke.MSBuild.Tasks
{
    public class ProcessPerfResults : Task
    {
        public string TrxPathIn { get; set; }
        public string HtmlPathOut { get; set; }

        public override bool Execute()
        {
            if (TrxPathIn == null || HtmlPathOut == null)
            {
                return false;
            }

            File.WriteAllText(TrxPathIn,
                              File.ReadAllText(TrxPathIn).Replace(
                                  "xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"", ""));

            var trxFile = new XmlDocument();
            trxFile.Load(TrxPathIn);

            var loadTestNode = trxFile.SelectSingleNode("/TestRun/Results/LoadTestResult");

            if (loadTestNode != null)
            {
                var resultsListNode = loadTestNode.SelectSingleNode("Summary/PerformanceCounterResults");

                var perfResult = resultsListNode.SelectNodes("PerformanceCounterResult");

                var htmlOut = new StringBuilder();

                if (File.Exists(HtmlPathOut))
                {
                    htmlOut.Append(File.ReadAllText(HtmlPathOut));
                }

                htmlOut.Append("<table>");
                htmlOut.AppendLine("<h2>");
                if (loadTestNode.Attributes != null)
                    htmlOut.AppendLine(loadTestNode.Attributes.GetNamedItem("testName").InnerText);
                htmlOut.AppendLine("</h2>");
                htmlOut.AppendLine("<tr><td>");
                htmlOut.AppendLine("<b>Machine Name</b>");
                htmlOut.AppendLine("</td><td>");
                htmlOut.AppendLine("<b>Counter Name</b>");
                htmlOut.AppendLine("</td><td>");
                htmlOut.AppendLine("<b>Value</b>");
                htmlOut.AppendLine("</td><td>");

                foreach (XmlNode result in perfResult)
                {
                    if (result.Attributes != null)
                    {
                        htmlOut.AppendLine("<tr><td>");
                        htmlOut.AppendLine(result.Attributes.GetNamedItem("machineName").InnerText);
                        htmlOut.AppendLine("</td><td>");
                        htmlOut.AppendLine(result.Attributes.GetNamedItem("counterName").InnerText);
                        htmlOut.AppendLine("</td><td>");
                        htmlOut.AppendLine(result.Attributes.GetNamedItem("instanceName").InnerText);
                        htmlOut.AppendLine("</td><td>");
                        htmlOut.AppendLine(result.Attributes.GetNamedItem("value").InnerText);
                        htmlOut.AppendLine("</td></tr>");
                    }
                }
                htmlOut.Append("</table>");

                using (var outfile = new StreamWriter(HtmlPathOut))
                {
                    outfile.Write(htmlOut.ToString());
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
