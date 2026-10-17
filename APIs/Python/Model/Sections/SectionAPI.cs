// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class SectionAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(SolidSection), "solid" },
            { typeof(ShellSection), "shell" },
            { typeof(CompositeShellSection), "composite_shell" },
            { typeof(MembraneSection), "membrane" },
            { typeof(PointMassSection), "point_mass" },
            { typeof(DistributedMassSection), "distributed_mass" }
        };
        private readonly string _sectionName;
        private readonly SectionService _service;


        // Properties
        [PythonAPI("The section name.", Returns = "str")]
        public string name { get { return _service.ResolveSection(_sectionName).Name; } }
        [PythonAPI("The section type, named after the add_* method that makes it without the add_ prefix, for " +
                   "example solid or shell.", Returns = "str")]
        public string item_type { get { return GetItemType(_service.ResolveSection(_sectionName)); } }
        [PythonAPI("The name of the material used by the section.", Returns = "str")]
        public string material_name { get { return _service.ResolveSection(_sectionName).MaterialName; } }
        [PythonAPI("The name of the region the section is applied to.", Returns = "str")]
        public string region_name { get { return _service.ResolveSection(_sectionName).RegionName; } }
        [PythonAPI("The region type the section is applied to.", Returns = "str")]
        public string region_type { get { return _service.ResolveSection(_sectionName).RegionType.ToString(); } }
        [PythonAPI("The thickness of a shell or membrane section, or of a solid section in a 2D model, or None " +
                   "for a section whose replace_* method takes no thickness.", Returns = "float or None")]
        public object thickness { get { return GetThickness(_service.ResolveSection(_sectionName)); } }
        [PythonAPI("The offset of a shell or membrane section, in thicknesses, or None for another section.",
                   Returns = "float or None")]
        public object offset { get { return GetOffset(_service.ResolveSection(_sectionName)); } }
        [PythonAPI("The mass of a point mass or distributed mass section, or None for another section.",
                   Returns = "float or None")]
        public object mass
        {
            get
            {
                MassSection massSection = _service.ResolveSection(_sectionName) as MassSection;
                return massSection == null ? null : (object)massSection.Mass.Value;
            }
        }
        [PythonAPI("The layers of a composite shell section as [thickness, material_name, angle_deg] rows from " +
                   "layer 1 up, a value entered as an expression read back as that expression, or None for " +
                   "another section.", Returns = "list[list] or None")]
        public object layers
        {
            get
            {
                CompositeShellSection section = _service.ResolveSection(_sectionName) as CompositeShellSection;
                if (section == null) return null;
                //
                object[] rows = new object[section.Layers.Count];
                for (int i = 0; i < rows.Length; i++)
                {
                    CompositeLayer layer = section.Layers[i];
                    rows[i] = PythonListConverter.ToPythonList(new object[] { ToValue(layer.Thickness), layer.MaterialName,
                                                                              ToValue(layer.Angle) });
                }
                return PythonListConverter.ToPythonList(rows);
            }
        }
        [PythonAPI("True when a composite shell section is completed by its mirror image, or None for another section.",
                   Returns = "bool or None")]
        public object symmetric
        {
            get
            {
                CompositeShellSection section = _service.ResolveSection(_sectionName) as CompositeShellSection;
                return section == null ? null : (object)section.Symmetric;
            }
        }
        [PythonAPI("True when the section resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return _service.ResolveSection(_sectionName).Valid; } }


        // Constructors
        internal SectionAPI(string sectionName, SectionService service)
        {
            _sectionName = sectionName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(Section section)
        {
            string itemType;
            if (ItemTypes.TryGetValue(section.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The section '" + section.Name + "' is a " + section.GetType().Name +
                                         ", which no add_* method of the Python API makes.");
        }
        private static object ToValue(EquationContainer value)
        {
            if (value.IsEquation()) return value.String;
            return value.Value;
        }
        private static object GetThickness(Section section)
        {
            if (section is CompositeShellSection compositeShellSection) return compositeShellSection.GetTotalThickness();
            if (!HasThickness(section)) return null;
            return section.Thickness.Value;
        }
        private static bool HasThickness(Section section)
        {
            if (section is ShellSection || section is MembraneSection) return true;
            SolidSection solidSection = section as SolidSection;
            return solidSection != null && solidSection.TwoD;
        }
        private static object GetOffset(Section section)
        {
            ShellSection shellSection = section as ShellSection;
            if (shellSection != null) return shellSection.Offset.Value;
            MembraneSection membraneSection = section as MembraneSection;
            if (membraneSection != null) return membraneSection.Offset.Value;
            return null;
        }


        // Methods
        [PythonAPI("Returns the expressions the section was entered with, without evaluating them, under " +
                   "thickness, offset and mass. Only a value entered as an expression is returned, with its " +
                   "leading equals sign; a value entered as a plain number is left out, and the property of the " +
                   "same name returns it as a number.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            Section section = _service.ResolveSection(_sectionName);
            // Only the values the replace_* method of the section type takes
            Dictionary<string, string[]> names;
            if (section is CompositeShellSection) names = ModelItemValueReader.Arguments("offset");
            else if (section is ShellSection || section is MembraneSection)
                names = ModelItemValueReader.Arguments("thickness", "offset");
            else if (section is MassSection) names = ModelItemValueReader.Arguments("mass");
            else if (HasThickness(section)) names = ModelItemValueReader.Arguments("thickness");
            else names = ModelItemValueReader.Arguments();
            //
            return ModelItemValueReader.GetScalarEquations(section, names);
        }
        public override string ToString()
        {
            Section section = _service.ResolveSection(_sectionName);
            return "Section(name=" + StringListAPI.ToDisplayString(section.Name) +
                   ", type=" + GetItemType(section) +
                   ", material=" + StringListAPI.ToDisplayString(section.MaterialName) + ")";
        }
    }
}
