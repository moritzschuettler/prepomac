// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    [PmxType(10323)]
    public abstract class ViewGeometryFeature : UserControls.IMissingPropertyValues
    {
        // Variables
        public const string RegionSelectionName = "RegionSelection";
        public const string SplitterSelectionName = "SplitterSelection";
        public const string DirectionSelectionName = "DirectionSelection";
        //
        [PmxKey(0)] protected DynamicCustomTypeDescriptor _dctd;


        // Properties
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Parts to split")]
        [DescriptionAttribute("Select the parts to split. Hold the Shift key to select more than one part.")]
        [Id(1, 1)]
        public string RegionSelection
        {
            get { return SelectionToString(GetBase().CreationIds, "part"); }
            set { }
        }


        // Methods
        public abstract GeometryFeature GetBase();
        public bool IsPropertyValueMissing(string propertyName)
        {
            if (propertyName != RegionSelectionName) return false;
            //
            GeometryFeature geometryFeature = GetBase();
            return geometryFeature != null &&
                   (geometryFeature.CreationIds == null || geometryFeature.CreationIds.Length == 0);
        }
        /// <summary>
        /// The region row asks for parts in one splitting tool and for surfaces in the other, so what it
        /// says is set by the form that built the view rather than by the attribute.
        /// </summary>
        public void SetRegionSelectionText(string displayName, string description)
        {
            // The rows of a category are ordered by their names, which the attribute does with zero
            // width spaces in front of them, so a name set here is given the same ones
            CustomPropertyDescriptor cpd = _dctd.GetProperty(RegionSelectionName);
            cpd.SetDisplayName(new OrderedDisplayNameAttribute(0, 10, displayName).DisplayName);
            cpd.SetDescription(description);
        }
        protected static string SelectionToString(int[] ids, string itemName)
        {
            return CountToString(ids == null ? 0 : ids.Length, itemName);
        }
        protected static string CountToString(int count, string itemName)
        {
            if (count <= 0) return "Empty";
            else if (count == 1) return "1 " + itemName;
            else return count + " " + itemName + "s";
        }
        /// <summary>
        /// The property grid rows fill from the picked geometry, so they are refreshed after every pick.
        /// </summary>
        public virtual void UpdateVisibility() { }
    }
}
