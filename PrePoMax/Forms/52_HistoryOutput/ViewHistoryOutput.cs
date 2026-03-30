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

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewHistoryOutput : ViewMultiRegion
    {
        // Variables                                                                                                                
        private string _selectionHidden;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the history output.")]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(10, 10, "Global")]
        [DescriptionAttribute("Parameter global controls whether the results are saved in a global or local coordinate system.")]
        public abstract bool Global { get; set; }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the history output.")]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        public string SelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }


        // Constructors                                                                                                             


        // Methods
        public abstract CaeModel.HistoryOutput GetBase();
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            // Hide SelectionHidden
            DynamicTypeDescriptor.CustomPropertyDescriptor cpd;
            if (base.RegionType == RegionTypeEnum.Selection.ToFriendlyString())
            {
                cpd = base.DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden));
                cpd.SetIsBrowsable(false);
            }
        }
    }
}
