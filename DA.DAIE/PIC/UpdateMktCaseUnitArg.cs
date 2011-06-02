using System;
using System.Text;
using DA.DAIE.Common;

namespace DA.DAIE.PIC
{
    class UpdateMktCaseUnitArg : AncestorArg
    {
        private string _CaseId;
        private PeakHour _PeakHour;
        private PICData _PICData;

        public UpdateMktCaseUnitArg
        (   string argCaseId,
            PeakHour argPeakHour,
            PICData argPICData )
        {
            this._CaseId = argCaseId;
            this._PeakHour = argPeakHour;
            this._PICData = argPICData;
        }

        public override string ToString()
        {
            base.Add("CaseId",_CaseId);
            base.Add("PeakHour",_PeakHour.ToString());
            base.Add("PICData",_PICData.ToString());
            return base.ToLogString();
        }

        public string CaseId
        {
            get { return this._CaseId; }
        }

        public int UnitId
        {
            get { return this._PICData.UnitId; }
        }

        public DateTime MktHour
        {
            get { return this._PeakHour.MktHour; }
        }

        public DateTime MktHourEnd
        {
            get { return this._PeakHour.MktHourEnd; }
        }

        public string UnitScheduleId
        {
            get { return this._PICData.UnitScheduleId; }
        }
    }
}
