using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DA.Common;
using log4net;

namespace DA.DAIE.Connections
{
    public class ConnectionsParser
    {

        internal static string MODULE_NAME = "Connection.Config";
        internal static string LOGGER_NAME = App.LOGGER_USER_ACCESS + "." + MODULE_NAME;
        internal static string LOGGER_ERROR_NAME = App.LOGGER_USER_ERROR + "." + MODULE_NAME;

        public static ILog log = LogManager.GetLogger(LOGGER_NAME);
        public static ILog logError = LogManager.GetLogger(LOGGER_ERROR_NAME);

        public static string CONNECTIONS_FILE_NAME = "Connections.xml";
        public static string getConnectionsFileName(string argBaseDirectory)
        {
            return argBaseDirectory + "\\" + CONNECTIONS_FILE_NAME;
        }



        public static List<ConnectionPassword> getConnectionPasswordList(string argConnectionsFileName)
        {

            string methodPurpose = "Get Password List For All External Resources from file: " + argConnectionsFileName;

            try
            {
                List<ConnectionPassword> returnList = new List<ConnectionPassword>();

                XmlDocument doc = new XmlDocument();
                doc.Load(argConnectionsFileName);

                // Get list of DB Connections.
                returnList.AddRange(getConnectionPasswordListForType("//databases", "database", doc));

                returnList.AddRange(getConnectionPasswordListForType("//ftplist", "ftp", doc));

                returnList.AddRange(getConnectionPasswordListForType("//networkfolders", "networkfolder", doc));

                returnList.AddRange(getConnectionPasswordListForType("//jasperservers", "jasperserver", doc));

                return returnList;
            }
            catch (Exception ex)
            {
                HandledException he = new HandledException( methodPurpose, ex, LOGGER_ERROR_NAME);
                throw he;
            }

        }

        private static List<ConnectionPassword> getConnectionPasswordListForType(string argXPathRoot, string argDataSourceType, XmlDocument argDoc)
        {
            // Get list of Encrypted Connections.
            List<ConnectionPassword> ecs = new List<ConnectionPassword>();

            XmlNodeList dataSourceNames = argDoc.SelectNodes(argXPathRoot + "/" + argDataSourceType);
            foreach (XmlNode xmlNode in dataSourceNames)
            {
                XmlAttribute attributeDataSourceName = xmlNode.Attributes["name"];
                string dataSourceName = (attributeDataSourceName == null ? "" : attributeDataSourceName.Value);

                ConnectionPassword ec = getConnectionPasswordMeta(argXPathRoot, dataSourceName, argDataSourceType, argDoc);
                ecs.Add(ec);
            }
            return ecs;
        }

        public static ConnectionPassword getConnectionPasswordMeta(string argXPathRoot, string argDataSourceName, string argDataSourceType, XmlDocument argDoc)
        {
            string xPath = argXPathRoot + "/" + argDataSourceType + "[@name='" + argDataSourceName + "']/dataSource";
            XmlNode xmlNode = argDoc.SelectSingleNode(argXPathRoot + "/" + argDataSourceType + "[@name='" + argDataSourceName + "']/dataSource");
            if (xmlNode != null)
            {
                XmlAttribute attributeUserName = xmlNode.Attributes["username"];
                XmlAttribute attributePassword = xmlNode.Attributes["password"];
                XmlAttribute attributeEncrypted = xmlNode.Attributes["encrypted"];

                if (argDataSourceType.ToLower().Equals("database"))
                {
                    attributeUserName = xmlNode.Attributes["connectionString"];
                }

                string username = (attributeUserName == null ? "" : attributeUserName.Value);
                string password = (attributePassword == null ? "" : attributePassword.Value);
                bool encrypted = (attributeEncrypted == null ? false : attributeEncrypted.Value.ToLower().Equals("true"));
                return new ConnectionPassword(argDataSourceName, argDataSourceType, xPath, username, new Encryptable(password, encrypted));
            }
            else
            {
                return null;
            }
        }

        public static void saveConnectionPasswordList(List<ConnectionPassword> argConnectionPasswords, string argConnectionsFileName)
        {
            foreach( ConnectionPassword cp in argConnectionPasswords )
            {
                saveConnectionPasswordMeta( cp, argConnectionsFileName );
            }
        }

        public static void saveConnectionPasswordMeta(ConnectionPassword argConnectionPassword, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(argConnectionsFileName);

            XmlNode loginNode = doc.SelectSingleNode(argConnectionPassword.XPath);

            if (loginNode != null)
            {
                XmlAttribute attributeUserName = null;
                XmlAttribute attributePassword = loginNode.Attributes["password"];
                XmlAttribute attributeEncrypted = loginNode.Attributes["encrypted"];

                if (argConnectionPassword.Type.ToLower().Equals("database"))
                {
                    attributeUserName = loginNode.Attributes["connectionString"];
                }
                else
                {
                    attributeUserName = loginNode.Attributes["username"];
                }

                if (attributeUserName == null )
                {
                    XmlAttribute attribute = null;
                    if (argConnectionPassword.UserName != null && argConnectionPassword.UserName.Length > 0)
                    {
                        if (argConnectionPassword.Type.Equals("database"))
                        {
                            attribute = doc.CreateAttribute("connectionString");
                        }
                        else
                        {
                            attribute = doc.CreateAttribute("username");
                        }
                        attribute.Value = argConnectionPassword.UserName;
                        loginNode.Attributes.Append(attribute);
                    }
                }
                else
                {
                    if (argConnectionPassword.Type.Equals("database"))
                    {
                        loginNode.Attributes["connectionString"].Value = argConnectionPassword.UserName;
                    }
                    else
                    {
                        loginNode.Attributes["username"].Value = argConnectionPassword.UserName;
                    }
                }

                // If password attribute was not found, then add it.
                if (attributePassword == null)
                {
                    if (argConnectionPassword.Password.EncryptableValue != null && argConnectionPassword.Password.EncryptableValue.Length > 0)
                    {
                        XmlAttribute attribute = doc.CreateAttribute("password");
                        attribute.Value = argConnectionPassword.Password.EncryptableValue;
                        loginNode.Attributes.Append(attribute);
                    }
                }
                else
                {
                    loginNode.Attributes["password"].Value = argConnectionPassword.Password.EncryptableValue;
                }

                // If encrypted attribute was not found, then add it.
                if (attributeEncrypted == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("encrypted");
                    attribute.Value = argConnectionPassword.Password.Encrypted.ToString();
                    loginNode.Attributes.Append(attribute);
                }
                else
                {
                    loginNode.Attributes["encrypted"].Value = argConnectionPassword.Password.Encrypted.ToString();
                }

                doc.Save(argConnectionsFileName);

            }
            else
            {
                throw new HandledException("Connection Login with DataSourceName: " + argConnectionPassword.Name + " Was Not Found In File: " + argConnectionsFileName);
            }
        }


        public static NetworkFolderMeta getNetworkFolderMeta(string argDataSourceName, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(argConnectionsFileName);

            XmlNode xmlNode = doc.SelectSingleNode("//networkfolders/networkfolder[@name='" + argDataSourceName + "']/dataSource");
            if (xmlNode != null)
            {
                XmlAttribute attributeServer = xmlNode.Attributes["server"];
                XmlAttribute attributePath = xmlNode.Attributes["path"];
                XmlAttribute attributeUserName = xmlNode.Attributes["username"];
                XmlAttribute attributePassword = xmlNode.Attributes["password"];
                XmlAttribute attributeEncrypted = xmlNode.Attributes["encrypted"];
                string server = (attributeServer == null ? "Unknown Server" : attributeServer.Value);
                string path = (attributePath == null ? "." : attributePath.Value);
                string username = (attributeUserName == null ? "" : attributeUserName.Value);
                string password = (attributePassword == null ? "" : attributePassword.Value);
                bool encrypted = (attributeEncrypted == null ? false : attributeEncrypted.Value.ToLower().Equals("true"));
                return new NetworkFolderMeta(argDataSourceName, server, path, username, password, encrypted);
            }
            else
            {
                return null;
            }
        }

        public static void saveNetworkFolderMeta(NetworkFolderMeta argNetworkFolderMeta, string argConnectionsFileName)
        {
            throw new NotImplementedException("Need to Finish saveNetworkFolderMeta before using");
            //XmlDocument doc = new XmlDocument();
            //doc.Load(argConnectionsFileName);

            //XmlNode xmlNode = doc.SelectSingleNode("//networkfolders/networkfolder[@name='" + argNetworkFolderMeta.DataSourceName + "']/dataSource");
            //if (xmlNode != null)
            //{
            //    XmlAttribute attributeServer = xmlNode.Attributes["server"];
            //    XmlAttribute attributePath = xmlNode.Attributes["path"];

            //    if (attributeServer == null)
            //    {
            //        XmlAttribute attribute = doc.CreateAttribute("server");
            //        attribute.Value = argNetworkFolderMeta.Server;
            //        xmlNode.Attributes.Append(attribute);
            //    }
            //    else
            //    {
            //        xmlNode.Attributes["server"].Value = argNetworkFolderMeta.Server;
            //    }

            //    if (attributePath == null)
            //    {
            //        XmlAttribute attribute = doc.CreateAttribute("path");
            //        attribute.Value = argNetworkFolderMeta.Path;
            //        xmlNode.Attributes.Append(attribute);
            //    }
            //    else
            //    {
            //        xmlNode.Attributes["path"].Value = argNetworkFolderMeta.Path;
            //    }
            //}
            //else
            //{
            //    throw new HandledException("Network Folder with DataSourceName: " + argNetworkFolderMeta.DataSourceName + " Was Not Found In File: " + CONNECTIONS_FILE_NAME);
            //}
            //doc.Save(argConnectionsFileName);
        }

        public static ReportLoginMeta getReportLoginMeta(string argDataSourceName, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(argConnectionsFileName);

            string username = null;
            string password = null;
            bool encrypted = true;
            string endpoint = null;
            string path = null;

            XmlNode loginNode = doc.SelectSingleNode("//jasperservers/jasperserver[@name='" + argDataSourceName + "']/dataSource");
            if (loginNode != null)
            {
                XmlAttribute attributeUserName = loginNode.Attributes["username"];
                XmlAttribute attributePassword = loginNode.Attributes["password"];
                XmlAttribute attributeEncrypted = loginNode.Attributes["encrypted"];
                XmlAttribute attributeEndPoint = loginNode.Attributes["endpoint"];
                XmlAttribute attributePath = loginNode.Attributes["path"];

                username = (attributeUserName == null ? "Unknown User" : attributeUserName.Value);
                password = (attributePassword == null ? "Unknown Password" : attributePassword.Value);
                encrypted = (attributeEncrypted == null ? false : attributeEncrypted.Value.ToLower().Equals("true"));
                endpoint = (attributeEndPoint == null ? "Unknown EndPoint" : attributeEndPoint.Value);
                path = (attributePath == null ? "." : attributePath.Value);


                return new ReportLoginMeta(argDataSourceName, username, password, encrypted, endpoint, path);

            }
            else
            {
                return null;
            }
        }

        public static void saveReportLoginMeta(ReportLoginMeta argJasperLogin, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(argConnectionsFileName);

            XmlNode loginNode = doc.SelectSingleNode("//jasperservers/jasperserver[@name='" + argJasperLogin.DataSourceName + "']/dataSource");

            if (loginNode != null)
            {
                XmlAttribute attributeUserName = loginNode.Attributes["username"];
                XmlAttribute attributePassword = loginNode.Attributes["password"];
                XmlAttribute attributeEncrypted = loginNode.Attributes["encrypted"];
                XmlAttribute attributeEndPoint = loginNode.Attributes["endpoint"];
                XmlAttribute attributePath = loginNode.Attributes["path"];

                if (attributeUserName == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("username");
                    attribute.Value = argJasperLogin.UserName;
                    loginNode.Attributes.Append(attribute);
                }
                else
                {
                    loginNode.Attributes["username"].Value = argJasperLogin.UserName;
                }

                // If password attribute was not found, then add it.
                if (attributePassword == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("password");
                    attribute.Value = argJasperLogin.Password.EncryptableValue;
                    loginNode.Attributes.Append(attribute);
                }
                else
                {
                    loginNode.Attributes["password"].Value = argJasperLogin.Password.EncryptableValue;
                }

                // If encrypted attribute was not found, then add it.
                if (attributeEncrypted == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("encrypted");
                    attribute.Value = argJasperLogin.Password.Encrypted.ToString();
                    loginNode.Attributes.Append(attribute);
                }
                else
                {
                    loginNode.Attributes["encrypted"].Value = argJasperLogin.Password.Encrypted.ToString();
                }

                if (attributeEndPoint == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("endpoint");
                    attribute.Value = argJasperLogin.EndPoint;
                    loginNode.Attributes.Append(attribute);
                }
                else
                {
                    loginNode.Attributes["endpoint"].Value = argJasperLogin.EndPoint;
                }

                if (attributePath == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("path");
                    attribute.Value = argJasperLogin.Path;
                    loginNode.Attributes.Append(attribute);
                }
                else
                {
                    loginNode.Attributes["path"].Value = argJasperLogin.Path;
                }

                doc.Save(argConnectionsFileName);

            }
            else
            {
                throw new HandledException("Jasper Login with DataSourceName: " + argJasperLogin.DataSourceName + " Was Not Found In File: " + argConnectionsFileName);
            }
        }


        internal static DBLoginMeta getDBLogin(string argDataSourceName, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(argConnectionsFileName);

            bool encrypted = true;
            string password = null;
            string connectionString = null;
            string currentschema = null;
            string commandtimeoutString = null;
            int commandtimeout = 180;

            XmlNode dbLoginNode = doc.SelectSingleNode("//databases/database[@name='" + argDataSourceName + "']/dataSource");
            if (dbLoginNode != null)
            {
                XmlAttribute attributeEncrypted = dbLoginNode.Attributes["encrypted"];
                XmlAttribute attributePassword = dbLoginNode.Attributes["password"];
                XmlAttribute attributeConnectionString = dbLoginNode.Attributes["connectionString"];
                XmlAttribute attributeCurrentSchema = dbLoginNode.Attributes["currentSchema"];
                XmlAttribute attributeCommandTimeout = dbLoginNode.Attributes["commandTimeout"];

                // If encrypted attribute was not found, then assume it's not encrypted.
                encrypted = (attributeEncrypted == null ? false : encrypted = attributeEncrypted.Value.ToLower().Equals("true"));
                password = (attributePassword == null ? "Unknown Password" : attributePassword.Value);
                connectionString = (attributeConnectionString == null ? "Unknown Connection String" : attributeConnectionString.Value);
                currentschema = (attributeCurrentSchema == null ? "" : attributeCurrentSchema.Value);
                commandtimeoutString = (attributeCommandTimeout == null ? "180" : attributeCommandTimeout.Value);

                try
                {
                    commandtimeout = int.Parse(commandtimeoutString);
                }
                catch (Exception)
                {
                    log.Warn("Invalid entry found for command timeout attribute.  Reverting to default commandtimout value: " + commandtimeout + " for datasource: " + argDataSourceName);
                }

                DBLoginMeta dbLogin = new DBLoginMeta(argDataSourceName, password, encrypted, connectionString, currentschema, commandtimeout);
                return dbLogin;
            }
            else
            {
                return null;
            }
        }

        internal static void saveDBLogin(DBLoginMeta argDBLogin, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(argConnectionsFileName);

            XmlNode dbLoginNode = doc.SelectSingleNode("//databases/database[@name='" + argDBLogin.DataSourceName + "']/dataSource");
            if (dbLoginNode != null)
            {
                XmlAttribute attributeEncrypted = dbLoginNode.Attributes["encrypted"];
                XmlAttribute attributePassword = dbLoginNode.Attributes["password"];
                XmlAttribute attributeConnectionString = dbLoginNode.Attributes["connectionString"];
                XmlAttribute attributeCurrentSchema = dbLoginNode.Attributes["currentschema"];
                XmlAttribute attributeCommandTimeout = dbLoginNode.Attributes["commandtimeout"];


                // If encrypted attribute was not found, then add it.
                if (attributeEncrypted == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("encrypted");
                    attribute.Value = argDBLogin.Encrypted.ToString();
                    dbLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    dbLoginNode.Attributes["encrypted"].Value = argDBLogin.Encrypted.ToString();
                }

                if (attributePassword == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("password");
                    attribute.Value = argDBLogin.PasswordXML;
                    dbLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    dbLoginNode.Attributes["password"].Value = argDBLogin.PasswordXML;
                }

                if (attributeConnectionString == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("connectionString");
                    attribute.Value = argDBLogin.ConnectionString;
                    dbLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    dbLoginNode.Attributes["connectionString"].Value = argDBLogin.ConnectionString;
                }

                if (attributeCurrentSchema == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("currentSchema");
                    attribute.Value = argDBLogin.CurrentSchema;
                    dbLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    dbLoginNode.Attributes["currentSchema"].Value = argDBLogin.CurrentSchema;
                }

                if (attributeCommandTimeout == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("commandTimeout");
                    attribute.Value = argDBLogin.CommandTimeout.ToString();
                    dbLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    dbLoginNode.Attributes["commandTimeout"].Value = argDBLogin.CommandTimeout.ToString();
                }

                doc.Save(argConnectionsFileName);

            }
            else
            {
                throw new HandledException("Database with DataSourceName: " + argDBLogin.DataSourceName + " Was Not Found In File: " + argConnectionsFileName);
            }
        }


        public static FTPLoginMeta getFTPLogin(string argDataSourceName, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(argConnectionsFileName);

            string username = null;
            string password = null;
            bool encrypted = true;
            string server = null;
            string path = null;
            string portString = null;
            int port = 21;
            string keepfiledaysString = null;
            int keepfiledays = 100000;

            XmlNode ftpLoginNode = doc.SelectSingleNode("//ftplist/ftp[@name='" + argDataSourceName + "']/dataSource");
            if (ftpLoginNode != null)
            {
                XmlAttribute attributeUserName = ftpLoginNode.Attributes["username"];
                XmlAttribute attributePassword = ftpLoginNode.Attributes["password"];
                XmlAttribute attributeEncrypted = ftpLoginNode.Attributes["encrypted"];
                XmlAttribute attributeServer = ftpLoginNode.Attributes["server"];
                XmlAttribute attributePath = ftpLoginNode.Attributes["path"];
                XmlAttribute attributePort = ftpLoginNode.Attributes["port"];
                XmlAttribute attributeKeepFileDays = ftpLoginNode.Attributes["keepfiledays"];

                username = (attributeUserName == null ? "Unknown User" : attributeUserName.Value);
                password = (attributePassword == null ? "Unknown Password" : attributePassword.Value);
                encrypted = (attributeEncrypted == null ? false : attributeEncrypted.Value.ToLower().Equals("true"));
                server = (attributeServer == null ? "Unknown Server" : attributeServer.Value);
                path = (attributePath == null ? "." : attributePath.Value);
                portString = (attributePort == null ? "21" : attributePort.Value);
                keepfiledaysString = (attributePort == null ? "100000" : attributeKeepFileDays.Value);

                try
                {
                    port = int.Parse(portString);
                }
                catch (Exception)
                {
                    throw new HandledException("Invalid Port " + portString + " specified for datasource " + argDataSourceName, LOGGER_ERROR_NAME);
                }

                try
                {
                    keepfiledays = int.Parse(keepfiledaysString);
                }
                catch (Exception)
                {
                    throw new HandledException("Invalid Keep File Days Value " + keepfiledaysString + " specified for datasourc " + argDataSourceName, LOGGER_ERROR_NAME);
                }

                return new FTPLoginMeta(argDataSourceName, username, password, encrypted, server, path, port, keepfiledays);

            }
            else
            {
                return null;
            }
        }

        internal static void saveFTPLogin(FTPLoginMeta argFTPLogin, string argConnectionsFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(CONNECTIONS_FILE_NAME);

            XmlNode ftpLoginNode = doc.SelectSingleNode("//ftplist/ftp[@name='" + argFTPLogin.DataSourceName + "']/dataSource");

            if (ftpLoginNode != null)
            {
                XmlAttribute attributeUserName = ftpLoginNode.Attributes["username"];
                XmlAttribute attributePassword = ftpLoginNode.Attributes["password"];
                XmlAttribute attributeEncrypted = ftpLoginNode.Attributes["encrypted"];
                XmlAttribute attributeServer = ftpLoginNode.Attributes["server"];
                XmlAttribute attributePath = ftpLoginNode.Attributes["path"];
                XmlAttribute attributePort = ftpLoginNode.Attributes["port"];
                XmlAttribute attributeKeepFileDays = ftpLoginNode.Attributes["keepfiledays"];


                if (attributeUserName == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("username");
                    attribute.Value = argFTPLogin.UserName;
                    ftpLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    ftpLoginNode.Attributes["username"].Value = argFTPLogin.UserName;
                }

                // If password attribute was not found, then add it.
                if (attributePassword == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("password");
                    attribute.Value = argFTPLogin.Password.EncryptableValue;
                    ftpLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    ftpLoginNode.Attributes["password"].Value = argFTPLogin.Password.EncryptableValue;
                }

                // If encrypted attribute was not found, then add it.
                if (attributeEncrypted == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("encrypted");
                    attribute.Value = argFTPLogin.Password.Encrypted.ToString();
                    ftpLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    ftpLoginNode.Attributes["encrypted"].Value = argFTPLogin.Password.Encrypted.ToString();
                }

                if (attributeServer == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("server");
                    attribute.Value = argFTPLogin.Server;
                    ftpLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    ftpLoginNode.Attributes["server"].Value = argFTPLogin.Server;
                }

                if (attributePath == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("path");
                    attribute.Value = argFTPLogin.Path;
                    ftpLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    ftpLoginNode.Attributes["path"].Value = argFTPLogin.Path;
                }

                if (attributePort == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("port");
                    attribute.Value = argFTPLogin.Port.ToString();
                    ftpLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    ftpLoginNode.Attributes["port"].Value = argFTPLogin.Port.ToString();
                }

                if (attributeKeepFileDays == null)
                {
                    XmlAttribute attribute = doc.CreateAttribute("keepfiledays");
                    attribute.Value = argFTPLogin.KeepFileDays.ToString();
                    ftpLoginNode.Attributes.Append(attribute);
                }
                else
                {
                    ftpLoginNode.Attributes["keepfiledays"].Value = argFTPLogin.KeepFileDays.ToString();
                }

                doc.Save(CONNECTIONS_FILE_NAME);

            }
            else
            {
                throw new HandledException("FTP Login with DataSourceName: " + argFTPLogin.DataSourceName + " Was Not Found In File: " + CONNECTIONS_FILE_NAME);
            }
        }

    }
}
