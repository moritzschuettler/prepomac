// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using System.Runtime.Serialization;

namespace CaeModel
{
    [PmxType(3245)]
    [Flags]
    public enum ElementHistoryVariable
    {
        // must start at 1 for the UI to work
        S = 1,
        E = 2,
        ME = 4,
        PEEQ = 8,
        // Thermal
        HFL = 16,
        // Whole element
        ENER = 32,
        ELSE = 64,
        ELKE = 128,
        EVOL = 256,
        EBHE = 512,
        //
        SDV = 1073741824,
    }

    [PmxType(3246)]
    public class ElementHistoryOutput : HistoryOutput
    {
        // Variables                                                                                                                
        [PmxKey(0)] private ElementHistoryVariable _variables;          //ISerializable


        // Properties                                                                                                               
        public ElementHistoryVariable Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public ElementHistoryOutput(string name, ElementHistoryVariable variables, string regionName, RegionTypeEnum regionType)
            : base(name, regionName, regionType)
        {
            _variables = variables;
        }
        public ElementHistoryOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_variables":
                    case "ElementHistoryOutput+_variables":     // Compatibility v2.1.0
                        _variables = (ElementHistoryVariable)entry.Value; break;
                }
            }
        }


    }
}
