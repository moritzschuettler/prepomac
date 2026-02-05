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
    public class CalculixUserKeyword : CalculixKeyword
    {
        // Variables                                                                                                                
        private string _firstLine;
        private string _data;
        private object _parent;
        private bool _suppressed;


        // Properties                                                                                                               
        public string FirstLine { get { return _firstLine; } }
        public string Data { get { return _data; } set { _data = value; UpdateFirstLine(); } }
        public object Parent { get { return _parent; } set { _parent = value; } }
        public bool IsSuppressed { get { return _suppressed; } }


        // Constructor                                                                                                              
        public CalculixUserKeyword(string data)
        {
            _data = data;
            _parent = null;
            UpdateFirstLine();
        }


        // Methods                                                                                                                  
        public void Suppress()
        {
            _suppressed = true;
        }
        public void Unsuppress()
        {
            _suppressed = false;
        }
        
        public override string GetKeywordString()
        {
            if (_data != null && _data.Length > 0) return string.Format("{0}{1}", _data, Environment.NewLine);
            else return "";
        }
        public override string GetDataString()
        {
            return "";
        }
        private void UpdateFirstLine()
        {
            string[] tmp = _data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Length > 0) _firstLine = tmp[0];
        }
    }
}
