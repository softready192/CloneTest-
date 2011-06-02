using System;
using System.Collections.Generic;
using log4net;
using DA.Common;
using DA.DAIE.Common;


namespace DA.DAIE.RSCConstraint
{


    /// <summary>
    /// </summary>
    class RSCConstraintBO 
    {
        
        internal static string MODULE_NAME = "RSCConstraint";
        public static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        public static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;


        private const Decimal MAX_SENSITIVITY_DIFFERENCE = 0.005M;

        private const string CLEANUP_DELETE_MKT_CASE_CONSTRAINT            = "Cleanup.DeleteMktCaseConstraint";
        private const string CLEANUP_UPDATE_MKT_CONSTRAINT                 = "Cleanup.UpdateMktConstraint";
        private const string CLEANUP_DELETE_MKT_CONSTRAINT                 = "Cleanup.DeleteMktConstraint";

        private const string SELECT_RSC_CASE                               = "SelectRSCCase";

        private const string CREATE_SELECT_RSC_CONSTRAINT_RELATED_CASES    = "Create.SelectRSCConstraintRelatedCases";
        private const string CREATE_INSERT_MKT_CASE_CONSTRAINT             = "Create.InsertMktCaseConstraint";
        private const string CREATE_SELECT_HOURS_IN_DAY                    = "Create.SelectHoursInDay";
        private const string CREATE_SELECT_IN_RSC_RANGE                    = "Create.SelectInRSCRange";

        private const string CHECK_SENSITIVITY_SELECT_UNIT_SENSITIVITY     = "CheckSensitivity.SelectUnitSensitivity";
        private const string CHECK_SENSITIVITY_SELECT_PNODE_SENSITIVITY    = "CheckSensitivity.SelectPNodeSensitivity";

        private const string UPDATE_SENSITIVITY_SELECT_UNIT_SENSITIVITY    = "UpdateSensitivity.SelectUnitSensitivity";
        private const string UPDATE_SENSITIVITY_INSERT_UNIT_SENSITIVITY    = "UpdateSensitivity.InsertUnitSensitivity";
        private const string UPDATE_SENSITIVITY_SELECT_PNODE_SENSITIVITY   = "UpdateSensitivity.SelectPNodeSensitivity";
        private const string UPDATE_SENSITIVITY_INSERT_PNODE_SENSITIVITY   = "UpdateSensitivity.InsertPNodeSensitivity";
        
        private const string INSERT_CONSTRAINT_INSERT_MKT_CONSTRAINT       = "InsertConstraint.InsertMktConstraint";
        private const string INSERT_CONSTRAINT_SELECT_NEW_CONSTRAINT_ID    = "InsertConstraint.SelectNewConstraintId";
        private const string INSERT_CONSTRAINT_INSERT_MKT_CONSTRAINT_UNIT  = "InsertConstraint.InsertMktConstraintUnit";
        private const string INSERT_CONSTRAINT_INSERT_MKT_CONSTRAINT_PNODE = "InsertConstraint.InsertMktConstraintPNode";
        private const string INSERT_CONSTRAINT_INSERT_MKT_CASE_CONSTRAINT =  "InsertConstraint.InsertMktCaseConstraint";

    ///'****************************************************************
    ///'*
    ///'* SIR 24544:  This procedure removes all RSC constraints from the
    ///'* current case, sets the RSC enabled flag to zero for all RSC
    ///'* constraints, and deletes any unused RSC constraints.
    ///'*
    ///'****************************************************************
    /// Added argUpdateRSCEnforce - Allow calling method to avoid update of RSCEnforce flags.
        public static bool CleanupRSCConstraints(string argOriginalCaseId, bool argUpdateRSCEnforce )
        {
            DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
            try
            {
                int rc = DBSqlMapper.Instance().Delete(CLEANUP_DELETE_MKT_CASE_CONSTRAINT, argOriginalCaseId, MODULE_NAME);
                if (argUpdateRSCEnforce)
                {
                    rc = DBSqlMapper.Instance().Update(CLEANUP_UPDATE_MKT_CONSTRAINT, null, MODULE_NAME);
                }
                rc = DBSqlMapper.Instance().Delete(CLEANUP_DELETE_MKT_CONSTRAINT, null, MODULE_NAME);
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
                return true;
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                LogManager.GetLogger(MODULE_LOGGER_ERROR).Error("An error occured while disabling RSC constraints from the previous cases. ", ex);
                return false;
            }
        }

    ///'****************************************************************
    ///'*
    ///'* SIR 24544:  Determines if the selected case will run RSC.
    ///'*
    ///'****************************************************************
        internal static bool RSCCase(RSCConstraintArgs args)
        {
            Int16 executesc = (Int16)(DBSqlMapper.Instance().QueryForObject<Int16>(SELECT_RSC_CASE, args,MODULE_NAME));
            if (executesc == 1) { return true; } else { return false; }
        }

    ///'****************************************************************
    ///'*
    ///'* SIR 24544:  This procedure finds all SPD and SFT constraints
    ///'* from any other Day Ahead cases for the same market day and
    ///'* creates RSC constraints for the current case.
    ///'*
    ///'* Returns 0 if an error occurs
    ///'* Returns 1 if the constraints were created sucessfully
    ///'* Returns 2 if no sensitivities were found
    ///'* Returns 3 if the selected case does not include RSC
    ///'*
    ///'****************************************************************
        internal static int CreateRSCConstraints(RSCConstraintArgs args)
        {
            string methodPurpose = "Creates RSC constraints from Passed Case";
            try
            {
                
                    
                // Checks if the selected case includes RSC";
                if (RSCCase(args))
                {
                    //'Find all SPD and SFT constraints from other DA cases
                    // Argument Parameter RSCConstraintExcludeSql contains...   
                    //    a list of constraints that should be excluded regardless of marginal value
                    IList<RSCConstraintRelatedCases> relatedCaseConstraints = DBSqlMapper.Instance().QueryForList<RSCConstraintRelatedCases>(CREATE_SELECT_RSC_CONSTRAINT_RELATED_CASES, args, MODULE_NAME);
                    if (relatedCaseConstraints == null || relatedCaseConstraints.Count == 0)
                    {
                        return 2;
                    }
                    else
                    {
                        //'Create RSC constraints for all records returned

                        RSCConstraintRelatedCases previousCase = null;
                        Int64 priorRSCConstraintId = 0;
                        foreach (RSCConstraintRelatedCases relatedCase in relatedCaseConstraints)
                        {
                            relatedCase.OriginalCaseId = args.OriginalCaseId;
                            //'If this is the same case and constraint as the previous record then check to see
                            //'if the sensitivities match to determine if a different constraint is needed.
                            if (previousCase != null
                                && previousCase.CaseId.Equals(relatedCase.CaseId)
                                && previousCase.ConstraintName.Equals(relatedCase.ConstraintName))
                            {

                                //'If there are more unit or pnode sensitivities for the current constraint, add
                                //'them to the constraint that was already entered.
                                if (SensitivitiesMatch(previousCase, relatedCase, MAX_SENSITIVITY_DIFFERENCE ))
                                {

                                    //'Insert the constraint into the case for the additional hour
                                    MktCaseConstraintArgs argMktCaseConstraint = new MktCaseConstraintArgs(relatedCase, priorRSCConstraintId, 1);
                                    DBSqlMapper.Instance().Insert(CREATE_INSERT_MKT_CASE_CONSTRAINT, argMktCaseConstraint, MODULE_NAME);

                                    //'Check that constraint is not out of case date range
                                    MktCaseConstraintArgs argMktCaseConstrint = new MktCaseConstraintArgs(relatedCase, 1);
                                    int hrsInDay = (int)(DBSqlMapper.Instance().QueryForObject<decimal>(CREATE_SELECT_HOURS_IN_DAY, relatedCase, MODULE_NAME));
                                    //argMktCaseConstraint.TerminationHour = argMktCaseConstraint.EffectiveHour.AddHours(hrsInDay);

                                    int inCaseRange = (int)(DBSqlMapper.Instance().QueryForObject<decimal>(CREATE_SELECT_IN_RSC_RANGE, argMktCaseConstraint, MODULE_NAME));
                                    if (inCaseRange != 1)
                                    {
                                        argMktCaseConstraint = new MktCaseConstraintArgs(relatedCase, priorRSCConstraintId, 1);
                                        argMktCaseConstraint.EffectiveHour = relatedCase.MktHour.AddHours(hrsInDay);
                                        argMktCaseConstraint.TerminationHour = relatedCase.MktHour.AddHours(hrsInDay).AddHours(1);
                                        DBSqlMapper.Instance().Insert(CREATE_INSERT_MKT_CASE_CONSTRAINT, argMktCaseConstraint, MODULE_NAME);
                                    }

                                    //'Add new sensitivities
                                        UpdateSensitivities(relatedCase, priorRSCConstraintId);
                                }
                                else
                                {
                                    // 'The sensitivities are not within the specified tolerance, so add a new constraint.
                                    //strPrevRSCConstr = InsertConstraint(rstResult!rscconstraintname, rstResult!operation, _
                                    //                    rstResult!mkthour, rstResult!caseID, rstResult!rhs, _
                                    //                    rstResult!constraintname)
                                    priorRSCConstraintId = InsertConstraint(relatedCase);
                                }


                            }
                            else 
                            {
                                // 'This is a different constraint or case, so add a new constraint
                                //strPrevRSCConstr = InsertConstraint(rstResult!rscconstraintname, rstResult!operation, _
                                //                        rstResult!mkthour, rstResult!caseID, rstResult!rhs, _
                                //                        rstResult!constraintname)
                                priorRSCConstraintId = InsertConstraint(relatedCase);
                            }
                            previousCase = relatedCase;    
                        }
                        return 1;
                    }
                }
                else
                {
                    return 3;
                }
            }
            catch (HandledException he)
            {
                he.setLoggerNameLogExisting(MODULE_LOGGER_ERROR);
                he.addCustomStackMessage(methodPurpose);
                throw;
            }
            catch( Exception ex )
            {
                throw new HandledException(methodPurpose, ex, MODULE_LOGGER_ERROR);
            }
            
        }

 

        /// <summary>
        /// '****************************************************************
        /// '*
        /// '* SIR 24544:  Creates a new RSC constraint and inserts it into
        /// '* the selected case
        /// '*
        /// '****************************************************************
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static System.Int64 InsertConstraint(RSCConstraintRelatedCases argRelatedCase)
        {
            MktCaseConstraintArgs arg = new MktCaseConstraintArgs(argRelatedCase, 1);
            DBSqlMapper mapper = DBSqlMapper.Instance();
            //'Create new record in mktconstraint
            mapper.Insert(INSERT_CONSTRAINT_INSERT_MKT_CONSTRAINT, argRelatedCase, MODULE_NAME);

            long constraintId = (long)((mapper.QueryForObject<decimal>(INSERT_CONSTRAINT_SELECT_NEW_CONSTRAINT_ID, argRelatedCase.RSCConstraintName, MODULE_NAME)));
            arg.ConstraintId = constraintId;
            mapper.Insert(INSERT_CONSTRAINT_INSERT_MKT_CONSTRAINT_UNIT, arg, MODULE_NAME);
            mapper.Insert(INSERT_CONSTRAINT_INSERT_MKT_CONSTRAINT_PNODE, arg, MODULE_NAME);
            mapper.Insert(INSERT_CONSTRAINT_INSERT_MKT_CASE_CONSTRAINT, arg, MODULE_NAME);

            //'Check that constraint is not out of case date range
            int hrsInDay = (int)((DBSqlMapper.Instance().QueryForObject<decimal>(CREATE_SELECT_HOURS_IN_DAY, argRelatedCase, MODULE_NAME)));
            int inCaseRange = (int)(DBSqlMapper.Instance().QueryForObject<decimal>(CREATE_SELECT_IN_RSC_RANGE, arg, MODULE_NAME));
            if (inCaseRange != 1)
            {
                arg.EffectiveHour = argRelatedCase.MktHour.AddHours(hrsInDay);
                arg.TerminationHour = argRelatedCase.MktHour.AddHours(hrsInDay).AddHours(1);
                mapper.Insert(INSERT_CONSTRAINT_INSERT_MKT_CASE_CONSTRAINT, arg, MODULE_NAME);
            }
            return (Int64)constraintId;

        }


        /// <summary>
        ///     '****************************************************************
        ///     '*
        ///     '* SIR 24544:  Determines if sensitivities for a given constraint
        ///     '* are equilivant between hours.
        ///     '*
        ///     '****************************************************************
        /// </summary>
        /// <param name="argPriorCase"></param>
        /// <param name="argRelatedCase"></param>
        /// <param name="argMaxSensitivityDiff"></param>
        /// <returns>true if Differences for Unit and PNode sensitivities are both within the specified tolerance.</returns>
        private static bool SensitivitiesMatch(RSCConstraintRelatedCases argPriorCase, RSCConstraintRelatedCases argCurrentCase, Decimal argMaxSensitivityDiff )
        {
            MktCaseConstraintArgs arg = new MktCaseConstraintArgs(argPriorCase, 1);
            arg.TerminationHour = argCurrentCase.MktHour;

            //'Check the unit sensitivities first.  If they are within the specified tolerance then
            //'check the pnode sensitivities.
            Decimal maxUnitsDiff = (DBSqlMapper.Instance().QueryForObject<decimal>(CHECK_SENSITIVITY_SELECT_UNIT_SENSITIVITY, arg, MODULE_NAME));
            if (maxUnitsDiff <= argMaxSensitivityDiff)
            {
                //'check the pnode sensitivities.
                Decimal maxPNodesDiff = (DBSqlMapper.Instance().QueryForObject<decimal>(CHECK_SENSITIVITY_SELECT_PNODE_SENSITIVITY, arg, MODULE_NAME));
                return maxPNodesDiff <= argMaxSensitivityDiff;
            }
            else
            {
                return false;
            }
                                    
        }

        /// <summary>
        ///  '****************************************************************
        /// '*
        /// '* SIR 24544: If there are more unit or pnode sensitivities for
        /// '* the current constraint, add them to the constraint that was
        /// '* already entered.
        /// '*
        /// '****************************************************************
        /// </summary>
        /// <param name="argRelatedCase"></param>
        private static void UpdateSensitivities(RSCConstraintRelatedCases argRelatedCase, Int64 argPriorRSCConstraintId)
        {
            MktCaseConstraintArgs args = new MktCaseConstraintArgs(argRelatedCase,1);
            args.RSCConstraintId = argPriorRSCConstraintId;

            // 'Add unit sensitivities
            IList<int> unitIdList = DBSqlMapper.Instance().QueryForList<int>(UPDATE_SENSITIVITY_SELECT_UNIT_SENSITIVITY, args, MODULE_NAME);
            foreach (int unitId in unitIdList)
            {
                args.UnitId = unitId;
                DBSqlMapper.Instance().Insert(UPDATE_SENSITIVITY_INSERT_UNIT_SENSITIVITY, args, MODULE_NAME);
            }

            // 'Add pnode sensitivities
            IList<Int64> pnodeIdList = DBSqlMapper.Instance().QueryForList<Int64>(UPDATE_SENSITIVITY_SELECT_PNODE_SENSITIVITY, args, MODULE_NAME);
            foreach (Int64 pnodeId in pnodeIdList)
            {
                args.PNodeId = pnodeId;
                DBSqlMapper.Instance().Insert(UPDATE_SENSITIVITY_INSERT_PNODE_SENSITIVITY, args, MODULE_NAME);
            }
        }

    }
}
