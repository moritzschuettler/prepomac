using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeResults;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultFieldOutputEquation : ViewResultFieldOutput
    {
        // Variables                                                                                                                
        private ResultFieldOutputEquation _fieldOutput;


        // Properties                                                                                                               
        public override string Name { get { return _fieldOutput.Name; } set { _fieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Equation")]
        [DescriptionAttribute("Equation for the field output evaluation. To include a field component into the equation " +
                              "use square brackets [] and refer to te component with its full name as " +
                              "Field_Name.Component_Name. All names are case sensitive.\r\n" +
                              "Example equation: =[DISP.ALL].")]
        [Id(2, 1)]
        public string Equation
        {
            get { return _fieldOutput.Equation; }
            set
            {
                if (!value.Trim().StartsWith("=")) value = "=" + value;
                _fieldOutput.Equation = value;
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "User defined unit")]
        [DescriptionAttribute("User defined unit for the history output equation value.")]
        [Id(3, 1)]
        public string Unit { get { return _fieldOutput.Unit; } set { _fieldOutput.Unit = value; } }


        // Constructors                                                                                                             
        public ViewResultFieldOutputEquation(ResultFieldOutputEquation fieldOutput)
        {
            // The order is important
            _fieldOutput = fieldOutput;
            //
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            return _fieldOutput;
        }
        private void UpdateVisibility()
        {
            
        }
    }



   
}
