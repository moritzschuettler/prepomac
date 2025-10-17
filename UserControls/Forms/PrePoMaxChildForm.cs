using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public class PrePoMaxChildForm : ChildMouseWheelManagedForm
    {
        // Variables                                                                                                                
        protected bool _propertyItemChanged;


        // Methods                                                                                                                  
        protected void CheckMissingValueRef(ref string[] allValues, string currentValue, Action<string> valueSetter)
        {
            if (allValues.Length == 0 || (allValues.Length > 0 && !allValues.Contains(currentValue)))
            {
                string value = "Missing";
                if (allValues.Length > 0) value = allValues[0];

                if (currentValue != null && currentValue != value)
                {
                    ShowMissingBox(currentValue, value);
                    valueSetter(value);
                    _propertyItemChanged = true;
                }

                if (allValues.Length == 0) allValues = new string[] { value };
            }
        }
        protected void CheckMissingValue(string[] allValues, string currentValue, Action<string> valueSetter)
        {
            if (allValues.Length == 0 || (allValues.Length > 0 && !allValues.Contains(currentValue)))
            {
                string value = "Missing";
                if (allValues.Length > 0) value = allValues[0];

                if (currentValue != null && currentValue != value)
                {
                    ShowMissingBox(currentValue, value);
                    valueSetter(value);
                    Array.Resize(ref allValues, 1);
                    allValues[0] = value;
                    _propertyItemChanged = true;
                }
            }
        }
        // There is a known issue when a DataGridView still holds an uncommitted or invalid value in its edit control when the form
        // closes. The problem is that even if you close the form, the grid’s current cell value hasn’t yet been committed or
        // reverted, so when you reopen it and try to clear the DataSource, it tries to push that invalid value back into the
        // underlying data model — causing the same converter error.
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (!Visible) CancelAllEdits(this);
            //
            base.OnVisibleChanged(e);
        }
        private void CancelAllEdits(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // Check for DataGridView
                if (ctrl is DataGridView dgv)
                {
                    if (dgv.IsCurrentCellInEditMode) dgv.CancelEdit();
                    // Cancel any binding edits
                    if (dgv.DataSource != null)
                    {
                        var bindingManager = this.BindingContext[dgv.DataSource, dgv.DataMember];
                        bindingManager?.CancelCurrentEdit();
                    }
                }
                // Recurse for child containers
                if (ctrl.HasChildren) CancelAllEdits(ctrl);
            }
        }
        //
        private void ShowMissingBox(string missingValue, string newValue)
        {
            CaeGlobals.MessageBoxes.ShowError("The property value '" + missingValue + "' no longer exists." + Environment.NewLine + 
                                              "The value was changed to '" + newValue + "'.");
        }
    }
}
