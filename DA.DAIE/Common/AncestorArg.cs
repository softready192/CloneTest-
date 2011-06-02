using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DA.DAIE.Common
{
    public enum ValueTypeEnum { MW };

    public class AncestorArg
    {
        private StringBuilder sb = new StringBuilder();

        public void Add(string argName, string value)
        {
            this.sb.Append(" " + argName + ":" + value);
        }

        public void Add( string argName, DateTime value )
        {
            Add(argName, DateTimeUtil.getLogString(value));
        }

        public void Add(string argName, double value)
        {
            Add(argName, value.ToString());
        }

        public void Add(string argName, int value)
        {
            Add(argName, value.ToString());
        }

        public void Add(string argName, Int64 value)
        {
            Add(argName, value.ToString());
        }

        public void Add(string argName, decimal value)
        {
            Add(argName, value.ToString());
        }

        private void Add(string argName, decimal value, ValueTypeEnum valueType )
        {
            Add(argName, value.ToString());
        }

        public void Add(string argName, bool value)
        {
            Add(argName, value.ToString());
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public string ToLogString()
        {
            string returnString = " Class:" + this.GetType().Name + " { " + this.sb.ToString() + " } ";
            this.sb = new StringBuilder();
            return returnString;
        }

    }
}
