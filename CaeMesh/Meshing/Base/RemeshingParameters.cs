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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    [PmxType(2094)]
    public class RemeshingParameters : MeshingParameters, IMultiRegion
    {
        // Variables                                                                                                                
        [PmxKey(0)] private RegionTypeEnum _regionType;         //ISerializable
        [PmxKey(1)] private string _regionName;                 //ISerializable
        [PmxKey(2)] private double _hausdorff;                  //ISerializable
        [PmxKey(3)] private bool _keepModelEdges;               //ISerializable


        // Properties                                                                                                               
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public double Hausdorff
        {
            get { return _hausdorff; }
            set
            {
                if (value <= 0) throw new Exception("The value must be larger than 0.");
                _hausdorff = value;
            }
        }
        public bool KeepModelEdges { get { return _keepModelEdges; } set { _keepModelEdges = value; } }


        // Constructors                                                                                                             
        public RemeshingParameters(string regionName, RegionTypeEnum regionType, MeshingParameters meshingParameters)
            : base(meshingParameters)
        {
            _regionName = regionName;
            _regionType = regionType;
            _hausdorff = 0.01;
            _keepModelEdges = true;
        }
        public RemeshingParameters(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_regionName":
                        _regionName = (string)entry.Value; break;
                    // Up to version v2.6.2 both values were written by MeshingParameters, into the
                    // same flat SerializationInfo, so an old remeshing command keeps them
                    case "_hausdorff":
                        _hausdorff = (double)entry.Value; break;
                    case "_keepModelEdges":
                        _keepModelEdges = (bool)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            base.Reset();
            //
            _regionName = null;
            _regionType = RegionTypeEnum.None;
            _hausdorff = 0.01;
            _keepModelEdges = true;
        }
    }
}
