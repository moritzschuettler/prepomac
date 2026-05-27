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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeMesh;
using System.Reflection;
using CaeGlobals;
using DynamicTypeDescriptor;
using UserControls;

namespace PrePoMax.Forms
{
    public class FrmViewObjectProperties : FrmProperties
    {
        // Variables                                                                                                                
        private object _object;
        private double _labelRatio = 2.0; // larger value means wider second column


        // Properties                                                                                                               
        public Object Object
        {
            get { return _object; }
            set { _object = value; } 
        }


        // Constructors                                                                                                             
        public FrmViewObjectProperties()
        {
            _object = null;
            propertyGrid.SetLabelColumnWidth(_labelRatio);
        }


        // Methods                                                                                                                  
        public void PrepareForm(string caption, Object objectToView)
        {
            btnOkAddNew.Visible = false;
            btnOK.Visible = false;
            //
            Text = caption;
            _object = objectToView;
            //
            propertyGrid.SelectedObject = _object;
        }
    }
}
