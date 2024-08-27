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
    [EnumResource("PrePoMax.Properties.Resources")]
    [Editor(typeof(StandardValueEditor), typeof(System.Drawing.Design.UITypeEditor))]
    [Flags]
    public enum ViewNodalHistoryVariable
    {
        // Must start at 1 for the UI to work
        [StandardValue("RF", Description = "Reaction forces.")]
        RF = 1,
        [StandardValue("U", Description = "Displacements.")]
        U = 2,
        [StandardValue("V", Description = "Velocities.")]
        V = 4,
        // Thermal
        [StandardValue("NT", Description = "Temperatures.")]
        NT = 8,
        [StandardValue("RFL", Description = "External concentrated heat sources.")]
        RFL = 16,
    }

    [Serializable]
    public class ViewNodalHistoryOutput : ViewHistoryOutput
    {
        // Variables                                                                                                                
        private NodalHistoryOutput _historyOutput;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [OrderedDisplayName(1, 10, "Variables to output")]
        [CategoryAttribute("Data")]
        [DescriptionAttribute("Nodal history variables")]
        public ViewNodalHistoryVariable Variables
        {
            get
            {
                return (ViewNodalHistoryVariable)_historyOutput.Variables;
            }
            set
            {
                _historyOutput.Variables = (NodalHistoryVariable)value;
            }
        }
        //
        [OrderedDisplayName(2, 10, "Totals")]
        [CategoryAttribute("Data")]
        [DescriptionAttribute("The parameter totals only applies to the external forces. Notice that the sum is always written " +
                              "in the global rectangular system, irrespective of the value of the GLOBAL parameter.")]
        public TotalsTypeEnum TotalsType { get { return _historyOutput.TotalsType; }set { _historyOutput.TotalsType = value; } }
        //
        public override bool Global { get { return _historyOutput.Global; } set { _historyOutput.Global = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Node set")]
        [DescriptionAttribute("Select the node set for the creation of the history output.")]
        public string NodeSetName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Reference point")]
        [DescriptionAttribute("Select the reference point for the creation of the history output.")]
        public string ReferencePointName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(4, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the history output.")]
        public string SurfaceName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }

       
        // Constructors                                                                                                             
        public ViewNodalHistoryOutput(NodalHistoryOutput historyOutput)
        {
            // The order is important
            _historyOutput = historyOutput;            
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(Global));
            //
            StringIntegerDefaultConverter.SetInitialValue = 1;
        }


        // Methods                                                                                                                  
        public override HistoryOutput GetBase()
        {
            return _historyOutput;
        }
        public void PopulateDropDownLists(string[] nodeSetNames, string[] surfaceNames, string[] referencePointNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ReferencePointName, referencePointNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
        }
    }



   
}
