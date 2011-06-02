using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DA.Common;
using DA.DAIE.Common;

namespace DA.DAIE.Reserve
{
    public class SelectReserveInterfaceAncestorArgs : AncestorArg
    {
        public enum CONFIG_NODENAME { HQPNODE_NAME, NBPNODE_NAME, HQPNODE_TRANS, NBPNODE_TRANS };

        private const String CONFIG_HQPNODE_NAME = "HQPNODE_NAME";
        private const String CONFIG_NBPNODE_NAME = "NBPNODE_NAME";
        private const String CONFIG_HQPNODE_TRANS = "HQPNODE_TRANS";
        private const String CONFIG_NBPNODE_TRANS = "NBPNODE_TRANS";

        private CONFIG_NODENAME _configNodeName;

        public SelectReserveInterfaceAncestorArgs(CONFIG_NODENAME argNodeName)
        {
            this._configNodeName = argNodeName;
        }

        public string NodeName
        {
            get
            {
                string entryName = "";
                if (this._configNodeName.Equals(CONFIG_NODENAME.HQPNODE_NAME))
                {
                    entryName = CONFIG_HQPNODE_NAME;
                }
                else if (this._configNodeName.Equals(CONFIG_NODENAME.NBPNODE_NAME))
                {
                    entryName = CONFIG_NBPNODE_NAME;
                }
                else if (this._configNodeName.Equals(CONFIG_NODENAME.HQPNODE_TRANS))
                {
                    entryName = CONFIG_HQPNODE_TRANS;
                }
                else if (this._configNodeName.Equals(CONFIG_NODENAME.NBPNODE_TRANS))
                {
                    entryName = CONFIG_NBPNODE_TRANS;
                }
                return AppConfig.getAppConfigEntry(entryName);
            }
        }
    }

    public class SelectReserveInterfaceHourlyArgs : SelectReserveInterfaceAncestorArgs
    {

        private DateTime _MktHour;

        public SelectReserveInterfaceHourlyArgs(DateTime argMktHour, CONFIG_NODENAME argConfigNodeName)
            : base( argConfigNodeName )
        {
            this._MktHour = argMktHour;
        }

        public DateTime MktHour
        {
            get { return this._MktHour; }
        }

        public override string ToString()
        {
            base.Add("MktHour", MktHour);
            base.Add("NodeName", NodeName);
            return base.ToLogString();
        }

    }

    public class SelectInterfaceTransactionTotalArg : SelectReserveInterfaceAncestorArgs
    {
        private string _CaseId;

        public SelectInterfaceTransactionTotalArg(string argCaseId, CONFIG_NODENAME argConfigNodeName)
            : base (argConfigNodeName)
        {
            this._CaseId = argCaseId;
        }

        public String CaseId
        {
            get { return this._CaseId; }
        }

        public override string ToString()
        {
            base.Add("CaseId", CaseId);
            base.Add("NodeName", NodeName);
            return base.ToLogString();
        }

    }
}
