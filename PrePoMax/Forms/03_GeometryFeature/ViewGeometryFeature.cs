// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
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
    [Serializable]
    public abstract class ViewGeometryFeature
    {
        // Variables
        public const string RegionSelectionName = "RegionSelection";
        public const string SplitterSelectionName = "SplitterSelection";
        public const string DirectionSelectionName = "DirectionSelection";
        //
        protected DynamicCustomTypeDescriptor _dctd;


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
        //
        [CategoryAttribute("Options")]
        [OrderedDisplayName(0, 10, "Fuzzy tolerance")]
        [DescriptionAttribute("Enter the tolerance the modelling kernel uses to recognize coincident geometry. " +
                              "Leave it at zero unless an imported geometry is too inaccurate to be split.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 9)]
        public double FuzzyValue { get { return GetBase().FuzzyValue; } set { GetBase().FuzzyValue = value; } }


        // Methods
        public abstract GeometryFeature GetBase();
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
