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
    [PmxType(3128)]
    internal class CalSteadyStateDynamicsStep : CalStep
    {
        // Variables                                                                                                                
        [PmxKey(0)] private SteadyStateDynamicsStep _step;
        [PmxKey(1)] private bool _cyclicSymmetry;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalSteadyStateDynamicsStep(SteadyStateDynamicsStep step, bool cyclicSymmetry)
        {
            _step = step;
            _cyclicSymmetry = cyclicSymmetry;
            OutputSolver = true;
            OutputNoAnalysis = true;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string solver = !OutputSolver || _step.SolverType == SolverTypeEnum.Default ?
                "" : ", Solver=" + _step.SolverType.GetDisplayedName();
            string harmonic = _step.Harmonic ? "" : ", Harmonic=No";
            string cyclicSymmetry = _cyclicSymmetry ? ", Cyclic symmetry" : "";
            return string.Format("*Steady state dynamics{0}{1}{2}{3}", harmonic, solver, cyclicSymmetry,
                                 Environment.NewLine);
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
