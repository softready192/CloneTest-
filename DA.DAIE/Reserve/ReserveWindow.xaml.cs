using System.Windows;
using DA.DAIE.CaseSelection;
using System.Collections.Generic;
using System;
using System.Windows.Data;
using DA.Common;
using System.Windows.Media;
using log4net;
using DA.DAIE.Common;
using Syncfusion.Windows.Shared;
using Syncfusion.Windows.Controls.Grid;

namespace DA.DAIE.Reserve
{
    /// <summary>
    /// Interaction logic for ReserveWindow.xaml
    /// </summary>
    public partial class ReserveWindow : Window
    {
        private MODES _Mode;
        private string _CaseId;
        private string _CaseName;
        public IList<Reserve> _Reserves;
        private ContingencyPercent _CP = null;

        //bool newInput = false;

        public ReserveWindow()
        {
            InitializeComponent();
        }

        private void readUserConfig()
        {
            try
            {
                this.integerTextBox1.Value = UserConfig.getContingency1Percent();
                this.integerTextBox2.Value = UserConfig.getContingency2Percent();
                this.integerTextBox3.Value = UserConfig.getTMSRPercent();

                _CP = new ContingencyPercent(
                    UserConfig.getContingency1Percent()
                   , UserConfig.getContingency2Percent()
                   , UserConfig.getTMSRPercent());
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(ReserveBO.MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("Read User Config");
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Read User Config", ex, ReserveBO.MODULE_LOGGER_ERROR);
            }

        }

        public ReserveWindow(MODES argMode, string argCaseId, string argCaseName)
        {
            try
            {
                this._Mode = argMode;
                this._CaseId = argCaseId;
                this._CaseName = argCaseName;
                InitializeComponent();
                SkinStorage.SetVisualStyle(groupBox1, App.VisualStyle);

                dataGrid.Model.RowHeights.DefaultLineSize = 16;


                readUserConfig();
                enablePercents(false);

                this.labelCase.Content = _CaseId + " | " + _CaseName;

                _Reserves = ReserveBO.retrieveReserves(this._Mode, this._CaseId, this._CP);
                BindingOperations.ClearBinding(this.dataGrid, Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty);

                Binding binding2 = new Binding();
                binding2.Source = _Reserves;
                this.dataGrid.SetBinding(Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty, binding2);
                //this.dataGrid.Model.HeaderStyle.Foreground = Brushes.White;
                if (!_Mode.Equals(MODES.DA))
                {
                    this.buttonLoadTransRSS.Visibility = Visibility.Hidden;
                }



            }
            catch (HandledException he)
            {
                he.addCustomStackMessage("Reserve Window Constructor");
                he.setLoggerNameLogExisting(ReserveBO.MODULE_LOGGER_ERROR);
                he.showError();
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Reserve Window Constructor", ex, ReserveBO.MODULE_LOGGER_ERROR);
                he.showError();
            }

        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Pressed Button Refresh");
                this.dataGrid.Model.CurrencyManager.EndEdit();
                readUserConfig();

                _Reserves = ReserveBO.retrieveReserves(this._Mode, this._CaseId, this._CP);
                BindingOperations.ClearBinding(this.dataGrid, Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty);

                Binding binding2 = new Binding();
                binding2.Source = _Reserves;
                this.dataGrid.SetBinding(Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty, binding2);

                enablePercents(false);
            }
            catch (HandledException he)
            {
                he.addCustomStackMessage("User Pressed Button Refresh");
                he.setLoggerNameLogExisting(ReserveBO.MODULE_LOGGER_ERROR);
                he.showError();
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("User Pressed Button Refresh", ex, ReserveBO.MODULE_LOGGER_ERROR);
                he.showError();
            }
        }

        private void buttonUpload_Click(object sender, RoutedEventArgs e)
        {
            LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Pressed Button Upload");

            try
            {
                validateRoundDecimal();
            }
            catch (Exception)
            {
                System.Console.Beep(440, 100);
                return;
            }

            ReserveBO.uploadReserves(this._Mode, this._Reserves);
        }

        private void buttonReturn_Click(object sender, RoutedEventArgs e)
        {
            LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Pressed Button Return");
            if (this.buttonRefresh.IsEnabled)
            {
                this.Close();
            }
            else
            {

                try
                {
                    validateRoundDecimal();
                }
                catch (Exception)
                {
                    System.Console.Beep(440, 100);
                    return;
                }

                this.dataGrid.Model.CurrencyManager.EndEdit();
                readUserConfig();
                recalc();
                this.enablePercents(false);

            }
        }

        /// <summary>
        /// Recalculate the values in the last 3 columns.
        /// </summary>
        private void recalc()
        {
            try
            {
                LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("Recalc with 1st, 2nd TMSR =  "

                    + integerTextBox1.MaskedText
                    + "," + integerTextBox2.MaskedText
                    + "," + integerTextBox3.MaskedText);



                BindingOperations.ClearBinding(this.dataGrid, Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty);

                double dcont1 = double.Parse(integerTextBox1.MaskedText + "0");
                double dcont2 = double.Parse(integerTextBox2.MaskedText + "0");
                double dTMSR = double.Parse(integerTextBox3.MaskedText + "0");

                ContingencyPercent cp = new ContingencyPercent(dcont1, dcont2, dTMSR);

                ReserveBO.calculateReserveRequirements(cp, _Reserves);

                Binding binding2 = new Binding();
                binding2.Source = _Reserves;
                this.dataGrid.SetBinding(Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty, binding2);

                this.dataGrid.RowBackground = new SolidColorBrush(Colors.LightGray);
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(ReserveBO.MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("Recalc Method");
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Recalc Method", ex, ReserveBO.MODULE_LOGGER_ERROR);
            }

        }

        private void integerTextBox1_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Changed First Cont Percent");
            dataGrid.Model.CurrencyManager.EndEdit();
            recalc();
        }

        private void integerTextBox2_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Changed Second Cont Percent");
            dataGrid.Model.CurrencyManager.EndEdit();
            recalc();
        }

        private void integerTextBox3_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Changed TMSR Percent");
            dataGrid.Model.CurrencyManager.EndEdit();
            recalc();
        }

        /// <summary>
        /// Button opens up percentages for editing.
        /// </summary>
        /// <param name="argEnabled"></param>
        private void enablePercents( bool argEnabled )
        {
            this.integerTextBox1.IsEnabled = argEnabled;
            this.integerTextBox2.IsEnabled = argEnabled;
            this.integerTextBox3.IsEnabled = argEnabled;

            this.buttonRefresh.IsEnabled = !argEnabled;
            this.buttonUpload.IsEnabled = !argEnabled;
            this.buttonLoadTransRSS.IsEnabled = !argEnabled;

            this.integerTextBox1.Background = (argEnabled ? Brushes.White : Brushes.LightGray);
            this.integerTextBox2.Background = (argEnabled ? Brushes.White : Brushes.LightGray);
            this.integerTextBox3.Background = (argEnabled ? Brushes.White : Brushes.LightGray);

            if (argEnabled)
            {
                this.buttonUpdatePct.Content = "Save Pct";
            }
            else
            {
                this.buttonUpdatePct.Content = "Edit Pct";
            }

        }

        private bool percentsEnabled()
        {
            return this.integerTextBox1.IsEnabled;
        }

        /// <summary>
        /// Saves user entries for Percentages back to common file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonUpdatePct_Click(object sender, RoutedEventArgs e)
        {
            LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Pressed Button Update PCT");

            if( !percentsEnabled())
            {
                enablePercents(true);
            }
            else
            {
            
                double dcont1 = integerTextBox1.Value.Value;
                double dcont2 = integerTextBox2.Value.Value;
                double dTMSR = integerTextBox3.Value.Value;

                UserConfig.setContingencyPercent(dcont1, dcont2, dTMSR);

                enablePercents(false);
            }
        }

        /// <summary>
        /// Loads Entries for HQ and NB for Mode = RA only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLoadTransRSS_Click(object sender, RoutedEventArgs e)
        {
            LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("User Pressed Button Load Trans RSS Button");

            try
            {
                validateRoundDecimal();
            }
            catch (Exception)
            {
                System.Console.Beep(440, 100);
                return;
            }

            try
            {
                dataGrid.Model.CurrencyManager.EndEdit();
                IList<RSSCase> caseList = ReserveBO.retrieveRSSCaseList(_Mode, _CaseId);


                //IList<RSSCase> tst = new List<RSSCase>();
                //RSSCase c1 = new RSSCase("3120110322190725", "Case1");
                //RSSCase c2 = new RSSCase("3120110322141431", "DAM23MAR2011NX-9 TTTST");
                //tst.Add(c1);
                //tst.Add(c2);
                //caseList = tst;


                if (caseList == null || caseList.Count == 0)
                {
                    LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("No RSS Case Found, No HQ or NB Transactions Populated from RSS");
                    MessageBox.Show("No RSS Case Found", "No HQ or NB Transactions Populated from RSS" );
                    return;
                }
                else if (caseList.Count == 1)
                {
                    ReserveBO.retrieveReserveInterfaceDA(_Mode, caseList[0].CaseId, ref _Reserves);
                    recalc();
                    MessageBox.Show("Success", "HQ and NB Transactions Populated from RSS" );
                }
                else
                {
                    LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("Multiple RSS Case Founds.  Open Window For User Selection");
                    RSSCaseSelectionWindow win = new RSSCaseSelectionWindow(caseList);
                    win.ShowDialog();
                    if (win.DialogResult.HasValue && win.DialogResult.Value)
                    {
                        LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("Multiple RSS Case Founds.  UserSelected " + win.SelectedCase.CaseDisplay);
                        ReserveBO.retrieveReserveInterfaceDA(_Mode, win.SelectedCase.CaseId, ref _Reserves);
                        recalc();
                    }
                    else
                    {
                        LogManager.GetLogger(ReserveBO.MODULE_LOGGER).Info("Multiple RSS Case Founds.  User Did Not Select A Case");
                    }
                    
                }

            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(ReserveBO.MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("Load Trans RSS");
                he.showError();
            }
            catch (Exception ex)
            {
                new HandledException("Load Trans RSS", ex, ReserveBO.MODULE_LOGGER_ERROR).showError();
            }
        }

        /// <summary>
        /// Validate that user input is always valid.
        /// Beep as soon as they try anything bad.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                string currentText = this.dataGrid.Model.CurrencyManager.CurrentCell.Renderer.ControlText;
                if (e.Text.Equals("-") && currentText == "") { return; }
                decimal.Parse(currentText + e.Text);
                //newInput = true;
            }
            catch (Exception)
            {
                e.Handled = true;
                System.Console.Beep(440, 100);
                //MessageBox.Show("Invalid Number Entered", "Validation Error" );
            }
        }

        bool cellMoving = false;

        /// <summary>
        /// Recalc when user has changed an moved away from the cell.
        /// Make sure to put user back on the same cell after recalc is done.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void dataGrid_CurrentCellMoved(object sender, Syncfusion.Windows.Controls.Grid.GridCurrentCellMovedEventArgs args)
            {
            if (!cellMoving)
            {
                cellMoving = true;
                //newInput = false;
                int row = dataGrid.Model.CurrencyManager.CurrentCell.RowIndex;
                int column = dataGrid.Model.CurrencyManager.CurrentCell.ColumnIndex;
                dataGrid.Model.CurrencyManager.CurrentCell.EndEdit();
                recalc();
                // don't loose your place.
                dataGrid.Model.CurrencyManager.MoveTo(row -1);
                cellMoving = false;
            }
        }

        private void dataGrid_CurrentCellMoving(object sender, Syncfusion.Windows.Controls.Grid.GridCurrentCellMovingEventArgs args)
        {

            try
            {
                validateRoundDecimal();
            }
            catch (Exception)
            {
                args.Cancel = true;
                args.Handled = true;
                System.Console.Beep(440, 100);
                //MessageBox.Show("Invalid Number Entered", "Validation Error" );
            }
        }

        private void validateRoundDecimal()
        {
                if (this.dataGrid.Model.CurrencyManager.CurrentCell.Renderer == null) { return; }
                if (this.dataGrid.Model.CurrencyManager.CurrentCell.RowIndex == 0) { return; }
                if (this.dataGrid.Model.CurrencyManager.CurrentCell.ColumnIndex == 0) { return; }
                string currentText = this.dataGrid.Model.CurrencyManager.CurrentCell.Renderer.ControlText;
                decimal.Parse(currentText);
        }

        private void dataGrid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F9)
            {
                GridCurrentCell currentCell = this.dataGrid.Model.CurrencyManager.CurrentCell;
                if (currentCell != null && currentCell.IsEditing)
                {
                    int currentRow = currentCell.RowIndex;
                    int currentColumn = currentCell.ColumnIndex;
                    string copyText = this.dataGrid.Model[(currentRow == 1 ? 1 : currentRow - 1), currentColumn].Text;
                    currentCell.Renderer.ControlText = copyText;
                    currentCell.EndEdit();
                    currentCell.MoveDown(1, true);
                    currentCell.BeginEdit();
                }

            }
        }

    }
}
