using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewCompositeLayup
    {
   // Variables          
        private CaeModel.CompositeLayup _compositeLayup;
        private DynamicCustomTypeDescriptor _dctd = null;


    // Properties     
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the composite layup.")]
        public string Name { get { return _compositeLayup.Name; } set { _compositeLayup.Name = value; } }

      [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Initial ply count")]
        [DescriptionAttribute("Initial number of plies in the composite layup.")]
        public int InitialPlyCount 
        { 
    get { return _compositeLayup.InitialPlyCount; } 
  set { _compositeLayup.InitialPlyCount = value; } 
        }

        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Element type")]
        [DescriptionAttribute("Type of element for the composite layup.")]
      public CaeModel.CompositeElementType ElementType 
        { 
  get { return _compositeLayup.ElementType; } 
            set { _compositeLayup.ElementType = value; } 
        }


 // Constructors         
     public ViewCompositeLayup(CaeModel.CompositeLayup compositeLayup)
   {
            _compositeLayup = compositeLayup;
  _dctd = ProviderInstaller.Install(this);
        }


        // Methods              
        public CaeModel.CompositeLayup GetBase()
        {
      return _compositeLayup;
        }
        public void Reset()
        {
            // Reset to default values if needed
        }
    }
}
