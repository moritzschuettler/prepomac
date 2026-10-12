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
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalSTLoad : CalLoad
    {
        // Variables                                                                                                                
        private STLoad _load;
        private CLoad[] _cLoads;
        private ComplexLoadTypeEnum _complexLoadType;
        private FeMesh _mesh;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalSTLoad(FeModel model, STLoad load, ComplexLoadTypeEnum complexLoadType)
        {
            _load = load;
            _cLoads = model.GetNodalCLoadsFromSurfaceTraction(_load);
            _complexLoadType = complexLoadType;
            _mesh = model.Mesh;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _load.Name);
            string amplitude = "";
            if (_load.AmplitudeName != Amplitude.DefaultAmplitudeName) amplitude = ", Amplitude=" + _load.AmplitudeName;
            //
            string loadCase = GetComplexLoadCase(_complexLoadType);
            //
            sb.AppendFormat("*Cload{0}{1}{2}{3}", amplitude, loadCase, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            //
            // A *Transform would rotate every load and boundary condition of the nodes in every step, so the components
            // given in a coordinate system are rotated here instead
            CoordinateSystem coordinateSystem;
            _mesh.CoordinateSystems.TryGetValue(_load.CoordinateSystemName, out coordinateSystem);
            int numOfDirections = _load.TwoD ? 2 : 3;
            //
            if (_cLoads != null)
            {
                foreach (var cLoad in _cLoads)
                {
                    double[] force = new double[] { cLoad.F1.Value, cLoad.F2.Value, cLoad.F3.Value };
                    if (coordinateSystem != null)
                        force = coordinateSystem.GetOrientedVectorAtPoint(force, _mesh.Nodes[cLoad.NodeId].Coor);
                    //
                    for (int dir = 1; dir <= numOfDirections; dir++)
                    {
                        if (force[dir - 1] == 0) continue;
                        sb.AppendFormat("{0}, {1}, {2}", cLoad.NodeId, dir, (ratio * force[dir - 1]).ToCalculiX16String());
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }
    }
}
