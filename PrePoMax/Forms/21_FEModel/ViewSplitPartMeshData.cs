using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;
using System.Security.Cryptography;

namespace PrePoMax
{
    [Serializable]
    public class ViewSplitPartMeshData : ViewMasterSlaveMultiRegion
    {
        // Variables                                                                                                                
        private string _selectionHidden;
        private SplitPartMeshData _splitPartMeshData;

        
        // BASE PART                                                                                                                
        [CategoryAttribute("Base Part Region")]
        [OrderedDisplayName(0, 10, "Base part region type")]
        [DescriptionAttribute("Select the base part region type for the creation of the split mesh part definition.")]
        [Id(1, 2)]
        public override string MasterRegionType { get { return base.MasterRegionType; } set { base.MasterRegionType = value; } }
        //
        [CategoryAttribute("Base Part Region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        [Id(2, 2)]
        public string BasePartSelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }
        //
        [CategoryAttribute("Base Part Region")]
        [OrderedDisplayName(2, 10, "Part")]
        [DescriptionAttribute("Select the part for the creation of the split mesh part definition.")]
        [Id(3, 2)]
        public string BasePartPartName
        {
            get { return _splitPartMeshData.BasePartRegionName; }
            set { _splitPartMeshData.BasePartRegionName = value; }
        }
        // SPLITTER SURFACE                                                                                                         
        [CategoryAttribute("Splitter Surface Region")]
        [OrderedDisplayName(3, 10, "Splitter surface region type")]
        [DescriptionAttribute("Select the splitter surface region type for the creation of the split mesh part definition.")]
        [Id(1, 3)]
        public override string SlaveRegionType { get { return base.SlaveRegionType; } set { base.SlaveRegionType = value; } }
        //
        [CategoryAttribute("Splitter Surface Region")]
        [OrderedDisplayName(4, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        [Id(2, 3)]
        public string SplitterSurfaceSelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }
        //
        [CategoryAttribute("Splitter Surface Region")]
        [OrderedDisplayName(5, 10, "Splitter surface")]
        [DescriptionAttribute("Select the splitter surface for the creation of the split mesh part definition.")]
        [Id(3, 3)]
        public string SplitterSurfaceSurfaceName
        {
            get { return _splitPartMeshData.SplitterSurfaceRegionName; }
            set { _splitPartMeshData.SplitterSurfaceRegionName = value; }
        }
        // Data                                                                                                                     
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Offset")]
        [DescriptionAttribute("Enter the value to offset the splitter surface.")]
        [Id(1, 4)]
        public double Offset { get { return _splitPartMeshData.Offset; } set { _splitPartMeshData.Offset = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Exact evaluation")]
        [DescriptionAttribute("Exact evaluation is recommended for larger offset values or higher precision " +
                              "but it is computationally much more expensive.")]
        [Id(2, 4)]
        public bool Exact { get { return _splitPartMeshData.Exact; } set { _splitPartMeshData.Exact = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(0, 10, "Max element size")]
        [Description("The value for the maximum element size.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 5)]
        public virtual double MaxH { get { return _splitPartMeshData.MaxH; } set { _splitPartMeshData.MaxH = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(1, 10, "Min element size")]
        [Description("The value for the minimum element size.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 5)]
        public virtual double MinH { get { return _splitPartMeshData.MinH; } set { _splitPartMeshData.MinH = value; } }
        // Maximal Hausdorff distance for the boundaries approximation.
        [Category("Mesh size")]
        [OrderedDisplayName(2, 10, "Hausdorff")]
        [Description("Maximal Hausdorff distance for the boundaries approximation. " +
                     "A value of 0.01 is a suitable value for an object of size 1 in each direction.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 5)]
        public double Hausdorff { get { return _splitPartMeshData.Hausdorff; } set { _splitPartMeshData.Hausdorff = value; } }
        


        // Constructors                                                                                                             
        public ViewSplitPartMeshData(SplitPartMeshData splitMeshPartData)
        {
            _splitPartMeshData = splitMeshPartData;           
            // Master
            Dictionary<RegionTypeEnum, string> basePartRegionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            basePartRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(BasePartSelectionHidden));
            basePartRegionTypePropertyNamePairs.Add(RegionTypeEnum.PartName, nameof(BasePartPartName));
            // Slave
            Dictionary<RegionTypeEnum, string> splitterSurfaceRegionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            splitterSurfaceRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SplitterSurfaceSelectionHidden));
            splitterSurfaceRegionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SplitterSurfaceSurfaceName));
            //
            SetBase(_splitPartMeshData, basePartRegionTypePropertyNamePairs, splitterSurfaceRegionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            ApplyYesNo();
        }


        // Methods                                                                                                                  
        public SplitPartMeshData GetBase()
        {
            return _splitPartMeshData;
        }
        protected void ApplyYesNo()
        {
            DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(Exact));
        }
        public void PopulateDropDownLists(string[] partNames, string[] surfaceNames)
        {
            Dictionary<RegionTypeEnum, string[]> basePartRegionTypeListItemsPairs =
                new Dictionary<RegionTypeEnum, string[]>();
            basePartRegionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            basePartRegionTypeListItemsPairs.Add(RegionTypeEnum.PartName, partNames);
            //
            Dictionary<RegionTypeEnum, string[]> splitterSurfaceRegionTypeListItemsPairs =
                new Dictionary<RegionTypeEnum, string[]>();
            splitterSurfaceRegionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            splitterSurfaceRegionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            //
            PopulateDropDownLists(basePartRegionTypeListItemsPairs, splitterSurfaceRegionTypeListItemsPairs);
        }
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            // Hide SelectionHidden
            CustomPropertyDescriptor cpd;
            // Master
            if (base.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString())
            {
                cpd = DynamicCustomTypeDescriptor.GetProperty(nameof(BasePartSelectionHidden));
                cpd.SetIsBrowsable(false);
            }
            // Slave
            if (base.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString())
            {
                cpd = DynamicCustomTypeDescriptor.GetProperty(nameof(SplitterSurfaceSelectionHidden));
                cpd.SetIsBrowsable(false);
            }
        }
    }

}
