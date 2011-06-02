using System.Collections.Generic;
using System.Xml;
using System.Configuration;
using System.Collections;

namespace DA.DAIE.FileUpload
{

    internal enum TAG_TYPE { LRR, N1 };

    /// <summary>
    /// Read from App.config 
    /// Element has following structure in App.Config
    /// [add MDB_shortname="CT" LRR_tag="CONN_LRR" N-1_tag="CONN_N-1"]
    /// </summary>
    public class ReserveZoneConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("MDB_shortname",
            IsRequired = true,
            IsKey = true)]
        public string MDBShortName
        {
            get { return (string)this["MDB_shortname"]; }
            set { this["MDB_shortname"] = value; }
                //get { return this._MDBShortName; }
        }

        [ConfigurationProperty("LRR_tag",
            IsRequired = true,
            IsKey = false)]
        public string LRRTag
        {
            get { return (string)this["LRR_tag"]; }
            set { this["LRR_tag"] = value; }
            //get { return this._LRRTag; }
        }

        [ConfigurationProperty("N-1_tag",
            IsRequired = true,
            IsKey = false)]
        public string N1Tag
        {
            get { return (string)this["N-1_tag"]; }
            set { this["N-1_tag"] = value; }
            //get { return this._N1Tag; }
        }
    }


    /// <summary>
    /// As the name suggests
    /// Contains a list of Configuration Elements of type ReserveZoneConfigurationElement
    /// </summary>
    public class ReserveZoneConfigurationElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ReserveZoneConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ReserveZoneConfigurationElement)element).MDBShortName;
        }

        internal bool containsTag(string argTag, TAG_TYPE argTagType, out string outFoundZoneShortName)
        {
            argTag = argTag.ToLower();
            List<ReserveZoneConfigurationElement> reserveZoneList = new List<ReserveZoneConfigurationElement>();

            IEnumerator en = base.GetEnumerator();
            while (en.MoveNext())
            {
                ReserveZoneConfigurationElement meta = (ReserveZoneConfigurationElement)en.Current;
                reserveZoneList.Add(meta);
            }

            if (argTagType.Equals(TAG_TYPE.LRR))
            {
                foreach (ReserveZoneConfigurationElement meta in reserveZoneList)
                {
                    if (meta.LRRTag.ToLower().Equals(argTag))
                    {
                        outFoundZoneShortName = meta.MDBShortName;
                        return true;
                    }
                }
            }
            else if (argTagType.Equals(TAG_TYPE.N1))
            {
                foreach (ReserveZoneConfigurationElement meta in reserveZoneList)
                {
                    if (meta.N1Tag.ToLower().Equals(argTag))
                    {
                        outFoundZoneShortName = meta.MDBShortName;
                        return true;
                    }
                }
            }
            outFoundZoneShortName = null;
            return false;
        }
    }

    public class ReserveZoneConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("ReserveZoneList")]
        [ConfigurationCollection(typeof(ReserveZoneConfigurationElementCollection), AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")] 
        public ReserveZoneConfigurationElementCollection ReserveZoneList
        {
            get
            {
                return (ReserveZoneConfigurationElementCollection)base["ReserveZoneList"];
            }
        }

    }
}