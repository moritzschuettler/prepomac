using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicTypeDescriptor;
using System.ComponentModel;
using CaeGlobals;

namespace PrePoMax.PropertyViews
{
    [Serializable]
    public class ViewResetStepControlParameter : ViewStepControlParameter
    {
        // Variables                                                                                                                
        private CaeModel.ResetStepControlParameter _parameter;


        // Properties                                                                                                               
        [Browsable(false)]
        public override string Name
        {
            get { return "Reset"; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 20, "Reset")]
        [DescriptionAttribute("This parameter resets the control parameters to their defaults.")]
        public string Reset { get { return "Yes"; } }
        //
        [Browsable(false)]
        public override CaeModel.StepControlParameter Base
        {
            get { return _parameter; }
        }


        // Constructors                                                                                                             
        public ViewResetStepControlParameter(CaeModel.ResetStepControlParameter parameter)
        {
            _parameter = parameter;
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
    }
}
