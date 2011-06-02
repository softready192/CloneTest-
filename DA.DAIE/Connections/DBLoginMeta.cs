// Depends ONLY on class Encryptable.
using DA.Common;

namespace DA.DAIE.Connections
{
    internal class DBLoginMeta
    {
        private Encryptable _Password;

        private string _DataSourceName;
        private string _ConnectionString;
        private string _CurrentSchema;
        private int _CommandTimeout;

        private string _ConnectionStringOld;
        private string _CurrentSchemaOld;
        private int _CommandTimeoutOld;

        internal bool isChanged()
        {
            bool unchanged =
                   !this._Password.isChanged()
                 && this._ConnectionString.Equals(_ConnectionStringOld)
                 && this._CurrentSchema.Equals(_CurrentSchemaOld)
                 && this._CommandTimeout.Equals(_CommandTimeoutOld);

            return !unchanged;
        }

        /// <summary>
        /// Construct using values read directly from XML file.
        /// </summary>
        /// <param name="argDataSourceName"></param>
        /// <param name="argPassword"></param>
        /// <param name="argEncrypted"></param>
        /// <param name="argConnectionString"></param>
        /// <param name="argCurrentSchema"></param>
        /// <param name="argCommandTimeout"></param>
        public DBLoginMeta(string argDataSourceName, string argPassword, bool argEncrypted, string argConnectionString, string argCurrentSchema, int argCommandTimeout)
        {
            this._DataSourceName = argDataSourceName;
            this._Password = new Encryptable(argPassword, argEncrypted);
            this._ConnectionString = argConnectionString;
            this._CurrentSchema = argCurrentSchema;
            this._CommandTimeout = argCommandTimeout;

            this._ConnectionStringOld = argConnectionString;
            this._CurrentSchemaOld = argCurrentSchema;
            this._CommandTimeoutOld = argCommandTimeout;
        }

        public string DataSourceName
        {
            get { return this._DataSourceName; }
        }

        /// <summary>
        /// Gets and Sets if Password is Encrypted.
        /// </summary>
        public bool Encrypted
        {
            get { return this._Password.Encrypted; }
            set { this._Password.Encrypted = value; }
        }

        /// <summary>
        /// Returns the raw password.
        /// It will be encrypted or not depending on the value of the _Encrypted attribute.  
        /// </summary>
        public string PasswordXML
        {
            get { return this._Password.EncryptableValue; }
        }

        /// <summary>
        /// Gets and Sets Decrypted Password Value.
        /// For use on admin screen.
        /// </summary>
        public string Password
        {
            get { return this._Password.DecryptedValue; }
            set { this._Password.DecryptedValue = value; }
        }

        /// <summary>
        /// Gets and Sets UNSUBSTITUED Connection String.
        /// 
        /// ConnectionString contains #Password#.
        /// This value will NOT be replaced with the password value.
        /// </summary>
        public string ConnectionString
        {
            get { return this._ConnectionString; }
            set { this._ConnectionString = value; }
        }

        public string CurrentSchema
        {
            get { return this._CurrentSchema; }
            set { this._CurrentSchema = value; }
        }

        public int CommandTimeout
        {
            get { return this._CommandTimeout; }
            set { this._CommandTimeout = value; }
        }

    }
}
