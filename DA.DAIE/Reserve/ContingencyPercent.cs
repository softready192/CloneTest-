using System;


namespace DA.DAIE.Reserve
{
    public class ContingencyPercent
    {
        double _Contingency1Percent;
        double _Contingency2Percent;
        double _TMSRPercent;

        public ContingencyPercent(double argPercent1, double argPercent2, double argTMSRPercent)
        {
            this._Contingency1Percent = argPercent1;
            this._Contingency2Percent = argPercent2;
            this._TMSRPercent = argTMSRPercent;
        }

        public double Contingency1Percent
        {
            get { return _Contingency1Percent; }
        }

        public double Contingency2Percent
        {
            get { return _Contingency2Percent; }
        }

        public double TMSRPercent
        {
            get { return _TMSRPercent; }
        }
    }
}
