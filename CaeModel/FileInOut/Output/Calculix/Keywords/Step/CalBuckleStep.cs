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


        // Constructor                                                                                                              
        public CalBuckleStep(BuckleStep step)
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
            return string.Format("*Buckle{0}{1}", solver, Environment.NewLine);
        }
        public override string GetDataString()
        {
            string accuracy = double.IsNaN(_step.Accuracy) ? "" : ", " + _step.Accuracy.ToCalculiX16String();
            string data = string.Format("{0}{1}{2}", _step.NumOfBucklingFactors, accuracy, Environment.NewLine);
            if (OutputNoAnalysis && !_step.RunAnalysis) data += "*No Analysis" + Environment.NewLine;
            return data;
        }
    }
}
