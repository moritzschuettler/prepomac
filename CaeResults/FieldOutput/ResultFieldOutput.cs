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
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{   
    [Serializable]
    public abstract class ResultFieldOutput : NamedClass
    {
        // Variables                                                                                                                
        private FieldResultFilter _filter1;
        private FieldResultFilter _filter2;


        // Properties                                                                                                               
        public FieldResultFilter Filter1 { get { return _filter1; } set { _filter1 = value; } }
        public FieldResultFilter Filter2 { get { return _filter2; } set { _filter2 = value; } }


        // Constructors                                                                                                             
        public ResultFieldOutput(string name)
            : base(name)
        {
            _filter1 = new FieldResultFilter();
            _filter2 = new FieldResultFilter();
        }


        // Methods                                                                                                                  
        public abstract string[] GetParentNames();     // for dependency check
        public abstract string[] GetComponentNames();
    }
}
