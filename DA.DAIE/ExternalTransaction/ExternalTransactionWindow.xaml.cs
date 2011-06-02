using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Syncfusion.Windows.Controls.Grid;
using log4net;
using DA.Common;
using System.Windows.Media;
using Syncfusion.Windows.Shared;
using System.Windows.Input;

namespace DA.DAIE.ExternalTransaction
{
    /// <summary>
    /// Interaction logic for ExternalTransactionWindow.xaml
    /// </summary>
    public partial class ExternalTransactionWindow : Window
    {
        private static string MODULE_NAME = ExternalTransactionBO.MODULE_NAME;
        private static string MODULE_LOGGER = ExternalTransactionBO.MODULE_LOGGER;
        private static string MODULE_LOGGER_ERROR = ExternalTransactionBO.MODULE_LOGGER_ERROR;

        private String _CaseId;
        private String _CaseName;
        private IList<String> _InterfaceList;
        private TransactionDetails _TransactionDetailList;

        // This property is used in a GUI binding
        public String CaseName
        {
            get { return this._CaseName; }
        }

        // This property is used in a GUI binding
        public String CaseId
        {
            get { return this._CaseId; }
        }

        // This property is used in a GUI binding
        public IList<string> InterfaceList
        {
            get { return this._InterfaceList; }
        }

        // This property is used in a GUI binding
        public TransactionDetails TransactionDetailList
        {
            get { return this._TransactionDetailList; }
            set { this._TransactionDetailList = value; }
        }

        public ExternalTransactionWindow()
        {
            InitializeComponent();
        }

        public ExternalTransactionWindow(string argCaseId, String argCaseName )
        {
            this._CaseId = argCaseId;
            this._CaseName = argCaseName;
            this._InterfaceList = ExternalTransactionBO.getExternalInterfaceList();
            InitializeComponent();
            SkinStorage.SetVisualStyle(groupBox1, App.VisualStyle);
            this.dataGrid.Model.Options.ColumnSizer = GridControlLengthUnitType.AutoWithLastColumnFill;
            this.dataGrid.Model.RowHeights.DefaultLineSize = 16;
        }

        private void comboBoxSelectInterface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged();
        }

        private void SelectionChanged()
        {
            string interfaceName = "";
            try
            {

                ILog log = LogManager.GetLogger(MODULE_LOGGER);
                interfaceName = comboBoxSelectInterface.SelectedValue.ToString();
                log.Info("Select Interface Selection Changed on Case: " + _CaseId + " Selected Interface = " + interfaceName);
                _TransactionDetailList = ExternalTransactionBO.getExternalTransactionDetailList(_CaseId, interfaceName);

                Binding binding2 = new Binding();
                binding2.Source = _TransactionDetailList;
                binding2.ValidatesOnExceptions = true;
                this.dataGrid.SetBinding(Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty, binding2);
                //this.dataGrid.Model.HeaderStyle.Foreground = new SolidColorBrush(Colors.White);


                this.dataGrid.Height = (this.dataGrid.Model.RowCount) * 16 + 5;
                this.dataGrid.Model.Options.AllowSelection = GridSelectionFlags.Cell;

                this.labelTotal.Content = _TransactionDetailList.FixedMWTotal().ToString();

                log.Info("Select Interface Selection Changed Complete");
            }
            catch (HandledException he)
            {
                he.showError("Error Retrieving Interface Details for " + interfaceName);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Unexpected Exception Occured", ex, MODULE_LOGGER_ERROR);
                he.showError("Error Retrieving Interface Details for " + interfaceName);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.comboBoxSelectInterface.Items.Count > 0)
            {
                this.comboBoxSelectInterface.SelectedIndex = 0;
            }
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            ILog log = LogManager.GetLogger(MODULE_LOGGER);
            log.Info("User Pressed Update Button");
            

            try
            {
                validateRoundDecimal(this.dataGrid );
            }
            catch (Exception)
            {
                System.Console.Beep(440, 100);
                return;
            }

            this.dataGrid.Model.CurrencyManager.EndEdit();
            if (ExternalTransactionBO.IsDirty(_TransactionDetailList))
            {
                Cursor originalCursor = this.Cursor;
                this.Cursor = Cursors.Wait;
                try
                {
                    string interfaceName = comboBoxSelectInterface.SelectedValue.ToString();
                    // method below is already logged.
                    ExternalTransactionBO.saveExternalTransactionDetailList(_CaseId, interfaceName, _TransactionDetailList);
                    SelectionChanged();
                }
                catch (HandledException he)
                {
                    he.showError("Update External Transaction Failed");
                }
                finally
                {
                    this.Cursor = originalCursor;
                }
            }
            else
            {
                log.Warn("User Pressed Update Button with Nothing to Save");
                MessageBox.Show("No Changes Were Found", "Save Failed");
            }
        }

        private void buttonReturn_Click(object sender, RoutedEventArgs e)
        {
            ILog log = LogManager.GetLogger(MODULE_LOGGER);
            if (ExternalTransactionBO.IsDirty(_TransactionDetailList))
            {
                MessageBoxResult mbr = MessageBox.Show("Select 'OK' If You Wish To Abandon Your Changes", "Confirm Close Without Saving", MessageBoxButton.OKCancel);
                if (mbr == MessageBoxResult.Cancel)
                {
                    log.Info("User Canceled Return for CaseId:" + _CaseId);
                    return;
                }
                else
                {
                    log.Info("User Abandoned Changes for CaseId:" + _CaseId);
                }
            }
            else
            {
                log.Info("Return Pressed With No Changes To Save");
            }
            this.Close();
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
                TextBox tb = ((TextBox)this.dataGrid.Model.CurrencyManager.CurrentCell.Renderer.CurrentCellUIElement);
                
                string preString = "";
                string midString = e.Text;
                string postString = "";
                if (tb.SelectionStart > 0)
                {
                    preString = tb.Text.Substring(0, tb.SelectionStart);
                }
                if (tb.SelectionStart + tb.SelectionLength < tb.Text.Length)
                {
                    postString = tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);
                }
                string text = preString + midString + postString;
                if (text.Equals("-")) { return; }

                decimal value = decimal.Parse(text);
             }
            catch (Exception)
            {
                e.Handled = true;
                System.Console.Beep(440, 100);
            }
        }

        private void dataGrid_CurrentCellMoving(object sender, Syncfusion.Windows.Controls.Grid.GridCurrentCellMovingEventArgs args)
        {

            try
            {
                validateRoundDecimal(this.dataGrid);
                //if (this.dataGrid.Model.CurrencyManager.CurrentCell.Renderer == null) { return; }
                //if (this.dataGrid.Model.CurrencyManager.CurrentCell.Na
                //string currentText = this.dataGrid.Model.CurrencyManager.CurrentCell.Renderer.ControlText;
                //decimal dv = decimal.Parse(currentText);
                //decimal dv_rounded = Decimal.Round(dv, 1);
                //this.dataGrid.Model.CurrencyManager.CurrentCell.Renderer.ControlText = dv_rounded.ToString();
                ////args.Handled = true;
                
            }
            catch (Exception)
            {
                args.Cancel = true;
                args.Handled = true;
                System.Console.Beep(440, 100);
            }
        }

        private void validateRoundDecimal(GridDataControl argGridDataControl)
        {
            if (argGridDataControl.Model.CurrencyManager.CurrentCell.Renderer == null) { return; }
            if (argGridDataControl.Model.CurrencyManager.CurrentCell.ColumnIndex == 0) { return; }
            if (argGridDataControl.Model.CurrencyManager.CurrentCell.RowIndex == 0) { return; }
            string currentText = argGridDataControl.Model.CurrencyManager.CurrentCell.Renderer.ControlText;
            decimal dv = decimal.Parse(currentText);
            decimal dv_rounded = Decimal.Round(dv, 1);
            argGridDataControl.Model.CurrencyManager.CurrentCell.Renderer.ControlText = dv_rounded.ToString();
        }

        private void dataGrid_CurrentCellMoved(object sender, GridCurrentCellMovedEventArgs args)
        {
            this.labelTotal.Content = _TransactionDetailList.FixedMWTotal().ToString();
            dataGrid.Model.CurrencyManager.CurrentCell.BeginEdit();
        }

        private void dataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F9)
            {
                GridCurrentCell currentCell = this.dataGrid.Model.CurrencyManager.CurrentCell;
                if (currentCell != null && currentCell.IsEditing )
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






