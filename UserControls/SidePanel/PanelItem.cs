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
using CaeGlobals;

namespace UserControls
{
    [PmxType(6202)]
    public class PanelItem
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected string _text;
        [PmxKey(1)] protected int _indentLevel;


        // Properties                                                                                                               
        public string Text { get { return _text; } set { _text = value; } }
        public int IndentLevel
        {
            get { return _indentLevel; }
            set
            {
                _indentLevel = value;
                if (_indentLevel < 0) _indentLevel = 0;
            }
        }


        // Constructors                                                                                                             
        public PanelItem()
        {
            _text = "";
            _indentLevel = 0;
        }


        // Methods                                                                                                                  
    }
}
