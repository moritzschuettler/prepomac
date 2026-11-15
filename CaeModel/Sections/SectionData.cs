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
using CaeGlobals;
using System.Runtime.Serialization;
using System.Drawing;

namespace CaeModel
{
    [PmxType(3204)]
    public abstract class SectionData : NamedClass, IMultiRegion
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _materialName;               //ISerializable
        [PmxKey(1)] private string _regionName;                 //ISerializable
        [PmxKey(2)] private RegionTypeEnum _regionType;         //ISerializable
        [PmxKey(3)] private int[] _creationIds;                 //ISerializable
        [PmxKey(4)] private Selection _creationData;            //ISerializable
        [PmxKey(5)] private double _thickness;                  //ISerializable


        // Properties                                                                                                               
        public string MaterialName { get { return _materialName; } set { _materialName = value; } }
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public double Thickness { get { return _thickness; } set { _thickness = value; } }


        // Constructors                                                                                                             
        public SectionData(string name, string materialName, string regionName, RegionTypeEnum regionType, double thickness) 
            : base(name)
        {
            _materialName = materialName;
            _regionName = regionName;
            _regionType = regionType;
            _creationIds = null;
            _creationData = null;
            _thickness = thickness;
        }
        public SectionData(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_materialName":
                        _materialName = (string)entry.Value; break;
                    case "_regionName":
                        _regionName = (string)entry.Value; break;
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_creationIds":
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                        _creationData = (Selection)entry.Value; break;
                    case "_thickness":
                        _thickness = (double)entry.Value; break;
                    default:
                        break;
                }
            }
        }


    }

}
