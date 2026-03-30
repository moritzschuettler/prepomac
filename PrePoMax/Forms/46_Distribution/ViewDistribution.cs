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
using CaeModel;
using DynamicTypeDescriptor;
using CaeMesh;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewDistribution
    {
        // Variables                                                                                                                
        private DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the distribution.")]
        [Id(1, 1)]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Type")]
        [DescriptionAttribute("Select the distribution type.")]
        [Id(2, 1)]
        public abstract DistributionTypeEnum DistributionType { get; set; }
        //
        [CategoryAttribute("Orientation")]
        [DisplayName("Coordinate system")]
        [DescriptionAttribute("Select the coordinate system for the boundary condition.")]
        [Id(1, 20)]
        public abstract string CoordinateSystemName { get; set; }
        //
        [Browsable(false)]
        public DynamicCustomTypeDescriptor DynamicCustomTypeDescriptor { get { return _dctd; } set { _dctd = value; } }
        //
        [Browsable(false)]
        public abstract Distribution GetBase();


        // Constructors                                                                                                             


        // Methods
        public void PopulateCoordinateSystemNames(string[] coordinateSystemNames)
        {
            List<string> names = new List<string>() { CoordinateSystem.DefaultCoordinateSystemName };
            names.AddRange(coordinateSystemNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(CoordinateSystemName), names.ToArray(), false, 2);
        }
    }
}
