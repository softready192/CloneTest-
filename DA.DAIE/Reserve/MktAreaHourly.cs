using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DA.DAIE.Reserve
{

    /// <summary>
    /// Holds data needed to call SQL id="UpdateMktAreaHourly"
    /// </summary>
    public class MktAreaHourly
    {
        DateTime _MktHour;
        double _Total10SpinRequirement;
        double _Total10Requirement;
        double _Total30Requirement;

        public MktAreaHourly
            (DateTime argMktHour
            , double argTotal10SpinRequirement
            , double argTotal10Requirement
            , double argTotal30Requirement)
        {
            this._MktHour = argMktHour;
            this._Total10Requirement = argTotal10Requirement;
            this._Total10SpinRequirement = argTotal10SpinRequirement;
            this._Total30Requirement = argTotal30Requirement;
        }

        public DateTime MktHour
        {
            get { return this._MktHour; }
        }

        public double Total10SpinRequirement
        {
            get { return this._Total10SpinRequirement; }
        }

        public double Total10Requirement
        {
            get { return this._Total30Requirement; }
        }

        public double Total30Requirement
        {
            get { return this._Total30Requirement; }
        }

    }
}
