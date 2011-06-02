using System;
using System.IO;
using DA.Common;
using log4net;
using Microsoft.Win32;
using System.Reflection;
using System.Windows;
using DA.DAIE.CaseSelection;
using System.Text;
using DA.DAIE.Common;
using System.Collections.Generic;
using DA.DAIE.Connections;

namespace DA.DAIE.FileUpload
{
    public class ClearMMMBidsArg : AncestorArg
    {
        int _CongestedScheduleTypeId;
        string _CaseId;

        public override string ToString()
        {
            base.Add("CaseId",_CaseId);
            base.Add("CongestedScheduleTypeId",_CongestedScheduleTypeId);
            return base.ToLogString();
        }

        public ClearMMMBidsArg(int argCongestedScheduleTypeId, string argCaseId)
        {
            this._CaseId = argCaseId;
            this._CongestedScheduleTypeId = argCongestedScheduleTypeId;
        }

        public int CongestedScheduleTypeId
        {
            get { return this._CongestedScheduleTypeId; }
        }

        public string CaseId
        {
            get { return this._CaseId; }
        }

    }

    class MMMBidBO
    {
        private static string MODULE_NAME = "MMMBid";
        public static string MODULE_LOGGER = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        public static string MODULE_LOGGER_ERROR = App.LOGGER_USER_ERROR + "." + MODULE_NAME;


        public static void LoadMMMFile( string argCaseId, MODES argMode)
        {
            ILog userLogger = LogManager.GetLogger(MODULE_LOGGER);
            userLogger.Info("Begin upload of MMM bid file for case " + argCaseId);
            int processedFileLines = 0;

            string mmmFileName = selectMMMFileName();
            if (mmmFileName != null)
            {
                userLogger.Info("MMM file " + mmmFileName + " was selected.");
                processedFileLines = ProcessFile(mmmFileName, argCaseId, argMode);
                MessageBox.Show("Processed " + processedFileLines + " MMM Records from File: " + mmmFileName, "Upload MMM Complete");
            }
            else
            {
                userLogger.Info("No MMM file was selected.  End process.");
                MessageBox.Show("No MMM file was selected", "Nothing To Process");
            }
        }

       /// <summary>
       /// '***************************************************************
       /// '*
       /// '* This procedure reads the CSV file supplied by MMM
       /// '* and writes the data to the markets database.  If an
       /// '* error is encountered in a record, that record will
       /// '* be skipped and processing will continue with the next record.
       /// '*
       /// '***************************************************************
       /// </summary>
       /// <param name="argFileName"></param>
       /// <param name="argCaseId"></param>

        private static int ProcessFile(string argFileName, string argCaseId, MODES argMode)
        {

            List<HandledException> errors = new List<HandledException>();
            DateTime localFileDate = DateTime.Now;
            ILog userLogger = LogManager.GetLogger(MODULE_LOGGER);
            userLogger.Info("Reading bid data from " + argFileName );
            
            //'Open the MMM file and loop through all records on the file
            StreamReader inFile = new StreamReader(argFileName);
            bool firstLine = true;
            string line;
            int processedLineCount = 0;
            while ((line = inFile.ReadLine()) != null)
            {
                MMMBid bidData = null;
                try
                {

                    string[] lineArray = line.Split(',');
                    int unitScheduleId = int.Parse(lineArray[0]);
                    DateTime localEffectiveDate = DateTime.Parse(lineArray[1]);
                    string scheduleTypeString = lineArray[2];
                    double noLoadPrice = double.Parse(lineArray[3]);
                    double coldStartPrice = double.Parse(lineArray[4]);
                    double interStartPrice = double.Parse(lineArray[5]);
                    double hotStartPrice = double.Parse(lineArray[6]);
                    int useBidSlope = int.Parse(lineArray[7]);

                    bidData = new MMMBid(
                        unitScheduleId, localEffectiveDate, scheduleTypeString, noLoadPrice, coldStartPrice, interStartPrice, hotStartPrice, useBidSlope);

                    int leadingIndex = 8;
                    int maxPriceMW = 10 + leadingIndex;
                    int arrayLength = lineArray.Length;
                    if (arrayLength > leadingIndex + 20)
                    {
                        throw new HandledException("Too many price/MW pairs.");
                    }
                    bool priceMWDone = false;

                    double priorMW = 0.0;
                    double priorPrice = 0.0;

                    while (!priceMWDone)
                    {
                        double mw = double.Parse(lineArray[leadingIndex]);
                        double price = double.Parse(lineArray[leadingIndex + 1]);
                        if (mw < priorMW || price < priorPrice)
                        {
                            throw new HandledException("MW-price pairs must have increasing values.");
                        }
                        else
                        {
                            priorMW = mw;
                            priorPrice = price;
                        }

                        bidData.addBidPrice(price, mw);
                        leadingIndex += 2;
                        if (leadingIndex + 1 > maxPriceMW || leadingIndex + 2 > lineArray.Length)
                        { priceMWDone = true; }
                    }

                    int bidCount = bidData.BidPriceMWs.Count;
                    while ( bidCount < 10)
                    {
                        double mw = 0.0;
                        double price = 0.0;
                        bidData.addBidPrice(price, mw);
                        bidCount++;
                    }

                }
                catch (HandledException he)
                {
                    errors.Add(he);
                    continue;
                }
                catch( Exception ex )
                {
                    errors.Add( new HandledException("Error Parsing Bid Data", ex ));
                    continue;
                }


                if( firstLine )
                {
                    firstLine = false;
                    CaseHourBO caseHours = new CaseHourBO(argCaseId);
                    localFileDate = bidData.LocalEffectiveDate;
                    if( !caseHours.VerifyFileDate(bidData.LocalEffectiveDate))
                    {
                         throw new HandledException("The date on the MMM file does not match the market day of the selected case.", 50001);
                    }

                    try
                    {
                        DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                        ClearMMMBidsArg arg = new ClearMMMBidsArg(MMMBid.CODE_CONGESTED_SCHEDULE, argCaseId);
                        DBSqlMapper.Instance().Insert("ClearMMMBids", arg, MODULE_NAME);
                        DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);

                    }
                    catch (Exception ex)
                    {
                        LogManager.GetLogger(MODULE_LOGGER_ERROR).Error("Failed to clear MMM bids.  " + ex.Message);
                        DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                        throw new HandledException("Clearing of old MMM bids failed.", ex);
                    }
                }

                try
                {
                    WriteMMMToDB(bidData);
                    processedLineCount += 1;
                }
                catch (HandledException he)
                {
                    errors.Add(he);
                }
                catch (Exception ex)
                {
                    errors.Add(new HandledException("Exception Adding Bid Data", ex));
                }


            }
            inFile.Close();

            MMMMktUnitScheduleArg updateArg = new MMMMktUnitScheduleArg(localFileDate, argMode);
            UpdateMktUnitSchedule(updateArg);

            if( errors.Count > 0 )
            {
                StringBuilder sb = new StringBuilder();
                int errorCount = 0;
                foreach( HandledException he in errors )
                {
                    errorCount++;
                    he.logOnly("Exception " + errorCount + " of " + errors.Count + " Errors");

                    sb.Append(he.Message + "\n\r");

                }
                MessageBox.Show(sb.ToString(), errors.Count + " Errors Occured Processing MMM Bids");
            }
            return processedLineCount;


        }

        private static void UpdateMktUnitSchedule( MMMMktUnitScheduleArg arg )
        {

            try
            {
                //  'Delete all remaining records with $0/0MW (Price/MW) pairs from v_mktunitschedulebiddaily table - SPR 29384
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                DBSqlMapper.Instance().Delete("MMMDeleteZeroPriceMWBids", arg, MODULE_NAME);
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
            }
            catch (HandledException he)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                he.showError("MMM Bids Were Uploaded, But Failed to Delete Zero Price MW Pairs");
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                HandledException he = new HandledException("Failed to Delete Zero Price MW Pairs", ex);
                he.showError("MMM Bids Were Uploaded, But Failed to Delete Zero Price MW Pairs");
            }

            try
            {
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                DBSqlMapper.Instance().Update("MMMUpdateUnitScheduleDaily", arg, MODULE_NAME);
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
            }
            catch (HandledException he)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                he.showError("MMM Bids Were Uploaded, But Failed to Update MktUnitScheduleDaily");
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                HandledException he = new HandledException("Failed to Update MktUnitScheduleDaily", ex);
                he.showError("MMM Bids Were Uploaded, But Failed to Update MktUnitScheduleDaily");
            }

            try
            {
                DBSqlMapper.Instance().BeginTransaction(MODULE_NAME);
                DBSqlMapper.Instance().Insert("MMMInsertUnitScheduleAvail", arg, MODULE_NAME);
                DBSqlMapper.Instance().CommitTransaction(MODULE_NAME);
            }
            catch (HandledException he)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                he.showError("MMM Bids Were Uploaded, But Failed to Insert mktunitscheduleavail");
            }
            catch (Exception ex)
            {
                DBSqlMapper.Instance().RollBackTransaction(MODULE_NAME);
                HandledException he = new HandledException("Failed to Insert MktUnitScheduleAvail", ex);
                he.showError("MMM Bids Were Uploaded, But Failed to Insert MktUnitScheduleAvail");
            }

        }

        private static void WriteMMMToDB(MMMBid argMMMBid)
        {
            MMMUnitName unitName = DBSqlMapper.Instance().QueryForObject<MMMUnitName>("SelectUnitName", argMMMBid, MODULE_NAME);
            if (unitName == null)
            {
                throw new HandledException("Unit " + argMMMBid.UnitId + " not found in market database.");
            }
            else
            {
                argMMMBid.UnitShortName = unitName.UnitShortName;
                argMMMBid.UnitLongName = unitName.UnitLongName;
            }

            MMMUnitName unitScheduleName = DBSqlMapper.Instance().QueryForObject<MMMUnitName>("SelectUnitScheduleName", argMMMBid, MODULE_NAME);

            if (unitScheduleName == null)
            {
                DBSqlMapper.Instance().Insert("InsertMktUnitSchedule", argMMMBid, MODULE_NAME);
            }
            else
            {
                if (   !unitScheduleName.UnitLongName.Equals(argMMMBid.CalcUnitLongName)
                    && !unitScheduleName.UnitShortName.Equals(argMMMBid.CalcUnitShortName))
                {
                    DBSqlMapper.Instance().Update("UpdateMktUnitSchedule", argMMMBid, MODULE_NAME);
                }
            }

            int segmentIndex = 1;
            foreach (BidPriceMW bidPriceMW in argMMMBid.BidPriceMWs)
            {
                bidPriceMW.SetBidAttributes(argMMMBid);
                bidPriceMW.SegmentId = segmentIndex;
                DBSqlMapper.Instance().Insert("InsertMktUnitScheduleBidDaily", bidPriceMW, MODULE_NAME);
                segmentIndex += 1;
            }

            DBSqlMapper.Instance().Insert("InsertMktUnitScheduleDaily", argMMMBid, MODULE_NAME);

        }


        /// <summary>
        /// Private Method.
        /// Selects a single GRT file for upload.
        /// </summary>
        /// <returns></returns>
        private static string selectMMMFileName()
        {

            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.DefaultExt = ".csv";
                dlg.Filter = "CSV documents (.csv)|*.csv";

                NetworkFolderMeta folderMeta = ConnectionsParser.getNetworkFolderMeta("MMMBidFolder", ConnectionsParser.CONNECTIONS_FILE_NAME);
                dlg.InitialDirectory = folderMeta.getFullName("");

                //dlg.InitialDirectory = "\\\\rtsmbdev\\mmm";

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    // return selected file name.
                    return dlg.FileName;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

        }

    }
}
