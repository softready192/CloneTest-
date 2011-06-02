using System;
using System.Collections.Generic;
using System.Text;
using com.jaspersoft.webservice;
using System.IO;
using com.jaspersoft.webservice.utilities;
using com.jaspersoft.webservice.xmlHelper;
using System.Collections;
using com.jaspersoft.webservice.interfaces;
using DA.Common;
using DA.DAIE;
using System.Windows;
using log4net;
using DA.DAIE.Common;
using DA.DAIE.Admin;
using DA.DAIE.CaseSelection;
using System.Printing;
using DA.DAIE.Connections;
using System.Net;
using DA.DAIE.CaseApprove;

namespace DA.DAIE.PrintReport
{

    public class PrintReportBO : AncestorBO
    {
        /// <summary>
        /// Static constructor initializes logging.
        /// </summary>
        static PrintReportBO()
        { 
            MODULE_NAME = "PrintReport"; 
        }

        /// <summary>
        /// Prints all reports found for the specified mode.
        /// The list of reports for each mode is found in PrintReportConfig.xml
        /// </summary>
        /// <param name="modeName"></param>
        /// <param name="argCaseId"></param>
        /// <param name="argPrinterName"></param>
        public static void printReports(String modeName, string argCaseId, string argPrinterName )
        {
            PrintReportConfig reportConfig = new PrintReportConfig();
            ReportLoginMeta reportLoginMeta = Connections.ConnectionsParser.getReportLoginMeta("JasperReports", Connections.ConnectionsParser.CONNECTIONS_FILE_NAME);
            List<ReportMetadata> reportList = reportConfig.getReportList(modeName);
            log.Info("Printing " + reportList.Count + " reports for Case:" + argCaseId + " Mode:" + modeName );
            int reportIndex = 1;
            foreach (ReportMetadata reportMetadata in reportList)
            {
                log.Info("Printing Report " + reportIndex + " of " + reportList.Count + " Reports.  Name:" + reportMetadata.Name);
                if (PrintReportBO.printReport(argCaseId, reportMetadata, argPrinterName, reportLoginMeta ) == false) { return; }
                reportIndex += 1;
            }
        }

        /// <summary>
        /// Prints a single report
        /// </summary>
        /// <param name="argCaseId"></param>
        /// <param name="argReportMetadata"></param>
        /// <param name="argPrinterName"></param>
        /// <returns></returns>
        public static bool printReport(
              string argCaseId
            , ReportMetadata argReportMetadata
            , string argPrinterName
            , ReportLoginMeta reportLoginMeta )
        {

            string reportURIPrefix = reportLoginMeta.Path;
            Repository rep = reportLoginMeta.getWSRepository();

            RunRequest request = new RunRequest(reportURIPrefix + argReportMetadata.URL, Constants.RUN_OUTPUT_FORMAT_PDF, true);
            //Parameter parm1 = new Parameter();

            //parm1.name = "CaseId";
            //parm1.parameterValue = argCaseId;
            //Parameter[] parms = new Parameter[] { parm1 };
            request.parameters = getReportParameters(argCaseId, argReportMetadata);

            Response response = rep.runReport(request);
            OperationResult result = response.toOperationResult();

            if (result.isError)
            {
                string errorString = result.parentResponse.message;
                errorString += result.message;
                logError.Error("WebService Error Printing Report " + argReportMetadata.Name + " Error:" + errorString);
                MessageBoxResult mbr = MessageBox.Show("Error Occured While Printing Report: " + argReportMetadata.Name, "Continue Printing?", MessageBoxButton.YesNo);
                if (mbr.Equals(MessageBoxResult.No))
                {
                    log.Info("User Did Not Continue After Printing Error");
                    return false;
                }
                else
                {
                    log.Info("User Continued After Printing Error");
                }
            }

            ICollection streams = response.attachments.Values;

            if (streams != null)
            {
                foreach (Stream stream in streams)
                {
                    RawPrinterHelper.SendStreamToPrinter(argPrinterName, stream);
                    stream.Close();
                }
            }
            return true;

        }


        /// <summary>
        /// Saves all reports found for the specified mode.
        /// The list of reports for each mode is found in PrintReportConfig.xml
        /// Saves report to the specified folder.
        /// </summary>
        /// <param name="modeName"></param>
        /// <param name="argCaseId"></param>
        /// <param name="argFolderName"></param>
        public static void saveReports(String modeName, string argCaseId, string argFolderName, SatelliteCaseHour firstLastHour, Window argWindow, string argWindowTitleOriginal, bool argChangeCase )
        {
            try
            {
                string zipFileName = "SCRA_Output_" + firstLastHour.DBNow.ToString("yyyyMMdd_HHmm") + ".zip";
                if (System.IO.File.Exists(argFolderName + "\\" + zipFileName))
                {
                    System.IO.File.Delete(argFolderName + "\\" + zipFileName);
                }

                PrintReportConfig reportConfig = new PrintReportConfig();
                List<ReportMetadata> reportList = reportConfig.getReportList(modeName);
                log.Info("Saving " + reportList.Count + " reports for Case:" + argCaseId + " Mode:" + modeName);
                int reportIndex = 1;

                foreach (ReportMetadata reportMetadata in reportList)
                {
                    argWindow.Title = argWindowTitleOriginal + " Save Rpt " + reportIndex + "-" + reportList.Count + " " + reportMetadata.File;
                    log.Info("Saving Report " + reportIndex + " of " + reportList.Count + " Reports.  Name:" + reportMetadata.File);
                    if (saveReport(argCaseId, reportMetadata, argFolderName, reportConfig, Constants.RUN_OUTPUT_FORMAT_PDF) == false) { return; }

                    int parameterCount = reportMetadata.Parameters.Length;
                    if (parameterCount > 0)
                    {
                        foreach (Parameter param in reportMetadata.Parameters)
                        {
                            if (param.name.Equals("genCSV"))
                            {
                                param.parameterValue = "1";
                            }
                        }
                    }

                    if (saveReport(argCaseId, reportMetadata, argFolderName, reportConfig, Constants.RUN_OUTPUT_FORMAT_CSV.ToLower()) == false) { return; }
                    ZipUtil.AddFileToZip(zipFileName, reportMetadata.File + "." + Constants.RUN_OUTPUT_FORMAT_CSV.ToLower(), argFolderName, "/reports/");
                    reportIndex += 1;
                }
                if (argChangeCase)
                {
                    CopyReportFiles();
                }

            }
            catch (HandledException he)
            {
                he.addCustomStackMessage("Error Saving Reports for ReportGroup: " + modeName);
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Error Saving Reports for ReportGroup: " + modeName, ex, LOGGER_ERROR_NAME);
            }
        }


        private  static void CopyReportFiles()
        {
            string networkDriveName = "";
            try
            {

                NetworkFolderMeta networkLoginMeta = ConnectionsParser.getNetworkFolderMeta("WEB_SERVER", ConnectionsParser.CONNECTIONS_FILE_NAME);

                NetworkCredential nc = new NetworkCredential(networkLoginMeta.UserName, networkLoginMeta.Password.DecryptedValue);

                string fileName = "";
                string fromDir = App.WorkingFolder;
                string toDir = "\\\\" + networkLoginMeta.Server + "\\" + networkLoginMeta.UserName + "\\" + networkLoginMeta.Path.Split(',')[0] + "\\";

                networkDriveName = NetworkDriveUtil.MapNetworkDrive("", toDir, nc);

                string[] filesPDF = Directory.GetFiles(fromDir, "*.pdf");

                foreach (string fullFileName in filesPDF)
                {
                    fileName = fullFileName.Substring(fromDir.Length);
                    sendNetworkFile(fileName, fromDir, toDir, nc);
                }

                string[] filesZIP = Directory.GetFiles(fromDir, "*.zip");
                //toDir = "\\\\webftpint\\unit_comm\\Archives1\\";
                toDir = "\\\\" + networkLoginMeta.Server + "\\" + networkLoginMeta.UserName + "\\" + networkLoginMeta.Path.Split(',')[1] + "\\";
                foreach (string fullFileName in filesZIP)
                {
                    fileName = fullFileName.Substring(fromDir.Length);
                    sendNetworkFile(fileName, fromDir, toDir, nc);
                }

            }
            catch (Exception ex)
            {
                throw new HandledException("Unable To Copy Report File", ex, LOGGER_ERROR_NAME);
            }
            finally
            {
                NetworkDriveUtil.DisconnectNetworkDrive(networkDriveName, true);
            }
        }


        private static void sendNetworkFile(string argFileName, string argFromDir, string argToDir, NetworkCredential argAuth)
        {
            WebClient client = new WebClient();
           
            client.Credentials = argAuth;

            string fileFrom = getFullFileName(argFileName, argFromDir);
            string fileTo = getFullFileName(argFileName, argToDir);
            Uri toURI = new Uri(fileTo);
            byte[] argReturn = client.UploadFile(toURI, fileFrom);
            log.Info("Network File Copied: " + argFileName + " from: " + argFromDir + " to: " + argToDir);
        }

        private static string getFullFileName(string argFileName, string argDir)
        {
            return (argDir == null || argDir.Length == 0 ? "" + argFileName : argDir + argFileName);
        }


        private static Parameter[] getReportParameters(string argCaseId, ReportMetadata argReportMetadata)
        {
            Parameter parm1 = new Parameter();
            parm1.name = "CaseId";
            parm1.parameterValue = argCaseId;
            Parameter[] argParams = argReportMetadata.Parameters;
            int paramCount = (argParams == null ? 0 : argParams.Length) + 1;
            Parameter[] parms = new Parameter[paramCount];
            parms[0] = parm1;

            int i = 1;
            if (argParams != null)
            {
                foreach (Parameter param in argParams)
                {
                    parms[i] = param;
                    i++;
                }
            }
            return parms;
        }


         /// <summary>
         /// Saves a single report
         /// </summary>
         /// <param name="argCaseId"></param>
         /// <param name="argReportMetadata"></param>
         /// <param name="argPrinterName"></param>
         /// <returns></returns>
         public static bool saveReport(
              string argCaseId
            , ReportMetadata argReportMetadata
            , string argFolderName
            , PrintReportConfig reportConfig
            , String argOutputType )
         {

             ReportLoginMeta jasperLoginMeta = Connections.ConnectionsParser.getReportLoginMeta("JasperReports", Connections.ConnectionsParser.CONNECTIONS_FILE_NAME);
             Repository rep = jasperLoginMeta.getWSRepository();

             RunRequest request =
                 new RunRequest(
                      jasperLoginMeta.Path + argReportMetadata.URL
                    , argOutputType
                    , true);

             request.parameters = getReportParameters(argCaseId, argReportMetadata);

             Response response = rep.runReport(request);
             OperationResult result = response.toOperationResult();

             if (result.isError)
             {
                 string errorString = result.parentResponse.message;
                 errorString += result.message;
                 logError.Error("WebService Error Saving Report for Case:" + argCaseId + " " + errorString);
                 MessageBoxResult mbr = MessageBox.Show("Webservice Error Occured While Saving Report: " + argReportMetadata.File, "Continue?", MessageBoxButton.YesNo);
                 if (mbr.Equals(MessageBoxResult.No))
                 {
                     return false;
                 }
             }

             ICollection streams = response.attachments.Values;

             if (streams != null)
             {
                 foreach (Stream stream in streams)
                 {
                     string folder = argFolderName == null ? "" : argFolderName + "/";
                     SaveStreamToFile( folder + argReportMetadata.File + "." + argOutputType, stream);
                     stream.Close();
                 }
             }
             return true;
         }


        private static void SaveStreamToFile(string fileFullPath, Stream stream)
        {
            try
            {
                if (stream.Length == 0) return;

                if( System.IO.File.Exists(fileFullPath))
                {
                    System.IO.File.Delete(fileFullPath);
                }

                // Create a FileStream object to write a stream to a file 
                using (FileStream fileStream = System.IO.File.Create(fileFullPath, (int)stream.Length))
                {
                    // Fill the bytes[] array with the stream data 
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, (int)bytesInStream.Length);

                    // Use FileStream object to write to the specified file 
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                }
            }
            catch (Exception ex)
            {
                throw new HandledException("Save Report Stream To File", ex, LOGGER_ERROR_NAME);
            }
        }

        public static bool TestConnection(string argDataSourceName)
        {
            string modeName = MODES.DA.ToString();
            string argCaseId = "12345";
            string argPrinterName = LocalPrintServer.GetDefaultPrintQueue().Name;

            PrintReportConfig reportConfig = new PrintReportConfig();
            ReportLoginMeta reportLoginMeta = Connections.ConnectionsParser.getReportLoginMeta(argDataSourceName, Connections.ConnectionsParser.CONNECTIONS_FILE_NAME);
            List<ReportMetadata> reportList = reportConfig.getReportList(modeName);
            log.Info("Testing Connection " + reportList.Count + " reports for Case:" + argCaseId + " Mode:" + modeName);
            ReportMetadata reportMetadata = reportList[0];

                log.Info("Testing Report Name:" + reportMetadata.Name);
                
                Repository rep = reportLoginMeta.getWSRepository();

                RunRequest request = new RunRequest(reportLoginMeta.Path + reportMetadata.URL, Constants.RUN_OUTPUT_FORMAT_PDF, true);
                Parameter parm1 = new Parameter();

                parm1.name = "CaseId";
                parm1.parameterValue = argCaseId;
                Parameter[] parms = new Parameter[] { parm1 };
                request.parameters = parms;

                Response response = rep.runReport(request);
                OperationResult result = response.toOperationResult();

                if (result.isError)
                {
                    return false;
                }
                else
                {
                    return true;
                }

        }




    }
}