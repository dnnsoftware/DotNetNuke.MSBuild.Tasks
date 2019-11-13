using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;

namespace DotNetNuke.MSBuild.Tasks
{
    public class DeleteIISSite : Task
    {
        private readonly IISManager iisMgr = new IISManager();
        public string WebsiteName { get; set; }

        public override bool Execute()
        {
            if (iisMgr.VDirExists(WebsiteName))
            {
                iisMgr.DeleteVirtualDirectory("localhost", WebsiteName);
            }
            return true;
        }
    }
}
