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
using CaeModel;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalFixedBC : CalBC
    {
        // Variables                                                                                                                
        private FixedBC _fixedBC;
        private Dictionary<string, int[]> _referencePointsNodeIds;
        private string _nodeSetNameOfSurface;
        protected int _maxNumberNodeDOFs;


        // Properties                                                                                                               
        public int MaxNumberNodeDOFs { get { return _maxNumberNodeDOFs; } set { _maxNumberNodeDOFs = value; } }


        // Constructor                                                                                                              
        public CalFixedBC(FixedBC fixedBC, Dictionary<string, int[]> referencePointsNodeIds,
                          string nodeSetNameOfSurface)
        {
            _fixedBC = fixedBC;
            _referencePointsNodeIds = referencePointsNodeIds;
            _nodeSetNameOfSurface = nodeSetNameOfSurface;
            _maxNumberNodeDOFs = 3; // 3 for Calculix and 6 for Abaqus
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
                AppendDOFs(sb, _fixedBC.RegionName);
            }
            // Surface
            else if (_fixedBC.RegionType == CaeGlobals.RegionTypeEnum.SurfaceName)
            {
                if (_nodeSetNameOfSurface == null) throw new ArgumentException();
                AppendDOFs(sb, _nodeSetNameOfSurface);
            }
            // Reference point
            else if (_fixedBC.RegionType == CaeGlobals.RegionTypeEnum.ReferencePointName)
            {
                int[] rpNodeIds = _referencePointsNodeIds[_fixedBC.RegionName];
                //
                if (_maxNumberNodeDOFs == 3)
                {
                    if (_fixedBC.TwoD)
                    {
                        sb.AppendFormat("{0}, 1, 2, 0", rpNodeIds[0]); sb.AppendLine();
                        sb.AppendFormat("{0}, 3, 3, 0", rpNodeIds[1]); sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendFormat("{0}, 1, 3, 0", rpNodeIds[0]); sb.AppendLine();
                        sb.AppendFormat("{0}, 1, 3, 0", rpNodeIds[1]); sb.AppendLine();
                    }
                }
                else if (_maxNumberNodeDOFs == 6) AppendDOFs(sb, rpNodeIds[0].ToString());
                else throw new NotSupportedException();
            }
            else throw new NotSupportedException();
            //
            return sb.ToString();
        }
        private void AppendDOFs(StringBuilder sb, string regionName)
        {
            if (_fixedBC.TwoD)
            {
                sb.AppendFormat("{0}, 1, 2, 0", regionName); sb.AppendLine();
                //sb.AppendFormat("{0}, 6, 6, 0", regionName); // not working
            }
            else
            {
                sb.AppendFormat("{0}, 1, 6, 0", regionName);
            }
            sb.AppendLine();
        }
    }
}
