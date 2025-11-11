using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    public class CalTitle : CalculixKeyword
    {
        // Variables                                                                                                                
        protected string _title;
        protected string _data;


        // Properties                                                                                                               
        public string Title { get { return _title; } }


        // Constructor                                                                                                              
        public CalTitle(string title, string data)
        {
            _title = title;
            _data = data;
            //
            while (_data.EndsWith(Environment.NewLine)) _data = _data.Substring(0, _data.Length - Environment.NewLine.Length);
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("************************************************************");
            sb.AppendLine("**");
            sb.AppendLine(("** " + _title + " ").PadRight(60, '+'));
            sb.AppendLine("**");
            //sb.AppendLine("************************************************************");
            return sb.ToString();
        }
        public override string GetDataString()
        {
            if (_data != null && _data.Length > 0)
            {
                string newLine = "";
                if (!_data.EndsWith(Environment.NewLine)) newLine = Environment.NewLine;
                return string.Format("{0}{1}", _data, newLine);
            }
            else return "";
        }
    }
}