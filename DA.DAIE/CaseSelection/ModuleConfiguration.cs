using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Threading;
using DA.DAIE;
using log4net;

namespace DA.DAIE.CaseSelection
{

    public enum PERMISSIONS : int { Modify, Read, Print, DataValidate }  ;


    public class ModuleConfiguration
    {
        public const string CONFIG_FILE = "DA.DAIE.CaseSelection.ModuleConfiguration.xml";
        public const string CONFIG_FILE_TEST = "DA.DAIE.CaseSelection.ModuleConfigurationTest.xml";
        private static bool isDevelopment = false;

        ///// <summary>
        ///// Tells if the user has the passed permission for specified mode.
        ///// 
        ///// Reads the associated configuration file.
        ///// Roles are mapped to permissions for each mode in the config file.
        ///// 
        ///// modes: 
        /////    Filters cases, and modules allowed to operate on the selected cases.
        /////    Allowed values are found in enumerated type MODES
        ///// 
        ///// permissions:
        /////    Tells you if user can Read, Modify, Print and ValidateData within a Mode.
        /////    Allowed values are found in enumerated type PERMISSIONS.
        ///// </summary>
        ///// <param name="argMode"></param>
        ///// <param name="argPermission"></param>
        ///// <returns></returns>
        //public static bool hasPermission(MODES argMode, PERMISSIONS argPermission, string argConfigFileName )
        //{
        //    ILog log = LogManager.GetLogger("Application.Module.Permission");
        //    log.Debug("calling hasPermission with Mode: " + argMode.ToString() + " permission: " + argPermission.ToString());

        //    IList<string> roleList = getModePermissionRoleNames(argMode, argPermission, argConfigFileName );
        //    foreach (string roleName in roleList)
        //    {
        //        if (App.CurrentPrincipal.IsInRole(roleName))
        //        {
        //            return true;
        //        }
                
        //    }
        //    return false;
        //}

        public static IList<ModuleMetaData> getModeModules(MODES argMode, string argConfigFileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(argConfigFileName);
            IList<ModuleMetaData> moduleList = new List<ModuleMetaData>();

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            XmlNodeList modeModuleNodeList = doc.SelectNodes("/moduleConfiguration/appMode[@name='" + argMode.ToString() + "']/button");
            foreach (XmlNode moduleNode in modeModuleNodeList)
            {
                moduleList.Add(getModuleMetaData(moduleNode));
            }
            return moduleList;

        }


        private static ModuleMetaData getModuleMetaData(XmlNode xnode)
        {
            string moduleName = xnode.Attributes.GetNamedItem("name").InnerText;
            string columnString = xnode["column"].InnerText;
            string rowString = xnode["row"].InnerText;
            string label = xnode["label"].InnerText;
            string comment = xnode["comment"].InnerText;
         
            bool allowApproved = false;
            if( xnode["allowApproved"] != null && xnode["allowApproved"].InnerText != null )
            {
                allowApproved = xnode["allowApproved"].InnerText.ToLower().Equals("true");
            }

            bool enabled = false;

            XmlNodeList nl = xnode.ChildNodes;
            
            foreach (XmlNode roleNode in nl)
            {
                if( roleNode.Name.Equals("role"))
                {
                    if ( isDevelopment || App.CurrentPrincipal.IsInRole(roleNode.InnerText))
                    {
                        enabled = true;
                    }
                }
            }

            return new ModuleMetaData(moduleName, int.Parse(rowString), int.Parse(columnString), enabled, label, comment, allowApproved);



        }

        private static List<string> getModePermissionRoleNames(MODES argMode, PERMISSIONS argPermission, string argConfigFileName )
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Stream stream = assembly.GetManifestResourceStream(argConfigFileName);
            
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            XmlNodeList xnodes = doc.SelectNodes("/moduleConfiguration/appMode[@name='" + argMode.ToString() + "']/roles/role[@permission='" + argPermission.ToString() + "']");

            List<string> roleList = new List<string>();
            foreach (XmlNode xnode in xnodes)
            {
                string str = xnode.InnerText;
                roleList.Add(str);
            }
            
            return roleList;
        }

    }

}
