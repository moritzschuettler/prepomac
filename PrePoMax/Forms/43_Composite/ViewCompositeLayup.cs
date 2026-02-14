using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewCompositeLayup
    {
        // Variables
        private CompositeLayup _compositeLayup;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the composite layup.")]
        public string Name { get { return _compositeLayup.Name; } set { _compositeLayup.Name = value; } }

        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Element type")]
        [DescriptionAttribute("Type of element for the composite layup.")]
        public CompositeElementType ElementType
        {
            get { return _compositeLayup.ElementType; }
            set { _compositeLayup.ElementType = value; }
        }

        [CategoryAttribute("Layup Orientation")]
        [OrderedDisplayName(2, 10, "Definition")]
        [DescriptionAttribute("Orientation definition type.")]
        public OrientationDefinitionType OrientationDefinition
        {
            get { return _compositeLayup.OrientationDefinition; }
            set { _compositeLayup.OrientationDefinition = value; }
        }

        [CategoryAttribute("Layup Orientation")]
        [OrderedDisplayName(3, 10, "Normal direction")]
        [DescriptionAttribute("Normal direction for the layup.")]
        public NormalDirectionType NormalDirection
        {
            get { return _compositeLayup.NormalDirection; }
            set { _compositeLayup.NormalDirection = value; }
        }

        [CategoryAttribute("Plies")]
        [OrderedDisplayName(4, 10, "Ply count")]
        [DescriptionAttribute("Number of plies in the composite layup.")]
        [ReadOnly(true)]
        public int PlyCount
        {
            get { return _compositeLayup.Plies?.Count ?? 0; }
        }


        // Constructors
        public ViewCompositeLayup(CompositeLayup compositeLayup)
        {
            _compositeLayup = compositeLayup;
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods
        public CompositeLayup GetBase()
        {
            return _compositeLayup;
        }

        public void Reset()
        {
            // Reset to default values if needed
        }
    }
}
