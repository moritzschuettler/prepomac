using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeResults;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultHistoryOutputFromElementSize : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private ResultHistoryOutputFromElementSize _historyOutput;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Size type")]
        [DescriptionAttribute("Select the element size type.")]
        [Id(2, 1)]
        public ElementSizeTypeEnum ElementSizeType
        {
            get { return _historyOutput.ElementSizeType; }
            set { _historyOutput.ElementSizeType = value; }
        }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Element set")]
        [DescriptionAttribute("Select the element set for the creation of the history output.")]
        [Id(3, 2)]
        public string ElementSetName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }
        //
        [CategoryAttribute("Mesh deformation")]
        [OrderedDisplayName(0, 10, "Variable")]
        [DescriptionAttribute("Select the deformation variable to be applied in the volume calculation.")]
        [Id(1, 3)]
        public string DeformationVariableName
        {
            get { return _historyOutput.DeformationVariableName; }
            set { _historyOutput.DeformationVariableName = value; }
        }


        // Constructors                                                                                                             
        public ViewResultHistoryOutputFromElementSize(ResultHistoryOutputFromElementSize historyOutput)
            : base(historyOutput)
        {
            // The order is important
            _historyOutput = historyOutput;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ElementSetName, nameof(ElementSetName));
            //
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override ResultHistoryOutput GetBase()
        {
            return _historyOutput;
        }
        public void PopulateDropDownLists(string[] elementSetNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.ElementSetName, elementSetNames);
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            //
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DeformationVariableName), 
                                                         FeResults.GetPossibleDeformationFieldOutputNamesMap().Keys.ToArray());
        }
        private void UpdateVisibility()
        {
            
        }
    }
}
