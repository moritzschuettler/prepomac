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

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalStepHeader : CalculixKeyword
    {
        // Variables                                                                                                                
        private Step _step;


        // Properties                                                                                                               
        public object GetBase { get { return _step; } }


        // Constructor                                                                                                              
        public CalStepHeader(Step step)
        {
            _step = step;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string perturbation = _step.Perturbation ? ", Perturbation" : "";
            string nlGeom = _step.Nlgeom ? ", Nlgeom" : "";
            string inc = "";
            if (_step.IncrementationType != IncrementationTypeEnum.Default) inc = ", Inc=" + _step.MaxIncrements;
            //
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Step{0}{1}{2}", perturbation, nlGeom, inc).AppendLine();
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
