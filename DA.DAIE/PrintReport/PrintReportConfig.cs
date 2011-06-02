using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.jaspersoft.webservice;
using DA.DAIE.CaseSelection;
using System.Xml;
using DA.DAIE.Admin;
using DA.Common;

namespace DA.DAIE.PrintReport
{
    public class PrintReportConfig
    {
        //public static string CONNECTIONS_FILE_NAME = ConnectionBO.CONNECTIONS_FILE_NAME;
        private string _FileName = "PrintReport/PrintReportConfig.xml";
        private XmlDocument _doc;

        /// <summary>
        /// Constructor takes defaulted value for _FileName.
        /// </summary>
        public PrintReportConfig()
        {
        }

        /// <summary>
        /// Constructor lets you choose another value for _FileName.
        /// </summary>
        /// <param name="argFileName"></param>
        public PrintReportConfig(string argFileName)
        {
            this._FileName = argFileName;
        }

        private void getDocument()
        {
            if (_doc == null)
            {
                _doc = new XmlDocument();
                _doc.Load(_FileName);
            }
        }


      
        public Repository getRepository()
        {
            string strLogin;
            string strPassword;

            Repository rep = new Repository();

            getDocument();

            XmlNode node = _doc.SelectSingleNode("//reportServer/endpoint");
            rep.url = node.InnerText;
            node = _doc.SelectSingleNode("//reportServer/login");
            strLogin = node.InnerText;
            node = _doc.SelectSingleNode("//reportServer/password");
            strPassword = node.InnerText;
            rep.setCredentials(strLogin, strPassword);
            return rep;
        }

        public string getPath()
        {
            getDocument();
            XmlNode node = _doc.SelectSingleNode("//reportServer/path");
            return node.InnerText;
        }


        public List<ReportMetadata> getReportList(string argReportGroupName)
        {

            List<ReportMetadata> list = new List<ReportMetadata>();
            getDocument();

            XmlNodeList reportNodes = _doc.SelectNodes("//report_group[@name='" + argReportGroupName + "']/report");
            foreach (XmlNode reportNode in reportNodes)
            {
                ReportMetadata rm = new ReportMetadata(reportNode);
                list.Add(rm);
            }
            return list;

        }
 
    }
}
