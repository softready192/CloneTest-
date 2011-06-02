using System;
using System.Collections.Generic;
using DA.DAIE.CaseSelection;
using DA.DAIE.RSCConstraint;
using System.IO;
using DA.Common;
using System.Windows;
using Microsoft.Win32;
using System.Reflection;
using log4net;
using DA.DAIE.Common;
using DA.DAIE.Connections;

namespace DA.DAIE.FileUpload
{
    public class GRTConstraintBO
    {

        internal static string MODULE_NAME = "GRTConstraint";
        internal static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;

        /// <summary>
        /// 'If the program is in RA mode and it is not during the DA period then disable any RSC constraints.
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argMode"></param>
        internal static void cleanupRSCConstraints(string argCaseId, MODES argMode)
        {
            string methodPurpose = "Cleanup RSC Constraints";
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER);
                log.Info(methodPurpose + " Begin");
                if (OKToDisable(argMode))
                {
                    if (!RSCConstraintBO.CleanupRSCConstraints(argCaseId, true))
                    {
                        string logMessage = "An error occurred while disabling RSC constraints from previous cases.";
                        log.Info(logMessage);
                        MessageBox.Show(logMessage, "Constraints Were Not Disabled", MessageBoxButton.OK);
                    }
                }
                log.Info(methodPurpose + " Complete");
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw he;
            }
            catch (Exception ex)
            {
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
        }

        internal static void LoadConstraints(IList<string> argGRTFileNames, string argCaseId, MODES argMode)
        {
            DBSqlMapper mapper = DBSqlMapper.Instance();
            string methodPurpose = "Load Constraints";
            List<HandledException> errorList = new List<HandledException>();
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER);
                log.Info(methodPurpose + " Begin");
                log.Info(methodPurpose + " " + argGRTFileNames.Count.ToString() + " Files To Upload");
                mapper.BeginTransaction(MODULE_NAME);
                //'Delete any generic constraints for the selected case.
                mapper.Delete("DeleteMktCaseConstraint", argCaseId, MODULE_NAME);

                //'Read all files and write to DB
                foreach (string fileName in argGRTFileNames)
                {
                    errorList.AddRange ( readGRTFile(argCaseId, fileName, argMode));
                }
                if (errorList.Count == 0)
                {
                    mapper.CommitTransaction(MODULE_NAME);
                    log.Info(methodPurpose + " Complete");
                }
                else
                {
                    mapper.RollBackTransaction(MODULE_NAME);
                    string errorString = "Errors Occured:\n\r";
                    foreach( HandledException he in errorList )
                    {
                        errorString += he.Message;
                    }
                    MessageBox.Show(errorString);
                }

            }
            catch (HandledException he)
            {
                mapper.RollBackTransaction(MODULE_NAME);
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose + " Changes Rolled Back");
                throw he;
            }
            catch (Exception ex)
            {
                mapper.RollBackTransaction(MODULE_NAME);
                throw new HandledException(methodPurpose + " Changes Rolled Back", ex, MODULE_LOGGER_ERROR);
            }
        }

        /// <summary>
        /// Reads values for hours specified in MktHourRow[] argStudyDayHours
        /// GRTLimit Files are for 24 hours regardless of long and short days.
        /// Long day:
        ///     argStudyDayHours should have 2 hours with local hour = 1 ( hour label starts with "02" );
        /// Short day:
        ///     hour ending "03" from the file will be skipped.
        /// 
        /// The passed MktHours will contain local hour ending strings to match on.
        /// 
        /// </summary>
        /// <param name="studyDayHours"></param>
        /// <param name="grtFileName"></param>
        /// <param name="argGRTLimitMapping"></param>
        /// <param name="argGRTLimit"></param>
        /// 
        private static List<HandledException> readGRTFile(string argCaseId, string grtFileName, MODES argMode)
        {
            string methodPurpose = "Read constraint data from file " + grtFileName;
            List<HandledException> errorList = new List<HandledException>();
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER);
                log.Info(methodPurpose + " Begin");
                string line = "";
                string priorGRTName = "-initialGRTName-";
                bool usedGRTName = false;

                Int64 constraintId = -1;
                CaseHourBO caseHours = new CaseHourBO(argCaseId);


                StreamReader inFile = new StreamReader(grtFileName);
                bool firstLine = true;
                DateTime[] gmtEffectiveHours;

                while ((line = inFile.ReadLine()) != null)
                {
                    GRTConstraint constraint = null;
                    try
                    {
                        constraint = new GRTConstraint(argCaseId, line, grtFileName);
                    }
                    catch (HandledException parseException)
                    {
                        errorList.Add(parseException);
                        continue;
                    }

                    if (firstLine)
                    {
                        if (!caseHours.VerifyFileDate(constraint.EffectiveDate))
                        {
                            throw new HandledException("The date on the GRT file does not match the market day of the selected case.", 50001);
                        }
                        firstLine = false;
                    }

                    string grtName = constraint.Name;
                    if (!grtName.Equals(priorGRTName))
                    {
                        decimal? dddd = DBSqlMapper.Instance().QueryForObject<decimal?>("SelectConstraintId", grtName, MODULE_NAME);
                        //Double? dbl = Mapper.Instance().QueryForObject<Double?>("SelectConstraintId", grtName);
                        if (dddd != null)
                        {
                            constraintId = (Int64)(dddd);
                            usedGRTName = true;
                        }
                        else
                        {
                            constraintId = -1;
                            usedGRTName = false;
                            log.Warn("Constraint, " + grtName + " not found on database.  Skipping to next record.");
                        }
                        priorGRTName = grtName;
                    }

                    if (usedGRTName && errorList.Count == 0 )
                    {
                        // 'If the current record is for hour three of the first day of daylight savings time skip
                        // 'this record.  Otherwise write it to the database
                        gmtEffectiveHours = caseHours.localToGMT(constraint.EffectiveHour, constraint.EffectiveDate);
                        foreach( DateTime gmtEffectiveHour in gmtEffectiveHours )
                        {
                            constraint.ConstraintId = constraintId;
                            constraint.GMTEffectiveDateTime = gmtEffectiveHour;
                            DBSqlMapper.Instance().Insert("InsertMktCaseConstraint", constraint, MODULE_NAME);
                        }
                    }
                }
                if (errorList.Count == 0)
                {
                    log.Info(methodPurpose + " Complete");
                }
                else
                {
                    log.Info(methodPurpose + " Errors Occured");
                }
                return errorList;
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
        }

        /// <summary>
        /// '****************************************************************
        /// '*
        /// '* If the selected case is not a Day Ahead case and the current
        /// '* time is not during the period in which the Day Ahead case is
        /// '* run, then it is OK to disable the RSC constraints.
        /// '*
        /// '****************************************************************
        /// </summary>
        /// <param name="argMode"></param>
        /// <returns></returns>
        private static bool OKToDisable(MODES argMode)
        {

            string methodPurpose = "Checking If Ok To Disable RSC Constraints";
            try
            {
                // 'If the program is in RAA mode then keep checking otherwise it is NOT OK to disable the constraints.
                if (argMode.Equals(MODES.RAA_SCRA))
                {
                    //'If the current hour is greater than or equal to the market closing hour then then keep checking
                    // Otherwise it IS OK to disable the constraints
                    decimal hoursTillClose = DBSqlMapper.Instance().QueryForObject<decimal>("OKToDisable.SelectCurrentHour", null, MODULE_NAME);
                    if (hoursTillClose <= 0)
                    {
                        // 'If there is not an approved Day Ahead case then it is NOT OK to disable the constraints,
                        // 'otherwise it IS OK to disable them.
                        // Translation...
                        // If there is an approved case, you can disable the constraints.
                        int caseCount = (int)DBSqlMapper.Instance().QueryForObject<decimal>("OKToDisable.SelectApprovedDACaseExistsMktDay", null, MODULE_NAME);
                        if (caseCount > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
        }


        
        /// <summary>
        /// Let user select a list of up to 10 GRT files to upload.
        /// </summary>
        /// <returns>IList string of FileNames.</returns>
        static internal IList<string> selectGRTFileNames()
        {
            string methodPurpose = "Select List Of GRT Filenames for Upload";
            try
            {
                int intFileCountMax = 10;
                int intFileCount = 0;
                bool getAnotherFile = true;

                List<string> fileList = new List<string>();

                while (getAnotherFile)
                {
                    string selectedFileName = selectGRTFileName();
                    if (selectedFileName != null && selectedFileName.Length > 0)
                    {
                        LogManager.GetLogger(MODULE_LOGGER).Info("Locational reserve file " + selectedFileName + " was selected.");
                        fileList.Add(selectedFileName);
                        intFileCount += 1;
                    }
                    else
                    {
                        LogManager.GetLogger(MODULE_LOGGER).Info("No locational reserve file was selected.");
                    }

                    if (intFileCount < intFileCountMax)
                    {
                        MessageBoxResult userMoreFiles = MessageBox.Show(
                            "Do you want to upload additional constraint files?",
                            "Upload another file?",
                            MessageBoxButton.YesNo);
                        getAnotherFile = userMoreFiles.Equals(MessageBoxResult.Yes);
                    }
                    else
                    {
                        getAnotherFile = false;
                    }
                }
                return fileList;
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw he;
            }
            catch (Exception ex)
            {
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
        }

        /// <summary>
        /// Private Method.
        /// Selects a single GRT file for upload.
        /// </summary>
        /// <returns></returns>
        internal static string selectGRTFileName()
        {
            string methodPurpose = "Select GRT File Name";
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.DefaultExt = ".csv";
                dlg.Filter = "CSV documents (.csv)|*.csv";

                NetworkFolderMeta folderMeta = ConnectionsParser.getNetworkFolderMeta("GRTFolder", ConnectionsParser.CONNECTIONS_FILE_NAME);
                string GRTFolder = folderMeta.getFullName("");

                dlg.InitialDirectory = GRTFolder;

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    // return selected file name.
                    return dlg.FileName;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
            finally
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

        }

    }
}
