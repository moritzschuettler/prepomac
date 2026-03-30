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
using PrePoMax.Commands;

namespace PrePoMax.Forms
{
    public partial class FrmRegenerate : Form, IFormBase
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        public RegenerateTypeEnum RegenerateType
        {
            get
            {
                RegenerateTypeEnum regenerateType;
                if (rbAll.Checked) regenerateType = RegenerateTypeEnum.All;
                else regenerateType = RegenerateTypeEnum.PreProcess;
                return regenerateType;
            }
        }


        // Constructors                                                                                                             
        public FrmRegenerate()
        {
            InitializeComponent();
        }


        // Event handlers                                                                                                            
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult = DialogResult.OK;
                //
                Hide();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
     

        // Methods                                                                                                                  
        public bool PrepareForm(string stepName, string itemToEditName)
        {
            return true;
        }


    }
}
