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
    public class ViewTimeIncrementationStepControlParameter : ViewStepControlParameter
    {
        // Variables                                                                                                                
        private CaeModel.TimeIncrementationStepControlParameter _parameter;


        // Properties                                                                                                               
        [Browsable(false)]
        public override string Name
        {
            get { return "Time Incrementation"; }
        }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(0, 20, "I0")]
        [DescriptionAttribute("Iteration after which a check is made whether the residuals increase in two consecutive " + 
                              "iterations (default: 4). If so, the increment is reattempted with Df times its size.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(1, 1)]
        public int I0 { get { return _parameter.I0; } set { _parameter.I0 = value; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(1, 20, "IR")]
        [DescriptionAttribute("Iteration after which a logarithmic convergence check is performed in each iteration " +
                              "(default: 8). If more than IC iterations are needed, the increment is reattempted with " +
                              "DC its size.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(2, 1)]
        public int IR { get { return _parameter.IR; } set { _parameter.IR = value; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(2, 20, "IP")]
        [DescriptionAttribute("Iteration after which the residual tolerance Rpα is used instead of Rnα (default: 9).")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(3, 1)] 
        public int IP { get { return _parameter.IP; } set { _parameter.IP = value; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(3, 20, "IC")]
        [DescriptionAttribute("Maximum number of iterations allowed (default: 16).")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(4, 1)] 
        public int IC { get { return _parameter.IC; } set { _parameter.IC = value; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(4, 20, "IL")]
        [DescriptionAttribute("Number of iterations after which the size of the subsequent increment will be reduced " +
                              "(default: 10).")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(5, 1)]
        public int IL { get { return _parameter.IL; } set { _parameter.IL = value; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(5, 20, "IG")]
        [DescriptionAttribute("Maximum number of iterations allowed in two consecutive increments for the size of the " +
                              "next increment to be increased (default: 4).")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(6, 1)] 
        public int IG { get { return _parameter.IG; } set { _parameter.IG = value; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(6, 20, "IS")]
        [DescriptionAttribute("Currently not used.")]
        [Id(7, 1)] 
        public string IS { get { return "Currently not used"; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(7, 20, "IA")]
        [DescriptionAttribute("Maximum number of cutbacks per increment (default: 5). A cutback is a reattempted " +
                              "increment.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(8, 1)] 
        public int IA { get { return _parameter.IA; } set { _parameter.IA = value; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(8, 20, "IJ")]
        [DescriptionAttribute("Currently not used.")]
        [Id(9, 1)] 
        public string IJ { get { return "Currently not used"; } }
        //
        [CategoryAttribute("Integer")]
        [OrderedDisplayName(9, 20, "IT")]
        [DescriptionAttribute("Currently not used.")]
        [Id(10, 1)] 
        public string IT { get { return "Currently not used"; } }
        //                                                                                                                          
        [CategoryAttribute("Double")]
        [OrderedDisplayName(0, 20, "Df")]
        [DescriptionAttribute("Cutback factor if the solution seems to diverge (default: 0.25).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(1, 2)] 
        public double Df { get { return _parameter.Df; } set { _parameter.Df = value; } }
        //
        [CategoryAttribute("Double")]
        [OrderedDisplayName(1, 20, "DC")]
        [DescriptionAttribute("Cutback factor if the logarithmic extrapolation predicts too many iterations " +
                              "(default: 0.5).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(2, 2)] 
        public double DC { get { return _parameter.DC; } set { _parameter.DC = value; } }
        //
        [CategoryAttribute("Double")]
        [OrderedDisplayName(2, 20, "DB")]
        [DescriptionAttribute("Cutback factor for the next increment if more than IL iterations were needed " +
                              "in the current increment (default: 0.75).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(3, 2)] 
        public double DB { get { return _parameter.DB; } set { _parameter.DB = value; } }
        //
        [CategoryAttribute("Double")]
        [OrderedDisplayName(3, 20, "DA")]
        [DescriptionAttribute("Cutback factor if the temperature change in two subsequent increments exceeds " +
                              " DELTMX (default: 0.85).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(4, 2)] 
        public double DA { get { return _parameter.DA; } set { _parameter.DA = value; } }
        //
        [CategoryAttribute("Double")]
        [OrderedDisplayName(4, 20, "DS")]
        [DescriptionAttribute("Currently not used.")]
        [Id(5, 2)] 
        public string DS { get { return "Currently not used"; } }
        //
        [CategoryAttribute("Double")]
        [OrderedDisplayName(5, 20, "DH")]
        [DescriptionAttribute("Currently not used.")]
        [Id(6, 2)] 
        public string DH { get { return "Currently not used"; } }
        //
        [CategoryAttribute("Double")]
        [OrderedDisplayName(6, 20, "DD")]
        [DescriptionAttribute("Factor by which the next increment will be increased if less than IG iterations " +
                              "are needed in two consecutive increments (default: 1.5).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(7, 2)] 
        public double DD { get { return _parameter.DD; } set { _parameter.DD = value; } }
        //
        [CategoryAttribute("Double")]
        [OrderedDisplayName(7, 20, "WG")]
        [DescriptionAttribute("Currently not used.")]
        [Id(8, 2)] 
        public string WG { get { return "Currently not used"; } }
        //                                                                                                                          
        [Browsable(false)]
        public override CaeModel.StepControlParameter Base
        {
            get { return _parameter; }
        }


        // Constructors                                                                                                             
        public ViewTimeIncrementationStepControlParameter(CaeModel.TimeIncrementationStepControlParameter parameter)
        {
            _parameter = parameter;
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
    }
}
