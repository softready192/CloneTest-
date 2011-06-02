using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DA.Common;
using DA.DAIE.Common;

namespace DA.DAIE.Connections
{
    public class NetworkFolderMeta : AncestorArg
    {
        private string _DataSourceName;
        private string _Server;
        private string _Path;
        private string _UserName;
        private Encryptable _Password;


        public NetworkFolderMeta(
            string argDataSourceName,
            string argServer,
            string argPath,
            string argUserName, 
            string argPassword, 
            bool argEncrypted
            )
        {
            this._DataSourceName = argDataSourceName;
            this._Server = argServer;
            this._Path = argPath;
            this._UserName = argUserName;
            this._Password = new Encryptable(argPassword, argEncrypted);
        }

        public override string ToString()
        {
            base.Add("DataSourceName", _DataSourceName);
            base.Add("Server", _Server);
            base.Add("UserName", _UserName);
            base.Add("Path", _Path);
            return base.ToLogString();
        }


        public string DataSourceName
        {
            get { return this._DataSourceName; } 
        }

        public string Server
        {
            get { return this._Server; }
        }

        public string Path
        {
            get { return this._Path; }
        }

        public string getFullName(string argFileName)
        {
            return "\\\\" + _Server + "\\" + _Path + "\\" + argFileName;
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
    }
}
