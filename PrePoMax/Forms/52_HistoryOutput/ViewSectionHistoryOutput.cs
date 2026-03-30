// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
    public enum ViewSectionHistoryVariableEnum
    {
        // Must start at 1 for the UI to work
        [StandardValue("SOF", Description = "Section forces.")]
        SOF = 1,
        [StandardValue("SOM", Description = "Section moments.")]
        SOM = 2,
        [StandardValue("SOAREA", Description = "Section area.")]
        SOAREA = 4,
    }

    [Serializable]
    public class ViewSectionHistoryOutput : ViewHistoryOutput
    {
        // Variables                                                                                                                
        private SectionHistoryOutput _historyOutput;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [OrderedDisplayName(1, 10, "Variables to output")]
        [CategoryAttribute("Data")]
        [DescriptionAttribute("Section history variables")]
        public ViewSectionHistoryVariableEnum Variables
        {
            get
            {
                return (ViewSectionHistoryVariableEnum)_historyOutput.Variables;
            }
            set
            {
                _historyOutput.Variables = (SectionHistoryVariableEnum)value;
            }
        }
        //
        [Browsable(false)]
        public TotalsTypeEnum TotalsType { get { return _historyOutput.TotalsType; } set { _historyOutput.TotalsType = value; } }
        //
        [Browsable(false)]
        public override bool Global { get { return _historyOutput.Global; } set { _historyOutput.Global = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(4, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the history output.")]
        public string SurfaceName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }

       
        // Constructors                                                                                                             
        public ViewSectionHistoryOutput(SectionHistoryOutput historyOutput)
        {
            // The order is important
            _historyOutput = historyOutput;            
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            StringIntegerDefaultConverter.SetInitialValue = 1;
        }


        // Methods                                                                                                                  
        public override HistoryOutput GetBase()
        {
            return _historyOutput;
        }
        public void PopulateDropDownLists(string[] surfaceNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
        }
    }



   
}
