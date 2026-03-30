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
    internal class CalElFile : CalculixKeyword
    {
        // Variables                                                                                                                
        protected ElementFieldOutput _elementFieldOutput;
        protected int _outputFrequency;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalElFile(CalElFile calElFile)
            : this(calElFile._elementFieldOutput, calElFile._outputFrequency)
        {
        }
        public CalElFile(ElementFieldOutput elementFieldOutput, int outputFrequency)
        {
            _elementFieldOutput = elementFieldOutput;
            _outputFrequency = outputFrequency;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            
            string lastIterations = _elementFieldOutput.LastIterations ? ", Last iterations" : "";
            string contactElements = _elementFieldOutput.ContactElements ? ", Contact elements" : "";
            string output = "";
            if (_elementFieldOutput.Output == ElementFieldOutputOutputEnum.TwoD) output += ", Output=2D";
            else if (_elementFieldOutput.Output == ElementFieldOutputOutputEnum.ThreeD) output += ", Output=3D";
            string global = !_elementFieldOutput.Global ? ", Global=No" : "";
            //
            return string.Format("*El file{0}{1}{2}{3}{4}", lastIterations, contactElements, output, global,
                                 Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _elementFieldOutput.GetVariablesString(), Environment.NewLine);
        }
    }
}
