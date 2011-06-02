using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Syncfusion.Windows.Controls.Grid;
using DA.Common;
using log4net;
using Syncfusion.Windows.Shared;
using DA.DAIE.Common;

namespace DA.DAIE.COP
{

    /// <summary>
    /// Interaction logic for COPUpdateWindow.xaml
    /// </summary>
    public partial class COPUpdateWindow : Window
    {
        COPS cops = new COPS();
        ObservableCollection<COPSchedule> dropDownModel = new ObservableCollection<COPSchedule>();

        public COPS COPs
        {
            get { return this.cops; }
            set { this.cops = value; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(COPBO.MODULE_LOGGER_ERROR);
                throw;
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Unexpected Exception Loading COP Update Window", ex, COPBO.MODULE_LOGGER_ERROR);
                throw he;
            }
        }

        /// <summary>
        /// Constructing Window.
        /// </summary>
        public COPUpdateWindow()
        {
            InitializeComponent();
            SkinStorage.SetVisualStyle(groupBox1, App.VisualStyle);


            IList<DateTime> mktDays = DBSqlMapper.Instance().QueryForList<DateTime>("COP.MKTDAYS", null, COPBO.MODULE_NAME );
            List<string> days = new List<string>();
            foreach (DateTime dt in mktDays)
            {
                days.Add(dt.ToString("dd-MMM-yyyy"));
            }
            Binding binding2 = new Binding();
            binding2.Source = days;
            this.comboBoxDates.SetBinding(ComboBox.ItemsSourceProperty, binding2);


            IList<string> unitNames = DBSqlMapper.Instance().QueryForList<string>("COPModify.SelectMktUnitNames", null, COPBO.MODULE_NAME );
            unitNames.Insert(0, "{Unit}");
            Binding binding = new Binding();
            binding.Source = unitNames;
            this.comboBox1.SetBinding(ComboBox.ItemsSourceProperty, binding);

            
        }

        /// <summary>
        /// Tells how many COPs have pending updates
        /// </summary>
        /// <returns></returns>
        private int getUpdateCount()
        {
            int updateCount = 0;
            foreach (COP cop in cops)
            {
                // Should never hit this error since setting boolean values will always set this flag to 0 or 1.
                if (!cop.isValid())
                {
                    throw new HandledException("COP Failed Validation \n\r Valid values for RSC Committed are 0 and 1");
                }

                if (cop.isChanged())
                {
                    updateCount += 1;
                }
            }
            return updateCount;
        }


        private bool validatePriceCost(IList<COP> argCops)
        {
            int copCostId = -1;
            int copPriceId = -1;

            foreach (COP cop in argCops)
            {
                if (cop.DBCalcScheduleType.Equals("Price"))
                {
                    if (copPriceId == -1)
                    {
                        copPriceId = cop.UnitScheduleId;
                    }
                    else if (copPriceId != cop.UnitScheduleId)
                    {
                        return false;
                    }
                }
                else
                {
                    if (copCostId == -1)
                    {
                        copCostId = cop.UnitScheduleId;
                    }
                    else if (copCostId != cop.UnitScheduleId)
                    {
                        return false;
                    }
                }
            }
            return true;

        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            Cursor originalCursor = this.Cursor;

            ILog log = LogManager.GetLogger(COPBO.MODULE_LOGGER);
            log.Info("User Pressed Button Update on COP Update Window");

            dataGrid.Model.CurrencyManager.EndEdit();

            if (!validatePriceCost(cops))
            {
                string errorMessage = "COP Validation Failed For Price and Cost Multiple Schedules";
                log.Error(errorMessage);
                MessageBox.Show(errorMessage, "Validation Error");
                return;
            }

            string methodPurpose = "Update COP for Unknown Unit";

            try
            {
                this.Cursor = Cursors.Wait;
                methodPurpose = "Update COP UnitId:" + cops[0].UnitId;
                log.Info(methodPurpose + " Begin");
                int updateCount = getUpdateCount();
                if (updateCount == 0)
                {
                    MessageBox.Show("Nothing To Update", "Update Fail");
                    log.Warn(methodPurpose + " With Nothing to Update");
                }

                else
                {
                    DBSqlMapper.Instance().BeginTransaction(COPBO.MODULE_NAME);
                    foreach (COP cop in cops)
                    {

                        if (cop.isChanged())
                        {
                            DBSqlMapper.Instance().Insert("COPModify.InsertMktUnitPlan", cop, COPBO.MODULE_NAME);
                        }
                    }
                    DBSqlMapper.Instance().CommitTransaction(COPBO.MODULE_NAME);
                    log.Info(methodPurpose + " Completed " + updateCount.ToString() + " Rows Were Updated");
                    retrieveUnitCOP();
                    MessageBox.Show(updateCount + " Rows Were Updated", "Update Completed");
                }

            }
            catch (HandledException he)
            {
                if (DBSqlMapper.Instance().IsSessionStarted)
                {
                    DBSqlMapper.Instance().RollBackTransaction(COPBO.MODULE_NAME);
                }
                he.setLoggerNameLogExisting(COPBO.MODULE_LOGGER_ERROR);
                he.showError("Update COP Failed");
            }
            catch (Exception ex)
            {
                if (DBSqlMapper.Instance().IsSessionStarted)
                {
                    DBSqlMapper.Instance().RollBackTransaction(COPBO.MODULE_NAME);
                }
                HandledException he = new HandledException("Unexpected Exception Updating COP", ex, COPBO.MODULE_LOGGER_ERROR);
                he.showError("Update COP Failed");
            }
            finally
            {
                this.Cursor = originalCursor;
            }
                
        }

        private void ComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                if (comboBox1.SelectedIndex >= 0)
                {
                    LogManager.GetLogger(COPBO.MODULE_LOGGER).Info("User Selected Unit " + comboBox1.SelectedItem.ToString());
                    retrieveUnitCOP();
                }
            }
        }

        private void ComboBoxDates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LogManager.GetLogger(COPBO.MODULE_LOGGER).Info("User Selected Date " + comboBoxDates.SelectedItem.ToString());
                retrieveUnitCOP();
            }
        }

        private void retrieveUnitCOP()
        {
            Cursor originalCursor = this.Cursor;

            try
            {
                this.Cursor = Cursors.Wait;

                IList<DateTime> mktDays = DBSqlMapper.Instance().QueryForList<DateTime>("COP.MKTDAYS", null, COPBO.MODULE_NAME);
                DateTime mktDaySelected = DateTime.Today;
                string selectedDateString = comboBoxDates.SelectedItem.ToString();
                foreach (DateTime dt in mktDays)
                {
                    if (dt.ToString("dd-MMM-yyyy").Equals(selectedDateString))
                    {
                        mktDaySelected = dt;
                    }
                }


                ILog log = LogManager.GetLogger(COPBO.MODULE_LOGGER);
                log.Info("Retrieve Unit COP Begin");

                string unitShortNameSelected = comboBox1.SelectedItem.ToString();



                COPScheduleArg arg = new COPScheduleArg(unitShortNameSelected, mktDaySelected);
                IList<COP> copList = DBSqlMapper.Instance().QueryForList<COP>("COPModify.SelectMktUnitCOP", arg, COPBO.MODULE_NAME);
                if (App.CurrentPrincipal.IsInRole("Imp_Exp_User_ITAdmin"))
                {
                    foreach (COP cop in copList)
                    {
                        cop.Enabled = 1;
                    }
                }
                cops = new COPS(copList);

                this.dataGrid.Model.RowHeights.DefaultLineSize = 16;
                BindingOperations.ClearBinding(this.dataGrid, Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty);

                Binding binding2 = new Binding();
                binding2.Source = cops;
                binding2.ValidatesOnExceptions = true;
                this.dataGrid.SetBinding(Syncfusion.Windows.Controls.Grid.GridDataControl.ItemsSourceProperty, binding2);

                // retrieve Schedules for selected date and unit.
                dropDownModel = new COPSchedules(DBSqlMapper.Instance().QueryForList<COPSchedule>("COPModify.SelectUnitSchedules", arg, COPBO.MODULE_NAME));
                this.dataGrid.VisibleColumns[1].ColumnStyle.ItemsSource = dropDownModel;

                // make last column a checkbox. 
                //dataGrid.VisibleColumns[5].ColumnStyle.CellType = "CheckBox";
                //dataGrid.VisibleColumns[5].ColumnStyle.HorizontalAlignment = HorizontalAlignment.Center;
                //dataGrid.VisibleColumns[5].ColumnStyle.VerticalAlignment = VerticalAlignment.Center;

                log.Info("Retrieve Unit COP Complete");

            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(COPBO.MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("Retrieve Unit COP");
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Exception Retrieving Unit COP", ex, COPBO.MODULE_LOGGER_ERROR);
            }
            finally
            {
                this.Cursor = originalCursor;
            }
        }

        private void buttonReturn_Click(object sender, RoutedEventArgs e)
        {
            LogManager.GetLogger(COPBO.MODULE_LOGGER).Info("User Pressed Button Return on COP Update Window");
            this.Close();
        }


        private void dataGrid_currentCellValidating(object sender, Syncfusion.Windows.ComponentModel.SyncfusionCancelRoutedEventArgs args)
        {
            string selectedScheduleName = dataGrid.Model.CurrencyManager.CurrentCell.Renderer.ControlText;
            int row = this.dataGrid.Model.CurrencyManager.CurrentCell.RowIndex;

            if (row <= 0) 
            { args.Cancel = true; return; }

            Object selectedItem = ((GridDataControl)sender).SelectedItem;
            COP selectedCOP = (COP)selectedItem;

            foreach (COPSchedule cs in dropDownModel)
            {
                if (cs.Name.Equals(selectedScheduleName))
                {
                    selectedCOP.Name = selectedScheduleName;
                    //selectedCOP.DBCalcEcoMax = cs.EcoMax;
                    //selectedCOP.DBCalcEcoMin = cs.EcoMin;
                    selectedCOP.UnitScheduleId = cs.getID();
                    selectedCOP.DBCalcScheduleType = cs.Type ;

                }
            }

           

            this.dataGrid.Model.InvalidateCell(new Syncfusion.Windows.Controls.Cells.RowColumnIndex(row, 1));
            this.dataGrid.Model.InvalidateCell(new Syncfusion.Windows.Controls.Cells.RowColumnIndex(row, 2));
            this.dataGrid.Model.InvalidateCell(new Syncfusion.Windows.Controls.Cells.RowColumnIndex(row, 3));
            this.dataGrid.Model.InvalidateCell(new Syncfusion.Windows.Controls.Cells.RowColumnIndex(row, 4));
        }


        private void dataGrid_currentCellAcceptedChanges(object sender, Syncfusion.Windows.ComponentModel.SyncfusionRoutedEventArgs args)
        {
            int row = this.dataGrid.Model.CurrencyManager.CurrentCell.RowIndex;
            this.dataGrid.Model.Grid.CurrentCell.MoveTo(row, 4);
        }
    }

    public class COPSchedules : ObservableCollection<COPSchedule>
    {
        public COPSchedules(IList<COPSchedule> argSchedules)
        {
            foreach (COPSchedule schedule in argSchedules)
            {
                this.Add(schedule);
            }
        }
    }

  

}
