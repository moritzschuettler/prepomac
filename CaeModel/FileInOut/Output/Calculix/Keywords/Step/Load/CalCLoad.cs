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
    internal class CalCLoad : CalLoad
    {
        // Variables                                                                                                                
        private CLoad _load;
        private Dictionary<string, CalReferencePoint> _referencePoints;
        private ComplexLoadTypeEnum _complexLoadType;
        private FeMesh _mesh;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalCLoad(FeMesh mesh, CLoad load, Dictionary<string, CalReferencePoint> referencePoints,
                        ComplexLoadTypeEnum complexLoadType)
        {
            _mesh = mesh;
            _load = load;
            _referencePoints = referencePoints;
            _complexLoadType = complexLoadType;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _load.Name);
            string amplitude;
            if (_load.AmplitudeName != Amplitude.DefaultAmplitudeName) amplitude = ", Amplitude=" + _load.AmplitudeName;
            else amplitude = "";
            //
            string loadCase = GetComplexLoadCase(_complexLoadType);
            //
            sb.AppendFormat("*Cload{0}{1}{2}{3}", amplitude, loadCase, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            CoordinateSystem coordinateSystem = null;
            if (_mesh != null) _mesh.CoordinateSystems.TryGetValue(_load.CoordinateSystemName, out coordinateSystem);
            if (coordinateSystem != null) return GetDataStringInGlobalDirections(coordinateSystem);
            //
            StringBuilder sb = new StringBuilder();
            //
            CalReferencePoint referencePoint = null;
            if (_load.RegionType == RegionTypeEnum.ReferencePointName) referencePoint = _referencePoints[_load.RegionName];
            //
            List<int> directions = new List<int>();
            if (_load.F1.Value != 0) directions.Add(1);
            if (_load.F2.Value != 0) directions.Add(2);
            if (_load.F3.Value != 0) directions.Add(3);
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            //
            foreach (var dir in directions)
            {
                if (_load.RegionType == RegionTypeEnum.NodeId)
                    sb.AppendFormat("{0}, {1}, {2}", _load.NodeId, dir,
                                    (ratio * _load.GetComponent(dir - 1)).ToCalculiX16String());
                else if (_load.RegionType == RegionTypeEnum.NodeSetName) // node set
                    sb.AppendFormat("{0}, {1}, {2}", _load.RegionName, dir,
                                    (ratio * _load.GetComponent(dir - 1)).ToCalculiX16String());
                else if (_load.RegionType == RegionTypeEnum.ReferencePointName) // reference point
                    sb.AppendFormat("{0}, {1}, {2}", referencePoint.ReferenceNodeId, dir,
                                    (ratio * _load.GetComponent(dir - 1)).ToCalculiX16String());
                //
                sb.AppendLine();
            }
            //
            return sb.ToString();
        }
        // A *Transform would rotate every load and boundary condition of the nodes in every step, so the components
        // are rotated here instead; a cylindrical system has directions of its own at every node
        private string GetDataStringInGlobalDirections(CoordinateSystem coordinateSystem)
        {
            int[] nodeIds;
            if (_load.RegionType == RegionTypeEnum.NodeId) nodeIds = new int[] { _load.NodeId };
            else if (_load.RegionType == RegionTypeEnum.NodeSetName) nodeIds = _mesh.NodeSets[_load.RegionName].Labels;
            else if (_load.RegionType == RegionTypeEnum.ReferencePointName)
                nodeIds = new int[] { _referencePoints[_load.RegionName].ReferenceNodeId };
            else throw new NotSupportedException();
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            double[] localForce = new double[] { _load.F1.Value, _load.F2.Value, _load.F3.Value };
            bool sameAtEveryNode = coordinateSystem.Type == CoordinateSystemTypeEnum.Rectangular &&
                                   _load.RegionType == RegionTypeEnum.NodeSetName;
            int numOfDirections = _load.TwoD ? 2 : 3;
            //
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < nodeIds.Length; i++)
            {
                double[] coor;
                if (_load.RegionType == RegionTypeEnum.ReferencePointName)
                    coor = _mesh.ReferencePoints[_load.RegionName].Coor();
                else coor = _mesh.Nodes[nodeIds[i]].Coor;
                double[] force = coordinateSystem.GetOrientedVectorAtPoint(localForce, coor);
                string nodeName = sameAtEveryNode ? _load.RegionName : nodeIds[i].ToString();
                //
                for (int dir = 1; dir <= numOfDirections; dir++)
                {
                    if (force[dir - 1] == 0) continue;
                    sb.AppendFormat("{0}, {1}, {2}", nodeName, dir, (ratio * force[dir - 1]).ToCalculiX16String());
                    sb.AppendLine();
                }
                if (sameAtEveryNode) break;
            }
            //
            return sb.ToString();
        }
    }
}
