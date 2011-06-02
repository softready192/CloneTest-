using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DA.DAIE.Common;

namespace DA.DAIE.COP
{

    public class COPScheduleArg : AncestorArg
    {
        private string _UnitShortName;
        private DateTime _MktDay;

        public COPScheduleArg(string argUnitShortName, DateTime argMktDay)
        {
            this._UnitShortName = argUnitShortName;
            this._MktDay = argMktDay;
        }

        public override string ToString()
        {
            base.Add("UnitShortName", _UnitShortName );
            base.Add("MktDay", _MktDay);
            return base.ToLogString();
        }

        public string UnitShortName { get { return this._UnitShortName; } }
        public DateTime MktDay { get { return this._MktDay; } }
    }

    /// <summary>
    /// 
    /// </summary>
    public class COPSchedule
    {
        private int _Id;
        private string _Name;
        private string _Type;
        private decimal _EcoMin;
        private decimal _EcoMax;

        public COPSchedule(
              int argId
            , string argName
            , string argType
            , decimal argEcoMin
            , decimal argEcoMax )
        {
            this._Id = argId;
            this._Name = argName;
            this._Type = argType;
            this._EcoMax = argEcoMax;
            this._EcoMin = argEcoMin;
        }

        public int getID() { return this._Id; }

        public string Name { get { return this._Name; } set { this._Name = value; } }
        public string Type { get { return this._Type; } }
        public int ID { get { return this._Id; } }
    }

}
