// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
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
    [Serializable]
    [Flags]
    public enum SectionHistoryVariableEnum
    {
        // Must start at 1 for the UI to work
        SOF = 1,
        SOM = 2,
        SOAREA = 4,
    }

    [Serializable]
    public class SectionHistoryOutput : HistoryOutput, ISerializable
    {
        // Variables                                                                                                                
        private SectionHistoryVariableEnum _variables;              //ISerializable


        // Properties                                                                                                               
        public SectionHistoryVariableEnum Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public SectionHistoryOutput(string name, SectionHistoryVariableEnum variables, string regionName, RegionTypeEnum regionType)
            : base(name, regionName, regionType)
        {
            _variables = variables;
        }
        public SectionHistoryOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_variables":
                        _variables = (SectionHistoryVariableEnum)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  


        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_variables", _variables, typeof(SectionHistoryVariableEnum));
        }
    }
}
