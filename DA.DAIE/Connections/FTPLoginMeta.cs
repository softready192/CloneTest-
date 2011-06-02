using System;
using System.Collections.Generic;
using System.Text;
using DA.Common;
using DA.DAIE.Common;

namespace DA.DAIE.Connections
{
    public class FTPLoginMeta : AncestorArg
    {
        private string _DataSourceName;
        private string _UserName,_UserNameOld;
        private string _Server,_ServerOld;
        private string _Path, _PathOld;
        private int _Port,_PortOld;
        private int _KeepFileDays, _KeepFileDaysOld;
        private Encryptable _Password;



        internal bool isChanged()
        {
            bool unchanged =
                    this._UserName.Equals(_UserNameOld)
                 && this._Server.Equals(_ServerOld)
                 && this._Path.Equals(_PathOld)
                 && this._Port.Equals(_PortOld)
                 && this._KeepFileDays.Equals(_KeepFileDaysOld)
                 && !_Password.isChanged();

            return !unchanged;
        }

        public FTPLoginMeta (
            string argDataSourceName, 
            string argUserName, 
            string argPassword, 
            bool argEncrypted, 
            string argServer, 
            string argPath, 
            int argPort, 
            int argKeepFileDays ) 
        {
            this._DataSourceName = argDataSourceName;
            this._UserName = argUserName;
            this._Password = new Encryptable(argPassword, argEncrypted);
            this._Server = argServer;
            this._Path = argPath;
            this._Port = argPort;
            this._KeepFileDays = argKeepFileDays;

            this._UserNameOld = argUserName;
            this._ServerOld = argServer;
            this._PathOld = argPath;
            this._PortOld = argPort;
            this._KeepFileDaysOld = argKeepFileDays;
        }

        public override string ToString()
        {
            base.Add("DataSourceName", _DataSourceName);
            base.Add("UserName", _UserName);
            base.Add("Server", _Server);
            base.Add("Path", _Path);
            base.Add("Port", _Port);
            base.Add("KeepFileDays", _KeepFileDays);
            return base.ToLogString();
        }

        public string DataSourceName
        {
            get { return this._DataSourceName; }
        }

        public string UserName
        {
            get { return this._UserName; }
            set { this._UserName = value; }
        }

        public Encryptable Password
        {
            get { return _Password; }
            set { _Password = value; }
        }

        public string Server
        {
            get { return this._Server; }
            set { this._Server = value; }
        }

        public string Path
        {
            get { return this._Path; }
            set { this._Path = value; }
        }

        public int Port
        {
            get { return this._Port; }
            set { this._Port = value; }
        }

        public int KeepFileDays
        {
            get { return this._KeepFileDays; }
            set { this._KeepFileDays = value; }
        }
    }
}
