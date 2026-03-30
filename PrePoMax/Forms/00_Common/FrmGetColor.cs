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

namespace PrePoMax.Forms
{
    public partial class FrmGetColor : Form
    {
        // Variables                                                                                                                
        private ViewColor _viewColor;
        private double _labelRatio = 2.0; // larger value means wider second column
        


        // Properties                                                                                                               
        public Color Color 
        {
            get { return _viewColor.Color; }
            set { _viewColor.Color = value; } 
        }


        // Constructors                                                                                                             
        public FrmGetColor()
        {
            InitializeComponent();
            //
            _viewColor = new ViewColor(Color.Beige);
            //
            propertyGrid.SetLabelColumnWidth(_labelRatio);
        }


        // Event handlers                                                                                                            
        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            propertyGrid.Refresh();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
     

        // Methods                                                                                                                  
        public void PrepareForm(string title, Color color)
        {
            Text = title;
            //
            _viewColor.Color = color;
            //
            propertyGrid.SelectedObject = _viewColor;
        }
    }
}
