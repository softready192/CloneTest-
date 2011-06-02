using System;
using System.Collections.Generic;
using System.Windows;
using log4net;
using System.Threading;
using System.Security.Principal;
using DA.DAIE.Connections;
using DA.DAIE.Common;

namespace DA.DAIE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string LOGGER_USER_ACCESS = "UseLog";
        public const string LOGGER_USER_ERROR = "Error";

        private const string MODULE_NAME = "Startup";
        private const string MODULE_LOGGER = LOGGER_USER_ACCESS + "." + MODULE_NAME;
        private const string MODULE_LOGGER_ERROR = LOGGER_USER_ERROR + "." + MODULE_NAME;

        public static IPrincipal CurrentPrincipal;
        public static string VisualStyle = AppConfig.getAppConfigEntry("VisualStyle");
        private static string workingFolder = null;

        public static string WorkingFolder
        {
            get
            {
                if( App.workingFolder == null )
                {
                    App.workingFolder = "WORKING_FOLDER\\" + CurrentPrincipal.Identity.Name + "\\";
                    if( !System.IO.Directory.Exists( "WORKING_FOLDER\\" ))
                    {
                        System.IO.Directory.CreateDirectory("WORKING_FOLDER\\");
                    }
                    if( !System.IO.Directory.Exists( App.workingFolder ))
                    {
                        System.IO.Directory.CreateDirectory(App.workingFolder);
                    }
                }
                return App.workingFolder;
            }
        }

        private string getVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly.GetName().Name + " ver. " + assembly.GetName().Version.ToString();
        }

        /// <summary>
        /// Forces ISQLMapper to Be Loaded and Cached on startup.
        /// No more lazy loading.
        ///    If someone changes SQL behind the scenes, 
        ///    You don't want to have some SQL loaded at startup and some later.
        ///    If you want to get the new SQL, 
        ///       You'd better restart the application!!!
        /// </summary>
        /// <param name="argFileName"></param>
        protected void forceCacheMapper(string argFileName)
        {
            CustomMapper customMapper = new CustomMapper();
            customMapper.getMapper(argFileName);
        }


        protected void Application_Startup(object sender, StartupEventArgs e)
        {
            bool isAuthorized = false;
            try
            {
                List<string> appRoles = AppConfig.getAppConfigList("Roles");
                bool allowMultipleRoles = false;
                isAuthorized = ADAuthAccessRoles.LoginModule.loginUser("Day Ahead Import Export", appRoles, allowMultipleRoles);
                //CurrentPrincipal = new GenericPrincipal(new GenericIdentity("DEBUG"), new string[]{ "Imp_Exp_User_ITAdmin" });
                CurrentPrincipal = Thread.CurrentPrincipal;
                AppDomain.CurrentDomain.SetThreadPrincipal(System.Threading.Thread.CurrentPrincipal);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Details\n\r  " + ex.Message, "Login Error");
                this.Shutdown();
            }

            try
            {
                System.IO.FileInfo configFile = new System.IO.FileInfo("log4net.config");
                log4net.Config.XmlConfigurator.Configure(configFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Details\n\r " + ex.Message, "Error Starting Logger");
                this.Shutdown();
            }

            if (!isAuthorized)
            {
                LogManager.GetLogger(MODULE_LOGGER_ERROR).Warn("User Login Failed: " 
                    + (CurrentPrincipal != null && CurrentPrincipal.Identity != null ? CurrentPrincipal.Identity.Name : "CurrentPrincipal is Unknown") );
                this.Shutdown();
            }
            else
            {
                try
                {
                    DBSqlMapper.InitMapper();
                    forceCacheMapper("SqlMapEES.config");
                    forceCacheMapper("SqlMapFCST.config");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Details\n\r " + ex.Message, "Error Caching SQL");
                    this.Shutdown();
                }

                LogManager.GetLogger(MODULE_LOGGER).Info(getVersion());
                LogManager.GetLogger(MODULE_LOGGER).Info("User Login: " + CurrentPrincipal.Identity.Name );
            }

            try
            {
                LogManager.GetLogger(MODULE_LOGGER).Info("User Work Directory: " + App.WorkingFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Details\n\r " + ex.Message, "Error Creating User Work Directory");
                this.Shutdown();
            }

        }

    }
}
