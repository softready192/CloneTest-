using System;
using System.Windows;
using DA.Common;
using System.Collections.Generic;
using DA.DAIE.Common;
using Syncfusion.Windows.Shared;

namespace DA.DAIE.CaseSelection
{
    /// <summary>
    /// Interaction logic for CaseSelectionWindow.xaml
    /// </summary>
    public partial class CaseSelectionWindow : Window
    {
        /// <summary>
        /// Tells how long ago user pressed an button.
        /// Used to prevent another window from opening until 2 seconds is up.
        /// Avoids double windows from opening due to Microsoft Bug.
        /// </summary>
        private DateTime _lastPressed = DateTime.Now;
        private int LastPressedSeconds
        {
            get
            {
                int seconds = DateTime.Now.Subtract(_lastPressed).Seconds;
                _lastPressed = DateTime.Now;
                return seconds;
            }
        }

        public CaseSelectionWindow()
        {
            InitializeComponent();
            SkinStorage.SetVisualStyle(grid1, App.VisualStyle);
        }

        private string getDisplayUserName()
        {
            string userName = App.CurrentPrincipal.Identity.Name;
            int positionSlash = userName.IndexOf("/");
            if (positionSlash > 0)
            {
                return userName.Substring(positionSlash, userName.Length - positionSlash);
            }
            else
            {
                return userName;
            }
        }

        private string getDisplayUserRole()
        {
            List<string> appRoles = AppConfig.getAppConfigList("Roles");
            foreach (string role in appRoles)
            {
                if (App.CurrentPrincipal.IsInRole(role))
                {
                    return role;
                }
            }
            return "NoRoleFound?";
        }

        private string getDisplayTitle()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly.GetName().Name + " ver. " + assembly.GetName().Version.ToString(); 
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            this.Title = getDisplayTitle();
            this.StatusTextDataBase.Text = Connections.CustomMapper.getDisplayConnection();
            this.StatusTextRole.Text = "Role: " + getDisplayUserRole();
            this.StatusTextUser.Text = "User: " + getDisplayUserName();

            bool enabledModuleExists = false;


            if (this.CaseSelectionRA.getHasEnabledModule())
            {
                this.CaseSelectionRA.retrieveCaseList(false);
                this.CaseSelectionRA.ParentWindow = this;
                this.tabControl1.SelectedIndex = 1;
                enabledModuleExists = true;
            }
            else
            {
                this.CaseSelectionRA.Visibility = Visibility.Collapsed;
                this.TabItemRA.Visibility = Visibility.Collapsed;
            }

            if (this.CaseSelectionDA.getHasEnabledModule())
            {
                this.CaseSelectionDA.retrieveCaseList(false);
                this.CaseSelectionDA.ParentWindow = this;
                this.tabControl1.SelectedIndex = 0;
                enabledModuleExists = true;
            }
            else
            {
                this.CaseSelectionDA.Visibility = Visibility.Collapsed;
                this.TabItemDA.Visibility = Visibility.Collapsed;
            }

            if (this.CaseSelectionDALR.getHasEnabledModule())
            {
                this.CaseSelectionDALR.retrieveCaseList(false);
                this.CaseSelectionDALR.ParentWindow = this;
                //this.tabControl1.SelectedIndex = 0;
                enabledModuleExists = true;
            }
            else
            {
                this.CaseSelectionDALR.Visibility = Visibility.Collapsed;
                this.TabItemDALR.Visibility = Visibility.Collapsed;
            }

            if (this.CaseSelectionAdmin.getHasEnabledModule())
            {
                //this.CaseSelectionAdmin.hideCaseSelection();
                this.CaseSelectionAdmin.retrieveCaseList(false);
                this.CaseSelectionAdmin.ParentWindow = this;
                this.tabControl1.SelectedIndex = 3;
                enabledModuleExists = true;
            }
            else
            {
                this.CaseSelectionAdmin.Visibility = Visibility.Collapsed;
                this.TabItemAdmin.Visibility = Visibility.Collapsed;
            }

            if (!enabledModuleExists)
            {
                HandledException he = new HandledException("User Did Not Have Permissions For Any Modules", App.LOGGER_USER_ERROR);
                he.showError();
                this.tabControl1.Visibility = Visibility.Collapsed;
            }

        }

        

        

    }
}
