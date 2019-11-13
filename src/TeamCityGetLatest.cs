using System;
using System.Net;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetNuke.MSBuild.Tasks
{
    public class TeamCityGetLatest : Task
    {
        [Required]
        public String BuildId
        { get; set; }

        [Required]
        public String OuputPath
        { get; set; }

        [Required]
        public String BeforeVersion
        { get; set; }

        [Required]
        public String AfterVersion
        { get; set; }

        [Output]
        public String OutputVersion { get; set; }

        public override bool Execute()
        {
            OutputVersion = DownloadLatestTeamCity(BuildId, OuputPath, BeforeVersion, AfterVersion);

            return true;
        }

        public static string DownloadLatestTeamCity(string buildId, string ouputPath, string beforeVersion, string afterVersion)
        {
            var user = ""; //TODO: Fill the username
            var pass = ""; //TODO: Fill the password
            var versionUrl = "http://dev-build1:8088/httpAuth/app/rest/buildTypes/id:" + buildId + "/builds/status:SUCCESS/number";

            string version = GetVersion(versionUrl, user, pass);

            var fileUrl = "http://dev-build1:8088/httpAuth/repository/download/" + buildId + "/.lastSuccessful/" + beforeVersion + version + afterVersion;

            // Function will return the number of bytes processed
            // to the caller. Initialize to 0 here.
            int bytesProcessed = 0;

            // Assign values to these objects here so that they can
            // be referenced in the finally block
            Stream remoteStream = null;
            Stream localStream = null;
            WebResponse response = null;

            // Use a try/catch/finally block as both the WebRequest and Stream
            // classes throw exceptions upon error
            try
            {
                // Create a request for the specified remote file name
                WebRequest request = WebRequest.Create(fileUrl);
                if (request != null)
                {
                    request.Credentials = new NetworkCredential(user, pass);

                    // Send the request to the server and retrieve the
                    // WebResponse object 
                    response = request.GetResponse();
                    if (response != null)
                    {
                        // Once the WebResponse object has been retrieved,
                        // get the stream object associated with the response's data
                        remoteStream = response.GetResponseStream();

                        // Create the local file
                        localStream = File.Create(ouputPath + beforeVersion + version + afterVersion);

                        // Allocate a 1k buffer
                        var buffer = new byte[1024];
                        int bytesRead;

                        // Simple do/while loop to read from stream until
                        // no bytes are returned
                        do
                        {
                            // Read data (up to 1k) from the stream
                            bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                            // Write the data to the local file
                            localStream.Write(buffer, 0, bytesRead);

                            // Increment total bytes processed
                            bytesProcessed += bytesRead;
                        } while (bytesRead > 0);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // Close the response and streams objects here 
                // to make sure they're closed even if an exception
                // is thrown at some point
                if (response != null) response.Close();
                if (remoteStream != null) remoteStream.Close();
                if (localStream != null) localStream.Close();
            }

            // Return total bytes processed to caller.
            return version;
        }

        public static string GetVersion(String remoteFilename, String httpUsername, String httpPassword)
        {
            // used to build entire input
            var sb = new StringBuilder();

            // used on each read operation
            var buf = new byte[8192];

            // prepare the web page we will be asking for
            var request = (HttpWebRequest)
                WebRequest.Create(remoteFilename);

            // If a username or password have been given, use them
            if (httpUsername.Length > 0 || httpPassword.Length > 0)
            {
                string username = httpUsername;
                string password = httpPassword;
                request.Credentials = new NetworkCredential(username, password);
            }

            // execute the request
            var response = (HttpWebResponse)
                request.GetResponse();

            // we will read data via the response stream
            var resStream = response.GetResponseStream();

            if (resStream != null)
            {
                int count;
                do
                {
                    // fill the buffer with data
                    count = resStream.Read(buf, 0, buf.Length);

                    // make sure we read some data
                    if (count != 0)
                    {
                        // translate from bytes to ASCII text
                        var tempString = Encoding.ASCII.GetString(buf, 0, count);

                        // continue building the string
                        sb.Append(tempString);
                    }
                } while (count > 0); // any more data to read?
            }

            return sb.ToString();
        }
    }
}