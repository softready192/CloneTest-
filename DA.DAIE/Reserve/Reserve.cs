using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DA.DAIE.Common;

namespace DA.DAIE.Reserve
{

    public class Reserve : AncestorArg
    {
        private DateTime _MktHour;
        private string _HourLabel;
        private string _CaseId;

        private double _Contingency1 = 0;
        private double _Contingency2 = 0;
        private double _InterfaceHQ = 0;
        private double _InterfaceNB = 0;
        //Value reset with each calculation. 
        //Variable to compare against at recalc to detect user edit of the field   
        private double _InterfaceNBLastCalc = 0;
        private double _MISNB = 0;
        //Value retrieved from DB
        private double _MISNBOld = 0;
        private double _MYS89 = 0;
        private double _Misc = 0;

        private double _CalcTotal10Requirement;
        private double _CalcTotal10SpinRequirement;
        private double _CalcTotal30Requirement;

        public override string ToString()
        {
            base.Add("MktHour", _MktHour);
            base.Add("HourLabel",_HourLabel );
            base.Add("CaseId", _CaseId);
            base.Add("Contingency1",_Contingency1 );
            base.Add("Contingency2",_Contingency2 );
            base.Add("InterfaceHQ",_InterfaceHQ );
            base.Add("InterfaceNB",_InterfaceNB);
            base.Add("MISNB", _MISNB);
            base.Add("MYS89",_MYS89 );
            base.Add("CalcTotal10Requirement", _CalcTotal10Requirement);
            base.Add("CalcTotal10SpinRequirement", _CalcTotal10SpinRequirement);
            base.Add("CalcTotal30Requirement", _CalcTotal30Requirement);
            return base.ToLogString();
        }


        public Reserve(DateTime argMktHour, string argHourLabel)
        {
            this._MktHour = argMktHour;
            this._HourLabel = argHourLabel;
        }

        public string HourLabel
        {
            get { return this._HourLabel; }
        }

        public string CaseId
        {
            get { return this._CaseId; }
            set { this._CaseId = value; }
        }

        public DateTime MktHour
        {
            get { return this._MktHour; }
        }

        public double Contingency1
        {
            get { return this._Contingency1; }
            set { this._Contingency1 = value; }
        }

        public double Contingency2
        {
            get { return this._Contingency2; }
            set { this._Contingency2 = value; }
        }

        public double InterfaceHQ
        {
            get { return this._InterfaceHQ; }
            set { this._InterfaceHQ = value; }
        }

        public double InterfaceNB
        {
            get { return this._InterfaceNB; }
            set { this._InterfaceNB = value; }
        }

        public double MISNB
        {
            get { return this._MISNB; }
            set { this._MISNB = value; }
        }

        public double MISNBDB
        {
            set { this._MISNBOld = value;  
                  this._MISNB = value; }
        }


        public double MYS89
        {
            get { return this._MYS89; }
            set { this._MYS89 = value; }
        }

        public double Misc
        {
            get { return this._Misc; }
            set { this._Misc = value; }
        }

        /// <summary>
        /// Calculates Reserve Requirements
        /// 
        /// Find the largest 2 values 
        /// from { contingencies, interfaces, MISNB, MYS89 and MISC }
        /// 
        /// Calculate values for 
        /// 1) Total10SpinRequirement 
        /// 2) Total10Requirement
        /// 3) Total30Requirement
        /// 
        /// Calculated values are rounded to 1 place past the decimal.
        /// </summary>
        /// <param name="argContingencyPercent"></param>
        public void CalcRequirement(ContingencyPercent argContingencyPercent)
        {

            if (_InterfaceNB != _InterfaceNBLastCalc)
            { 
              _MISNB = _MISNBOld + _InterfaceNB;
              _InterfaceNBLastCalc = _InterfaceNB;
            }
                       
            double[] array = new double[]{
                _Contingency1
              , _Contingency2 
              , _InterfaceHQ  
              , _InterfaceNB 
              , _MISNB 
              , _MYS89 
              , _Misc };

            // Sort and find the largest two values.
            Array.Sort(array);

            double large1 = array[array.Length - 1];
            double large2 = array[array.Length - 2];
            double percent1 = ((double)argContingencyPercent.Contingency1Percent / 100.0);
            double percent2 = ((double)argContingencyPercent.Contingency2Percent / 100.0);
            double percent3 = ((double)argContingencyPercent.TMSRPercent / 100.0);

            _CalcTotal10SpinRequirement = Math.Round( large1 * percent1 * percent3 , 1);
            _CalcTotal10Requirement     = Math.Round(large1 * percent1, 1);
            _CalcTotal30Requirement     = Math.Round(( large1 * percent1 ) + ( large2 * percent2 ), 1);

        }

        public double CalcTotal10SpinRequirement
        {
            get { return this._CalcTotal10SpinRequirement; }
        }

        public double CalcTotal10Requirement
        {
            get { return this._CalcTotal10Requirement; }
        }

        public double CalcTotal30Requirement
        {
            get { return this._CalcTotal30Requirement; }
        }

    }
}
