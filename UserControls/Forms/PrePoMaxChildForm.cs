using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        //
        private void ShowMissingBox(string missingValue, string newValue)
        {
            CaeGlobals.MessageBoxes.ShowError("The property value '" + missingValue + "' no longer exists." + Environment.NewLine + 
                                              "The value was changed to '" + newValue + "'.");
        }        
    }
}
