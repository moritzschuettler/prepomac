using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    public class FeGroup : NamedClass
    {
        // Variables                                                                                                                
        protected int[] _labels;
        //
        public IMultiRegion ParentMultiRegion; // temporary storage 


        // Properties                                                                                                               
        [Browsable(false)]
        public int[] Labels { get { return _labels; } set { _labels = value; } }
        //
        [CategoryAttribute("Data"),
        DisplayName("Count"),
        DescriptionAttribute("Number of items.")]
        public int Count { get { return Labels.Length; } }


        // Constructors                                                                                                             
        public FeGroup(string name, int[] labels) 
            : this(name, null, labels)
        {
        }
        public FeGroup(string name, HashSet<char> additionalCharacters, int[] labels)
            : base(name, additionalCharacters)
        {
            _labels = labels;
        }
        public FeGroup(FeGroup group)
            :base(group) // NamedClass
        {
            _labels = group.Labels.ToArray();
        }


        // Methods                                                                                                                  
    }
}
