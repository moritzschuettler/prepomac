using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicTypeDescriptor;
using System.ComponentModel;
using CaeGlobals;
using System.Windows.Forms;

namespace PrePoMax.PropertyViews
{
    [Serializable]
    public class ViewLineSearchStepControlParameter : ViewStepControlParameter
    {
        // Variables
        private CaeModel.LineSearchStepControlParameter _parameter;


        // Properties
        [Browsable(false)]
        public override string Name
        {
            get { return "Line Search"; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 20, "slsmax")]
        [DescriptionAttribute("Maximum value of the line search parameter (default: 1.01).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(1, 1)]
        public double SlsMax { get { return _parameter.SlsMax; } set { _parameter.SlsMax = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 20, "slsmin")]
        [DescriptionAttribute("Minimum value of the line search parameter (default: 0.25).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(2, 1)]
        public double SlsMin { get { return _parameter.SlsMin; } set { _parameter.SlsMin = value; } }
        //
        [Browsable(false)]
        public override CaeModel.StepControlParameter Base
        {
            get { return _parameter; }
        }


        // Constructors
        public ViewLineSearchStepControlParameter(CaeModel.LineSearchStepControlParameter parameter)
        {
            _parameter = parameter;
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods
    }
}
