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
using CaeModel;
using CaeMesh;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [PmxType(3047)]
    internal class AbqFixedBC : CalBC
    {
        // Variables                                                                                                                
        [PmxKey(0)] private FixedBC _fixedBC;
        [PmxKey(1)] private Dictionary<string, CalReferencePoint> _referencePoints;
        [PmxKey(2)] private string _nodeSetNameOfSurface;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqFixedBC(FixedBC fixedBC, Dictionary<string, CalReferencePoint> referencePoints,
                          string nodeSetNameOfSurface)
        {
            _fixedBC = fixedBC;
            _referencePoints = referencePoints;
            _nodeSetNameOfSurface = nodeSetNameOfSurface;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _fixedBC.Name);
            sb.AppendLine("*Boundary" + OpTypeString());
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            // *Boundary
            // 6975, 1, 1, 0        node id, start DOF, end DOF, value
            int[] directions = _fixedBC.GetConstrainedDirections();
            // Node set
            if (_fixedBC.RegionType == CaeGlobals.RegionTypeEnum.NodeSetName)
            {
                if (_fixedBC.TwoD)
                {
                    sb.AppendFormat("{0}, 1, 2, 0", _fixedBC.RegionName); sb.AppendLine();
                    sb.AppendFormat("{0}, 6, 6, 0", _fixedBC.RegionName);
                }
                else
                {
                    sb.AppendFormat("{0}, 1, 6, 0", _fixedBC.RegionName);
                }
                sb.AppendLine();
            }
            // Surface
            else if (_fixedBC.RegionType == CaeGlobals.RegionTypeEnum.SurfaceName)
            {
                if (_nodeSetNameOfSurface == null) throw new ArgumentException();
                if (_fixedBC.TwoD)
                {
                    sb.AppendFormat("{0}, 1, 2, 0", _nodeSetNameOfSurface); sb.AppendLine();
                    sb.AppendFormat("{0}, 6, 6, 0", _nodeSetNameOfSurface);
                }
                else
                {
                    sb.AppendFormat("{0}, 1, 6, 0", _nodeSetNameOfSurface);
                }
                sb.AppendLine();
            }
            // Reference point
            else if (_fixedBC.RegionType == CaeGlobals.RegionTypeEnum.ReferencePointName)
            {
                CalReferencePoint referencePoint = _referencePoints[_fixedBC.RegionName];
                if (_fixedBC.TwoD) sb.AppendFormat("{0}, 1, 2, 0", referencePoint.ReferenceNodeId);
                else sb.AppendFormat("{0}, 1, 6, 0", referencePoint.ReferenceNodeId);
                sb.AppendLine();
            }
            else throw new NotSupportedException();
            //
            return sb.ToString();
        }
    }
}
