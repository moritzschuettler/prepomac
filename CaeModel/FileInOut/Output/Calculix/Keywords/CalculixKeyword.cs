// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
    public abstract class CalculixKeyword //: ISerializable - this would mean that all derived classes must be Serializable !!!
    {
        // Variables                                                                                                                
        protected List<CalculixKeyword> _keywords;          //ISerializable
        private bool _active;                               //ISerializable


        // Properties                                                                                                               
        public List<CalculixKeyword> Keywords { get { return _keywords; } }
        public bool Active { get { return _active; } set { _active = value; } }


        // Constructor                                                                                                              
        public CalculixKeyword()
        {
            _keywords = new List<CalculixKeyword>();
            _active = true;
        }

        public CalculixKeyword(SerializationInfo info, StreamingContext context)
        {
            _active = true; // Compatibility v 2.4.0
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_keywords":
                        _keywords = (List<CalculixKeyword>)entry.Value; break;
                    case "_active":
                        _active = (bool)entry.Value; break;
                }
            }
        }

        // Methods                                                                                                                  
        public void AddKeyword(CalculixKeyword keyword)
        {
            _keywords.Add(keyword);
        }
        public void ClearKeywords()
        {
            _keywords.Clear();
        }
        public abstract string GetKeywordString();
        public abstract string GetDataString();
        //
        protected string GetComplexLoadCase(ComplexLoadTypeEnum complexLoadType)
        {
            string loadCase;
            if (complexLoadType == ComplexLoadTypeEnum.None) loadCase = "";
            else if (complexLoadType == ComplexLoadTypeEnum.Real) loadCase = ", Load case=1";
            else if (complexLoadType == ComplexLoadTypeEnum.Imaginary) loadCase = ", Load case=2";
            else throw new NotImplementedException();
            //
            return loadCase;
        }
        protected double GetComplexRatio(ComplexLoadTypeEnum complexLoadType, double angleDeg)
        {
            double ratio;
            if (complexLoadType == ComplexLoadTypeEnum.None)
            {
                ratio = 1;
            }
            else if (complexLoadType == ComplexLoadTypeEnum.Real)
            {
                if (angleDeg == 0) ratio = 1;
                else if (angleDeg == 90) ratio = 0;
                else if (angleDeg == 180) ratio = -1;
                else if (angleDeg == 270) ratio = 0;
                else ratio = Math.Cos(angleDeg * Math.PI / 180);
            }
            else if (complexLoadType == ComplexLoadTypeEnum.Imaginary)
            {
                if (angleDeg == 0) ratio = 0;
                else if (angleDeg == 90) ratio = 1;
                else if (angleDeg == 180) ratio = 0;
                else if (angleDeg == 270) ratio = -1;
                else ratio = Math.Sin(angleDeg * Math.PI / 180);
            }
            else throw new NotSupportedException();
            //
            return ratio;
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_keywords", _keywords, typeof(List<CalculixKeyword>));
            info.AddValue("_active", _active, typeof(bool));
        }
    }
}
