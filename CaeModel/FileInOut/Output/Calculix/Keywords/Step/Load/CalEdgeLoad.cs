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
    [PmxType(3288)]
    internal class CalEdgeLoad : CalLoad
    {
        // Variables
        [PmxKey(0)] private EdgeLoad _load;
        [PmxKey(1)] private ComplexLoadTypeEnum _complexLoadType;
        [PmxKey(2)] private int[] _nodeIds;
        [PmxKey(3)] private double[][] _forces;
        [PmxKey(4)] private int[] _elementIds;
        [PmxKey(5)] private int[] _edgeNumbers;
        [PmxKey(6)] private double[] _values;


        // Properties
        public bool ShellEdgeNormal { get { return _elementIds != null; } }


        // Constructor
        public CalEdgeLoad(FeModel model, EdgeLoad load, ComplexLoadTypeEnum complexLoadType)
        {
            _load = load;
            _complexLoadType = complexLoadType;
            if (load.Direction == EdgeLoadDirectionEnum.Normal && !load.TwoD)
                model.GetShellEdgeNormalLoadsFromEdgeLoad(load, out _elementIds, out _edgeNumbers, out _values);
            else
                model.GetNodalForcesFromEdgeLoad(load, out _nodeIds, out _forces);
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
            string keyword = ShellEdgeNormal ? "*Dload" : "*Cload";
            //
            sb.AppendFormat("{0}{1}{2}{3}{4}", keyword, amplitude, loadCase, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            if (ShellEdgeNormal)
            {
                for (int i = 0; i < _elementIds.Length; i++)
                {
                    if (_values[i] == 0) continue;
                    sb.AppendFormat("{0}, EDNOR{1}, {2}", _elementIds[i], _edgeNumbers[i],
                                    (ratio * _values[i]).ToCalculiX16String());
                    sb.AppendLine();
                }
            }
            else
            {
                int numOfDirections = _load.TwoD ? 2 : 3;
                for (int i = 0; i < _nodeIds.Length; i++)
                {
                    for (int dir = 1; dir <= numOfDirections; dir++)
                    {
                        if (_forces[i][dir - 1] == 0) continue;
                        sb.AppendFormat("{0}, {1}, {2}", _nodeIds[i], dir,
                                        (ratio * _forces[i][dir - 1]).ToCalculiX16String());
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }
    }
}
