using System.Net;
using System.IO;
using System;
using log4net;
using DA.Common;

namespace DA.DAIE.Connections
{

    public class FTPUtil
    {
        private const string MODULE_NAME = "FTP";
        public const string LOGGER_NAME = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        public const string LOGGER_ERROR_NAME = App.LOGGER_USER_ERROR + "." + MODULE_NAME;

        public static ILog log = LogManager.GetLogger(LOGGER_NAME);
        public static ILog logError = LogManager.GetLogger(LOGGER_ERROR_NAME);

        public const string CONNECTION_FILE_NAME = "Connections.xml";

        /// <summary>
        /// Test FTP Connection
        ///   Opens Connection 
        ///   Calls WCF WebRequestMethod.FTP.ListDirectory and Reads Results.
        /// </summary>
        /// <param name="ftpLoginMeta">Metadata for FTP connection</param>
        public static void ftpTestConnection(FTPLoginMeta ftpLoginMeta)
        {
            try
            {
                log.Info("Test FTP Connection: " + ftpLoginMeta.ToString());
           
                //create ftp object 
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create("ftp://" + ftpLoginMeta.Server + "/" + ftpLoginMeta.Path + "/");
                request.Credentials = new NetworkCredential(ftpLoginMeta.UserName, ftpLoginMeta.Password.DecryptedValue);

                request.KeepAlive = true;
                request.UseBinary = true;
                request.UsePassive = true;
                request.Method = System.Net.WebRequestMethods.Ftp.ListDirectory;

                System.Net.FtpWebResponse response = (System.Net.FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                while (!reader.EndOfStream)
                {
                    string strTemp = reader.ReadLine();
                }
                reader.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                throw new HandledException("Test FTP Failed With Exception For Connection: " + ftpLoginMeta.ToString(), ex, LOGGER_ERROR_NAME);
            }
        }

        /// <summary>
        /// Purges Old Files from Satellite FTP Site.
        /// Info for Directory and How Long To Keep Files 
        ///   is contained in the metadata for the connection.  - FTPLoginMeta.
        /// </summary>
        /// <param name="argCurrentTime"></param>
        /// <param name="ftpLoginMeta"></param>
        public static void ftpPurgeSatelliteFiles(DateTime argCurrentTime, FTPLoginMeta ftpLoginMeta)
        {
            try
            {
                log.Info("Purge Old Satellite Files from FTP Connection: " + ftpLoginMeta.ToString());
            
                //create ftp object 
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create("ftp://" + ftpLoginMeta.Server + "/" + ftpLoginMeta.Path + "/" );
                request.Credentials = new NetworkCredential(ftpLoginMeta.UserName, ftpLoginMeta.Password.DecryptedValue);
                request.KeepAlive = false;
                request.UseBinary = true;
                request.UsePassive = true;
                request.Method = System.Net.WebRequestMethods.Ftp.ListDirectory;

                System.Net.FtpWebResponse response = (System.Net.FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                while (!reader.EndOfStream)
                {
                    string strTemp = reader.ReadLine();

                    if (strTemp.ToLower().EndsWith(".csv"))
                    {
                        string fileName = strTemp;
                        FtpWebRequest request2 = (FtpWebRequest)FtpWebRequest.Create("ftp://" + ftpLoginMeta.Server + "/" + ftpLoginMeta.Path + "/" + fileName );
                        request2.Credentials = new NetworkCredential(ftpLoginMeta.UserName, ftpLoginMeta.Password.DecryptedValue);
                        request2.Method = System.Net.WebRequestMethods.Ftp.GetDateTimestamp;
                        request2.KeepAlive = false;
                        request2.UseBinary = true;
                        request2.UsePassive = true;

                        System.Net.FtpWebResponse response2 = (System.Net.FtpWebResponse)request2.GetResponse();
                        DateTime lastModified = response2.LastModified;
                        response2.Close();
                        int FileDaysOld = DateTime.Now.Subtract(lastModified).Days;

                        if (FileDaysOld > ftpLoginMeta.KeepFileDays )
                        {
                            FtpWebRequest request3 = (FtpWebRequest)FtpWebRequest.Create("ftp://" + ftpLoginMeta.Server + "/" + ftpLoginMeta.Path + "/" + fileName);
                            request3.Credentials = new NetworkCredential(ftpLoginMeta.UserName, ftpLoginMeta.Password.DecryptedValue);
                            request3.Method = System.Net.WebRequestMethods.Ftp.DeleteFile;
                            request3.KeepAlive = false;
                            request3.UseBinary = true;
                            request3.UsePassive = true;

                            System.Net.FtpWebResponse response3 = (System.Net.FtpWebResponse)request3.GetResponse();
                            if (response3.StatusCode == FtpStatusCode.CommandOK)
                            {
                                log.Info("Purge File: " + fileName + " Status: " + response.StatusDescription);
                            }
                            else
                            {
                                log.Error("Purge File: " + fileName + " Status: " + response.StatusDescription);
                            }
                            response3.Close();
                        }
                    }
                }
                reader.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable To Purge Old Satellite Files from FTP Connection: " + ftpLoginMeta.ToString(), ex, LOGGER_ERROR_NAME);
            }
        }

        /// <summary>
        /// Uploads File to FTP site.
        /// </summary>
        /// <param name="argInputFilePath">File to Upload.</param>
        /// <param name="ftpLoginMeta">Metadata to Make FTP Connection.</param>
        public static void ftpUpload(string argInputFilePath, FTPLoginMeta ftpLoginMeta)
        {
            try
            {

                log.Info("Uploading File: " + argInputFilePath + " with FTP: " + ftpLoginMeta.ToString());

                System.IO.FileInfo f = new System.IO.FileInfo(argInputFilePath);


                //create ftp object 
                string ftpRequestString = "ftp://"
                    + ftpLoginMeta.Server + "/"
                    + (ftpLoginMeta.Path != null && ftpLoginMeta.Path.Length > 0 ? ftpLoginMeta.Path + "/" : "")
                    + f.Name;
                FtpWebRequest ftp = (FtpWebRequest)FtpWebRequest.Create(ftpRequestString);
                
                //userid and password for the ftp server 
                ftp.Credentials = new NetworkCredential(ftpLoginMeta.UserName, ftpLoginMeta.Password.DecryptedValue);
                //Set keepalive property true to multiple use of same instance   
                ftp.KeepAlive = false;
                //use binary method to upload data 
                ftp.UseBinary = false;
                //set action (upload or download 
                ftp.Method = WebRequestMethods.Ftp.UploadFile;
                //open file in filesteam 
                FileStream fs = File.OpenRead(argInputFilePath);
                byte[] buffer = new byte[fs.Length];
                //read the file in filestream 
                fs.Read(buffer, 0, buffer.Length);
                //close the filesteam 
                fs.Close();
                //now open ftpsteam 
                Stream ftpstream = ftp.GetRequestStream();
                //write at ftp 
                ftpstream.Write(buffer, 0, buffer.Length);
                //close the ftp stream 
                ftpstream.Close();

                
            }
            catch (Exception ex)
            {
                throw new HandledException("Unable To Upload File: " + argInputFilePath + " with FTP: " + ftpLoginMeta.ToString(), ex, LOGGER_ERROR_NAME); 
            }
        }
    }
}
