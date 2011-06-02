using System;
using System.Collections.Generic;
using System.Text;
using com.jaspersoft.webservice;
using DA.Common;

namespace DA.DAIE.Connections
{
    public class ReportLoginMeta
    {
        private string _DataSourceName;
        private string _UserName, _UserNameOld;
        private Encryptable _Password;
        private string _EndPoint, _EndPointOld;
        private string _Path, _PathOld;

        internal bool isChanged()
        {
            bool unchanged =
                    this._UserName.Equals(_UserNameOld)
                 && this._EndPoint.Equals(_EndPointOld)
                 && this._Path.Equals(_PathOld)
                 && !_Password.isChanged();

            return !unchanged;
        }

        public ReportLoginMeta(
            string argDataSourceName,
            string argUserName,
            string argPassword,
            bool argEncrypted,
            string argEndPoint,
            string argPath )
        {
            this._DataSourceName = argDataSourceName;
            this._UserName = argUserName;
            this._Password = new Encryptable(argPassword, argEncrypted);
            this._EndPoint = argEndPoint;
            this._Path = argPath;
            
            this._UserNameOld = argUserName;
            this._EndPointOld = argEndPoint;
            this._PathOld = argPath;
        }

        public Repository getWSRepository()
        {
            Repository rep = new Repository();

            rep.url = _EndPoint;
            rep.setCredentials(_UserName, _Password.DecryptedValue);
            return rep;
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

        public string EndPoint
        {
            get { return this._EndPoint; }
            set { this._EndPoint = value; }
        }

        public string Path
        {
            get { return this._Path; }
            set { this._Path = value; }
        }
    }
}
