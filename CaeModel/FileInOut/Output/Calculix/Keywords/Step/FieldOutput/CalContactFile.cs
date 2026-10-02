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
    internal class CalContactFile : CalculixKeyword
    {
        // Variables                                                                                                                
        protected ContactFieldOutput _contactFieldOutput;
        protected bool _binary;


        // Properties
        public bool Binary { get { return _binary; } set { _binary = value; } }
        // CalculiX 2.22 writes wrong contact energies into a binary results file and PCON is not verified there
        public bool BinarySupported
        {
            get
            {
                return !_contactFieldOutput.Variables.HasFlag(ContactFieldVariable.CELS) &&
                       !_contactFieldOutput.Variables.HasFlag(ContactFieldVariable.PCON);
            }
        }


        // Constructor
        public CalContactFile(CalContactFile calContactFile)
            : this(calContactFile._contactFieldOutput)
        {
            _binary = calContactFile._binary;
        }
        public CalContactFile(ContactFieldOutput contactFieldOutput)
        {
            _contactFieldOutput = contactFieldOutput;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string lastIterations = _contactFieldOutput.LastIterations ? ", Last iterations" : "";
            string contactElements = _contactFieldOutput.ContactElements ? ", Contact elements" : "";
            //
            string keyword = _binary ? "*Contact output" : "*Contact file";
            //
            return string.Format("{0}{1}{2}{3}", keyword, lastIterations, contactElements, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _contactFieldOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}
