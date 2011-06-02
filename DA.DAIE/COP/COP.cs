using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DA.DAIE.Common;

namespace DA.DAIE.COP
{
    public class COPS : ObservableCollection<COP>
    {
        public COPS(IList<COP> argCops ): base()
        {
            foreach (COP cop in argCops)
            {
                this.Add(cop);
            }
        }

        public COPS() : base() { }
    }

    public class COP : AncestorArg, IDataErrorInfo 
    {
        public string Error
        {
            get { return string.Empty; }
        }

        public bool isValid()
        {
            if (this.RSCCommittedFlag == 0 || this.RSCCommittedFlag == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool isChanged()
        {
            if (this.RSCCommittedFlag.Equals(this.RSCCommittedFlagOld)
             && this._UnitScheduleId.Equals(this._UnitScheduleIdOld))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public string this[string columnName]
        {
            get
            {
                var result = string.Empty;
                if (columnName == "RSCCommittedFlag")
                {
                    if (this.RSCCommittedFlag < 0)
                    {
                        result = "Invalid Boolean Value Too Low";
                    }
                    else if (this.RSCCommittedFlag > 1)
                    {
                        result = "Invalid Boolean Value Too High";
                    }
                }
                return result;
            }
        }

        private int _Enabled;
        private DateTime _MktHour;
        private int _UnitId;
        private string _UnitShortName;
        private string _DayLabel;
        private string _HourLabel;
        private string _UnitScheduleShortName;
        private string _DBCalcScheduleType;
        private decimal _DBCalcEcoMax;
        private decimal _DBCalcEcoMin;
        private int _RSCCommittedFlag;
        private int _RSCCommittedFlagOld;
        private int _UnitScheduleId;
        private int _UnitScheduleIdOld;
        private int _MustRunFlag;
        private int _EconomicFlag;
        private int _EmergencyFlag;

        public COP
        ( 

            int argEnabled,
            DateTime argMktHour,
            int argUnitId,
            string argUnitShortName,
            string argDayLabel,
            string argHourLabel,
            string argUnitScheduleShortName,
            string argDBCalcScheduleType,
            decimal argDBCalcEcoMax,
            decimal argDBCalcEcoMin,
            int argRscCommittedFlag,
            int argUnitScheduleId,
            int argMustRunFlag,
            int argEconomicFlag,
            int argEmergencyFlag


        )
        {
            this._Enabled = argEnabled;
            this._MktHour = argMktHour;
            this._UnitId = argUnitId;
            this._UnitShortName = argUnitShortName;
            this._DayLabel = argDayLabel;
            this._HourLabel = argHourLabel;
            this._UnitScheduleShortName = argUnitScheduleShortName;
            this._DBCalcScheduleType = argDBCalcScheduleType;
            this._DBCalcEcoMax = argDBCalcEcoMax;
            this._DBCalcEcoMin = argDBCalcEcoMin;
            this._RSCCommittedFlag = argRscCommittedFlag;
            this._RSCCommittedFlagOld = argRscCommittedFlag;
            this._UnitScheduleId = argUnitScheduleId;
            this._UnitScheduleIdOld = argUnitScheduleId;
            this._MustRunFlag = argMustRunFlag;
            this._EconomicFlag = argEconomicFlag;
            this._EmergencyFlag = argEmergencyFlag;
        }

        public override string ToString()
        {

            base.Add("UnitId", UnitId );
            base.Add("MktHour", MktHour);
            base.Add("CalcTerminationHour", CalcTerminationHour);
            base.Add("UnitScheduleId", UnitScheduleId);
            base.Add("CalcPlannedMW", CalcPlannedMW);
            base.Add("RSCCommittedFlag", RSCCommittedFlag);
            base.Add("MustRunFlag", MustRunFlag);
            base.Add("EconomicFlag", EconomicFlag);
            base.Add("EmergencyFlag", EmergencyFlag);

            return base.ToLogString();
        }

        public int Enabled
        {
            get { return this._Enabled; }
            set { this._Enabled = value; }
            //get { return this._Enabled.Equals(1); }
            //set { this._Enabled = ( value ? 1 : 0 ); }
        }

        public DateTime MktHour
        {
            get{ return this._MktHour; }
        }

        public DateTime CalcTerminationHour
        {
            get { return this._MktHour.AddHours(1); }
        }

        public int UnitId
        {
            get{ return this._UnitId; }
        }
        public string UnitShortName
        {
            get{ return this._UnitShortName; }
        }
        public string DayLabel
        {
            get{ return this._DayLabel; }
        }
        public string HourLabel
        {
            get{ return this._HourLabel; }
        }

        public string Name
        {
            get { return this._UnitScheduleShortName; }
            set { this._UnitScheduleShortName = value; }
        }

        public int UnitScheduleId
        {
            get { return this._UnitScheduleId; }
            set { this._UnitScheduleId = value; }
        }

        public int UnitScheduleIdOld
        {
            get { return this._UnitScheduleIdOld; }
        }

        public string UnitScheduleShortName
        {
            get{ return this._UnitScheduleShortName; }
            set { this._UnitScheduleShortName = value; }
        }
        public string DBCalcScheduleType
        {
            get{ return this._DBCalcScheduleType; }
            set{ this._DBCalcScheduleType = value; }
        }

        
        public decimal DBCalcEcoMax
        {
            get{ return this._DBCalcEcoMax; }
            set { this._DBCalcEcoMax = value; }
        }
        public decimal DBCalcEcoMin
        {
            get{ return this._DBCalcEcoMin; }
            set { this._DBCalcEcoMin = value; }
        }

        public decimal CalcPlannedMW
        {
            get
            {
                if (this._RSCCommittedFlag == 1) 
                  { return this._DBCalcEcoMin; }
                else 
                  { return 0.0M; }
            }
        }

        public int RSCCommittedFlag
        {
            get{ return this._RSCCommittedFlag; }
            set 
            {
                this._RSCCommittedFlag = value; 
            }
        }

        public int RSCCommittedFlagOld
        {
            get { return this._RSCCommittedFlagOld; }
        }

        public bool RSCCommittedBool
        {
            get { return this._RSCCommittedFlag == 1; }
            set
            {
                this._RSCCommittedFlag = ( value ? 1 : 0 );
            }
        }

        public int MustRunFlag
        {
            get{ return this._MustRunFlag; }
        }
        public int EconomicFlag
        {
            get{ return this._EconomicFlag; }
        }
        public int EmergencyFlag  
        {
            get{ return this._EmergencyFlag; }
        }

    }
}
