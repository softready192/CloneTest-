using System;
using System.Collections.Generic;
using DA.Common;
using log4net;
using DA.DAIE.PrintReport;
using DA.DAIE.Common;
using System.Net;

namespace DA.DAIE.Connections
{
    public class ConnectionBO
    {
        internal static string MODULE_NAME = "Connection.Test";
        internal static string LOGGER_NAME = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string LOGGER_ERROR_NAME = App.LOGGER_USER_ERROR + "." + MODULE_NAME;

        public static ILog log = LogManager.GetLogger(LOGGER_NAME);
        public static ILog logError = LogManager.GetLogger(LOGGER_ERROR_NAME);

        public static List<ConnectionResult> testConnections()
        {
            List<ConnectionResult> results = new List<ConnectionResult>();
            results.Add( testDBConnection("MDB"));
            results.Add( testDBConnection("EES"));
            results.Add( testDBConnection("FCST"));
            results.Add( testDBConnection("CLU"));
            results.Add( testFTPConnection("MISFTP"));
            results.Add( testFTPConnection("SatelliteFTP"));
            results.Add( testNetworkFolderConnection("WEB_SERVER"));
            results.Add(testReportConnection("JasperReports"));
            return results;
        }

        /// <summary>
        /// Test DB Connection and SQL Mappings found in IBatis Configuration For DB Connection.
        /// </summary>
        /// <param name="argFileName"></param>
        private static ConnectionResult testDBConnection( string argDBPrefix )
        {
            string methodPurpose = "Test DB Connection and Mappings for " + argDBPrefix;
            
            try
            {
                string mapperFileName = "SqlMap" + ( argDBPrefix.Equals("MDB") ? "" : argDBPrefix ) + ".config";
                CustomMapper customMapper = new CustomMapper();
                DBSqlMapper mapper = customMapper.getMapper(mapperFileName);

                string expectedString = "test" + argDBPrefix;
                string selectName = "SelectTest" + argDBPrefix;
                string actualString = mapper.QueryForObject<string>(selectName, null,MODULE_NAME);

                if (expectedString.Equals(actualString))
                {
                    log.Info(methodPurpose + " Success");
                    return new ConnectionResult(argDBPrefix, "Database", "Success");
                }
                else
                {
                    log.Info(methodPurpose + " Failure");
                    return new ConnectionResult(argDBPrefix, "Database", "Failure");
                }
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException("Testing DB Connection and Mappings for " + argDBPrefix, ex, LOGGER_ERROR_NAME);
                he.logOnly();
                return new ConnectionResult(argDBPrefix, "Database", "Failure:" + ex.Message);
            }

        }

        private static ConnectionResult testFTPConnection( string name )
        {
            
            try
            {
                FTPLoginMeta ftpLoginMeta = ConnectionsParser.getFTPLogin(name, ConnectionsParser.CONNECTIONS_FILE_NAME);
                FTPUtil.ftpTestConnection(ftpLoginMeta);
            }
            catch (Exception ex)
            {
                return new ConnectionResult(name, "FTP", "Failed with exception:" + ex.Message );
            }

            return new ConnectionResult(name, "FTP", "Success");
            
        }

        private static ConnectionResult testReportConnection(string argDataSourceName)
        {

            try
            {
                PrintReportBO.TestConnection(argDataSourceName);
            }
            catch (Exception ex)
            {
                return new ConnectionResult(argDataSourceName, "ReportServer", "Failed with exception:" + ex.Message);
            }

            return new ConnectionResult(argDataSourceName, "ReportServer", "Success");

        }

        private static ConnectionResult testNetworkFolderConnection(string name)
        {
            try
            {

                NetworkFolderMeta networkLoginMeta = ConnectionsParser.getNetworkFolderMeta(name, ConnectionsParser.CONNECTIONS_FILE_NAME);

                NetworkCredential nc = new NetworkCredential(networkLoginMeta.UserName, networkLoginMeta.Password.DecryptedValue);

                string fileName = "";
                string fromDir = App.WorkingFolder;
                string toDir = "\\\\" + networkLoginMeta.Server + "\\" + networkLoginMeta.UserName + "\\" + networkLoginMeta.Path.Split(',')[0] + "\\";

                string networkDriveName = NetworkDriveUtil.MapNetworkDrive("", toDir, nc);
                NetworkDriveUtil.DisconnectNetworkDrive(networkDriveName, true);

            }
            catch (Exception ex)
            {
                return new ConnectionResult(name, "NetworkFolder", "Failed with exception:" + ex.Message);
            }
            return new ConnectionResult(name, "NetworkFolder", "Success");

        }

    }

    public class ConnectionResult
    {
        public string _Name;
        public string _Type;
        public string _Status;

        public ConnectionResult(string argName, string argType, string argStatus)
        {
            this._Name = argName;
            this._Type = argType;
            this._Status = argStatus;
        }

        public string Name
        {
            get { return _Name; }
        }

        public string Type
        {
            get { return _Type; }
        }

        public string Status
        {
            get { return _Status; }
        }





    }

    
}
