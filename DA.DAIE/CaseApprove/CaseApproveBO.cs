using System;
using System.Collections.Generic;
using System.Text;
using DA.DAIE.CaseSelection;
//using IBatisNet.DataMapper;
using DA.Common;
using System.Windows;
using DA.DAIE.PrintReport;
using DA.DAIE.Connections;
using DA.DAIE.Common;
using System.Windows.Input;

namespace DA.DAIE.CaseApprove
{
    class CaseApproveBO : AncestorBO
    {
        // Add quotes into Satellite Limit and Satellite Forecast Files.
        private static string z = "\"";

        /// <summary>
        /// 1) Show last action in the title of the passed window.
        /// 2) Log the last action
        /// 3) Conveniently return it so it can be used to assign a variable in the calling method in a single line.
        /// </summary>
        /// <param name="argLastAction"></param>
        /// <param name="argWindow"></param>
        /// <param name="argWindowTitleOriginal"></param>
        /// <returns></returns>
        private static string showLastAction( string argLastAction, Window argWindow, string argWindowTitleOriginal )
        {
            log.Info(argLastAction);
            argWindow.Title = argWindowTitleOriginal + " " + argLastAction;
            return argLastAction;
        }
   

        /// <summary>
        /// Identify Module for Logging
        /// </summary>
        static CaseApproveBO(){MODULE_NAME = "Approve";}
        //private const string WORKING_FOLDER = "WORKING_FOLDER/";
        /// <summary>
        /// Theres alot of logic on what to do if you encounter an already approved case.
        /// I want to remove this logic since we are dealing with a list of Unapproved Cases Only!!!
        /// 
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argMode"></param>
        /// <param name="argLabel"></param>
        public static void approveSCRACase(string argCaseId, MODES argMode, Window argWindow, bool argChangeCase )
        {
            string windowTitleOriginal = argWindow.Title;
            string lastAction = "";
            Cursor originalCursor = argWindow.Cursor;

            try
            {
                DBSqlMapper mapper = DBSqlMapper.Instance();
                argWindow.Cursor = Cursors.Wait;

                // You can skip this logic if we are only recreating approval files.
                if (argChangeCase)
                {
                    // 'Check to make sure the case is an SCRA case
                    lastAction = "Validate Case is SCRA";
                    validateCaseSCRA(argCaseId, mapper);


                    //'Check to make sure the case has not already been approved.
                    lastAction = "Check If Case is Already Approved";
                    bool isCaseApproved = SelectCaseApprovalStatus(argCaseId, mapper);


                    // If it is already approved, check if the user wishes to 're-approve'
                    lastAction = "Check Ok To Reapprove";
                    if (VerifyCaseApprovalStatus(argCaseId, isCaseApproved) == false)
                    {
                        return;
                    }

                    // Assume DRI Changes Are Now In Effect!
                    // 'Determines if the user wants to disable the RTPRP
                    lastAction = "Validate RTPRP";
                    validateRTPRP(argCaseId, mapper);
                    lastAction = showLastAction("Approve Case " + argCaseId, argWindow, windowTitleOriginal);

                    if (isCaseApproved)
                    {
                        // 'Do not try to approve a already approved case.
                    }
                    else
                    {
                        //'update mktplan for initunitplan flag
                        int updatedMarketPlanResult = mapper.Update("UpdateMktPlan", argCaseId, MODULE_NAME);

                        //'call mssdatapac.approvecase(with caseid as parameter)
                        //object approveCaseResult = mapper.Update("ProcedureApproveCase", argCaseId);
                        approveCase(argCaseId);
                    }

                    try
                    {
                        //'Update the schedule ID for any mitigated units
                        lastAction = showLastAction("Update Schedules Mitigated Units.", argWindow, windowTitleOriginal);
                        mapper.Insert("UpdateMitigatedUnits_InsertMktUnitPlan", argCaseId, MODULE_NAME);
                    }
                    catch (HandledException he)
                    {
                        he.showError("Case Was Approved, but Error Occured In " + lastAction);
                    }
                    catch (Exception ex)
                    {
                        HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                        he.showError("Case Was Approved, but Error Occured In " + lastAction);
                    }
                }

                SatelliteCaseHour firstLastHour = null;
                NetworkFolderMeta folderMeta = null;
                try
                {
                    // Returns MktHour for the first and last hours for case.
                    lastAction = "Get MktHour for First And Last Hours Of Case";
                    firstLastHour = getCaseHours(argCaseId);

                    lastAction = "Get NetworkFolderMeta";
                    folderMeta = ConnectionsParser.getNetworkFolderMeta("PowerFlowFolder", ConnectionsParser.CONNECTIONS_FILE_NAME);
                }
                catch (HandledException he)
                {
                    he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                    he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction);
                }


                try
                {
                    if (firstLastHour != null && folderMeta != null)
                    {
                        //'Create powerflow files
                        lastAction = showLastAction("Create CCU Power Flow Files.", argWindow, windowTitleOriginal);
                        IList<string> listCCU = mapper.QueryForList<string>("CreatePowerFlow_SelectCCU", argCaseId, MODULE_NAME);
                        listCCU.Add(" 9999");
                        writeCSVFile(listCCU, folderMeta, "UC" + firstLastHour.MktDay.ToString("MMdd") + ".dat", argChangeCase);

                        lastAction = showLastAction("Create ARD Power Flow File.", argWindow, windowTitleOriginal);
                        IList<string> listARD = mapper.QueryForList<string>("CreatePowerFlow_SelectARD", argCaseId, MODULE_NAME );
                        listARD.Add(" 9999");
                        writeCSVFile(listARD, folderMeta, "AR" + firstLastHour.MktDay.ToString("MMdd") + ".dat", argChangeCase);
                    }
                }
                catch (HandledException he)
                {
                    he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                    he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction);
                }

                if (argChangeCase)
                {
                    try
                    {
                        //'Export external interface LMPs to Transmart
                        lastAction = showLastAction("Export LMPs to Transmart.", argWindow, windowTitleOriginal);
                        exportToTransmart(argCaseId, mapper);

                        //'Set the case_id for MIS
                        lastAction = showLastAction("Send Case Info to MIS.", argWindow, windowTitleOriginal);
                        setMISCase(argCaseId, mapper);
                    }
                    catch (HandledException he)
                    {
                        he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction);
                    }
                    catch (Exception ex)
                    {
                        HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                        he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction);
                    }
                }

                //'Create reports for satellite web site.
                lastAction = showLastAction("Create Satellite Files.", argWindow, windowTitleOriginal);
                try
                {
                    publishSatelliteReports(argCaseId, mapper, firstLastHour, argWindow, windowTitleOriginal, argChangeCase);
                }
                catch (HandledException he)
                {
                    he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction );
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                    he.showError(argChangeCase ? "Case Was Approved, but " : "" + "Error Occured In " + lastAction );
                }

                argWindow.Title = windowTitleOriginal;
                string logMessage = "Case " + argCaseId + ( argChangeCase ? " has been approved." : " approval files created");
                MessageBox.Show(logMessage, argChangeCase ? "Case Approved" : "Approval Files Created", MessageBoxButton.OK, MessageBoxImage.Information);
                log.Info(logMessage);

            }
            catch (HandledException he)
            {
                he.addCustomStackMessage(" SCRA Case Was Not Approved. Exception Occured In " + lastAction  );
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException(" SCRA Case Was Not Approved. Exception Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
            }
            finally
            {
                argWindow.Title = windowTitleOriginal;
                argWindow.Cursor = originalCursor;
            }
            
        }

        /// <summary>
        /// 'Publish Satellite Reports
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="mapper"></param>
        /// <param name="firstLastHour"></param>
        /// <param name="argWindow"></param>
        /// <param name="argWindowTitleOriginal"></param>
        /// <param name="argChangeCase">false means we are debugging case approval files without changing db or exporting files.</param>
        
        private static void publishSatelliteReports(string argCaseId, DBSqlMapper mapper, SatelliteCaseHour firstLastHour, Window argWindow, string argWindowTitleOriginal, bool argChangeCase )
        {
            string lastAction = showLastAction("Publish Satellite Reports.", argWindow, argWindowTitleOriginal);
            FTPLoginMeta ftpLoginMeta = null;

            try
            {
                lastAction = "Get FTP Login Info for Satellite Files";
                ftpLoginMeta = ConnectionsParser.getFTPLogin("MISFTP", ConnectionsParser.CONNECTIONS_FILE_NAME);
            }
            catch (HandledException he)
            {
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }

            try
            {
                //'Purge old satellite export files.
                if( ftpLoginMeta != null && argChangeCase )
                {
                    lastAction = showLastAction("Purge Satellite Export Files", argWindow, argWindowTitleOriginal);
                    FTPUtil.ftpPurgeSatelliteFiles(firstLastHour.DBNow, ftpLoginMeta);
                }
            }
            catch (HandledException he)
            {
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }

            try
            {
                //'Create Satellite Exp Files.
                if( ftpLoginMeta != null )
                {
                    lastAction = showLastAction("Create Satellite Export Files", argWindow, argWindowTitleOriginal );
                    createSatelliteForecastFile(argCaseId, mapper, firstLastHour, ftpLoginMeta, argChangeCase);
                    createSatelliteLimitFile(argCaseId, mapper, firstLastHour, ftpLoginMeta, argChangeCase);
                }
            }
            catch (HandledException he)
            {
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }

            try
            {
                //'Create Satellite Reports.
                if( ftpLoginMeta != null )
                {
                    lastAction = showLastAction("Create Satellite Reports", argWindow, argWindowTitleOriginal );
                    PrintReportBO.saveReports("Satellite", argCaseId, App.WorkingFolder, firstLastHour, argWindow, argWindowTitleOriginal, argChangeCase);
                }
            }
            catch (HandledException he)
            {
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Error Occured In " + lastAction, ex, LOGGER_ERROR_NAME);
                he.showError("Case Was Approved, but Error Occured In " + lastAction);
            }


            
        }

        private static SatelliteCaseHour getCaseHours(string argCaseId)
        {
            SatelliteCaseHour caseHour = DBSqlMapper.Instance().QueryForObject<SatelliteCaseHour>("Satellite_SelectCaseHours", argCaseId, MODULE_NAME );
            return caseHour;
        }

        private static void createSatelliteLimitFile(string argCaseId, DBSqlMapper mapperMDB, SatelliteCaseHour firstLastHour, FTPLoginMeta ftpLoginMeta, bool argChangeCase)
        {

            string methodPurpose = "Create Satellite Limit File";
            log.Info(methodPurpose + " Begin");

            string dtmStart = firstLastHour.StartHourLabel;
            string dtmEnd = firstLastHour.EndHourLabel;
            string strHeader = dtmStart + ", " + dtmEnd;

            /// Formatting File Name for Satellite Limit.
            /// equivalent to Sysdate Formatted as 'yyyymmddhh24' 
            string fileVersionNumber = firstLastHour.DBNow.ToString("yyyyMMddHH");

            string fileShortName = "SL_000000000_" + firstLastHour.LocalDay.ToString("yyyyMMdd") + "_" + fileVersionNumber + ".CSV";
            string fileName = App.WorkingFolder + fileShortName;
            log.Info(methodPurpose + " fileName:" + fileName);

            log.Info(methodPurpose + " Unit Limits Begin....");
            IList<SatelliteLimitUnit> unitLimits = mapperMDB.QueryForList<SatelliteLimitUnit>("SatelliteLimit_Unit", argCaseId, MODULE_NAME);
            if (unitLimits == null || unitLimits.Count == 0)
            {
                throw new HandledException("No Unit Limit data for " + dtmStart, LOGGER_ERROR_NAME);
            }

            System.IO.StreamWriter sw;
            try
            {
                sw = System.IO.File.CreateText(fileName);
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable To Create or Overwrite Satellite Limit File: " + fileName, ex, LOGGER_ERROR_NAME);
            }

            int lineTotal = 0;
            sw.WriteLine(z + "C    " + z + "," + z + "OPERATING LIMIT DATA" + z + "," + z + strHeader + z );
            sw.WriteLine(z + "C    " + z + "," + z + fileShortName + z );
            sw.WriteLine(z + "H    " + z + "," + z + "Today's Unit Operating Limits" + z );
            sw.WriteLine(z + "H    " + z + "," + z + "Unitid" + z + "," + z + "Plant" + z + "," + z + "Name" + z + "," + z + "Limit Type" + z + "," + z + "Hour1" + z + "," + z + "Hour2" + z + "," + z + "Hour3" + z + "," + z + "Hour4" + z + "," + z + "Hour5" + z + "," + z + "Hour6" + z + "," + z + "Hour7" + z + "," + z + "Hour8" + z + "," + z + "Hour9" + z + "," + z + "Hour10" + z + "," + z + "Hour11" + z + "," + z + "Hour12" + z + "," + z + "Hour13" + z + "," + z + "Hour14" + z + "," + z + "Hour15" + z + "," + z + "Hour16" + z + "," + z + "Hour17" + z + "," + z + "Hour18" + z + "," + z + "Hour19" + z + "," + z + "Hour20" + z + "," + z + "Hour21" + z + "," + z + "Hour22" + z + "," + z + "Hour23" + z + "," + z + "Hour24" + z );

            string strOutputEcomax = null;
            string strOutputEcomin = null;
            string strOutputSS = null;
            

            SatelliteLimitUnit unitLimitPrior = null;
            foreach (SatelliteLimitUnit unitLimit in unitLimits)
            {
                // 'if new unit then write to strOutput
                if (unitLimitPrior != null && !unitLimit.UnitShortName.Equals(unitLimitPrior.UnitShortName))
                {
                    sw.WriteLine("D    ,        " + strOutputEcomax);
                    sw.WriteLine("D    ,        " + strOutputEcomin);
                    sw.WriteLine("D    ,        " + strOutputSS);
                    lineTotal += 3;
                }

                //' if first hour of day then initialize else add to strOutput
                if ( DateTime.Equals( unitLimit.MktDay, unitLimit.MktHour ))
                {
                    strOutputEcomin = unitLimit.UnitId.ToString() + "," + unitLimit.PlantShortName + "," + unitLimit.UnitShortName +  "," +  "2," + unitLimit.EcominCSV;
                    strOutputEcomax = unitLimit.UnitId.ToString() + "," + unitLimit.PlantShortName + "," + unitLimit.UnitShortName +  "," +  "1," + unitLimit.EcomaxCSV;
                    strOutputSS = unitLimit.UnitId.ToString() + "," + unitLimit.PlantShortName + "," + unitLimit.UnitShortName +  "," +  "3," + unitLimit.SelfScheduleCSV;
                }
                else
                {
                    strOutputEcomin = strOutputEcomin + "," + unitLimit.EcominCSV;
                    strOutputEcomax = strOutputEcomax + "," + unitLimit.EcomaxCSV;
                    strOutputSS = strOutputSS + "," + unitLimit.SelfScheduleCSV;
                }
                unitLimitPrior = unitLimit;
            }
            //'print last unit
            sw.WriteLine("D    ,        " + strOutputEcomax);
            sw.WriteLine("D    ,        " + strOutputEcomin);
            sw.WriteLine("D    ,        " + strOutputSS);
            lineTotal += 3;

            sw.WriteLine(z + "T    " + z + "," + z + lineTotal.ToString() + " Lines" + z );
            sw.WriteLine(z + "NOTE:     Limit Type Definition - 1=ecomax, 2=ecomin, 3=Self-Schedule/Basepoint" + z );

            sw.Close();

            if (argChangeCase)
            {
                FTPUtil.ftpUpload(fileName, ftpLoginMeta);
                System.IO.File.Delete(fileName);
            }
            log.Info(methodPurpose + ":" + fileName + " Complete");

        }

        /// <summary>
        /// 'Create the Satellite Forecast file and write to FTP site
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="mapperMDB"></param>
        /// <param name="argChangeCase">false means we are debugging case approval files without changing db or exporting files.</param>
        /// <returns></returns>
        private static void createSatelliteForecastFile(string argCaseId, DBSqlMapper mapperMDB, SatelliteCaseHour firstLastHour, FTPLoginMeta ftpLoginMeta, bool argChangeCase )
        {

            string methodPurpose = "Create Satellite Forecast File";
            log.Info(methodPurpose + " Begin");

            string dtmStart = firstLastHour.StartHourLabel;
            string dtmEnd = firstLastHour.EndHourLabel;
            string strHeader = dtmStart + ", " + dtmEnd;

            /// Formatting File Name for Satellite Forecast.
            /// equivalent to Sysdate Formatted as 'yyyymmddhh24' 
            string fileVersionNumber = firstLastHour.DBNow.ToString("yyyyMMddHH");

            string fileShortName = "SF_000000000_" + firstLastHour.LocalDay.ToString("yyyyMMdd") + "_" + fileVersionNumber + ".CSV";
            string fileName = App.WorkingFolder + fileShortName;
            
            log.Info(methodPurpose + " fileName:" + fileName );

            #region Unit Loadings 
            log.Info(methodPurpose + " Unit Loadings Begin....");
            IList<SatelliteForecastUnitCommitment> unitCommitments = mapperMDB.QueryForList<SatelliteForecastUnitCommitment>("SatelliteForecast_UnitCommitment", argCaseId, MODULE_NAME);
            if (unitCommitments == null || unitCommitments.Count == 0)
            {
                throw new HandledException("No SCRA results found for " + dtmStart, LOGGER_ERROR_NAME);
            }

            System.IO.StreamWriter sw;
            try
            {
                sw = System.IO.File.CreateText(fileName);
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable To Create or Overwrite Satellite Forecast File: " + fileName, ex, LOGGER_ERROR_NAME);
            }


            sw.WriteLine(z + "C    " + z + "," + z + "Satellite Unit Commitment Data" + z );
            sw.WriteLine(z + "C    " + z + "," + z + fileShortName + z );
            sw.WriteLine(z + "C    " + z + "," + z + "U/C Data, " + strHeader + z );
            sw.WriteLine(z + "H    " + z + "," + z + "Today 's Unit Commitment Schedule" + z );
            sw.WriteLine(z + "H    " + z + "," + z + "Unitid" + z + "," + z + "Plant" + z + "," + z + "NAME" + z + "," + z + "Hour1" + z + "," + z + "Hour2" + z + "," + z + "Hour3" + z + "," + z + "Hour4" + z + "," + z + "Hour5" + z + "," + z + "Hour6" + z + "," + z + "Hour7" + z + "," + z + "Hour8" + z + "," + z + "Hour9" + z + "," + z + "Hour10" + z + "," + z + "Hour11" + z + "," + z + "Hour12" + z + "," + z + "Hour13" + z + "," + z + "Hour14" + z + "," + z + "Hour15" + z + "," + z + "Hour16" + z + "," + z + "Hour17" + z + "," + z + "Hour18" + z + "," + z + "Hour19" + z + "," + z + "Hour20" + z + "," + z + "Hour21" + z + "," + z + "Hour22" + z + "," + z + "Hour23" + z + "," + z + "Hour24" + z );

            SatelliteForecastUnitCommitment prior = null;
            int totalLines = 0;
            StringBuilder concat = new StringBuilder();
            foreach (SatelliteForecastUnitCommitment row in unitCommitments)
            {
                // If you are on the first row, then start a string for the first unit.
                if( prior == null )
                {
                    concat.Append("D    ,        " + row.UnitId + "," + row.PlantShortName + "," + row.UnitShortName + "," + row.MWCSV);
                }
                //' if a new unit write previous units data
                // and then start the row for the next units hours
                else if( prior != null && !prior.UnitShortName.Equals(row.UnitShortName))
                {
                    sw.WriteLine(concat.ToString());
                    totalLines += 1;
                    concat = new StringBuilder();
                    concat.Append("D    ,        " + row.UnitId + "," + row.PlantShortName + "," + row.UnitShortName + "," + row.MWCSV);

                }
                // You are not on a new unit of any kind, so write the next hourly value for the unit you are already on.
                else
                {
                    concat.Append( "," + row.MWCSV );
                }
                prior = row;
            }
            sw.WriteLine(concat);
            totalLines += 1;
            concat = new StringBuilder();

            log.Info(methodPurpose + " Unit Loadings Complete....");
            #endregion Unit Loadings 

            #region Net Interchange
            //'Get Net Interchange Values by interface
            log.Info(methodPurpose + " Interchanges Begin....");

            sw.WriteLine(z + "H    " + z + "," + z + "SCHEDULED INTERCHANGE DATA" + z );
            sw.WriteLine(z + "H    " + z + "," + z + "Line" + z + "," + z + "Hour1" + z + "," + z + "Hour2" + z + "," + z + "Hour3" + z + "," + z + "Hour4" + z + "," + z + "Hour5" + z + "," + z + "Hour6" + z + "," + z + "Hour7" + z + "," + z + "Hour8" + z + "," + z + "Hour9" + z + "," + z + "Hour10" + z + "," + z + "Hour11" + z + "," + z + "Hour12" + z + "," + z + "Hour13" + z + "," + z + "Hour14" + z + "," + z + "Hour15" + z + "," + z + "Hour16" + z + "," + z + "Hour17" + z + "," + z + "Hour18" + z + "," + z + "Hour19" + z + "," + z + "Hour20" + z + "," + z + "Hour21" + z + "," + z + "Hour22" + z + "," + z + "Hour23" + z + "," + z + "Hour24" + z );
            IList<SatelliteForecastInterchange> interchanges = mapperMDB.QueryForList<SatelliteForecastInterchange>("SatelliteForecast_Interchange", argCaseId, MODULE_NAME);
            SatelliteForecastInterchange interchangePrior = null;
            foreach (SatelliteForecastInterchange interchange in interchanges)
            {
                // First Row
                // Start the next csv line, but nothing to write to file since this is the first db row processed.
                if( interchangePrior == null )
                {
                    concat = new StringBuilder("D    ,        " + interchange.Region + "," + interchange.InterchangeCSV );
                }
                // You are continuing a csv Line you already started.
                else if( interchangePrior.Region.Equals(interchange.Region))
                {
                    concat.Append("," + interchange.InterchangeCSV);
                }
                // You are starting a new csv line.
                // Write the existing data to file, and then start a new csv line.
                else
                {
                    sw.WriteLine(concat.ToString());
                    concat = new StringBuilder("D    ,        " + interchange.Region + "," + interchange.InterchangeCSV);
                    totalLines += 1;
                }
                interchangePrior = interchange;
            }
            // output last interchange to the file.
            if (interchanges.Count > 0)
            {
                sw.WriteLine(concat.ToString());
                concat = new StringBuilder();
                totalLines += 1;
            }

            log.Info(methodPurpose + " Interchanges Complete....");
            #endregion Net Interchange

            #region Load
            log.Info(methodPurpose + " Load Begin....");
            
            IList<SatelliteForecastLoad> loads = mapperMDB.QueryForList<SatelliteForecastLoad>("SatelliteForecast_Load", argCaseId, MODULE_NAME);
            if (loads.Count == 0)
            {
                throw new HandledException("No Load Data for " + dtmStart);
            }
            else
            {
                sw.WriteLine(z + "H    " + z + "," + z + "FORECAST LOAD DATA" + z );
                sw.WriteLine(z + "H    " + z + "," + z + "Hour1" + z + "," + z + "Hour2" + z + "," + z + "Hour3" + z + "," + z + "Hour4" + z + "," + z + "Hour5" + z + "," + z + "Hour6" + z + "," + z + "Hour7" + z + "," + z + "Hour8" + z + "," + z + "Hour9" + z + "," + z + "Hour10" + z + "," + z + "Hour11" + z + "," + z + "Hour12" + z + "," + z + "Hour13" + z + "," + z + "Hour14" + z + "," + z + "Hour15" + z + "," + z + "Hour16" + z + "," + z + "Hour17" + z + "," + z + "Hour18" + z + "," + z + "Hour19" + z + "," + z + "Hour20" + z + "," + z + "Hour21" + z + "," + z + "Hour22" + z + "," + z + "Hour23" + z + "," + z + "Hour24" + z );
                
                foreach (SatelliteForecastLoad load in loads)
                {
                    if (concat.Length > 0)
                    {
                        concat.Append(",");
                    }
                    concat.Append(load.DemandForecastMWCSV);
                }
                sw.WriteLine("D    ,        " + concat.ToString());
                sw.WriteLine("T    ,        " + (totalLines).ToString() + " Lines ");
            }
            log.Info(methodPurpose + " Load Complete....");
            #endregion Load

            sw.Close();

            if (argChangeCase)
            {
                FTPUtil.ftpUpload(fileName, ftpLoginMeta);
                System.IO.File.Delete(fileName);
            }
            log.Info(methodPurpose + ":" + fileName + " Complete");
        }


        /// <summary>
        /// Overwrites a CSV file with specified name
        /// Creates file from generic IList<object> passed from QueryForObject<object>
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="fileName"></param>
        /// <param name="argChangeCase">false means we are debugging case approval files without changing db or exporting files.</param>
        private static void writeCSVFile(IList<string> inputData, NetworkFolderMeta argNetworkFolder, string fileName, bool argChangeCase)
        {
            try
            {
                System.IO.StreamWriter sw = System.IO.File.CreateText( App.WorkingFolder + fileName);

                foreach (String fileLine in inputData)
                {
                    sw.WriteLine(fileLine);
                }
                sw.Close();
                if (argChangeCase)
                {
                    System.IO.File.Copy(App.WorkingFolder + fileName, argNetworkFolder.getFullName(fileName), true);
                    System.IO.File.Delete(App.WorkingFolder + fileName);
                }
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable to save file: " + fileName, ex);
            }

        }


        private static void setMISCase(string argCaseId, DBSqlMapper mapperMDB)
        {
            DateTime caseBeginDate = mapperMDB.QueryForObject<DateTime>("SetMISCase_SelectCaseStartHour", argCaseId, MODULE_NAME);
            RAACaseArg arg = new RAACaseArg(argCaseId, caseBeginDate);
            int countMIS = (int)mapperMDB.QueryForObject<decimal>("SetMISCase_SelectCountRAACase", null, MODULE_NAME);

            if (countMIS == 0)
            {
                mapperMDB.Insert("SetMISCase_InsertRAACase", arg, MODULE_NAME);
            }
            else if (countMIS == 1)
            {
                mapperMDB.Insert("SetMISCase_UpdateRAACase", arg, MODULE_NAME);
            }
            else
            {
                HandledException he = new HandledException("Unexpected number of records encountered on isogateway.raa_case_t.");
                he.LoggerName = LOGGER_ERROR_NAME + ".setMISCase";
                throw he;
            }

        }

        /// <summary>
        /// '****************************************************************
        /// '*
        /// '* Transfers the external interface LMPs to Transmart
        /// '*
        /// '****************************************************************
        /// </summary>
        /// <param name="argCaseId"></param>
        private static void exportToTransmart(string argCaseId, DBSqlMapper mapperMDB)
        {
            
            try
            {
                log.Info("Begin transfer of LMP data to Transmart for case " + argCaseId);

                // connect to the EES database specified in SqlMapEES.xml.
                // In more detail... 
                // value of attribute dataSource name is mapped to a section of file Connections.xml
                // Logic to map SqlMaps to Connections.xml is in 
                //   IBatisNet.DataMapper.Configuration.DomSqlMapBuilder.Initialize()
                //   in this method there is a call that now passes database providers. 
                //   modified method... -> DataSourceDeSerializer.Deserialize( nodeDataSource, _configScope.Providers );
                CustomMapper customMapper = new CustomMapper();
                DBSqlMapper mapperEES = customMapper.getMapper("SqlMapEES.config");

                //'Get the interface data from the MDB
                IList<InterfaceLMP> interfaceLMPFromMDB = mapperMDB.QueryForList<InterfaceLMP>("ExportToTransmart_SelectInterfaceLMP", argCaseId, MODULE_NAME );

                foreach (InterfaceLMP row in interfaceLMPFromMDB)
                {
                    mapperEES.Insert("EES_ExportToTransmart_InsertInterfaceLMP", row, MODULE_NAME);
                }

                log.Info("Transfer of LMP data completed successfully.");
            }
            catch (HandledException he)
            {
                log.Info("An error occurred while transferring the Interface LMPs to TransMart." + he.Message);
                he.addCustomStackMessage("An error occurred while transferring the Interface LMPs to TransMart.");
                throw;
            }
            catch (Exception ex)
            {
                log.Info("An error occurred while transferring the Interface LMPs to TransMart." + ex.Message);
                HandledException he = new HandledException("An error occurred while transferring the Interface LMPs to TransMart.", ex);
                throw he;
            }
        }

        /// <summary>
        /// 'Check to make sure the case is an SCRA case
        /// If not, a HandledException will be thrown.
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argMapper"></param>
        private static void validateCaseSCRA(string argCaseId,  DBSqlMapper argMapper)
        {
            String caseStudyMode = argMapper.QueryForObject<String>("ValidateCaseType_SelectCaseStudyMode", argCaseId, MODULE_NAME);
            if (caseStudyMode == null || !caseStudyMode.ToLower().Equals("scra"))
            {
                throw new HandledException("Validation Error.  The selected case is not an SCRA case", LOGGER_ERROR_NAME);
            }
        }

        /// <summary>
        /// Returns True If the Case Is Already Approved.
        /// </summary>
        private static bool SelectCaseApprovalStatus(string argCaseId, DBSqlMapper argMapper)
        {
            try
            {
                String caseStatus = argMapper.QueryForObject<string>("ValidateApprovalStatus_SelectCaseState", argCaseId, MODULE_NAME);
                if (caseStatus != null && caseStatus.ToLower().Equals("approved"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (HandledException he)
            {
                he.addCustomStackMessage("Unable To Select Approval Status for Case:" + argCaseId);
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable To Select Approval Status for Case:" + argCaseId, ex, LOGGER_ERROR_NAME);
            }
        }

        /// <summary>
        /// '****************************************************************
        /// '*
        /// '* Determines if the currently selected case has been approved.
        /// '* Gives the user the option to approve the case again.  If the
        /// '* case has not been approved or the user wishes to reapprove
        /// '* the selected case, this function returns a value of true.
        /// '* Otherwise the function returns a value of false.
        /// '*
        /// '****************************************************************
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argCaseApprovalStatus"></param>
        /// <returns></returns>
        private static bool VerifyCaseApprovalStatus(string argCaseId, bool argCaseApprovalStatus)
        {
            try
            {
                if (argCaseApprovalStatus)
                {
                    MessageBoxResult mbr = MessageBox.Show("Do you want to approve this case again?"
                        , "The selected case has alreay been approved", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    bool doesUserWantToReapprove = mbr.Equals(MessageBoxResult.Yes);

                    log.Info(doesUserWantToReapprove ?
                        "User selected to reapprove case " + argCaseId :
                        "User selected NOT to reapprove case " + argCaseId);

                    return doesUserWantToReapprove;
                }
                else
                {
                    return true;
                }
            }
            catch( HandledException he )
            {
                he.addCustomStackMessage( "Unable To Verify Approval Status With User for Case:" + argCaseId );
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable To Verify Approval Status With User for Case:" + argCaseId, ex, LOGGER_ERROR_NAME);
            }
        }


        /// <summary>
        /// Looks for a threshold for real time price response.
        /// If a threshold is found then the user is asked if they want to disable 
        /// the Real Time Price Response Program.
        /// 
        /// yes:
        ///     Set the disable flag to true.  
        ///     True will translate to 'Y' in the db.
        ///     Update any existing rows to 'Y'
        ///     If the rows are not there, then insert them with 'Y'.
        ///     
        /// No:
        ///     Set the disable flag to false.  
        ///     True will translate to 'N' in the db.
        ///     Update any rows to 'N'
        ///     If the rows are not there, then do nothing.
        ///     
        /// </summary>
        /// <param name="argCaseId"></param>
        private static void validateRTPRP(string argCaseId, DBSqlMapper argMapper)
        {
            try
            {
                decimal? thresholdLMPObject = argMapper.QueryForObject<decimal?>("ValidateRealTimePriceResponse_SelectThreshold", argCaseId, MODULE_NAME);

                if (thresholdLMPObject.HasValue)
                {
                    decimal thresholdLMP = thresholdLMPObject.Value;
                    MessageBoxResult mbr = MessageBox.Show(
                        "This case may have prices greater than " + thresholdLMP.ToString() + "$." +
                        "Do you want to DISABLE Real Time Price Response Program?",
                        "Confirm Disable Real Time Price Response Program",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    int rowCount = (int)argMapper.QueryForObject<decimal>("ValidateRealTimePriceResponse_Count", argCaseId, MODULE_NAME);

                    // If user answers yes, then set the disable flag to true.  
                    // True will translate to 'Y' in the db.
                    // If the rows are not there, then insert them.
                    if (mbr.Equals(MessageBoxResult.Yes))
                    {
                        log.Info("User Selected Yes - For Disable Real Time Price Respone Program On Case: " + argCaseId);
                        UpdateRTPRDisabledCaseListArg arg = new UpdateRTPRDisabledCaseListArg(argCaseId, true);
                        if (rowCount == 0)
                        {
                            argMapper.Insert("ValidateRealTimePriceResponse_InsertDisabledCaseList", arg, MODULE_NAME);
                        }
                        else
                        {
                            argMapper.Update("ValidateRealTimePriceResponse_UpdateDisabledCaseList", arg, MODULE_NAME);
                        }

                    }

                    // If user answers No, then set the disable flag to true.  
                    // True will translate to 'Y' in the db.
                    // If the rows are not there, then do nothing.
                    else
                    {
                        log.Info("User Selected No - For Disable Real Time Price Respone Program On Case: " + argCaseId);
                        if (rowCount > 0)
                        {
                            UpdateRTPRDisabledCaseListArg arg = new UpdateRTPRDisabledCaseListArg(argCaseId, false);
                            argMapper.Update("ValidateRealTimePriceResponse_UpdateDisabledCaseList", arg, MODULE_NAME);
                        }
                    }
                }
            }
            catch (HandledException he)
            {
                he.addCustomStackMessage("Disable Real Time Price Response Program for caseid: " + argCaseId);
                throw;
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Disable Real Time Price Response Program for caseid: " + argCaseId, ex);
                throw;
            }
        }


        /// <summary>
        /// Calls A Stored Procedure To Approve The Case.
        /// A single parameter is passed for CaseId.
        /// Value object of type CaseApproveArg is passed.

        /// Note: Stored Procedure Paremeters must be mapped to an object with a ParameterMap.
        ///       The Actual Mapping is in Sql/CaseApproveMap.xml
        /// 
        /// Prior Comments from VB Code.
        ///   'Build the ADO Command
        ///   'Add the parameter
        ///   'Execute the procedure
        /// <summary>
        private static void approveCase(string argCaseId)
        {
            string methodPurpose = "Approve Case";

            try
            {
                log.Info("Execute Case with CaseId = " + (argCaseId == null || argCaseId.Length == 0 ? "NULL" : argCaseId));
                CaseApproveArg arg = new CaseApproveArg(argCaseId);
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                DBSqlMapper.Instance().Insert("ProcedureApproveCase", arg, MODULE_NAME);
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
            }
            catch (HandledException he)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                he.setLoggerNameLogExisting(LOGGER_ERROR_NAME);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                throw new HandledException(methodPurpose, ex, LOGGER_ERROR_NAME);
            }

        }


    }
}
