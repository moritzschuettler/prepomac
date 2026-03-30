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
    internal class CalDisplacementRotation : CalBC
    {
        // Variables                                                                                                                
        protected DisplacementRotation _displacementRotation;
        protected Dictionary<string, int[]> _referencePointsNodeIds;
        protected string _nodeSetNameOfSurface;
        protected ComplexLoadTypeEnum _complexLoadType;
        protected int _maxNumberNodeDOFs;


        // Properties                                                                                                               
        public int MaxNumberNodeDOFs { get { return _maxNumberNodeDOFs; } set { _maxNumberNodeDOFs = value; } }


        // Constructor                                                                                                              
        public CalDisplacementRotation(CalDisplacementRotation displacementRotation)
            : this(displacementRotation._displacementRotation, displacementRotation._referencePointsNodeIds,
                   displacementRotation._nodeSetNameOfSurface, displacementRotation._complexLoadType)
        {
        }
        public CalDisplacementRotation(DisplacementRotation displacementRotation, Dictionary<string, int[]> referencePointsNodeIds,
                                       string nodeSetNameOfSurface, ComplexLoadTypeEnum complexLoadType)
        {
            _displacementRotation = displacementRotation;
            _referencePointsNodeIds = referencePointsNodeIds;
            _nodeSetNameOfSurface = nodeSetNameOfSurface;
            _complexLoadType = complexLoadType;
            _maxNumberNodeDOFs = 3; // 3 for Calculix and 6 for Abaqus
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _displacementRotation.Name);
            string fixedBc = "";
            if (_displacementRotation.GetFixedDirections().Length > 0) fixedBc = ", Fixed";
            string amplitude = "";
            if (_displacementRotation.AmplitudeName != Amplitude.DefaultAmplitudeName)
                amplitude = ", Amplitude=" + _displacementRotation.AmplitudeName;
            //
            string loadCase = GetComplexLoadCase(_complexLoadType);
            //
            sb.AppendFormat("*Boundary{0}{1}{2}{3}{4}", fixedBc, amplitude, loadCase, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            //                                                                                                  
            //                                                                                                  
            //  Changing the boundary condition definition - change the computation of GetAllZeroDisplacements  
            //                                                                                                  
            //                                                                                                  
            StringBuilder sb = new StringBuilder();
            // *Boundary
            // 6975, 1, 1, 0        node id, start DOF, end DOF, value
            bool fixedBc = true;
            int[] directions = _displacementRotation.GetFixedDirections();
            if (directions.Length == 0)
            {
                fixedBc = false;
                directions = _displacementRotation.GetConstrainedDirections();
            }
            //
            double ratio = GetComplexRatio(_complexLoadType, _displacementRotation.PhaseDeg.Value);
            //
            double[] values = _displacementRotation.GetConstrainValues();
            string[] stringValues = new string[values.Length];
            for (int i = 0; i < values.Length; i++) stringValues[i] = (ratio * values[i]).ToCalculiX16String();
            // Node set
            if (_displacementRotation.RegionType == RegionTypeEnum.NodeSetName)
            {
                for (int i = 0; i < directions.Length; i++)
                {
                    sb.AppendFormat("{0}, {1}, {2}", _displacementRotation.RegionName, directions[i], directions[i]);
                    if (!fixedBc) sb.AppendFormat(", {0}", stringValues[i]);
                    sb.AppendLine();
                }
            }
            // Surface
            else if (_displacementRotation.RegionType == RegionTypeEnum.SurfaceName)
            {
                if (_nodeSetNameOfSurface == null) throw new ArgumentException();
                for (int i = 0; i < directions.Length; i++)
                {
                    sb.AppendFormat("{0}, {1}, {2}", _nodeSetNameOfSurface, directions[i], directions[i]);
                    if (!fixedBc) sb.AppendFormat(", {0}", stringValues[i]);
                    sb.AppendLine();
                }
            }
            // Reference point
            else if (_displacementRotation.RegionType == RegionTypeEnum.ReferencePointName)
            {
                int[] rpNodeIds = _referencePointsNodeIds[_displacementRotation.RegionName];
                for (int i = 0; i < directions.Length; i++)
                {
                    // Translational directions - first node id:        6975, 1, 1, 0
                    if (directions[i] <= _maxNumberNodeDOFs)
                        sb.AppendFormat("{0}, {1}, {2}", rpNodeIds[0], directions[i], directions[i]);
                    // Rotational directions - second node id:          6976, 2, 2, 0
                    else sb.AppendFormat("{0}, {1}, {2}", rpNodeIds[1], directions[i] - 3, directions[i] - 3);
                    if (!fixedBc) sb.AppendFormat(", {0}", stringValues[i]);
                    sb.AppendLine();
                }
            }
            else throw new NotSupportedException();
            //
            return sb.ToString();
        }

    }
}
