// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;

namespace APIs.Python
{
    // Names an existing region of the model, so that a model item can point at a node set, a surface,
    // an element set, a reference point or a part instead of carrying its own selection. It is a
    // detached description; the name is checked when the item that uses it is built.
    public class RegionAPI : BaseAPI
    {
        // Variables
        internal static readonly RegionTypeEnum[] NamedRegionTypes =
        {
            RegionTypeEnum.NodeSetName, RegionTypeEnum.ElementSetName, RegionTypeEnum.SurfaceName,
            RegionTypeEnum.ReferencePointName, RegionTypeEnum.PartName, RegionTypeEnum.MassSection
        };
        private readonly RegionTypeEnum _regionType;
        private readonly string _name;


        // Properties
        [PythonAPI("The region type, for example NodeSetName.", Returns = "str")]
        public string region_type { get { return _regionType.ToString(); } }
        [PythonAPI("The name of the region.", Returns = "str")]
        public string name { get { return _name; } }
        internal RegionTypeEnum RegionType { get { return _regionType; } }


        // Constructors
        internal RegionAPI(RegionTypeEnum regionType, string name)
        {
            _regionType = regionType;
            _name = name;
        }


        // Methods
        public override string ToString()
        {
            return "Region(type=" + _regionType + ", name=" + StringListAPI.ToDisplayString(_name) + ")";
        }
    }
}
