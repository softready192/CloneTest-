using System;
using System.Collections.Generic;
using log4net;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using DA.DAIE.Connections;

namespace DA.DAIE.PIC
{
    class PICFileParser
    {

        public static IList<PICData> LoadFile(string argCaseId, ref string argSelectedUploadFileName)
        {

            IList<PICData> dataList = null;

            string methodPurpose = "Upload PIC File";
            ILog log = LogManager.GetLogger(PICBO.MODULE_LOGGER);
            log.Info(methodPurpose + " Begin for case: " + argCaseId);

            string picFileName = selectFileName();
            if (picFileName != null)
            {
                log.Info("file " + picFileName + " was selected.");
                dataList = ParseFile(picFileName);
                argSelectedUploadFileName = picFileName;
                return dataList;
            }
            else
            {
                log.Info("No file was selected.  End process.");
                return null;
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

        private static IList<PICData> ParseFile(string argFileName)
        {
            ILog log = LogManager.GetLogger(PICBO.MODULE_LOGGER);
            log.Info("Reading bid data from " + argFileName);

            List<PICData> dataList = new List<PICData>();

            //'Open the MMM file and loop through all records on the file
            StreamReader inFile = new StreamReader(argFileName);
            string line;
            while ((line = inFile.ReadLine()) != null)
            {
                string[] lineArray = line.Split(',');
                PICData picData = new PICData(lineArray[0], lineArray[1], lineArray[2]);
                dataList.Add(picData);
            }
            inFile.Close();
            return dataList;
        }


        /// <summary>
        /// Private Method.
        /// Selects a single GRT file for upload.
        /// </summary>
        /// <returns></returns>
        private static string selectFileName()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            try
            {
                dlg.DefaultExt = ".csv";
                dlg.Filter = "CSV documents (.csv)|*.csv";

                NetworkFolderMeta folderMeta = ConnectionsParser.getNetworkFolderMeta("MMMPICInputFolder", ConnectionsParser.CONNECTIONS_FILE_NAME);
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
