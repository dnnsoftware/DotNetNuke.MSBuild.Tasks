using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace DotNetNuke.MSBuild.Tasks
{
    /// <summary>
    /// Summary description for IISManager.
    /// </summary>
    public class IISManager
    {
        private const string VirDirSchemaName = "IIsWebVirtualDir";
        private const string ServerName = "localhost";

        private DirectoryEntry _iisServer;

        public IISManager()
        {
            Connect();
        }

        void Connect()
        {
            try
            {
                _iisServer = new DirectoryEntry("IIS://" + ServerName + "/W3SVC/1");
            }
            catch (Exception e)
            {
                throw new Exception("Could not connect to: " + ServerName, e);
            }
        }

        public void DeleteVDir(string name)
        {
            DirectoryEntry e = _iisServer.Children.Find(name, "VirDirSchemaName");
            if (e != null) DeleteVDir(e);
        }

        public void DeleteVDir(DirectoryEntry virtualDirectoryEntry)
        {
            virtualDirectoryEntry.DeleteTree();
        }


        /// <summary>
        ///		Create a virtual directory
        /// </summary>
        /// <param name="nameDirectory">Name of the new virtual directory</param>
        /// <param name="realPath">Path of the directory</param>
        ///<param name="appPoolName">Name of the app pool</param>
        public void CreateVirtualDirectory(string nameDirectory, string realPath, string appPoolName)
        {
            var folderRoot = GetFolderRoot();
            try
            {
                var newVirDir = folderRoot.Children.Add(nameDirectory, VirDirSchemaName);
                // Set Properties
                newVirDir.Properties["AccessRead"][0] = true;
                newVirDir.Properties["AccessWrite"][0] = true;
                newVirDir.Properties["AccessExecute"][0] = true;
                newVirDir.Properties["Path"][0] = realPath;
                newVirDir.Properties["AppFriendlyName"][0] = realPath;
                newVirDir.Properties["AspEnableParentPaths"][0] = true;
                // Create a Application

                object[] param = { 0, EnsureAppPool(appPoolName), true };
                newVirDir.Invoke("AppCreate3", param);
                newVirDir.Properties["AppIsolated"][0] = "2";

                // Save Changes
                newVirDir.CommitChanges();
                folderRoot.CommitChanges();
                _iisServer.CommitChanges();
            }
            catch (Exception e)
            {
                throw new Exception("Error creating Virtual Directory " + nameDirectory, e);
            }
        }

        public string EnsureAppPool(string appPoolName)
        {
            if (!AppPools().Contains(appPoolName))
            {
                var root = new DirectoryEntry("IIS://" + ServerName + "/W3SVC/AppPools");
                var newpool = root.Children.Add(appPoolName, "IIsApplicationPool");
                newpool.Properties["managedPipelineMode"][0] = 0; //0 is integrated
                newpool.Properties["appPoolIdentityType"][0] = 2; //2 is using network service
                newpool.CommitChanges();
            }

            return appPoolName;
        }



        public bool VDirExists(string name)
        {
            var matchingEntries = GetFolderRoot().Children.Cast<DirectoryEntry>().Where(v => v.Name.ToLower() == name.ToLower());
            return matchingEntries.Count() != 0;
        }

        private DirectoryEntry GetFolderRoot()
        {
            return _iisServer.Children.Find("Root", VirDirSchemaName);
        }

        public bool IsCompatible()
        {
            try
            {
                GetFolderRoot();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        public List<DirectoryEntry> VDirs()
        {
            var list = new List<DirectoryEntry>();
            var srv = GetFolderRoot();
            if (srv != null)
            {
                if (srv.SchemaClassName.Equals("IIsWebVirtualDir")) list.Add(srv);
                foreach (DirectoryEntry e in srv.Children)
                {
                    if (e.SchemaClassName.Equals("IIsWebVirtualDir")) list.Add(e);
                }
            }
            return list;
        }

        /// <summary>
        /// Get a list of available Application Pools
        /// </summary>
        /// <returns></returns>
        public List<string> AppPools()
        {

            var list = new List<string>();
            var srv = new DirectoryEntry("IIS://" + ServerName + "/W3SVC/APPPOOLS");
            foreach (DirectoryEntry child in srv.Children)
            {
                list.Add(child.Name);
            }
            return list;
        }


        #region Delete Virtual Directory
        /*
* Usage : DeleteVirtualDirectory("localhost","MyWebApplication");
*
*/
        public bool DeleteVirtualDirectory(string sWebSite, string sAppName)
        {
            var iisSchema = new DirectoryEntry("IIS://" + sWebSite + "/Schema/AppIsolated");
            bool bCanCreate = iisSchema.Properties["Syntax"].Value.ToString().ToUpper() != "BOOLEAN";
            iisSchema.Dispose();

            if (bCanCreate)
            {
                try
                {
                    var iisAdmin = new DirectoryEntry("IIS://" + sWebSite + "/W3SVC/1/Root");

                    //If the virtual directory already exists then delete it
                    foreach (DirectoryEntry vd in iisAdmin.Children)
                    {
                        if (vd.Name == sAppName)
                        {
                            iisAdmin.Invoke("Delete", new[] { vd.SchemaClassName, sAppName });
                            iisAdmin.CommitChanges();
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
        #endregion
    }
}