using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DA.DAIE.ExternalTransaction
{

    public class TransactionDetails : List<SelectExternalTransactionDetail>
    {
        public TransactionDetails(IList<SelectExternalTransactionDetail> argDetails)
        {
            foreach (SelectExternalTransactionDetail dt in argDetails)
            {
                this.Add(dt);
            }
        }

        public Decimal FixedMWTotal()
        {
            Decimal dTotal = 0.0M;
            foreach (SelectExternalTransactionDetail dt in this )
            {
                dTotal = dTotal + dt.FixedMW;
            }
            return dTotal;
        }
    }

    public partial class SelectExternalTransactionDetail
    {
        private string _HourLabel;
        private DateTime _LocalHour;
        private DateTime _MktHour;
        private DateTime _MktDay;
        private Int64    _TransactionId;
        private Decimal  _FixedMW;
        private Decimal  _PurchaseMW;
        private Decimal  _SaleMW;

        private Decimal _OriginalFixedMW;

        // Returns true if FixedMW has changed.
        public bool IsDirty
        {
            get { return !( this._OriginalFixedMW.Equals(this._FixedMW)); }
        }

        public SelectExternalTransactionDetail
            ( string argHourLabel
            , DateTime argLocalHour
            , DateTime argMktHour
            , DateTime argMktDay
            , Int64 argTransactionId
            , Decimal argFixedMW
            , Decimal argPurchaseMW
            , Decimal argSaleMW)
        {
            this._HourLabel = argHourLabel;
            this._LocalHour = argLocalHour;
            this._MktHour = argMktHour;
            this._MktDay = argMktDay;
            this._TransactionId = argTransactionId;
            this._FixedMW = argFixedMW;
            this._OriginalFixedMW = argFixedMW;
            this._PurchaseMW = argPurchaseMW;
            this._SaleMW = argSaleMW;
        }

        public string HourLabel
        {
            get { return this._HourLabel; }
        }

        public DateTime LocalHour
        {
            get { return this._LocalHour; }
        }

        public DateTime MktHour
        {
            get { return this._MktHour; }
        }

        public DateTime MktDay
        {
            get { return this._MktDay; }
        }

        public Int64 TransactionId
        {
            get { return this._TransactionId; }
        }

        public Decimal FixedMW
        {
            get { return Decimal.Round(this._FixedMW, 1); }
            set 
            { 
                this._FixedMW = Decimal.Round(value,1); 
            }
        }

        public Decimal PurchaseMW
        {
            get { return this._PurchaseMW; }
        }

        public Decimal SaleMW
        {
            get { return this._SaleMW; }
        }

    }
}
