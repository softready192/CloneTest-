using System;
using log4net;
using System.IO;
using DA.Common;
using DA.DAIE.CaseSelection;
using System.Configuration;
using DA.DAIE.Common;

namespace DA.DAIE.FileUpload
{
    class GRTLocReserveRequirementBO
    {
        internal static string MODULE_NAME = "GRTLocResReqt";
        internal static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;
        internal static ILog log = LogManager.GetLogger(MODULE_LOGGER);



        public static string[] loadGRTFile(string argCaseId)
        {
            string grtFileName = GRTConstraintBO.selectGRTFileName();
            return readGRTFile(argCaseId, grtFileName);
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
        private static string[] readGRTFile(string argCaseId, string grtFileName )
        {



            string methodPurpose = "Read locational reserve requirements";
            
            try
            {
                log.Info(methodPurpose + " Begin");
                log.Info(methodPurpose + " from file " + grtFileName);
                string line = "";
                string priorGRTName = "-initialGRTName-";

                int uploadCount = 0;
                //' Open the file and loop through all the records
                StreamReader inFile = new StreamReader(grtFileName);
                bool firstLine = true;
                object reserveZoneId = null;
                object interfaceId = null;

                CaseHourBO caseHours = new CaseHourBO(argCaseId);

                int expectedReserveZoneCount = (int)DBSqlMapper.Instance().QueryForObject<decimal>("SelectMktReserveZoneCount", null,MODULE_NAME);
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                while ((line = inFile.ReadLine()) != null)
                {
                    GRTConstraint constraint = new GRTConstraint(argCaseId, line, grtFileName );
                    //'if this is the first record check to make sure the date on the file
                    //'matches the date of the case and determine the first day of daylight savings time.
                    if (firstLine)
                    {
                        if (!caseHours.VerifyFileDate(constraint.EffectiveDate))
                        {
                            throw new HandledException("The date on the GRT file does not match the market day of the selected case.", 50001);
                        }
                        firstLine = false;
                    }

                    //'if this is the first record for a GRT constraint check to see if this
                    //'is a locational reserve requirement

                    string grtName = constraint.Name;
                    string foundZoneShortName = null;
                    if (!grtName.Equals(priorGRTName))
                    {
                        reserveZoneId = null;
                        interfaceId = null;

                        ReserveZoneConfigurationSection config = (ReserveZoneConfigurationSection)ConfigurationManager.GetSection("ReserveZoneSection");
                        //ReserveZoneConfig config = new ReserveZoneConfig();
                        ReserveZoneConfigurationElementCollection rzList = config.ReserveZoneList;
                        if (rzList.containsTag(grtName, TAG_TYPE.LRR, out foundZoneShortName))
                        {
                            reserveZoneId = DBSqlMapper.Instance().QueryForObject<Object>("SelectReserveZoneId", foundZoneShortName, MODULE_NAME);
                        }
                        if (rzList.containsTag(grtName, TAG_TYPE.N1, out foundZoneShortName))
                        {
                            interfaceId = DBSqlMapper.Instance().QueryForObject<Object>("SelectInterfaceId", foundZoneShortName, MODULE_NAME);
                        }
                        priorGRTName = grtName;
                    }

                    foreach (DateTime gmtEffectiveDate in caseHours.localToGMT(constraint.EffectiveHour, constraint.EffectiveDate ))
                    {
                        constraint.GMTEffectiveDateTime = gmtEffectiveDate;
                        if (reserveZoneId != null)
                        {
                            GRTReserveInsertArg arg = new GRTReserveInsertArg(reserveZoneId.ToString(), constraint.GMTEffectiveDateTime, constraint.ConstraintValue);
                            DBSqlMapper.Instance().Insert("InsertMktReserveZoneHourly", arg, MODULE_NAME);
                            uploadCount += 1;
                        }
                        else if( interfaceId != null )
                        {
                            GRTReserveInsertArg arg = new GRTReserveInsertArg(interfaceId.ToString(), constraint.GMTEffectiveDateTime, constraint.ConstraintValue);
                            DBSqlMapper.Instance().Insert("InsertMktInterfaceHourly", arg, MODULE_NAME);
                            uploadCount += 1;
                        }
                    }
                }
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
                int expectedCount = expectedReserveZoneCount * caseHours.CaseHourCount() * 2;
                if (expectedCount == uploadCount)
                {
                    string message = uploadCount + " Locational Reserve Requirement records successfully uploaded to database.";
                    log.Info(message);
                    log.Info(methodPurpose + " Complete");
                    return new string[] { message, "Upload Complete" };
                }
                else
                {
                    string message = "An unexpected number of reserve requirement records were uploaded to the database. \n\r" +
                              uploadCount + " records were uploaded, but " + expectedCount + " were expected.";
                    log.Warn(message);
                    log.Warn(methodPurpose + " Complete with Warning");
                    return new string[] { message, "Upload Complete With Warning" };
                }
                
            }
            catch (HandledException he)
            {
                if (DBSqlMapper.Instance().IsSessionStarted) { DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME); }
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch (Exception ex)
            {
                if (DBSqlMapper.Instance().IsSessionStarted) { DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME); }
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
        }
    }
}
