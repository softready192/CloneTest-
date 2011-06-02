using System;
using System.Collections.Generic;
using DA.DAIE.CaseSelection;
using System.Windows;
using DA.Common;
using log4net;
using DA.DAIE.Common;

namespace DA.DAIE.Reserve
{
    class ReserveBO
    {
        internal static string MODULE_NAME = "Reserve";
        internal static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;


        public static IList<Reserve> calculateReserveRequirements(ContingencyPercent argContingencyPercent, IList<Reserve> argReserves)
        {
            try
            {
                foreach (Reserve reserve in argReserves)
                {
                    reserve.CalcRequirement(argContingencyPercent);
                }
                return argReserves;
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("Calculate Reserve Requirements");
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Calculate Reserve Requirements", ex, MODULE_LOGGER_ERROR);
            }
        }

        public static IList<Reserve> retrieveReserves(MODES argMode, string argCaseId, ContingencyPercent argCP)
        {
            try
            {
                IList<Reserve> reserves = retrieveReserves(argMode, argCaseId);
                reserves = calculateReserveRequirements(argCP, reserves);
                return reserves;
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage("Retrieve Reserves For CaseId: " + argCaseId);
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException("Retrieve Reserves For CaseId: " + argCaseId, ex, MODULE_LOGGER_ERROR);
            }
        }

        /// <summary>
        /// Retrieves related RSS Cases for the passed CaseId.
        /// uses SQLId = "SelectRSSCaseList"
        /// 
        /// In Code That Calls This Method...
        /// If no rows returned, user should be warned and values for HQ and NB will be unchanged.
        /// If one row is returned, it should be used by SQL that selects values for HQ and NB.
        /// If multiple rows are returned, user should be prompted to select one.
        /// 
        /// </summary>
        /// <param name="argMode"></param>
        /// <param name="argCaseId"></param>
        /// <returns></returns>
        public static IList<RSSCase> retrieveRSSCaseList(MODES argMode, string argCaseId)
        {
            // Assert we are in DA Mode for this Method.
            if (!argMode.Equals(MODES.DA))
            {
                throw new HandledException("RSSCaselist an Only Be Retrieved for " + MODES.DA.ToString() + " Mode.");
            }

            IList<RSSCase> caseList = DBSqlMapper.Instance().QueryForList<RSSCase>("SelectRSSCaseList", argCaseId, MODULE_NAME);
            return caseList;
        }

        /// <summary>
        /// Enhance reserveList with hourly transaction totals for HQ and NB interfaces.
        /// This method is only supposed to be run for DA mode, and will throw an exception if this assertion fails.
        /// </summary>
        /// <param name="argMode"></param>
        /// <param name="argCaseId"></param>
        /// <param name="argReserveList"></param>
        public static void retrieveReserveInterfaceDA(MODES argMode, string argCaseId, ref IList<Reserve> argReserveList )
        {
            // Assert we are in DA Mode for this Method.
            if (!argMode.Equals(MODES.DA))
            {
                throw new HandledException("Interface Data for HQ and NB Can Only Be Retrieved for " + MODES.DA.ToString() + " Mode.");
            }

            SelectInterfaceTransactionTotalArg argHQ = new SelectInterfaceTransactionTotalArg(argCaseId, SelectReserveInterfaceAncestorArgs.CONFIG_NODENAME.HQPNODE_NAME);
            IList<MktHourMW> HQMWList = DBSqlMapper.Instance().QueryForList<MktHourMW>("SelectInterfaceTransactionTotal", argHQ, MODULE_NAME);
            SelectInterfaceTransactionTotalArg argNB = new SelectInterfaceTransactionTotalArg(argCaseId, SelectReserveInterfaceAncestorArgs.CONFIG_NODENAME.NBPNODE_NAME);
            IList<MktHourMW> NBMWList = DBSqlMapper.Instance().QueryForList<MktHourMW>("SelectInterfaceTransactionTotal", argNB, MODULE_NAME);

            foreach (Reserve reserve in argReserveList)
            {
                reserve.InterfaceHQ = 0.0;
                reserve.InterfaceNB = 0.0;

                foreach (MktHourMW hourMW in HQMWList)
                {
                    if (hourMW.MktHour.Equals(reserve.MktHour))
                    {
                        reserve.InterfaceHQ = hourMW.MW;
                    }
                }

                foreach (MktHourMW hourMW in NBMWList)
                {
                    if (hourMW.MktHour.Equals(reserve.MktHour))
                    {
                        reserve.InterfaceNB = hourMW.MW;
                    }
                }
            }

        }


        public static void uploadReserves(MODES argMode, IList<Reserve> argReserves)
        {
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER + ".UPLOAD");
                log.Info("Upload Reserves Begin");
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);

                foreach (Reserve reserveData in argReserves)
                {
                    DBSqlMapper.Instance().Update("UpdateMktAreaHourly", reserveData, MODULE_NAME);
                }
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
                log.Info("Upload Reserves Success");
                MessageBox.Show("Reserves Uploaded", "Success");
                
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                throw new HandledException("Reserves Upload Failed.  No Changes Made", ex, MODULE_LOGGER_ERROR);
            }
        }

        private static IList<Reserve> retrieveReserves(MODES argMode, string argCaseId)
        {
            string methodLastStep = "Retrieve Begin CaseId: " + argCaseId;
            try
            {
                ILog log = LogManager.GetLogger(MODULE_LOGGER + ".Retrieve");
                log.Info(methodLastStep);

                IList<Reserve> reserves = DBSqlMapper.Instance().QueryForList<Reserve>("SelectCaseHours", argCaseId, MODULE_NAME);

                methodLastStep = "Retrieve Before Load Contingencies 1 and 2";
                log.Info(methodLastStep);
                foreach (Reserve reserve in reserves)
                {
                    reserve.CaseId = argCaseId;
                    IList<string> contingencies = DBSqlMapper.Instance().QueryForList<string>("SelectReserveContingencies", reserve, MODULE_NAME);
                    int contingencyNumber = 1;
                    foreach (string contingencyString in contingencies)
                    {
                        double contingency = double.Parse(contingencyString);
                        if (contingencyNumber == 1) { reserve.Contingency1 = contingency; }
                        else if (contingencyNumber == 2) { reserve.Contingency2 = contingency; }
                        contingencyNumber++;

                        if (contingencyNumber > 2) { break; }
                    }

                    if (argMode.Equals(MODES.RAA_SCRA))
                    {
                        SelectReserveInterfaceHourlyArgs argsInterface = new SelectReserveInterfaceHourlyArgs(reserve.MktHour, SelectReserveInterfaceHourlyArgs.CONFIG_NODENAME.NBPNODE_TRANS);

                        reserve.InterfaceNB = DBSqlMapper.Instance().QueryForObject<double>("SelectReserveInterface", argsInterface,MODULE_NAME);


                        argsInterface = new SelectReserveInterfaceHourlyArgs(reserve.MktHour, SelectReserveInterfaceHourlyArgs.CONFIG_NODENAME.HQPNODE_TRANS);
                        reserve.InterfaceHQ = DBSqlMapper.Instance().QueryForObject<double>("SelectReserveInterface", argsInterface, MODULE_NAME);
                    }
                }
                methodLastStep = "Retrieve Before Load MISNB";
                log.Info(methodLastStep);
                System.Collections.Generic.IList<MktHourMW> misnbEntries = DBSqlMapper.Instance().QueryForList<MktHourMW>("SelectReserveMISNB", argCaseId, MODULE_NAME);
                foreach (MktHourMW misnbData in misnbEntries)
                {
                    foreach (Reserve r in reserves)
                    {
                        if (r.MktHour.Equals(misnbData.MktHour))
                        {
                            r.MISNBDB = misnbData.MW;
                        }
                    }
                }
                methodLastStep = "Retrieve Before Load MYS89";
                log.Info(methodLastStep);

                System.Collections.Generic.IList<MktHourMW> mys89Entries = DBSqlMapper.Instance().QueryForList<MktHourMW>("SelectReserveMYS89", argCaseId, MODULE_NAME);
                foreach (MktHourMW mys89Data in mys89Entries)
                {
                    foreach (Reserve r in reserves)
                    {
                        if (r.MktHour.Equals(mys89Data.MktHour))
                        {
                            r.MYS89 = mys89Data.MW;
                        }
                    }
                }

                methodLastStep = "Retrieve Completed";
                log.Info(methodLastStep);
                return reserves;
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodLastStep);
                throw;
            }
            catch (Exception ex)
            {
                throw new HandledException(methodLastStep, ex, MODULE_LOGGER_ERROR);
            }
        }


    }
}
