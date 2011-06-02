using System;
using System.Collections.Generic;
using DA.Common;
using DA.DAIE.CaseSelection;
using log4net;
using System.Threading;
using System.Windows;
using DA.DAIE.Connections;
using DA.DAIE.Common;

namespace DA.DAIE.PIC
{

    class PICBO
    {
        internal static string MODULE_NAME = "PIC";
        internal static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;

        internal static ILog log = LogManager.GetLogger(MODULE_LOGGER);
        internal static ILog logError = LogManager.GetLogger(MODULE_LOGGER_ERROR);

        /// <summary>
        /// Returns Data to Display Status of PIC Processing.
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argNewCaseList"></param>
        /// <returns></returns>
        public static IList<MktCaseControlData> SelectStatusDisplay(string argCaseId, IList<string> argNewCaseList )
        {
            PeakHour ph = getPeakHour(argCaseId);

            if (argNewCaseList != null && argNewCaseList.Count > 0)
            {
                SelectMktCaseControlArg arg = new SelectMktCaseControlArg(ph, argNewCaseList);
                return DBSqlMapper.Instance().QueryForList<MktCaseControlData>("SelectStatusDisplay", arg, MODULE_NAME);

            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns false if there are any pending cases.
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argNewCaseList"></param>
        /// <returns></returns>
        public static bool SelectStatusIsComplete(string argCaseId, IList<string> argNewCaseList)
        {
            PeakHour ph = getPeakHour(argCaseId);

            if (argNewCaseList != null && argNewCaseList.Count > 0)
            {
                SelectMktCaseControlArg arg = new SelectMktCaseControlArg(ph, argNewCaseList);
                int pendingCaseCount = (int)DBSqlMapper.Instance().QueryForObject<decimal>("SelectStatusPendingCount", arg,MODULE_NAME);
                if (pendingCaseCount == 0)
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
                // It's complete because there were no cases to process.
                
                return true;
            }
        }

        /// <summary>
        /// Throws Exception if there are any failed cases.
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argNewCaseList"></param>
        /// <returns></returns>
        private static bool SelectStatusIsSuccess(string argCaseId, IList<string> argNewCaseList)
        {
            PeakHour ph = getPeakHour(argCaseId);

            if (argNewCaseList != null && argNewCaseList.Count > 0)
            {
                SelectMktCaseControlArg arg = new SelectMktCaseControlArg(ph, argNewCaseList);
                int failedCaseCount = (int)DBSqlMapper.Instance().QueryForObject<decimal>("SelectStatusFailedCount", arg, MODULE_NAME);
                if (failedCaseCount == 0)
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
                // It's successful because there were no cases to fail.

                return true;
            }
        }


        private static bool archiveCase(string argCaseId)
        {
            try
            {
                DBSqlMapper.Instance().Insert("ArchiveCase", argCaseId, MODULE_NAME);
                return true;
            }
            catch (HandledException he)
            {
                he.logOnly("Archive Case Failed for Case: " + argCaseId);
                return false;
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Exception While Archiving Case: " + argCaseId, ex, MODULE_LOGGER_ERROR);
                he.logOnly("Archive Case Failed for Case: " + argCaseId);
                return false;
            }
        }

        public static string GetPICFileName(string argSelectedUploadFileName )
        {
            NetworkFolderMeta folderMeta = ConnectionsParser.getNetworkFolderMeta("MMMPICResultsFolder", ConnectionsParser.CONNECTIONS_FILE_NAME);
            int lastDirectoryIndex = argSelectedUploadFileName.LastIndexOf("\\") + 1;
            string uploadFileName = argSelectedUploadFileName.Substring(lastDirectoryIndex);
            string dateTimeToday = DateTime.Now.ToString("ddMMMyyyy");
            string fileName = uploadFileName.Substring(0, 2) + dateTimeToday + ".csv";
            return folderMeta.getFullName(fileName);
        }



        public static void processComplete(string argCaseId, IList<string> argNewCaseList, string argPICFileName )
        {
            if (SelectStatusIsSuccess(argCaseId, argNewCaseList))
            {
                CreatePICFiles(argPICFileName, argCaseId, argNewCaseList);
        


                bool allArchived = true;
                foreach (string caseid in argNewCaseList)
                {
                    if (!archiveCase(caseid))
                    {
                        allArchived = false;
                    }
                }

                if (allArchived)
                {
                     string message = "The PIC cases have finished executing. ";
                     log.Info(message);
                     MessageBox.Show(message, message, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    string message = "The PIC cases finished executing, but were not all archived. ";
                    log.Warn(message);
                    MessageBox.Show(message, message, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                

            }
            else
            {
                string message = "One or more PIC cases failed.";
                log.Error(message);
                MessageBox.Show(message, message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private static void CreatePICFiles( string argFileName, string argCaseId, IList<string> argNewCaseList )
        {
            string caseIdBase = argNewCaseList[0];
            bool first = true;
            bool firstFile = true;

            NetworkFolderMeta folderMeta = ConnectionsParser.getNetworkFolderMeta("MMMPICResultsFolder", ConnectionsParser.CONNECTIONS_FILE_NAME);
            foreach (string caseId in argNewCaseList)
            {
                // Skip the first loop since it contains the base case id.
                if( first ) 
                {
                    first = false;
                }
                else
                {
                    SelectResultFileArg arg = new SelectResultFileArg(caseIdBase, caseId);
                    IList<object> results = DBSqlMapper.Instance().QueryForList<object>("SelectResultFile", arg, MODULE_NAME);
                    writeCSVFile(results, argFileName, firstFile );
                    firstFile = false;
                }
            }
        }

        /// <summary>
        /// Overwrites a CSV file with specified name
        /// Creates file from generic IList<object> passed from QueryForObject<object>
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="fileName"></param>
        private static void writeCSVFile(IList<object> inputData, string argFileName, bool isFirstFile )
        {
            try
            {
                System.IO.StreamWriter sw = null;
                if (isFirstFile)
                {
                    sw = System.IO.File.CreateText(argFileName);
                }
                else
                {
                    sw = System.IO.File.AppendText(argFileName);
                }

                foreach (Object[] obj in inputData)
                {
                    string concat = "";
                    foreach (object field in obj)
                    {
                        concat += field.ToString().Trim() + ",";
                    }
                    concat = concat.Substring(0, concat.Length - 1);
                    sw.WriteLine(concat);
                }
                sw.Close();
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable to save file: " + argFileName, ex);
            }

        }

        private static bool validateDataList(IList<PICData> dataList, string argSelectedUploadFileName)
        {
            /// validate all elements in dataList.
            /// Return error message to user if any are invalid.
            bool dataListValid = true;
            string validateMessage = "";
            int lineNumber = 0;

            if (dataList == null)
            {
                validateMessage = "No PIC Scenario File Selected";
                logError.Error(validateMessage);
                MessageBox.Show(validateMessage, "No PIC Scenario File Selected");
                return false;
            }
            if (dataList.Count == 0)
            {
                validateMessage = "No PIC Scenarios Found In File:" + argSelectedUploadFileName;
                logError.Error(validateMessage);
                MessageBox.Show(validateMessage, "Empty PIC Scenario File Selected");
                return false;
            }

            foreach (PICData data in dataList)
            {
                lineNumber += 1;
                if (!data.validate())
                {
                    dataListValid = false;
                    validateMessage += "\n\r error on line:" + lineNumber.ToString() + " " + data.ValidationMessage + " " + data.ToLogString();
                }
            }

            if (dataListValid)
            {
                return true;
            }
            else
            {
                validateMessage = "Invalid Data In Uploaded PIC Scenario File:" + argSelectedUploadFileName + validateMessage;
                logError.Error(validateMessage);
                MessageBox.Show(validateMessage, "Invalid Data In Uploaded PIC Scenario File: " + argSelectedUploadFileName);
                return false;
            }

        }

        public static bool RunPIC(MODES argMode, string argCaseId, ref string argSelectedUploadFileName, ref IList<string> argNewCaseList)
        {
            string methodPurpose = "Peak Impact Calculator";
            try
            {
                log.Info(methodPurpose + " Begin");
                IList<PICData> dataList = PICFileParser.LoadFile(argCaseId, ref argSelectedUploadFileName);

                

                if( !validateDataList(dataList, argSelectedUploadFileName))
                {
                    return false;
                }
                if (dataList != null)
                {
                    DateTime dtNow = DateTime.Now;
                    String scenario = "Base";
                    PeakHour ph = getPeakHour(argCaseId);
                    string copyCaseId = copyCaseTillSuccess(argCaseId, scenario, ph);
                    argNewCaseList.Add(copyCaseId);
                    execCase(copyCaseId);
                    RunPICCases(argCaseId, dataList, ph, ref argNewCaseList);
                    log.Info(methodPurpose + " Running");
                    return true;
                }
                else
                {
                    log.Info(methodPurpose + " Canceled");
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

        private static string copyCaseTillSuccess(string argCaseId, string argScenario, PeakHour argPH)
        {

            string newCaseId = "";
            bool goAgain = true;
            int maxTries = 10;
            int tries = 0;
            while (goAgain)
            {
                tries += 1;
                try
                {
                    newCaseId = copyCase(argCaseId, argScenario, argPH);
                    log.Info("New CaseId = " + newCaseId);
                    goAgain = false;
                }
                catch (HandledException he)
                {
                    if (tries < maxTries)
                    {
                        goAgain = true;
                    }
                    else
                    {
                        he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                        he.addCustomStackMessage("Exceeded " + maxTries.ToString() + " Tries When Copying PIC Case");
                        he.logOnly();
                        throw;
                    }
                }
                catch (Exception )
                {
                    System.Windows.MessageBox.Show("Hey not supposed to get here.");
                }

            }
            return newCaseId;
        }


        /// <summary>
        /// Return the copied case id 
        /// or Empty string if error occurs.
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argPH"></param>
        /// <returns></returns>
        private static string copyCase(string argCaseId, string argScenario, PeakHour argPH)
        {
            CopyMktCaseArg argCopy = null;
            try
            {
                argCopy = new CopyMktCaseArg(argCaseId);
                DBSqlMapper.Instance().Insert("CopyMktCase", argCopy, MODULE_NAME);
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("HandledException While Copying Case.  Assume a duplicate ID Occured.");
                he.logOnly(MODULE_LOGGER_ERROR);
                throw;
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Exception Copying Case.  Assume a duplicate ID Occured.", ex, MODULE_LOGGER_ERROR);
                he.logOnly(MODULE_LOGGER_ERROR);
                throw he;
            }

            try
            {
                UpdateMktCaseArg argUpdate =
                    new UpdateMktCaseArg(
                    argCaseId,
                    argCopy.result.ToString(),
                    argPH.MktDay,
                    argPH.MktHour,
                    DateTime.Now,
                    argScenario);

                DBSqlMapper.Instance().Update("UpdateMktCase", argUpdate, MODULE_NAME);
                return argCopy.result.ToString();
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("Updating Newly Copied CaseId: " + argCopy.result.ToString());
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Updating Newly Copied CaseId: " + argCopy.result.ToString(), ex, MODULE_LOGGER_ERROR);
            }

        }


        private static void RunPICCases(string argCaseId, IList<PICData> argPICDataList, PeakHour ph, ref IList<string> argNewCaseList )
        {
            PICData priorData = null;
            bool firstTime = true;
            UpdateMktCaseUnitArg arg;
            string copyCaseId = "";
            foreach (PICData currentData in argPICDataList)
            {
                
                // First Time 
                if ( firstTime )
                {
                    firstTime = false;
                }
                else if (!currentData.Name.Equals(priorData.Name))
                {
                    Thread.Sleep(100);
                    copyCaseId = copyCaseTillSuccess(argCaseId, priorData.Name, ph);
                    argNewCaseList.Add(copyCaseId);
                    arg = new UpdateMktCaseUnitArg(copyCaseId, ph, currentData);
                    DBSqlMapper.Instance().Insert("InsertMktCaseUnit", arg, MODULE_NAME);
                    execCase(copyCaseId);
                }
                priorData = currentData;
            }
            copyCaseId = copyCaseTillSuccess(argCaseId, priorData.Name, ph);
            argNewCaseList.Add(copyCaseId);
            arg = new UpdateMktCaseUnitArg(copyCaseId, ph, priorData);
            DBSqlMapper.Instance().Insert("InsertMktCaseUnit", arg, MODULE_NAME);
            execCase(copyCaseId);
        }


        /// <summary>
        ///'****************************************************************
        ///'*
        ///'* Calls the ESCA procedure that adds a case to the queue for
        ///'* execution.
        ///'*
        ///'****************************************************************
        /// <summary>
        private static void execCase(string argCaseId)
        {
            string methodPurpose = "Execute Case";

            try
            {
                ExecMktCaseArg arg = new ExecMktCaseArg(argCaseId);
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                DBSqlMapper.Instance().Insert("ExecMktCase", arg, MODULE_NAME);
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
            }
            catch (HandledException he)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
        
        }

        

        private static PeakHour getPeakHour(string argCaseId)
        {
            string methodPurpose = "Get Peak Hour For Case: " + argCaseId;
            try
            {
                // Retrieves house for case with demand mw descending.
                // To get the peak hour, return the first row.
                IList<PeakHour> peakHourList = DBSqlMapper.Instance().QueryForList<PeakHour>("SelectCasePeakHourLoad", argCaseId, MODULE_NAME);
                if (peakHourList == null)
                {
                    return null;
                }
                else if (peakHourList.Count == 0)
                {
                    return null;
                }
                else
                {
                    return peakHourList[0];
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


    }
}
