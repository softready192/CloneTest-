using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.jaspersoft.webservice.xmlHelper;
using System.Xml;

namespace DA.DAIE.PrintReport
{
    public class ReportMetadata
    {

        private string _Name;
        private string _Label;
        private string _File;
        private string _URL;
        private string _Orientation;
        private string _CleanCSV;
        private List<Parameter> _Parameters = new List<Parameter>();

        public string Name
        {
            get { return this._Name; }
        }

        public string Label
        {
            get { return this._Label; }
        }

        public string File
        {
            get { return this._File; }
        }

        public string URL
        {
            get { return this._URL; }
        }

        public string Orientation
        {
            get { return this._Orientation; }
        }

        public Parameter[] Parameters
        {
            get
            {
                if (this._Parameters == null || this._Parameters.Count == 0)
                {
                    return new Parameter[] { };
                }
                else
                {
                    Parameter[] parameters = new Parameter[_Parameters.Count];
                    int i = 0;
                    foreach (Parameter p in _Parameters)
                    {
                        parameters[i] = p;
                        i++;
                    }
                    return parameters;
                }
            }
        }


        public ReportMetadata(string argName, string argLabel, string argFile, string argURL, string argOrientation, List<Parameter> argParameters)
        {
            this._Name = argName;
            this._Label = argLabel;
            this._File = argFile;
            this._URL = argURL;
            this._Orientation = argOrientation;
            this._Parameters = argParameters;
        }

        public ReportMetadata(XmlNode argReportNode)
        {
            foreach (XmlNode childNode in argReportNode.ChildNodes)
            {
                if (childNode.Name.Equals("name"))
                {
                    this._Name = childNode.InnerText;
                }
                else if (childNode.Name.Equals("label"))
                {
                    this._Label = childNode.InnerText;
                }
                else if (childNode.Name.Equals("file"))
                {
                    this._File = childNode.InnerText;
                }
                else if (childNode.Name.Equals("url"))
                {
                    this._URL = childNode.InnerText;
                }
                else if (childNode.Name.Equals("orientation"))
                {
                    this._Orientation = childNode.InnerText;
                }
                else if (childNode.Name.Equals("clean_csv"))
                {
                    this._CleanCSV = childNode.InnerText;
                }
                else if (childNode.Name.Equals("parameters"))
                {
                    string parametersString = childNode.InnerText;
                    if (parametersString != null && parametersString.Length > 0)
                    {
                        string[] parameters = parametersString.Split(' ');
                        foreach (string parameter in parameters)
                        {
                            string[] keyValue = parameter.Split('=');
                            Parameter newParameter = new Parameter();
                            newParameter.name = keyValue[0];
                            newParameter.parameterValue = keyValue[1];
                            _Parameters.Add(newParameter);
                        }
                    }
                }
            }
        }

    }
}
