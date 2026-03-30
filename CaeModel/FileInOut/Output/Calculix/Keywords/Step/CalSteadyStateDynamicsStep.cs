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
    internal class CalSteadyStateDynamicsStep : CalStep
    {
        // Variables                                                                                                                
        private SteadyStateDynamicsStep _step;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalSteadyStateDynamicsStep(SteadyStateDynamicsStep step)
        {
            _step = step;
            OutputSolver = true;
            OutputNoAnalysis = true;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string solver = !OutputSolver || _step.SolverType == SolverTypeEnum.Default ?
                "" : ", Solver=" + _step.SolverType.GetDisplayedName();
            string harmonic = _step.Harmonic ? "" : ", Harmonic=No";
            return string.Format("*Steady state dynamics{0}{1}{2}", harmonic, solver, Environment.NewLine);
        }
        public override string GetDataString()
        {
            string data = string.Format("{0}, {1}, {2}, {3}", _step.LowerFrequency.ToCalculiX16String(),
                                                              _step.UpperFrequency.ToCalculiX16String(),
                                                              _step.NumDataPoints,
                                                              _step.Bias.ToCalculiX16String());
            if (!_step.Harmonic) data += string.Format(", {0}, {1}, {2}", _step.NumFourierTerms,
                                                                        _step.TimeLower.ToCalculiX16String(),
                                                                        _step.TimeUpper.ToCalculiX16String());
            data += Environment.NewLine;
            if (OutputNoAnalysis && !_step.RunAnalysis) data += "*No Analysis" + Environment.NewLine;
            return data;
        }
    }
}
