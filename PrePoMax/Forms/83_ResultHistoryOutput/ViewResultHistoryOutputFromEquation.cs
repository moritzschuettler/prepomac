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
    public class ViewResultHistoryOutputFromEquation : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private ResultHistoryOutputFromEquation _historyOutput;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "User defined unit")]
        [DescriptionAttribute("User defined unit for the history output equation value.")]
        [Id(2, 1)]
        public string Unit { get { return _historyOutput.Unit; } set { _historyOutput.Unit = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Equation")]
        [DescriptionAttribute("Equation for the history output evaluation.")]
        [Id(3, 1)]
        public string Equation
        {
            get { return _historyOutput.Equation; }
            set
            {
                if (!value.Trim().StartsWith("=")) value = "=" + value;
                _historyOutput.Equation = value;
            }
        }


        // Constructors                                                                                                             
        public ViewResultHistoryOutputFromEquation(ResultHistoryOutputFromEquation historyOutput)
            : base(historyOutput)
        {
            // The order is important
            _historyOutput = historyOutput;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.None, nameof(SelectionHidden));
            //
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override ResultHistoryOutput GetBase()
        {
            return _historyOutput;
        }
        public void PopulateDropDownLists()
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.None, new string[] { "None" });
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            //
            DynamicCustomTypeDescriptor dctd = DynamicCustomTypeDescriptor;
            dctd.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
            dctd.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
        }
        private void UpdateVisibility()
        {
            
        }
    }



   
}
