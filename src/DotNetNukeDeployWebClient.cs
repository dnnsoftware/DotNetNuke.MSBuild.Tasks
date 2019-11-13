using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DotNetNuke.MSBuild.Tasks
{
    class DotNetNukeDeployWebClient : WebClient 
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request.GetType() == typeof(HttpWebRequest))
            {
                ((HttpWebRequest)request).UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/13.0.782.220 Safari/535.1";
            }
            return request;
        }
    }
}
