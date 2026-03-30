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

namespace UserControls
{
    [Serializable]
    public class AdvisorItem
    {
        // Variables                                                                                                                
        protected string _text;
        protected int _indentLevel;


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
        public AdvisorItem()
        {
            _text = "";
            _indentLevel = 0;
        }


        // Methods                                                                                                                  
    }
}
