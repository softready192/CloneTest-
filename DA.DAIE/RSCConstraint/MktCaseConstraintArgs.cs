using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DA.DAIE.Common;

namespace DA.DAIE.RSCConstraint
{
    public class MktCaseConstraintArgs : AncestorArg
    {
        private string _CaseId;
        private string _OriginalCaseId;
        private Int64 _ConstraintId;
        private Int64 _RSCConstraintId;
        private string _Operation;
        private string _ConstraintName;
        private DateTime _EffectiveHour;
        private DateTime _TerminationHour;
        private Decimal _RHS;


        // For UnitId and PNodeId, 
        // It could be cleaner to add these attribute by inheritance
        // But just didn't seem worth the extra classes.

        // Added atributes for specifying a UnitId in addition to the CaseConstraints.
        private int _UnitId = 0;
        // Added attribute for specifying a PNode in addition to the CaseConstraints.
        private Int64 _PNodeId = 0;

        public override string ToString()
        {
            base.Add("CaseId",_CaseId);
            base.Add("ConstraintId", _ConstraintId);
            base.Add("RSCConstraintId", _RSCConstraintId);
            base.Add("Operation", _Operation);
            base.Add("ConstraintName", _ConstraintName);
            base.Add("EffectiveHour",_EffectiveHour);
            base.Add("TerminationHour",_TerminationHour);
            base.Add("RHS",_RHS);
            base.Add("PNodeId", _PNodeId);
            return base.ToLogString();
        }

        public MktCaseConstraintArgs(RSCConstraintRelatedCases argRelatedCase, long argRSCConstraintId, int argDurationHours)
        {
            this._CaseId = argRelatedCase.CaseId;
            this._OriginalCaseId = argRelatedCase.OriginalCaseId;
            this._ConstraintName = argRelatedCase.ConstraintName;
            this._RSCConstraintId = argRSCConstraintId;
            this._Operation = argRelatedCase.Operation;
            this._EffectiveHour = argRelatedCase.MktHour;
            this._TerminationHour = argRelatedCase.MktHour.AddHours(argDurationHours);
            this._RHS = argRelatedCase.RHS;
        }

        public MktCaseConstraintArgs(RSCConstraintRelatedCases argRelatedCase, int argDurationHours)
        {
            this._CaseId = argRelatedCase.CaseId;
            this._OriginalCaseId = argRelatedCase.OriginalCaseId;
            this._ConstraintName = argRelatedCase.ConstraintName;
            this._Operation = argRelatedCase.Operation;
            this._EffectiveHour = argRelatedCase.MktHour;
            this._TerminationHour = argRelatedCase.MktHour.AddHours(argDurationHours);
            this._RHS = argRelatedCase.RHS;
        }


        public string CaseId
        {
            get { return this._CaseId; }
        }

        public string OriginalCaseId
        {
            get { return this._OriginalCaseId; }
        }


        public Int64 ConstraintId
        {
            get { return this._ConstraintId; }
            set { this._ConstraintId = value; }
        }

        public string ConstraintName
        {
            get { return this._ConstraintName; }
        }

        public Int64 RSCConstraintId
        {
            get { return this._RSCConstraintId; }
            set { this._RSCConstraintId = value; }
        }

        public string Operation
        {
            get { return this._Operation; }
        }

        
  

        public DateTime EffectiveHour
        {
            get { return this._EffectiveHour; }
            set { this._EffectiveHour = value; }
        }

        public DateTime TerminationHour
        {
            get { return this._TerminationHour; }
            set { this._TerminationHour = value; }
        }

        public string TerminationHourString
        {
            get { return this._TerminationHour.ToString("MM-dd-yyyy HH"); }
        }

        public Decimal RHS
        {
            get { return this._RHS; }
        }

        public int UnitId
        {
            get{ return this._UnitId; }
            set{ this._UnitId = value; }
        }

        public Int64 PNodeId
        {
            get { return this._PNodeId; }
            set { this._PNodeId = value; }
        }

    }
}
