using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.AccessControl;
using System.Security;
using System.Security.Principal;

namespace DotNetNuke.MSBuild.Tasks
{
    class DirectoryManager
    {
        public delegate void FileCopied(string Source, string Destination);
        public static event FileCopied OnFileCopied;

        public static void DeleteFolder(string Path)
        {
            System.IO.Directory.Delete(Path, true);
        }


        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it’s new directory.
            foreach (var fi in source.GetFiles())
            {
                //System.Diagnostics.Trace.WriteLine(string.Format(@"Copying {0}\{1}", target.FullName, fi.Name));
                var filePath = Path.Combine(target.ToString(), fi.Name);

                if(File.Exists(filePath))
                    File.Delete(filePath);
                try
                {
                    fi.CopyTo(filePath, true);
                }
                catch (Exception)
                {
                }
                if (OnFileCopied != null) OnFileCopied(fi.FullName, Path.Combine(target.ToString(), fi.Name));
            }

            // Copy each subdirectory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static void SetFolderPermissions(FileInfo Target, string ACLUser)
        {
            FileSecurity fileSec = Target.GetAccessControl();

            FileSystemAccessRule fsRule = new FileSystemAccessRule(ACLUser, FileSystemRights.FullControl, AccessControlType.Allow);
            fileSec.AddAccessRule(fsRule);

            bool modified = false;
            fileSec.ModifyAccessRule(AccessControlModification.Add, fsRule, out modified);

            Target.SetAccessControl(fileSec);

        }

        public static void SetFolderPermissions(string ACLUser, DirectoryInfo Target)
        {

            //RemoveInheritablePermissons(Target);
            DirectorySecurity dirSec = new DirectorySecurity(Target.FullName, AccessControlSections.All);
            
            FileSystemAccessRule fsRuleCurrentFolder = new FileSystemAccessRule(ACLUser, FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.ChangePermissions | FileSystemRights.Delete, AccessControlType.Allow);
            dirSec.SetAccessRule(fsRuleCurrentFolder);

            FileSystemAccessRule fsRuleSubFoldersAndFiles = new FileSystemAccessRule(ACLUser, FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.ChangePermissions | FileSystemRights.Delete, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AccessControlType.Allow);
            dirSec.AddAccessRule(fsRuleSubFoldersAndFiles);

            Target.SetAccessControl(dirSec);

            
        }
        // Removes an ACL entry on the specified directory for the specified account. 
        public static void RemoveInheritablePermissons(DirectoryInfo Directory)
        {
            // Create a new DirectoryInfo object.             
            // Get a DirectorySecurity object that represents the  
            // current security settings. 
            DirectorySecurity dSecurity = Directory.GetAccessControl();
            // Add the FileSystemAccessRule to the security settings. 
            const bool IsProtected = true;
            const bool PreserveInheritance = false;
            dSecurity.SetAccessRuleProtection(IsProtected, PreserveInheritance);
            // Set the new access settings. 
            Directory.SetAccessControl(dSecurity);
        } 

    }
}