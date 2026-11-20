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
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Runtime.Serialization;

namespace CaeModel
{
    [PmxType(3247)]
    public enum TotalsTypeEnum
    {
        [StandardValue("Yes", Description = "Sum of external forces is printed in addition to the individual node values.")]
        Yes,
        [StandardValue("Only", Description = "Only sum of external forces is printed.")]
        Only,
        [StandardValue("No", Description = "Only individual node values are printed (default).")]
        No
    }

    [PmxType(3248)]
    public abstract class HistoryOutput : NamedClass, IMultiRegion
    {
        // Variables                                                                                                                
        [PmxKey(0)] private RegionTypeEnum _regionType;         //ISerializable
        [PmxKey(1)] private string _regionName;                 //ISerializable
        [PmxKey(2)] private TotalsTypeEnum _totals;             //ISerializable
        [PmxKey(3)] private int[] _creationIds;                 //ISerializable
        [PmxKey(4)] private Selection _creationData;            //ISerializable
        [PmxKey(5)] private bool _global;                       //ISerializable


        // Properties                                                                                                               
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public TotalsTypeEnum TotalsType { get { return _totals; } set { _totals = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public bool Global { get { return _global; } set { _global = value; } }


        // Constructors                                                                                                             
        public HistoryOutput(string name, string regionName, RegionTypeEnum regionType)
            : base(name)
        {
            _regionName = regionName;
            _regionType = regionType;
            _totals = TotalsTypeEnum.No;
            _creationIds = null;
            _creationData = null;
            _global = true;
        }
        public HistoryOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            // Compatibility v2.1.0
            _global = true;
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionType":
                    case "HistoryOutput+_regionType":       // Compatibility v2.1.0
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_regionName":
                    case "HistoryOutput+_regionName":       // Compatibility v2.1.0
                        _regionName = (string)entry.Value; break;
                    case "_totals":
                    case "HistoryOutput+_totals":           // Compatibility v2.1.0
                        _totals = (TotalsTypeEnum)entry.Value; break;
                    case "_creationIds":
                    case "HistoryOutput+_creationIds":      // Compatibility v2.1.0
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                    case "HistoryOutput+_creationData":     // Compatibility v2.1.0
                        _creationData = (Selection)entry.Value; break;
                    case "_global":
                        _global = (bool)entry.Value; break;
                }
            }
        }


    }
}
