// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using CaeModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    public class CalculixUserKeyword : CalculixKeyword, ISerializable
    {
        // Variables                                                                                                                
        private string _firstLine;                  //ISerializable
        private string _data;                       //ISerializable
        private object _parent;                     //ISerializable
        private bool _suppressed;                   //ISerializable


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
        public CalculixUserKeyword(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_firstLine":
                        _firstLine = (string)entry.Value; break;
                    case "_data":
                        _data = (string)entry.Value; break;
                    case "_parent":
                        _parent = (object)entry.Value; break;
                    case "_suppressed":
                        _suppressed = (bool)entry.Value; break;
                }
            }
            //
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
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_firstLine", _firstLine, typeof(string));
            info.AddValue("_data", _data, typeof(string));
            info.AddValue("_parent", _parent, typeof(object));
            info.AddValue("_suppressed", _suppressed, typeof(bool));
        }
    }
}
