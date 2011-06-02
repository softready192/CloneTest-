using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using DA.DAIE.Connections;
using log4net;
using DA.Common;
using DA.DAIE.Common;

namespace DA.DAIE.ExternalTransaction
{

    public partial class SelectExternalTransactionDetail : IDataErrorInfo
    {
        public string Error
        {
            get { return string.Empty; }
        }

        public string this[string columnName]
        {
            get
            {
                var result = string.Empty;

                //For Now, don't set any upper or lower limits.
                //if (columnName == "FixedMW")
                //{
                //
                //    if (this.FixedMW < -1000.0M )
                //    {
                //        result = "FixedMW is very low";
                //    }
                //    else if (this.FixedMW > 1000.0M)
                //    {
                //        result = "FixedMW is very high";
                //    }
                //}
                return result;
            }
        }
    }


    class ExternalTransactionBO 
    {
        internal static string MODULE_NAME = "ExtTrans";
        internal static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;
        
        public static IList<String> getExternalInterfaceList()
        {
            return DBSqlMapper.Instance().QueryForList<String>("SelectInterfaceList", null, MODULE_NAME);
        }

        public long getTransactionId(string argInterfaceName)
        {
            return DBSqlMapper.Instance().QueryForObject<long>("SelectTransactionIdFromInterfaceName", argInterfaceName, MODULE_NAME);
        }

        public static TransactionDetails getExternalTransactionDetailList(string argCaseId, String argInterfaceName)
        {
            SelectExternalTransactionDetailArg args = new SelectExternalTransactionDetailArg(argCaseId, argInterfaceName);
            return new TransactionDetails(DBSqlMapper.Instance().QueryForList<SelectExternalTransactionDetail>("SelectExternalTransactionDetail", args, MODULE_NAME));
        }

        // Tells if there is something to save on the screen.
        public static bool IsDirty(IList<SelectExternalTransactionDetail> argDetailList)
        {
            foreach (SelectExternalTransactionDetail detail in argDetailList)
            {
                if (detail.IsDirty)
                {
                    return true;
                }
            }
            return false;
        }

        public static void saveExternalTransactionDetailList(string argCaseId, String argInterfaceName, IList<SelectExternalTransactionDetail> argDetailList )
        {

            string methodPurpose = "Save External Transaction";
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER);
                log.Info(methodPurpose + " Begin");

                Int64 transactionId = DBSqlMapper.Instance().QueryForObject<Int64>("SelectTransactionIdFromInterfaceName", argInterfaceName, MODULE_NAME);
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                InsertMktTransactionHourlyArg arg;
                foreach (SelectExternalTransactionDetail detail in argDetailList)
                {
                    if (detail.IsDirty)
                    {
                        arg = new InsertMktTransactionHourlyArg(transactionId, detail.MktHour, detail.FixedMW);
                        DBSqlMapper.Instance().Insert("InsertMktTransactionHourly", arg, MODULE_NAME);
                    }
                }
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
                log.Info(methodPurpose + " Complete");
                MessageBox.Show("External Transactions Saved for Interface: " + argInterfaceName, "Success");
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("No Changes Saved");
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                throw;
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                throw new HandledException("No Changes Saved", ex, MODULE_LOGGER_ERROR);
            }
        }

        public static void ExecuteProcedureUploadExternalTransaction(string argCaseId)
        {
            string methodPurpose = "Execute Procedure Upload External Transaction";
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER);
                log.Info(methodPurpose + " Begin");
                string mktDayString = DBSqlMapper.Instance().QueryForObject<string>("SelectCaseMktDay", argCaseId, MODULE_NAME);
                log.Info("Found Date:" + mktDayString);
                CustomMapper customMapper = new CustomMapper();
                DBSqlMapper mapper = customMapper.getMapper("SqlMapEES.config");
                mapper.Insert("ProcedureUploadExternalTransaction", mktDayString, MODULE_NAME);
                log.Info(methodPurpose + " Complete");
                MessageBox.Show("External Transaction Initialized for " + mktDayString, "External Transaction Initialized");
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch(Exception ex)
            {
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
        }
    }
}
