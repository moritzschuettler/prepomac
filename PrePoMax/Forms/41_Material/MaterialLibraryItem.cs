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


namespace PrePoMax.Forms
{
    [Serializable]
    [PmxType(10368)]
    public class MaterialLibraryItem : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] private bool _expanded;
        [PmxKey(1)] private List<MaterialLibraryItem> _items;
        [PmxKey(2)] private CaeModel.Material _tag;


        // Properties                                                                                                               
        public bool Expanded { get { return _expanded; } set { _expanded = value; } }
        public List<MaterialLibraryItem> Items { get { return _items; } set { _items = value; } }
        public CaeModel.Material Tag { get { return _tag; } set { _tag = value; } }


        // Constructors                                                                                                             
        public MaterialLibraryItem()
        {
            _expanded = false;
            _items = new List<MaterialLibraryItem>();
            _tag = null;
        }
        public MaterialLibraryItem(string name)
            : base(name)
        {
            _expanded = false;
            _items = new List<MaterialLibraryItem>();
            _tag = null;
        }


        // Methods                                                                                                                  
    }
}
