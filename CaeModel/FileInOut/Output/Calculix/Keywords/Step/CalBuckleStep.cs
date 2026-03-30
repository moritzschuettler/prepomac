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
    internal class CalBuckleStep : CalStep
    {
        // Variables                                                                                                                
        private BuckleStep _step;


        // Properties                                                                                                               
        public double Accuracy { get { return _step.Accuracy; } set { _step.Accuracy = value; } }
        public bool OutputAccuracy;


        // Constructor                                                                                                              
        public CalBuckleStep(BuckleStep step)
        {
            _step = step;
            OutputAccuracy = true;
            OutputSolver = true;
            OutputNoAnalysis = true;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string solver = !OutputSolver || _step.SolverType == SolverTypeEnum.Default ?
                "" : ", Solver=" + _step.SolverType.GetDisplayedName();
            return string.Format("*Buckle{0}{1}", solver, Environment.NewLine);
        }
        public override string GetDataString()
        {
            string accuracy = OutputAccuracy ? ", " + _step.Accuracy.ToCalculiX16String() : "";
            string data = string.Format("{0}{1}{2}", _step.NumOfBucklingFactors, accuracy, Environment.NewLine);
            if (OutputNoAnalysis && !_step.RunAnalysis) data += "*No Analysis" + Environment.NewLine;
            return data;
        }
    }
}
