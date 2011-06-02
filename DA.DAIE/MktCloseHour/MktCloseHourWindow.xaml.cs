using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DA.DAIE.CaseSelection;
using System.Windows.Threading;
using DA.Common;
using log4net;
using Syncfusion.Windows.Shared;

namespace DA.DAIE.MktCloseHour
{
    /// <summary>
    /// Interaction logic for MktCloseHourWindow.xaml
    /// </summary>
    public partial class MktCloseHourWindow : Window
    {
        // Instance is kept for purpose of recalculating DB time.
        private MktCloseHourBO bo = new MktCloseHourBO();

        // newline characters 
        private static string CRLF = "\n\r";
        
        // Get Logger from Business Object.
        ILog log = MktCloseHourBO.log;

        public MktCloseHourWindow()
        {
            InitializeComponent();
            SkinStorage.SetVisualStyle(grid1, App.VisualStyle);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 'Refresh screen with close hours from DB for MDB and EES
            retrieveDBValues();

            // Start a timer to refresh the current database time.
            startRetrieveTimer();

        }

        /// <summary>
        /// 'Refresh screen with close hours from DB for MDB and EES
        /// 
        /// 'Get the Current Close hour from MDB and EES
        /// 'Initialize the new hour combo box
        /// 'Update New Hour text boxes
        /// </summary>
        private void retrieveDBValues()
        {
            string mktCloseHourMDB = MktCloseHourBO.getMktCloseHourMDB();
            string mktCloseHourEES = MktCloseHourBO.getMktCloseHourEES();

            // Set current hours to whats found in MDB and EES.
            this.labelCurrentCloseHourMDB.Content = mktCloseHourMDB;
            this.labelCurrentCloseHourEES.Content = mktCloseHourEES;

            // Default new settings to their originally retrieved values.
            this.labelNewCloseHourMDB.Content = mktCloseHourMDB;
            this.labelNewCloseHourEES.Content = mktCloseHourEES;

            // Populate hour combobox
            // and select hour retrieved for MDB.
            Binding binding = new Binding();
            IList<string> hourList = MktCloseHourBO.getCloseHourList();
            binding.Source = hourList;
            int selectedIndex = hourList.IndexOf(mktCloseHourMDB);
            this.comboBoxHours.SetBinding(ComboBox.ItemsSourceProperty, binding);
            this.comboBoxHours.SetValue(ComboBox.SelectedIndexProperty, selectedIndex);
        }

        /// <summary>
        /// Starts a loop that refreshes database time on the screen each second.
        /// </summary>
        private void startRetrieveTimer()
        {
            // sets up MktCloseHourBO
            //   will hold the difference between db and computer time.
            //   will use this difference to calculate db time based on local time.
            this.bo = new MktCloseHourBO();
            this.bo.setClockDiffMillisecond();

            DispatcherTimer retrieveTimer = new DispatcherTimer();
            retrieveTimer.Tick += new EventHandler(retrieveTimer_Tick);
            retrieveTimer.Interval = new TimeSpan(0, 0, 1);
            retrieveTimer.Start();
        }

        /// <summary>
        /// Refreshes Current Time with Calculated DB Current Time.
        /// Does NOT make db call
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void retrieveTimer_Tick(object sender, EventArgs e)
        {
            DateTime dt = bo.calcDbDate();
            this.labelCurrentTime.Content = "Current Time: " + dt.ToString("HH:mm:ss");
        }

        /// <summary>
        /// User selected another hour.
        /// Updates all checked hour types.
        /// { MDB and/or EES }
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxHours_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string newHourString = comboBoxHours.SelectedValue.ToString();
            log.Info("User selected another hour:" + newHourString);
            if (this.checkBoxUpdateCloseHourMDB.IsChecked.Value) { this.labelNewCloseHourMDB.Content = newHourString; }
            if (this.checkBoxUpdateCloseHourEES.IsChecked.Value) { this.labelNewCloseHourEES.Content = newHourString; }
        }

        /// <summary>
        /// Close Window.
        /// Prompt user if unsaved changes are detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonReturn_Click(object sender, RoutedEventArgs e)
        {
            if ( !isEESHourChanged() && !isMDBHourChanged())
            {
                log.Info("User Pressed Return Button with nothing to save");
                this.Close();
            }
            // There are unsaved changes, and user will be prompted before closing the window.
            else
            {
                MessageBoxResult userReply = MessageBox.Show( "Changes Not Saved \n\r Press OK If To Abandon Changes", "Confirm Exit Without Saving", MessageBoxButton.OKCancel, MessageBoxImage.Question );
                if (userReply.Equals(MessageBoxResult.OK))
                {
                    log.Info("User Pressed Return Button And Abandoned Changes");
                    this.Close();
                }
                else
                {
                    log.Info("User Pressed Return Button But Decided To Keep Their Changes");
                }
            }
        }

        /// <summary>
        /// Tell if user has unsaved changes for MDB Hour
        /// </summary>
        /// <returns></returns>
        private bool isMDBHourChanged()
        {
            return this.labelNewCloseHourMDB.Content.ToString().Length > 0
               && !this.labelCurrentCloseHourMDB.Content.Equals(this.labelNewCloseHourMDB.Content);
        }

        /// <summary>
        /// Tell if user has unsaved changes for EES Hour
        /// </summary>
        /// <returns></returns>
        private bool isEESHourChanged()
        {
            return this.labelNewCloseHourEES.Content.ToString().Length > 0
               && !this.labelCurrentCloseHourEES.Content.Equals(this.labelNewCloseHourEES.Content);
        }

        // User checked the MDB checkbox
        private void checkBoxUpdateCloseHourMDB_Checked(object sender, RoutedEventArgs e)
        {
            log.Info("User Checked MDB, Changes new settings to value selected in hours combobox");
            this.labelNewCloseHourMDB.Content = this.comboBoxHours.SelectedValue.ToString();
        }

        // User unchecked the MDB checkbox
        private void checkBoxUpdateCloseHourMDB_Unchecked(object sender, RoutedEventArgs e)
        {
            log.Info("User Unchecked MDB, Puts hour back to value originally retrieved for MDB");
            this.labelNewCloseHourMDB.Content = this.labelCurrentCloseHourMDB.Content;
        }

        // User checked the EES checkbox
        private void checkBoxUpdateCloseHourEES_Checked(object sender, RoutedEventArgs e)
        {
            log.Info("User Checked EES, Changes new settings to value selected in hours combobox.");
            this.labelNewCloseHourEES.Content = this.comboBoxHours.SelectedValue.ToString();
        }

        // User unchecked the EES checkbox
        private void checkBoxUpdateCloseHourEES_Unchecked(object sender, RoutedEventArgs e)
        {
            log.Info("User Unchecked EES, Puts hour back to value originally retrieved for EES");
            this.labelNewCloseHourEES.Content = this.labelCurrentCloseHourEES.Content;
        }


        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                log.Info("User Pressed Update Button");
                updateHour();
            }
            catch (HandledException he)
            {
                he.showError("Unable To Update Market Close Hour");
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Error Updating Market Close Hour", ex, MktCloseHourBO.LOGGER_ERROR_NAME);
                he.showError("Unable To Update Market Close Hour");
            }
        }

        private void updateHour()
        {
            // Check for unsaved changes on MDB Hour.
            bool mdbChanged = isMDBHourChanged();
            // Check for unsaved changes on EES Hour.
            bool eesChanged = isEESHourChanged();


            if (mdbChanged || eesChanged)
            {
                // 'Generate confirmation message
                StringBuilder sb = new StringBuilder();
                sb.Append("Please click OK to continue with the following changes:" + CRLF + CRLF);

                if (mdbChanged)
                {
                    sb.Append("The closing hour on the MDB will be changed from " +
                                        this.labelCurrentCloseHourMDB.Content.ToString() +
                                        " to " +
                                        this.labelNewCloseHourMDB.Content.ToString() +
                                        CRLF);
                }
                else
                {
                    sb.Append("The closing hour on MDB will not be modified" + CRLF);
                }

                if (eesChanged)
                {
                    sb.Append("The closing hour on EES will be changed from " +
                                        this.labelCurrentCloseHourEES.Content.ToString() +
                                        " to " +
                                        this.labelNewCloseHourEES.Content.ToString() +
                                        CRLF);
                }
                else
                {
                    sb.Append("The closing hour on EES will not be modified" + CRLF);
                }

                int newMarketHour = int.Parse(comboBoxHours.SelectedValue.ToString());

                if (bo.calcDbDate().Hour >= newMarketHour)
                {
                    sb.Append(CRLF + "Warning: The new market closing hour is earlier than the current time.");
                }

                if (!newMarketHour.Equals(12))
                {
                    sb.Append(CRLF + "Warning: The new market closing hour is not a typical hour.");
                }

                if (!this.labelNewCloseHourMDB.Content.ToString().Equals(this.labelNewCloseHourEES.Content.ToString()))
                {
                    sb.Append(CRLF + "Warning: The market closing hours for MDB and EES do not match.");
                }

                log.Info("User MessageBox:" + CRLF + sb.ToString());

                MessageBoxResult userReply = MessageBox.Show(sb.ToString(), "Confirm Market Closing Hour Changes", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (userReply.Equals(MessageBoxResult.OK))
                {
                    log.Info("User Pressed OK.  Proceeding with Update.");
                    MktCloseHourBO.UpdateCloseHours(mdbChanged, eesChanged, newMarketHour);
                    // After Update Succeeds, Refresh screen with close hours from DB for MDB and EES
                    retrieveDBValues();
                    MessageBox.Show("Market Close Hour updated successfully", "Update Successful");
                }
                else
                {
                    string message = "Update cancelled. No changes were made.";
                    log.Info(message);
                    MessageBox.Show(message, "Update Cancelled");
                }
            }
            else
            {
                string message = "No changes were made." + CRLF + "No databases selected or new closing hour selected matches current closing hour";
                log.Info(message);
                MessageBox.Show(message, "Nothing To Save");
            }

        }
    }
}
