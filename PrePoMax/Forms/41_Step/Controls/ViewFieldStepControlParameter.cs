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
    public class ViewFieldStepControlParameter : ViewStepControlParameter
    {
        // Variables                                                                                                                
        private CaeModel.FieldStepControlParameter _parameter;


        // Properties                                                                                                               
        [Browsable(false)]
        public override string Name
        {
            get { return "Field"; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 20, "Rnα")]
        [DescriptionAttribute("Convergence criterion for the ratio of the largest residual to the average force " +
                              "(default: 0.005). The average force is defined as the average over all increments " +
                              "in the present step of the instantaneous force. The instantaneous force in an increment " +
                              "is defined as the mean of the absolute value of the nodal force components within " +
                              "all elements.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(1, 1)] 
        public double Rna { get { return _parameter.Rna; } set { _parameter.Rna = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 20, "Cnα")]
        [DescriptionAttribute("Convergence criterion for the ratio of the largest solution correction to the largest " +
                              "incremental solution value (default: 0.01).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(2, 1)] 
        public double Cna { get { return _parameter.Cna; } set { _parameter.Cna = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 20, "q0α")]
        [DescriptionAttribute("Initial value at the start of a new step of the time average force (default: the time " +
                              "average force from the previous steps or 0.01 for the first step).")]
        [TypeConverter(typeof(StringDoubleDefaultConverter))]
        [Id(3, 1)] 
        public double Q0a { get { return _parameter.Q0a; } set { _parameter.Q0a = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 20, "quα")]
        [DescriptionAttribute("User-defined average force. If defined, the calculation of the average force is replaced " +
                              " by this value.")]
        [TypeConverter(typeof(StringDoubleDefaultConverter))]
        [Id(4, 1)] 
        public double Qua { get { return _parameter.Qua; } set { _parameter.Qua = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(4, 20, "Rpα")]
        [DescriptionAttribute("Alternative residual convergence criterion to be used after IP iterations instead of " +
                              "Rnα (default: 0.02).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(5, 1)]
        public double Rpa { get { return _parameter.Rpa; } set { _parameter.Rpa = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(5, 20, "εα")]
        [DescriptionAttribute("Criterion for zero flux relative to qα (default: 10⁻⁵).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(6, 1)]
        public double Ea { get { return _parameter.Ea; } set { _parameter.Ea = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(6, 20, "Cεα")]
        [DescriptionAttribute("Convergence criterion for the ratio of the largest solution correction to the largest " +
                              "incremental solution value in case of zero flux (default: 10⁻³).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(7, 1)] 
        public double Cea { get { return _parameter.Cea; } set { _parameter.Cea = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(7, 20, "Rlα")]
        [DescriptionAttribute("Convergence criterion for the ratio of the largest residual to the average force " +
                              "for convergence in a single iteration (default: 10⁻⁸).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(8, 1)]
        public double Rla { get { return _parameter.Rla; } set { _parameter.Rla = value; } }
        //                                                                                                                          
        [Browsable(false)]
        public override CaeModel.StepControlParameter Base
        {
            get { return _parameter; }
        }


        // Constructors                                                                                                             
        public ViewFieldStepControlParameter(CaeModel.FieldStepControlParameter parameter)
        {
            _parameter = parameter;
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
    }
}
