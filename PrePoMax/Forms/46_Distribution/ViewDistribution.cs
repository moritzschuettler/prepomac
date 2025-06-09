using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewDistribution
    {
        // Variables                                                                                                                
        private DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the distribution.")]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Type")]
        [DescriptionAttribute("Select the distribution type.")]
        public abstract DistributionTypeEnum DistributionType { get; set; }
        //
        [Browsable(false)]
        public DynamicCustomTypeDescriptor DynamicCustomTypeDescriptor { get { return _dctd; } set { _dctd = value; } }
        //
        [Browsable(false)]
        public abstract Distribution GetBase();


        // Constructors                                                                                                             


        // Methods
    }
}
