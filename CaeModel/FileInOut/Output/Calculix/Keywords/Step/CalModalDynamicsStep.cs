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
    internal class CalModalDynamicsStep : CalStep
    {
        // Variables                                                                                                                
        private ModalDynamicsStep _step;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalModalDynamicsStep(ModalDynamicsStep step)
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
            string steadyState = _step.SteadyState ? ", Steady state" : "";
            return string.Format("*Modal dynamics{0}{1}{2}", solver, steadyState, Environment.NewLine);
        }
        public override string GetDataString()
        {
            string data;
            if (_step.SteadyState)
            {
                data = string.Format("{0}, {1}{2}", _step.InitialTimeIncrement.ToCalculiX16String(),
                                                    _step.RelativeError.ToCalculiX16String(),
                                                    Environment.NewLine);
            }
            else
            {
                data = string.Format("{0}, {1}{2}", _step.InitialTimeIncrement.ToCalculiX16String(),
                                                    _step.TimePeriod.ToCalculiX16String(),
                                                    Environment.NewLine);
            }
            
            //
            if (OutputNoAnalysis && !_step.RunAnalysis) data += "*No Analysis" + Environment.NewLine;
            return data;
        }
    }
}
