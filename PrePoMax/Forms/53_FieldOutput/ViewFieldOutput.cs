using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewFieldOutput
    {
        // Variables                                                                                                                
        protected DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the field output.")]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Last iterations")]
        [DescriptionAttribute("Turning last iterations on is useful for debugging purposes in case of divergent solution.")]
        public abstract bool LastIterations { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Contact elements")]
        [DescriptionAttribute("Turning contact elements on stores the contact elements in a file with the .cel extension.")]
        public abstract bool ContactElements { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(10, 10, "Global")]
        [DescriptionAttribute("Parameter global controls whether the results are saved in a global or local coordinate system.")]
        public abstract bool Global { get; set; }
        //
        [Browsable(false)]
        public abstract FieldOutput Base { get; set; }


        // Constructors                                                                                                             


        // Methods
       
    }
}
