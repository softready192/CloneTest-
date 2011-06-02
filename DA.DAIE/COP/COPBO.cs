using System;
using log4net;
using DA.Common;
using DA.DAIE.Common;

namespace DA.DAIE.COP
{
    public class COPBO
    {
        public const string MODULE_NAME = "COP";
        public const string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        public const string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;
        

        public static void duplicateCOP(COPDuplicateArg arg)
        {
            string methodPurpose = "COP Duplication";
            string loggerEnding = ".Duplicate";
            DBSqlMapper sql = DBSqlMapper.Instance();
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER + loggerEnding);
                log.Info(methodPurpose + " Begin with " + arg.ToString());

                // Throws an exception if copy or paste day has invalid approval status
                validateDate(arg, sql, log);

                // Make sure inserts to mktunitplan and mktardplan are in a single transaction.
                sql.BeginTransaction(MODULE_NAME);

                sql.Insert("COPDuplicate.InsertMktUnitPlan", arg, MODULE_NAME);
                sql.Insert("COPDuplicate.InsertMktARDPlan", arg, MODULE_NAME);



                sql.CommitTransaction(MODULE_NAME);

            }
            catch (HandledException he)
            {
                if (sql.IsSessionStarted)
                {
                    sql.RollBackTransaction(MODULE_NAME);
                }
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR + loggerEnding);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch (Exception ex)
            {
                if (sql.IsSessionStarted)
                {
                    sql.RollBackTransaction(MODULE_NAME);
                }
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR + loggerEnding);
            }
        }

        /// <summary>
        /// 'Make sure the dates are valid, this is to trap any changes in the time between the user
        /// 'selecting a date and click the duplicate button.  Since the COP is never deleted we only
        /// 'need to check to make sure the DA case was not approved for a date that did not have an
        /// 'approved case at the time of selection.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="sql"></param>
        /// <param name="argLoggerName"></param>
        private static void validateDate(COPDuplicateArg arg, DBSqlMapper sql, ILog log)
        {
            log.Info("Validate Begin");
            Int16 copyDateStatus = sql.QueryForObject<Int16>("COPDuplicate.SelectDateStatus", arg.CopyDay,MODULE_NAME);
            if (copyDateStatus != 1)
            {
                throw new HandledException("The selected source day does not have a valid current operating plan.", log.Logger.Name);
            }

            Int16 pasteDateStatus = sql.QueryForObject<Int16>("COPDuplicate.SelectDateStatus", arg.PasteDay, MODULE_NAME);

            if (pasteDateStatus != 0)
            {
                throw new HandledException("The Day Ahead case has been approved for the selected destination day.  Please select a future day without an approved case.", log.Logger.Name);
            }
            log.Info("Validate Complete");
        }
    }
}
