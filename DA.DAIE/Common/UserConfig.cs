using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace DA.DAIE.Common
{
    public class UserConfig
    {
        public static double getContingency1Percent()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("UserConfig.xml");
            XmlNode xnode = doc.SelectSingleNode("//IE_UserConfig/FirstContOpResPct");
            String str = xnode.InnerXml;
            double dd = double.Parse(str);
            dd = Math.Round(dd, 2);
            return dd;
        }

        public static double getContingency2Percent()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("UserConfig.xml");
            XmlNode xnode = doc.SelectSingleNode("//IE_UserConfig/SecondContOpResPct");
            String str = xnode.InnerXml;
            double dd = double.Parse(str);
            dd = Math.Round(dd, 2);
            return dd;
        }

        public static double getTMSRPercent()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("UserConfig.xml");
            XmlNode xnode = doc.SelectSingleNode("//IE_UserConfig/TMSRPct");
            String str = xnode.InnerXml;
            double dd = double.Parse(str);
            dd = Math.Round(dd, 2);
            return dd;
        }

        public static void setContingencyPercent(double argPercent1, double argPercent2, double argTMSRPercent )
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("UserConfig.xml");

            double percent1 = Math.Round(argPercent1, 2);
            double percent2 = Math.Round(argPercent2, 2);
            double percentTMSR = Math.Round(argTMSRPercent, 2);

            XmlNode xnode = doc.SelectSingleNode("//IE_UserConfig/FirstContOpResPct");
            xnode.InnerText = percent1.ToString();
            XmlNode xnode2 = doc.SelectSingleNode("//IE_UserConfig/SecondContOpResPct");
            xnode2.InnerText = percent2.ToString();
            XmlNode xnode3 = doc.SelectSingleNode("//IE_UserConfig/TMSRPct");
            xnode3.InnerText = percentTMSR.ToString();


            doc.Save("UserConfig.xml");
        }
    }
}
