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
    internal class CalNodeFile : CalculixKeyword
    {
        // Variables                                                                                                                
        protected NodalFieldOutput _nodalFieldOutput;
        protected int _outputFrequency;
        protected bool _binary;


        // Properties
        public bool Binary { get { return _binary; } set { _binary = value; } }


        // Constructor
        public CalNodeFile(CalNodeFile calNodeFile)
            : this(calNodeFile._nodalFieldOutput, calNodeFile._outputFrequency)
        {
            _binary = calNodeFile._binary;
        }
        public CalNodeFile(NodalFieldOutput nodalFieldOutput, int outputFrequency)
        {
            _nodalFieldOutput = nodalFieldOutput;
            _outputFrequency = outputFrequency;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string lastIterations = _nodalFieldOutput.LastIterations ? ", Last iterations" : "";
            string contactElements = _nodalFieldOutput.ContactElements ? ", Contact elements" : "";
            string global = !_nodalFieldOutput.Global ? ", Global=No" : "";
            //
            string keyword = _binary ? "*Node output" : "*Node file";
            //
            return string.Format("{0}{1}{2}{3}{4}", keyword, lastIterations, contactElements, global, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _nodalFieldOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}
