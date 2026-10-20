using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using CaeModel;


namespace PrePoMax.Forms
{
    class BladeAeroTableUIEditor : UITypeEditor
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
                ViewBladeAeroLoad viewLoad = context?.Instance as ViewBladeAeroLoad;
                ViewBladeAeroPressureLoad viewPressureLoad = context?.Instance as ViewBladeAeroPressureLoad;
                bool invertForces = viewLoad != null ? viewLoad.InvertForces : false;
                BladeAeroDistanceTypeEnum distanceType = BladeAeroDistanceTypeEnum.Absolute;
                if (viewLoad != null) distanceType = ((BladeAeroLoad)viewLoad.GetBase()).DistanceType;
                else if (viewPressureLoad != null)
                    distanceType = ((BladeAeroPressureLoad)viewPressureLoad.GetBase()).DistanceType;
                //
                using (var frm = new FrmBladeAeroTable((List<BladeAeroTableRow>)value, invertForces, distanceType))
                {
                    if (svc.ShowDialog(frm) == DialogResult.OK)
                    {
                        value = frm.TableRows;
                        if (viewLoad != null) viewLoad.InvertForces = frm.InvertForces;
                    }
                }
            }
            return value;
        }
    }
}
