using System;
using System.Collections.Generic;
using System.Text;
using DA.DAIE.CaseSelection;
using System.Windows;
using DA.Common;
using log4net;
using DA.DAIE.Common;

namespace DA.DAIE.DataValidation
{
    class DataValidationBO
    {

        internal static string MODULE_NAME = "DataValidation";
        internal static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;

        public static IList<DataValidation> retrieve(string argCaseId, MODES argMode, Window argWindow )
        {
            string originalTitle = argWindow.Title;
            ILog log = LogManager.GetLogger(MODULE_LOGGER);
            log.Info("Retrieve Data Validations Begin for CaseId:" + argCaseId + " Mode:" + argMode);

            IList<string> validationIds = DataValidationConfig.getIDs(argMode);
            DataValidationArgs args = new DataValidationArgs(argMode, argCaseId);
            IList<DataValidation> tempList = new List<DataValidation>();
            IList<DataValidation> validations = new List<DataValidation>();

            // Holds exceptions that occured along the way.
            IList<HandledException> validationExceptions = new List<HandledException>();
                        
            foreach (string validationId in validationIds)
            {
                
                try
                {
                    // Show user progress as each sql is run.
                    argWindow.Title = originalTitle + " " + validationId;

                    bool hasParameters = DBSqlMapper.Instance().HasParameters(validationId);
                    if (hasParameters)
                    {
                        tempList = DBSqlMapper.Instance().QueryForList<DataValidation>(validationId, args, MODULE_NAME);
                    }
                    else
                    {
                        tempList = DBSqlMapper.Instance().QueryForList<DataValidation>(validationId, null, MODULE_NAME);
                    }
                    foreach (DataValidation dataValidation in tempList )
                    {
                        validations.Add(dataValidation);
                    }

                }
                catch (HandledException he)
                {
                    he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                    he.addCustomStackMessage("Error in DataValidation for ID: " + validationId);
                    handleValidationException(validationId, he, ref validations, ref validationExceptions);
                }
                catch (Exception ex)
                {
                    HandledException he = new HandledException("Error in DataValidation for ID: " + validationId, ex);
                    handleValidationException(validationId, he, ref validations, ref validationExceptions);
                }
            }

            if (validationExceptions.Count > 0)
            {
                StringBuilder exceptionsDisplayString = new StringBuilder();
                foreach (HandledException he in validationExceptions)
                {
                    exceptionsDisplayString.Append(he.Message + "\n\r");
                }
                LogManager.GetLogger(MODULE_LOGGER_ERROR).Error(validationExceptions.Count.ToString() + " Exception(s) occured during Data Validation for CaseId:" + argCaseId + " + Mode:" + argMode + " Exceptions:" + exceptionsDisplayString.ToString());
                MessageBox.Show("Exceptions: \n\r \n\r " + exceptionsDisplayString.ToString(), "CAUTION!  Exceptions Occured During Data Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            argWindow.Title = originalTitle;
            log.Info("Retrieve Data Validations Finished for CaseId:" + argCaseId + " Mode:" + argMode);
            return validations;
        }


        private static void handleValidationException(string validationId, HandledException argHe, ref IList<DataValidation> validationList, ref IList<HandledException> argValidationExceptions)
        {
            argHe.logOnly();
            DataValidation badValidation = new DataValidation(validationId, "!!NOT RUN!! - Exception Occured Running This Data Validation - " + argHe.Message, "Page_IT");
            validationList.Add(badValidation);
            argValidationExceptions.Add(argHe);
        }

    }
}
