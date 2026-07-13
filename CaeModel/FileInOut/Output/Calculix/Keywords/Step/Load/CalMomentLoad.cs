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
    internal class CalMomentLoad : CalLoad
    {
        // Variables                                                                                                                
        private MomentLoad _load;
        private Dictionary<string, CalReferencePoint> _referencePoints;
        protected ComplexLoadTypeEnum _complexLoadType;
        protected int _maxNumberNodeDOFs;


        // Properties                                                                                                               
        public int MaxNumberNodeDOFs { get { return _maxNumberNodeDOFs; } set { _maxNumberNodeDOFs = value; } }


        // Constructor                                                                                                              
        public CalMomentLoad(MomentLoad load, Dictionary<string, CalReferencePoint> referencePoints,
                             ComplexLoadTypeEnum complexLoadType)
        {
            _load = load;
            _referencePoints = referencePoints;
            _complexLoadType = complexLoadType;
            _maxNumberNodeDOFs = 3; // 3 for Calculix and 6 for Abaqus
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
            CalReferencePoint referencePoint = null;
            if (_load.RegionType == RegionTypeEnum.ReferencePointName) referencePoint = _referencePoints[_load.RegionName];
            //
            List<int> directions = new List<int>();
            if (_load.M1.Value != 0) directions.Add(4);
            if (_load.M2.Value != 0) directions.Add(5);
            if (_load.M3.Value != 0) directions.Add(6);
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            //
            foreach (var dir in directions)
            {
                if (_load.RegionType == RegionTypeEnum.NodeId)
                    sb.AppendFormat("{0}, {1}, {2}", _load.NodeId, dir,
                                    (ratio * _load.GetDirection(dir - 4)).ToCalculiX16String());
                else if (_load.RegionType == RegionTypeEnum.NodeSetName) // node set
                    sb.AppendFormat("{0}, {1}, {2}", _load.RegionName, dir,
                                    (ratio * _load.GetDirection(dir - 4)).ToCalculiX16String());
                else if (_load.RegionType == RegionTypeEnum.ReferencePointName) // reference point
                {
                    if (_maxNumberNodeDOFs == 3 && referencePoint.HasRotationNode)
                        sb.AppendFormat("{0}, {1}, {2}", referencePoint.RotationNodeId.Value, dir - 3,
                                        (ratio * _load.GetDirection(dir - 4)).ToCalculiX16String());
                    else if (_maxNumberNodeDOFs == 3 || _maxNumberNodeDOFs == 6)
                        sb.AppendFormat("{0}, {1}, {2}", referencePoint.ReferenceNodeId, dir,
                                        (ratio * _load.GetDirection(dir - 4)).ToCalculiX16String());
                    else throw new NotSupportedException();
                }
                sb.AppendLine();
            }
            //
            return sb.ToString();
        }
    }
}
