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
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using CaeJob;


namespace PrePoMax.Forms
{
    class DampingRatiosAndValuesUIEditor : UITypeEditor
    {
        // Overrides                                                                                                                
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (svc != null)
            {
                using (var frm = new FrmGetDampingRatios((List<DampingRatioAndRange>)value))
                {
                    if (svc.ShowDialog(frm) == DialogResult.OK)
                    {
                        value = frm.DampingRatiosAndRange;
                    }
                }
            }
            return value;
        }
    }
}
