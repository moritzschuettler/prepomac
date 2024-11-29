using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using System.Drawing.Design;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewLoad: ViewMultiRegion
    {
        // Variables                                                                                                                
        private string _selectionHidden;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the load.")]
        [Id(1, 1)]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the load.")]
        [Id(1, 2)]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        [Id(2, 2)]
        public string SelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }
        //
        [CategoryAttribute("Time/Frequency")]
        [OrderedDisplayName(0, 10, "Amplitude")]
        [DescriptionAttribute("Select the amplitude for the load.")]
        [Id(1, 18)]
        public abstract string AmplitudeName { get; set; }
        //
        [CategoryAttribute("Orientation")]
        [DisplayName("Coordinate system")]
        [DescriptionAttribute("Select the coordinate system for the boundary condition.")]
        [Id(1, 19)]
        public abstract string CoordinateSystemName { get; set; }
        //
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select load color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 20)]
        public abstract System.Drawing.Color Color { get; set; }


        // Constructors                                                                                                             


        // Methods                                                                                                                  
        public abstract CaeModel.Load GetBase();
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            // Hide SelectionHidden
            if (base.RegionType == RegionTypeEnum.Selection.ToFriendlyString())
            {
                DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
            }
        }
        public void PopulateAmplitudeNames(string[] amplitudeNames)
        {
            List<string> names = new List<string>() { CaeModel.Load.DefaultAmplitudeName };
            names.AddRange(amplitudeNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(AmplitudeName), names.ToArray(), false, 2);
        }
        public void PopulateCoordinateSystemNames(string[] coordinateSystemNames)
        {
            List<string> names = new List<string>() { CaeModel.BoundaryCondition.DefaultCoordinateSystemName };
            names.AddRange(coordinateSystemNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(CoordinateSystemName), names.ToArray(), false, 2);
        }
    }
}
