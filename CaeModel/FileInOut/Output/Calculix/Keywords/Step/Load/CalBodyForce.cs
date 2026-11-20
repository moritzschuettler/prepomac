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
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [PmxType(3286)]
    internal class CalBodyForce : CalLoad
    {
        // Variables
        [PmxKey(0)] private BodyForce _load;
        [PmxKey(1)] private ComplexLoadTypeEnum _complexLoadType;
        [PmxKey(2)] private double[] _uniformForce;
        [PmxKey(3)] private int[] _elementIds;
        [PmxKey(4)] private double[][] _elementForces;


        // Properties
        public bool CanHideData { get { return _elementIds != null; } }


        // Constructor
        public CalBodyForce(FeModel model, BodyForce load, ComplexLoadTypeEnum complexLoadType)
        {
            _load = load;
            _complexLoadType = complexLoadType;
            //
            if (load.IsUniform(model))
            {
                _uniformForce = load.GetUniformForcePerVolume(model);
                _elementIds = null;
                _elementForces = null;
            }
            else
            {
                _uniformForce = null;
                load.GetElementCentroidsAndForcesPerVolume(model, out _elementIds, out _, out _elementForces);
            }
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
            sb.AppendFormat("*Dload{0}{1}{2}{3}", amplitude, loadCase, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            int numOfDirections = _load.TwoD ? 2 : 3;
            //
            if (_uniformForce != null)
            {
                bool written = false;
                for (int dir = 1; dir <= numOfDirections; dir++)
                {
                    if (_uniformForce[dir - 1] == 0) continue;
                    AppendLine(sb, _load.RegionName, dir, ratio * _uniformForce[dir - 1]);
                    written = true;
                }
                // A zero load still has to be written to replace the load of a previous step
                if (!written) AppendLine(sb, _load.RegionName, 1, 0);
            }
            else
            {
                for (int i = 0; i < _elementIds.Length; i++)
                {
                    for (int dir = 1; dir <= numOfDirections; dir++)
                    {
                        if (_elementForces[i][dir - 1] == 0) continue;
                        AppendLine(sb, _elementIds[i].ToString(), dir, ratio * _elementForces[i][dir - 1]);
                    }
                }
            }
            //
            return sb.ToString();
        }
        private static void AppendLine(StringBuilder sb, string region, int direction, double value)
        {
            sb.AppendFormat("{0}, B{1}, {2}{3}", region, direction == 1 ? "X" : direction == 2 ? "Y" : "Z",
                            value.ToCalculiX16String(), Environment.NewLine);
        }
    }
}
