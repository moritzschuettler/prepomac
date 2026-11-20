// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CaeResults
{
    [PmxType(5036)]
    public class HistoryResultData : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] public string SetName;                  //ISerializable
        [PmxKey(1)] public string FieldName;                //ISerializable
        [PmxKey(2)] public string ComponentName;            //ISerializable



        // Constructors                                                                                                              
        public HistoryResultData(string setName, string fieldName, string componentName)
            :base()
        {
            _checkName = false;
            _name = setName + "_" + fieldName + "_" + componentName;
            SetName = setName;
            FieldName = fieldName;
            ComponentName = componentName;
        }
        //ISerializable
        public HistoryResultData(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "SetName":
                        SetName = (string)entry.Value; break;
                    case "FieldName":
                        FieldName = (string)entry.Value; break;
                    case "ComponentName":
                        ComponentName = (string)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  
        public string GetDisplayName()
        {
            if (this == null) return "";
            return this.SetName + "." + this.FieldName + "." + this.ComponentName;
        }


    }
}
