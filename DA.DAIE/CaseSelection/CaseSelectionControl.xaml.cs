using System;
using System.Collections.Generic;
using System.IO;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DA.Common;
using DA.DAIE.Admin;
using DA.DAIE.CaseApprove;
using DA.DAIE.Common;
using DA.DAIE.Connections;
using DA.DAIE.COP;
using DA.DAIE.DataValidation;
using DA.DAIE.DRZonalForecast;
using DA.DAIE.EESBridge;
using DA.DAIE.ExternalTransaction;
using DA.DAIE.FileUpload;
using DA.DAIE.LoadResponse;
using DA.DAIE.LogView;
using DA.DAIE.MktCloseHour;
using DA.DAIE.PIC;
using DA.DAIE.PrintReport;
using DA.DAIE.Reserve;
using DA.DAIE.RSCConstraint;
using DA.DAIE.SFTViolation;
using DA.DAIE.ZonalDemand;
using log4net;
using Syncfusion.Windows.Shared;

namespace DA.DAIE.CaseSelection
{
    /// <summary>
    /// Interaction logic for BaseMode.xaml
    /// </summary>
    /// 
    public partial class CaseSelectionControl : UserControl
    {
        private const string MODULE_NAME = "CaseSelect";
        private bool showApproved = false;
        internal const string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal const string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;

        private bool isLoaded = false;
        private IList<Case> _CaseList = new List<Case>();
        private string _CaseId;
        private string _CaseName;

        private Window parentWindow = null;
        public Window ParentWindow
        {
            set { this.parentWindow = value; }
        }

        private TextBlock _TextBlockStatus = new TextBlock();

        public TextBlock TextBlockStatus
        {
            get { return _TextBlockStatus; }
            set { this._TextBlockStatus = value; }
        }

        private MODES _Mode;
        public MODES Mode
        {
            get { return _Mode; }
            set { this._Mode = value; }
        }

        public void hideCaseSelection()
        {
            this.chooseCaseGroupBox.Visibility = Visibility.Collapsed;
        }

        public CaseSelectionControl()
        {
            InitializeComponent();
            this.buttonList.Visibility = Visibility.Visible;

            SkinStorage.SetVisualStyle(MyGrid, App.VisualStyle);
        }

        private void Button_Refresh_Case_List(object sender, RoutedEventArgs e)
        {
            bool approvedOnly = false;
            log4net.LogManager.GetLogger(App.LOGGER_USER_ACCESS + ".CaseSelect.ListCases").Info("User Pressed Button To Refresh Case List Case: " + this._CaseId + " Mode: " + this._Mode);
            foreach (UIElement ui in this.MyGrid.Children)
            {
                if (typeof(ModuleSelectionControl) == ui.GetType())
                {
                    ((ModuleSelectionControl)ui).IsApprovedOnlyMode = false;
                    ((ModuleSelectionControl)ui).setEnabled();
                }
            }
            this.labelChooseCase.Text = "Choose Case";
            this.showApproved = false;
            retrieveCaseList(approvedOnly);
        }

        private void Button_KeyPressed(object sender, System.Windows.Input.AccessKeyPressedEventArgs e)
        {
            if (e.Key.Equals("A") && sender.Equals(this.labelShowApprovedCasesShortcutAltA))
            {
                    bool approvedOnly = true;
                    log4net.LogManager.GetLogger(App.LOGGER_USER_ACCESS + ".CaseSelect.ListCases").Info("User Pressed Button To Refresh Case List Case: " + this._CaseId + " Mode: " + this._Mode);
                    foreach (UIElement ui in this.MyGrid.Children)
                    {
                        if (typeof(ModuleSelectionControl) == ui.GetType())
                        {
                            ((ModuleSelectionControl)ui).IsApprovedOnlyMode = true;
                            ((ModuleSelectionControl)ui).setEnabled();
                        }
                    }
                    this.labelChooseCase.Text = "Choose Approved Case";
                    this.showApproved = true;
                    retrieveCaseList(approvedOnly);
                    e.Handled = true;
            }

        }


        /// <summary>
        /// Refreshes the list of cases for the current mode.
        /// </summary>
        public void retrieveCaseList(bool argApprovedOnly)
        {
            
            try
            {
                string methodName = ".SelectList";
                log4net.LogManager.GetLogger(MODULE_LOGGER + methodName).Info("User Refreshed Case List for Mode: " + this._Mode);

                _CaseList = DBSqlMapper.Instance().QueryForList<Case>("CaseList" + this._Mode.ToString() + (argApprovedOnly ? "Approved" : ""), null, MODULE_NAME);
                comboBoxSelectCase.ItemsSource = _CaseList;
                if (_CaseList != null && _CaseList.Count > 0)
                {
                    comboBoxSelectCase.SelectedIndex = -1;
                    comboBoxSelectionChanged();
                }
                comboBoxSelectCase.DisplayMemberPath = "Name";
                comboBoxSelectCase.SelectedValuePath = "Id";

                foreach (UIElement ui in this.MyGrid.Children)
                {
                    if (typeof(ModuleSelectionControl) == ui.GetType())
                    {
                        ((ModuleSelectionControl)ui).IsApprovedOnlyMode = argApprovedOnly;
                        ((ModuleSelectionControl)ui).setEnabled();
                    }
                }


            }
            catch (HandledException he)
            {
                he.addCustomStackMessage("Error while retriving case list for mode: " + _Mode);
                he.LoggerName = MODULE_LOGGER_ERROR;
                he.showError();
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Error while retriving case list for mode: " + _Mode, ex);
                he.LoggerName = MODULE_LOGGER_ERROR;
                he.showError();
            }
        }

        public IList<Case> CaseList
        {
            get { return this._CaseList; }
            set { this._CaseList = value; }
        }

        //private void cmdSetMIP_Click(object sender, RoutedEventArgs e)
        //{
        //    if (CaseSelected)
        //    {
        //        Window win = null;
        //        try
        //        {
        //            win = new CaseMIPWindow(this._CaseId);
        //            win.ShowDialog();
        //            this.Focus();
        //        }
        //        catch (HandledException he)
        //        {
        //            he.setLoggerNameLogExisting(CaseMIPControl.MODULE_LOGGER_ERROR);
        //            if (win != null) { win.Close(); }
        //            he.showError("Case MIP Window Failed To Open");
        //        }
        //        catch (Exception ex)
        //        {
        //            HandledException he = new HandledException("Unexpected Exception While Creating Case MIP Window", ex, CaseMIPControl.MODULE_LOGGER_ERROR);
        //            if (win != null) { win.Close(); }
        //            he.showError("Case MIP Window Failed To Open");
        //        }
        //    }
        //}

        private void comboBoxSelectCase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxSelectionChanged();
        }

        private void comboBoxSelectionChanged()
        {
            int selectedIndex = comboBoxSelectCase.SelectedIndex;
            if (selectedIndex >= 0)
            {
                string selectedCaseId = ((Case)this.comboBoxSelectCase.SelectedItem).Id;
                string selectedCaseName = ((Case)this.comboBoxSelectCase.SelectedItem).Name;

                // Object ooo = ((Case)this.comboBoxSelectCase.SelectedItem).Name;
                if (selectedCaseId.Length > 0)
                {
                    _CaseId = selectedCaseId;
                    // Grab just the last part of case name in case it's already concatenated with caseid.
                    String[] selectedCaseStringSplit = selectedCaseName.Split(new char[] { '|' });
                    if (selectedCaseStringSplit.Length > 1)
                    {
                        _CaseName = selectedCaseStringSplit[selectedCaseStringSplit.Length - 1];
                    }
                    else
                    {
                        _CaseName = selectedCaseName;
                    }
                }
                else
                {
                    _CaseId = null;
                    _CaseName = null;
                }
            }
            else
            {
                // See private bool CaseSelected.  
                // Make sure this string is empty if no case is selected.
                _CaseId = "";
                _CaseName = "Case Not Selected";

            }
        }

        /// <summary>
        /// Returns true if user has selected a case.
        /// </summary>
        private bool CaseSelected
        {
            get 
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER);
                bool caseSelected = _CaseId != null && _CaseId.Length > 0;
                if (!caseSelected)
                {
                    log.Warn("No Case Was Selected");
                    MessageBox.Show("Please Select A Case", "No Case Was Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return caseSelected; 
            }
        }

        private void cmdDataValid_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Window win = null;
                string errorloggerName = DataValidationBO.MODULE_LOGGER_ERROR;
                string errorMessage = "Unable To Run Data Validation";
                string exceptionMessage = "Exception Running Data Validation";
                try
                {
                    win = new DataValidationWindow(this._Mode, this._CaseId, this._CaseName);
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                
            }
        }

        private void cmdCleanRSCConstraints_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    RSCConstraintBO.CleanupRSCConstraints(this._CaseId, false);
                    MessageBox.Show("Clean RSC Constraints Complete", "Clean Constraints Complete");
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(RSCConstraintBO.MODULE_LOGGER_ERROR);
                    he.showError("Unable To Clean RSC Constraints");
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException("Exception Cleaning RSC Constraints", ex, RSCConstraintBO.MODULE_LOGGER_ERROR);
                    he.showError("Unable To Clean RSC Constraints");
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }


        private void cmdGetRSCConstraints_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Window win = null;
                string errorloggerName = RSCConstraintBO.MODULE_LOGGER_ERROR;
                string errorMessage = "Unable To Process RSC Constraints";
                string exceptionMessage = "Exception Processing RSC Constraints";
                try
                {
                    win = new RSCConstraintWindow(this._CaseId,this._CaseName);
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
            }
        }

        private void cmdConstraint_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    GRTConstraintBO.cleanupRSCConstraints(this._CaseId, this._Mode);
                    IList<string> grtFileNames = GRTConstraintBO.selectGRTFileNames();
                    if (grtFileNames.Count > 0)
                    {
                        GRTConstraintBO.LoadConstraints(grtFileNames, this._CaseId, this._Mode);
                        MessageBox.Show("Upload Constraints Complete", "Upload Complete");
                    }
                    else
                    {
                        MessageBox.Show("No constraint file was selected.", "Nothing To Upload");
                    }
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(GRTConstraintBO.MODULE_LOGGER_ERROR);
                    he.showError("Unable To Upload GRT Constraints");
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException("Exception Uploading GRT Constraints", ex, GRTConstraintBO.MODULE_LOGGER_ERROR);
                    he.showError("Unable To Upload GRT Constraints");
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
                
            }
        }

        private void cmdMMM_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    MMMBidBO.LoadMMMFile(this._CaseId, this._Mode );
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(MMMBidBO.MODULE_LOGGER_ERROR);
                    he.showError("Unable to Upload MMM Bids");
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException("Exception Uploading MMM Bids", ex, MMMBidBO.MODULE_LOGGER_ERROR);
                    he.showError("Unable to Upload MMM Bids");
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdReserves_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Window win = null;
                string errorloggerName = ReserveBO.MODULE_LOGGER_ERROR;
                string moduleName = ReserveBO.MODULE_NAME;
                string errorMessage = "Unable To Process " + moduleName;
                string exceptionMessage = "Exception Processing " + moduleName;
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    win = new ReserveWindow(this._Mode, this._CaseId, this._CaseName);
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdCopModify_Click(object sender, RoutedEventArgs e)
        {
                Window win = null;
                string errorloggerName = COPBO.MODULE_LOGGER_ERROR;
                string moduleName = COPBO.MODULE_NAME;
                string errorMessage = "Unable To Process " + moduleName;
                string exceptionMessage = "Exception Processing " + moduleName;
                try
                {
                    win = new COPUpdateWindow();
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
        }

        private void cmdExtTrans_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Window win = null;
                string errorloggerName = ExternalTransactionBO.MODULE_LOGGER_ERROR;
                string moduleName = ExternalTransactionBO.MODULE_NAME;
                string errorMessage = "Unable To Process " + moduleName;
                string exceptionMessage = "Exception Processing " + moduleName;

                Cursor originalCursor = this.Cursor;
                this.Cursor = Cursors.Wait;

                try
                {
                    win = new ExternalTransactionWindow(_CaseId, _CaseName);
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdInitExtTrans_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    ExternalTransactionBO.ExecuteProcedureUploadExternalTransaction(_CaseId);
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(ExternalTransactionBO.MODULE_LOGGER_ERROR);
                    he.showError("Initialize External Transaction Failed");
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdInitLoadResp_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Cursor originalCursor = this.Cursor;
                string methodPurpose = "Initialize Load Response";
                try
                {
                    this.Cursor = Cursors.Wait;
                    LoadResponseBO.InitializeLoadResponse(_CaseId);
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(LoadResponseBO.MODULE_LOGGER_ERROR);
                    he.showError("Failed To " + methodPurpose);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(methodPurpose, ex, LoadResponseBO.MODULE_LOGGER_ERROR);
                    he.showError("Failed To " + methodPurpose);
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdInitZoneDemand_Click(object sender, RoutedEventArgs e)
        {
            string methodPurpose = "Initialize Zonal Demand";
            Cursor originalCursor = this.Cursor;
            try
            {
                this.Cursor = Cursors.Wait;
                ZonalDemandBO.CreateZonalDemand();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(ZonalDemandBO.MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                he.showError();
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(methodPurpose, ex, ZonalDemandBO.MODULE_LOGGER_ERROR);
                he.showError();
            }
            finally
            {
                this.Cursor = originalCursor;
            }
        }

        private void cmdPrint_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                string methodPurpose = "Print Reports for Case: " + _CaseId;
                try
                {

                    if (this._Mode.Equals(MODES.RAA_SCRA) && showApproved == false )
                    {
                        MessageBoxResult mbr = MessageBox.Show("Would you like to approve this SCRA case now?", "SCRA Approval Reminder", MessageBoxButton.YesNoCancel);
                        if (mbr.Equals(MessageBoxResult.Yes))
                        {
                            string defaultPrinterName = LocalPrintServer.GetDefaultPrintQueue().Name;
                            PrintReportBO.printReports(this._Mode.ToString(), this._CaseId, defaultPrinterName);
                            CaseApproveBO.approveSCRACase(_CaseId, _Mode, this.parentWindow, true);
                        }
                        else if (mbr.Equals(MessageBoxResult.No))
                        {
                            string defaultPrinterName = LocalPrintServer.GetDefaultPrintQueue().Name;
                            PrintReportBO.printReports(this._Mode.ToString(), this._CaseId, defaultPrinterName);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        string defaultPrinterName = LocalPrintServer.GetDefaultPrintQueue().Name;
                        PrintReportBO.printReports(this._Mode.ToString(), this._CaseId, defaultPrinterName);
                    }
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(PrintReportBO.LOGGER_ERROR_NAME);
                    he.addCustomStackMessage(methodPurpose);
                    he.showError();
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(methodPurpose, ex, PrintReportBO.LOGGER_ERROR_NAME);
                    he.showError();
                }
            }
        }

        private void cmdEditDRZonalFcst_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Window win = null;
                string errorloggerName = DRZonalForecastBO.MODULE_LOGGER_ERROR;
                string moduleName = DRZonalForecastBO.MODULE_NAME;
                string errorMessage = "Unable To Process " + moduleName;
                string exceptionMessage = "Exception Processing " + moduleName;
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    win = new DRZonalForecastWindow(_CaseId, _CaseName);
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdApprove_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                string methodPurpose = "Approve Case: " + _CaseId;
                try
                {
                    CaseApproveBO.approveSCRACase(_CaseId, _Mode, this.parentWindow, true );
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(CaseApproveBO.LOGGER_ERROR_NAME);
                    he.addCustomStackMessage(methodPurpose);
                    he.showError();
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(methodPurpose, ex, CaseApproveBO.LOGGER_ERROR_NAME);
                    he.showError();
                }
            }
        }

        private void cmdApproveFilesOnly_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                string methodPurpose = "Approve Case Files Only: " + _CaseId;
                try
                {
                    CaseApproveBO.approveSCRACase(_CaseId, _Mode, this.parentWindow, false );
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(CaseApproveBO.LOGGER_ERROR_NAME);
                    he.addCustomStackMessage(methodPurpose);
                    he.showError();
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(methodPurpose, ex, CaseApproveBO.LOGGER_ERROR_NAME);
                    he.showError();
                }
            }
        }



        private void cmdPIC_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Window win = null;
                string errorloggerName = PICBO.MODULE_LOGGER_ERROR;
                string moduleName = PICBO.MODULE_NAME;
                string errorMessage = "Unable To Run (PIC) Peak Impact Calculator for Case: " + _CaseId;
                string exceptionMessage = "Exception Running (PIC) Peak Impact Calculator for Case: " + _CaseId;
                try
                {
                    win = new PICWindow(_Mode, _CaseId, _CaseName);
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
            }
        }

        private void cmdSFT_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                Window win = null;
                string errorloggerName = SFTViolationBO.MODULE_LOGGER_ERROR;
                string moduleName = SFTViolationBO.MODULE_NAME;
                string errorMessage = "Unable To Show SFT Violations for Case: " + _CaseId;
                string exceptionMessage = "Exception Showing SFT Violations for Case: " + _CaseId;

                NetworkFolderMeta folderMeta = null;
                try
                {

                    folderMeta = ConnectionsParser.getNetworkFolderMeta("SFTFolder", ConnectionsParser.CONNECTIONS_FILE_NAME);


                    string SFTFolder = folderMeta.getFullName("");
                    //"//mktsmbdev/mktfiles_readonly/export";
                    win = new SFTViolationWindow(_CaseId, _CaseName, SFTFolder);
                    win.ShowDialog();
                    this.Focus();
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                    if (win != null) { win.Close(); }
                    he.showError(errorMessage);
                }
            }
        }

        private void cmdRunEES_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                string methodPurpose = "Rerun EES Bridge for Case: " + _CaseId;
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    EESBridgeBO.RunBridge(_CaseId, _CaseName);
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(EESBridgeBO.MODULE_LOGGER_ERROR);
                    he.addCustomStackMessage(methodPurpose);
                    he.showError("Unable to Run EES Bridge.");
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(methodPurpose, ex, EESBridgeBO.MODULE_LOGGER_ERROR);
                    he.showError("Unable to Run EES Bridge.");
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdCopDuplicate_Click(object sender, RoutedEventArgs e)
        {
            Window win = null;
            string errorloggerName = COPBO.MODULE_LOGGER_ERROR;
            string moduleName = COPBO.MODULE_NAME;
            string errorMessage = "Unable To Process " + moduleName;
            string exceptionMessage = "Exception Processing " + moduleName;
            try
            {
                win = new COPDuplicateWindow();
                win.ShowDialog();
                this.Focus();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
        }

        //private void cmdMitigate_Click(object sender, RoutedEventArgs e)
        //{
        //    Window win = null;
        //    string errorloggerName = RTMitigationBO.MODULE_LOGGER_ERROR;
        //    string moduleName = RTMitigationBO.MODULE_NAME;
        //    string errorMessage = "Unable To Process Real Time Mitigation for Case: " + _CaseId;
        //    string exceptionMessage = "Exception Processing Real Time Mitigation for Case: " + _CaseId;
        //    try
        //    {
        //        win = new RTMitigationWindow();
        //        win.ShowDialog();
        //        this.Focus();
        //    }
        //    catch (HandledException he)
        //    {
        //        he.setLoggerNameLogExisting(errorloggerName);
        //        if (win != null) { win.Close(); }
        //        he.showError(errorMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
        //        if (win != null) { win.Close(); }
        //        he.showError(errorMessage);
        //    }
        //}

        private void cmdInitDRZonalFcst_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                string methodPurpose = "Initialize DR Zonal Forecast for Case: " + _CaseId;
                Cursor originalCursor = this.Cursor;
                try
                {
                    this.Cursor = Cursors.Wait;
                    DRZonalForecastBO.initializeDRZonalForecast(_CaseId);
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(DRZonalForecastBO.MODULE_LOGGER_ERROR);
                    he.addCustomStackMessage(methodPurpose);
                    he.showError();
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(methodPurpose, ex, DRZonalForecastBO.MODULE_LOGGER_ERROR);
                    he.showError();
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
        }

        private void cmdUpdateCloseHour_Click(object sender, RoutedEventArgs e)
        {
            Window win = null;
            string errorloggerName = MktCloseHourBO.LOGGER_ERROR_NAME;
            //string moduleName = MktCloseHourBO.MODULE_NAME;
            string errorMessage = "Unable To Update Market Close Hour ";
            string exceptionMessage = "Exception Updating Market Close Hour ";
            try
            {
                win = new MktCloseHourWindow();
                win.ShowDialog();
                this.Focus();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
        }

        private void cmdLocReserves_Click(object sender, RoutedEventArgs e)
        {
            if (CaseSelected)
            {
                string methodPurpose = "Update Locational Reserve Requirements for Case:" + _CaseId;
                try
                {
                    string[] returnMessage = GRTLocReserveRequirementBO.loadGRTFile(_CaseId);
                    MessageBox.Show(returnMessage[0], returnMessage[1]);
                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(GRTLocReserveRequirementBO.MODULE_LOGGER_ERROR);
                    he.addCustomStackMessage(methodPurpose);
                    he.showError();
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException(methodPurpose, ex, GRTLocReserveRequirementBO.MODULE_LOGGER_ERROR);
                    he.showError();
                }
            }
        }

        private void cmdAdminSql_Click(object sender, RoutedEventArgs e)
        {
            string methodPurpose = "Admin Application SQL and DB Connections:" + _CaseId;
            AdminWindow win = null;
            try
            {
                win = new AdminWindow();
                win.ShowDialog();
                e.Handled = true;

                // This is a really special case here.
                // You may want to clear cached sql statements.
                // Because...
                // In this application SQL must be Uniquely Identified by ID.
                // So an assertion is made by the application.
                //
                // If a sql statement with the same ID is found
                //    the sql is compared to the existing sql for that ID.
                //    If the SQL are identical, no harm will happen and the duplicate can be ignored.
                //    If they are different, then an exception will occur.
                //
                // Now...
                //    If you've changed the sql in the SQL Admin Screen
                //    The sql would always be different when it is reloaded, and an error will be thrown.
                //    To help, clear the cache after running This Admin SQL Screen
                //
                // NOTE: 
                //    The situation you really never want to happen is....
                //    SqlID actually occurs twice but with different value.
                //    To avoid this...
                //    SqlCache must throw an error ( and not a warning )
                //       when you load a SQLID and find that the underlying SQL has changed.

                //DBLayer.DBDomSqlMapBuilder.ClearSqlIdCache();
                CustomMapper.ClearSqlIdCache();
                DBSqlMapper.InitMapper();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(AdminBO.LOGGER_ERROR_NAME);
                he.addCustomStackMessage(methodPurpose);
                if (win != null) { win.Close(); }
                he.showError();
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(methodPurpose, ex, AdminBO.LOGGER_ERROR_NAME );
                if (win != null) { win.Close(); }
                he.showError();
            }
        }

        private void cmdAdminLoginTest_Click(object sender, RoutedEventArgs e)
        {
            Window win = null;
            string errorloggerName = AdminBO.LOGGER_ERROR_NAME;
            //string moduleName = AdminBO.MODULE_NAME;
            string errorMessage = "Unable To Process Admin Application SQL and DB Connections:" + _CaseId;
            string exceptionMessage = "Exception Processing Admin Application SQL and DB Connections:" + _CaseId;

            Cursor originalCursor = this.Cursor;
            try
            {
                this.Cursor = Cursors.Wait;
                win = new ConnectionsWindow();
                win.ShowDialog();
                this.Focus();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            finally
            {
                this.Cursor = originalCursor;
            }
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

        private void cmdConnectionPasswords_Click(object sender, RoutedEventArgs e)
        {
            Window win = null;
            string errorloggerName = AdminBO.LOGGER_ERROR_NAME;
            //string moduleName = AdminBO.MODULE_NAME;
            string errorMessage = "Unable To Set Connection Passwords";
            string exceptionMessage = "Exception Setting Connection Passwords";

            Cursor originalCursor = this.Cursor;
            try
            {
                this.Cursor = Cursors.Wait;

                string currentDirectory = Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                win = new ConnectionPasswordWindow(currentDirectory);
                win.ShowDialog();
                this.Focus();

                CustomMapper.ClearSqlIdCache();
                DBSqlMapper.InitMapper();
                forceCacheMapper("SqlMapEES.config");
                forceCacheMapper("SqlMapFCST.config");


            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            finally
            {
                this.Cursor = originalCursor;
            }
        }


        private void cmdAdminFTPLogin_Click(object sender, RoutedEventArgs e)
        {
            Window win = null;
            string errorloggerName = AdminBO.LOGGER_ERROR_NAME;
            //string moduleName = AdminBO.MODULE_NAME;
            string errorMessage = "Unable To Process Admin FTP Connection:";
            string exceptionMessage = "Exception Processing Admin FTP Connection:";
            try
            {
                string currentDirectory = Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                win = new ConfigFTPLoginWindow(currentDirectory + "\\" + "Connections.xml", "MISFTP");
                win.ShowDialog();
                this.Focus();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
        }

        private void cmdAdminReportLogin_Click(object sender, RoutedEventArgs e)
        {
            Window win = null;
            string errorloggerName = AdminBO.LOGGER_ERROR_NAME;
            string errorMessage = "Unable To Process Admin Report Connection:";
            string exceptionMessage = "Exception Processing Admin Report Connection:";
            try
            {
                string currentDirectory = Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                win = new ConfigReportLoginWindow(currentDirectory + "\\" + "Connections.xml", "JasperReports");
                win.ShowDialog();
                this.Focus();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
        }

        private void cmdLogView_Click(object sender, RoutedEventArgs e)
        {
            Window win = null;
            string errorloggerName = App.LOGGER_USER_ERROR + ".LogView";
            //string moduleName = "Log Viewer";
            string errorMessage = "Unable To Process Admin Log Viewer:";
            string exceptionMessage = "Exception Processing Admin Log Viewer:";
            Cursor originalCursor = this.Cursor;
            try
            {
                this.Cursor = Cursors.Wait;
                string currentDirectory = Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                win = new LogViewWindow();
                win.ShowDialog();
                this.Focus();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException(exceptionMessage, ex, errorloggerName);
                if (win != null) { win.Close(); }
                he.showError(errorMessage);
            }
            finally
            {
                this.Cursor = originalCursor;
            }
        }


        /// <summary>
        /// The passed Module Name 
        /// Maps Button on Control -to- Specific Method Call
        /// ( Sorry, Couldn't Get .Net Reflection To Make This More Generic. )
        /// </summary>
        /// <param name="argModuleName"></param>
        /// <param name="argButton"></param>
        private void addEventHandlerFromString(string argModuleName, Button argButton )
        {
            if (argModuleName.Equals("cmdCopModify")) { argButton.Click += new RoutedEventHandler(cmdCopModify_Click); }
            else if (argModuleName.Equals("cmdExtTrans")) { argButton.Click += new RoutedEventHandler(cmdExtTrans_Click); }
            else if (argModuleName.Equals("cmdReserves")) { argButton.Click += new RoutedEventHandler(cmdReserves_Click); }
            else if (argModuleName.Equals("cmdEditDRZonalFcst")) { argButton.Click += new RoutedEventHandler(cmdEditDRZonalFcst_Click); }
            else if (argModuleName.Equals("cmdDataValid")) { argButton.Click += new RoutedEventHandler(cmdDataValid_Click); }
            //else if (argModuleName.Equals("cmdSetMIP")) { argButton.Click += new RoutedEventHandler(cmdSetMIP_Click); }
            else if (argModuleName.Equals("cmdMMM")) { argButton.Click += new RoutedEventHandler(cmdMMM_Click); }
            else if (argModuleName.Equals("cmdGetRSCConstraints")) { argButton.Click += new RoutedEventHandler(cmdGetRSCConstraints_Click); }
            else if (argModuleName.Equals("cmdCleanRSCConstraints")) { argButton.Click += new RoutedEventHandler(cmdCleanRSCConstraints_Click);}
            else if (argModuleName.Equals("cmdConstraint")) { argButton.Click += new RoutedEventHandler(cmdConstraint_Click); }
            else if (argModuleName.Equals("cmdInitExtTrans")) { argButton.Click += new RoutedEventHandler(cmdInitExtTrans_Click); }
            else if (argModuleName.Equals("cmdInitLoadResp")) { argButton.Click += new RoutedEventHandler(cmdInitLoadResp_Click); }
            else if (argModuleName.Equals("cmdInitZoneDemand")) { argButton.Click += new RoutedEventHandler(cmdInitZoneDemand_Click); }
            else if (argModuleName.Equals("cmdEditDRZonalFcst")) { argButton.Click += new RoutedEventHandler(cmdEditDRZonalFcst_Click); }
            else if (argModuleName.Equals("cmdPrint")){ argButton.Click += new RoutedEventHandler(cmdPrint_Click); }
            else if (argModuleName.Equals("cmdApprove")) { argButton.Click += new RoutedEventHandler(cmdApprove_Click); }
            else if (argModuleName.Equals("cmdPIC")) { argButton.Click += new RoutedEventHandler(cmdPIC_Click); }
            else if (argModuleName.Equals("cmdSFT")) { argButton.Click += new RoutedEventHandler(cmdSFT_Click); }
            else if (argModuleName.Equals("cmdRunEES")) { argButton.Click += new RoutedEventHandler(cmdRunEES_Click); }
            else if (argModuleName.Equals("cmdCopDuplicate")) { argButton.Click += new RoutedEventHandler(cmdCopDuplicate_Click); }
            //else if (argModuleName.Equals("cmdMitigate")) { argButton.Click += new RoutedEventHandler(cmdMitigate_Click); }
            else if (argModuleName.Equals("cmdInitDRZonalFcst")) { argButton.Click += new RoutedEventHandler(cmdInitDRZonalFcst_Click);}
            else if (argModuleName.Equals("cmdUpdateCloseHour")) { argButton.Click += new RoutedEventHandler(cmdUpdateCloseHour_Click);}
            else if (argModuleName.Equals("cmdLocReserves")) { argButton.Click += new RoutedEventHandler(cmdLocReserves_Click); }
            else if (argModuleName.Equals("cmdAdminSql")) { argButton.Click += new RoutedEventHandler(cmdAdminSql_Click); }
            else if (argModuleName.Equals("cmdAdminLoginTest")){ argButton.Click += new RoutedEventHandler(cmdAdminLoginTest_Click);}
            else if (argModuleName.Equals("cmdAdminFTPLogin")) { argButton.Click += new RoutedEventHandler(cmdAdminFTPLogin_Click); }
            else if (argModuleName.Equals("cmdAdminReportLogin")) { argButton.Click += new RoutedEventHandler(cmdAdminReportLogin_Click); }
            else if (argModuleName.Equals("cmdLogView")) { argButton.Click += new RoutedEventHandler(cmdLogView_Click); }
            else if (argModuleName.Equals("cmdApproveFilesOnly")) { argButton.Click += new RoutedEventHandler(cmdApproveFilesOnly_Click); }
            else if (argModuleName.Equals("cmdConnectionPasswords")) { argButton.Click += new RoutedEventHandler(cmdConnectionPasswords_Click); }

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) == false)
            {
                if (!isLoaded)
                {
                    formatScreen();
                    foreach (UIElement ui in this.MyGrid.Children)
                    {
                        if (typeof(ModuleSelectionControl) == ui.GetType())
                        {
                            ((ModuleSelectionControl)ui).IsApprovedOnlyMode = false;
                            ((ModuleSelectionControl)ui).setEnabled();
                        }
                    }
                    isLoaded = true;
                }
            }
        }

        public bool getHasEnabledModule()
        {
            IList<ModuleMetaData> mmd = new List<ModuleMetaData>();
            // Do this only so the screen can draw in design mode.
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) == false)
            {
                mmd = ModuleConfiguration.getModeModules(_Mode, ModuleConfiguration.CONFIG_FILE);
            }
            foreach (UIElement ui in this.MyGrid.Children)
            {
                if (typeof(ModuleSelectionControl) == ui.GetType())
                {
                    string moduleName = ((ModuleSelectionControl)ui).Name;
                    ((ModuleSelectionControl)ui).Visibility = Visibility.Hidden;
                    ((ModuleSelectionControl)ui).IsEnabled = false;
                    foreach (ModuleMetaData meta in mmd)
                    {
                        if (moduleName.Equals(meta.Name))
                        {
                            if (meta.Enabled)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }



        private void formatScreen()
        {
            IList<ModuleMetaData> mmd = new List<ModuleMetaData>();
            // Do this only so the screen can draw in design mode.
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) == false)
            {
                mmd = ModuleConfiguration.getModeModules(_Mode, ModuleConfiguration.CONFIG_FILE);
            }

            foreach (UIElement ui in this.MyGrid.Children)
            {
                if (typeof(ModuleSelectionControl) == ui.GetType())
                {
                    string moduleName = ((ModuleSelectionControl)ui).Name;
                    ((ModuleSelectionControl)ui).Visibility = Visibility.Hidden;
                    ((ModuleSelectionControl)ui).IsEnabled = false;
                    foreach (ModuleMetaData meta in mmd)
                    {
                        if (moduleName.Equals(meta.Name))
                        {
                            ui.SetValue(Grid.RowProperty, meta.Row + 1);
                            ui.SetValue(Grid.ColumnProperty, meta.Column);
                            ui.SetValue(IsEnabledProperty, meta.Enabled);
                            ((ModuleSelectionControl)ui).SetValue(ModuleSelectionControl.LabelTextProperty, meta.Label);
                            ((ModuleSelectionControl)ui).SetValue(ModuleSelectionControl.CommentProperty, meta.Comment);
                            ((ModuleSelectionControl)ui).SetValue(ModuleSelectionControl.AllowNormallyProperty, meta.Enabled);
                            ((ModuleSelectionControl)ui).SetValue(ModuleSelectionControl.AllowApprovedProperty, meta.AllowApproved);
                            if (moduleName.StartsWith("spacer"))
                            {
                                ui.SetValue(VisibilityProperty, Visibility.Hidden);
                            }
                            else
                            {
                                ui.SetValue(VisibilityProperty, Visibility.Visible);
                            }

                            // Maps Button on User Control Clicked Event To Execute A Specific Method 
                            // Based on passed moduleName.
                            addEventHandlerFromString(moduleName, ((ModuleSelectionControl)ui).button1);

                        }
                    }
                }
            }
        }





    }
}
