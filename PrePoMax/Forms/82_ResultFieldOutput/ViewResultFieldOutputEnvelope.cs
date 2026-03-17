using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeResults;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultFieldOutputEnvelope : ViewResultFieldOutput
    {
        // Variables                                                                                                                
        private Dictionary<string, string[]> _filedNameComponentNames;


        // Properties                                                                                                               
        private ResultFieldOutputEnvelope ResultFieldOutput
        {
            get { return (ResultFieldOutputEnvelope)_resultFieldOutput; }
        }
        public override string Name { get { return ResultFieldOutput.Name; } set { ResultFieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Field name")]
        [DescriptionAttribute("Filed name for the field output.")]
        [Id(2, 1)]
        public string FieldName
        {
            get { return ResultFieldOutput.FieldName; }
            set
            {
                if (ResultFieldOutput.FieldName != value)
                {
                    ResultFieldOutput.FieldName = value;
                    UpdateComponents();
                }
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Component name")]
        [DescriptionAttribute("Component name for the field output.")]
        [Id(3, 1)]
        public string ComponentName
        {
            get { return ResultFieldOutput.ComponentName; }
            set { ResultFieldOutput.ComponentName = value; }
        }


        // Constructors                                                                                                             
        public ViewResultFieldOutputEnvelope(ResultFieldOutputEnvelope resultFieldOutput)
            : base(resultFieldOutput)
        {
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            return _resultFieldOutput;
        }
        public void PopulateDropDownLists(Dictionary<string, string[]> filedNameComponentNames)
        {
            _filedNameComponentNames = filedNameComponentNames;
            _dctd.PopulateProperty(nameof(FieldName), _filedNameComponentNames.Keys.ToArray());
            UpdateComponents();
        }
        private void UpdateComponents()
        {
            string[] componentNames;
            if (_filedNameComponentNames != null && _filedNameComponentNames.TryGetValue(FieldName, out componentNames) &&
                componentNames != null && componentNames.Length > 0)
            {
                _dctd.PopulateProperty(nameof(ComponentName), componentNames);
                if (!componentNames.Contains(ComponentName)) ComponentName = componentNames[0];
            }
        }
        private void UpdateVisibility()
        {
           
        }
    }



   
}
