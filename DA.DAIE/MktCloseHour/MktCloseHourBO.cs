using System;
using DA.Common;
using DA.DAIE.Connections;
using log4net;
using System.Collections.Generic;
using DA.DAIE.Common;


namespace DA.DAIE.MktCloseHour
{
    public class MktCloseHourBO 
    {
        public const string MODULE_NAME = "UpdateCloseHour";
        public const string LOGGER_NAME = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        public const string LOGGER_ERROR_NAME = App.LOGGER_USER_ERROR + "." + MODULE_NAME;
        public static ILog log = LogManager.GetLogger(LOGGER_NAME);
        public static ILog logError = LogManager.GetLogger(LOGGER_ERROR_NAME);

        /// <summary>
        /// Difference in milliseconds between computers time and MDB database time.
        /// Defaults to zero difference.
        /// </summary>
        int clockDiffMillisecond = 0;

        /// <summary>
        /// 'Retrieve the system time from MDB
        /// 'Set the difference between the system time and database time
        /// </summary>
        internal void setClockDiffMillisecond()
        {
            string methodPurpose = "Set difference between system time and database time";
            try
            {
                DateTime dbDate = DBSqlMapper.Instance().QueryForObject<DateTime>("SelectCurrentDBTime", null, MODULE_NAME);
                DateTime now = DateTime.Now;
                TimeSpan ts = dbDate.Subtract(now);
                clockDiffMillisecond = ts.Milliseconds;
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(LOGGER_ERROR_NAME);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException(methodPurpose, ex, LOGGER_ERROR_NAME);
            }
        }

        /// <summary>
        /// Returns Database Current Time
        /// Adjusted for any differnce found between system time and db time.
        /// </summary>
        /// <returns></returns>
        internal DateTime calcDbDate()
        {
            return DateTime.Now.AddMilliseconds(clockDiffMillisecond);
        }

        /// <summary>
        /// Retrieves current close hour for MDB.
        /// </summary>
        /// <returns></returns>
        internal static string getMktCloseHourMDB()
        {
            string closeHourString = DBSqlMapper.Instance().QueryForObject<string>("SelectMKTCloseHour", null, MODULE_NAME);
            if (closeHourString != null && closeHourString.Length == 1)
            {
                closeHourString = "0" + closeHourString;
            }
            return closeHourString;
        }

        /// <summary>
        /// Retrieves current close hour for EES.
        /// </summary>
        /// <returns></returns>
        internal static string getMktCloseHourEES()
        {
            CustomMapper customMapper = new CustomMapper();
            DBSqlMapper mapperEES = customMapper.getMapper("SqlMapEES.config");
            string closeHourString = mapperEES.QueryForObject<string>("SelectMKTCloseHourEES", null, MODULE_NAME);
            return closeHourString.Substring(0,2);
            
        }

        /// <summary>
        /// Populates List with values "01" thru "24"
        /// </summary>
        /// <returns></returns>
        internal static IList<string> getCloseHourList()
        {
            IList<string> hourList = new List<string>();
            for (int hourEnding = 1; hourEnding <= 24; hourEnding++)
            {
                hourList.Add(hourEnding.ToString("0#"));
            }
            return hourList;
        }

        internal static void UpdateCloseHours(bool argMDBChanged, bool argEESChanged, int argNewMktHour )
        {
            string methodPurpose = "Update Market Close Hour";

            log.Info(methodPurpose + " Begin");
            log.Info(methodPurpose + " Parameters argMDBChanged:" + argMDBChanged.ToString() +
                                                " argEESChanged:" + argEESChanged.ToString() +
                                                " argNewMktHour:" + argNewMktHour.ToString());

            DBSqlMapper mapperEES = null;
            DBSqlMapper mapperMDB = null;
            int updateMDBCount = 0;

            try
            {
                if (argMDBChanged)
                {
                    mapperMDB = DBSqlMapper.Instance();
                    mapperMDB.BeginTransaction(MODULE_NAME);
                    updateMDBCount = mapperMDB.Update("UpdateMKTCloseHour", argNewMktHour.ToString(), MODULE_NAME);
                }
                if (argEESChanged)
                {
                    CustomMapper customMapper = new CustomMapper();
                    mapperEES = customMapper.getMapper("SqlMapEES.config");
                    mapperEES.BeginTransaction(MODULE_NAME);
                    mapperEES.Insert("UpdateMKTCloseHourEES", argNewMktHour.ToString(), MODULE_NAME);
                }

                if (mapperMDB != null) { mapperMDB.CommitTransaction(MODULE_NAME); }
                if (mapperEES != null) { mapperEES.CommitTransaction(MODULE_NAME); }

                log.Info(methodPurpose + " Complete");
            }
            catch (HandledException he)
            {
                log.Info(methodPurpose + " Failed, Changes Rolled Begin");
                if( mapperMDB != null ){ mapperMDB.RollBackTransaction(MODULE_NAME); }
                if (mapperEES != null) { mapperEES.RollBackTransaction(MODULE_NAME); }
                he.setLoggerNameLogExisting(LOGGER_ERROR_NAME);
                log.Info(methodPurpose + " Failed, Changes Rolled Complete");
                throw he;
            }
            catch( Exception ex )
            {
                log.Info(methodPurpose + " Failed, Changes Rolled Begin");
                if( mapperMDB != null ){ mapperMDB.RollBackTransaction(MODULE_NAME); }
                if (mapperEES != null) { mapperEES.RollBackTransaction(MODULE_NAME); }
                throw new HandledException( methodPurpose + " Failed, Changes Rolled Complete", ex, LOGGER_ERROR_NAME);
            }
        }
    }
}
