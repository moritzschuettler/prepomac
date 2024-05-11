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
    public class ViewContactStepControlParameter : ViewStepControlParameter
    {
        // Variables                                                                                                                
        private CaeModel.ContactStepControlParameter _parameter;


        // Properties                                                                                                               
        [Browsable(false)]
        public override string Name
        {
            get { return "Contact"; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 20, "delcon")]
        [DescriptionAttribute("The maximum relative difference in number of contact elements to allow for convergence " +
                              "(default: 0.001).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(1, 1)] 
        public double Delcon { get { return _parameter.Delcon; } set { _parameter.Delcon = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 20, "alea")]
        [DescriptionAttribute("The fraction of contact elements which is removed in an aleatoric way before repeating " +
                              "an increment in case of a local minimum in the solution (default: 0.1).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(2, 1)] 
        public double Alea { get { return _parameter.Alea; } set { _parameter.Alea = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 20, "kscalemax")]
        [DescriptionAttribute("The integer factor by which the normal spring stiffness (in case of linear " +
                              "pressure-overclosure) and stick slope are reduced in case of divergence or too " +
                              "slow convergence (default: 100).")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(3, 1)]
        public int Kscalemax { get { return _parameter.Kscalemax; } set { _parameter.Kscalemax = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 20, "itf2f")]
        [DescriptionAttribute("The maximum number of iterations per increment (default: 60).")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(4, 1)] 
        public int Itf2f { get { return _parameter.Itf2f; } set { _parameter.Itf2f = value; } }
        //                                                                                                                          
        [Browsable(false)]
        public override CaeModel.StepControlParameter Base
        {
            get { return _parameter; }
        }


        // Constructors                                                                                                             
        public ViewContactStepControlParameter(CaeModel.ContactStepControlParameter parameter)
        {
            _parameter = parameter;
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
    }
}
