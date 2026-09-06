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

namespace APIs.Python.Model
{
    public class SectionsAPI : ModelItemsAPI<Section, SectionAPI>
    {
        // Variables
        private readonly SectionService _service;


        // Constructors
        internal SectionsAPI(SectionService service)
            : base("section")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a solid section using command history. The region is a selection or a named " +
                   "region; the selection target is elements.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, thickness: float = 1",
                   Returns = "Section",
                   Example = "pmx.model.sections.add_solid('Sec-1', 'Steel', selection)")]
        public SectionAPI add_solid(string name, string material_name, object region, object thickness = null)
        {
            SolidSection section = new SolidSection(name, material_name, null, RegionTypeEnum.Selection, 1,
                                                    _service.GetIsTwoDimensional());
            ApplyThickness(section, thickness);
            AddSection(name, material_name, section, region, "elements");
            return this[name];
        }

        [PythonAPI("Adds a shell section using command history. The region is a selection or a named " +
                   "region; the selection target is elements.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, thickness: float = 1",
                   Returns = "Section")]
        public SectionAPI add_shell(string name, string material_name, object region, object thickness = null)
        {
            ShellSection section = new ShellSection(name, material_name, null, RegionTypeEnum.Selection, 1,
                                                    _service.GetIsTwoDimensional());
            ApplyThickness(section, thickness);
            AddSection(name, material_name, section, region, "elements");
            return this[name];
        }

        [PythonAPI("Adds a membrane section using command history. The region is a selection or a named " +
                   "region; the selection target is elements.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, thickness: float = 1",
                   Returns = "Section")]
        public SectionAPI add_membrane(string name, string material_name, object region,
                                       object thickness = null)
        {
            MembraneSection section = new MembraneSection(name, material_name, null, RegionTypeEnum.Selection, 1,
                                                          _service.GetIsTwoDimensional());
            ApplyThickness(section, thickness);
            AddSection(name, material_name, section, region, "elements");
            return this[name];
        }

        [PythonAPI("Adds a point mass section using command history. The region is a selection or a named " +
                   "region; the selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, mass: float", Returns = "Section")]
        public SectionAPI add_point_mass(string name, object region, object mass)
        {
            PointMassSection section = new PointMassSection(name, null, RegionTypeEnum.Selection, 0,
                                                            _service.GetIsTwoDimensional());
            section.Mass.Equation = PythonValueConverter.ToEquationString(mass, "mass");
            AddSection(name, null, section, region, "nodes");
            return this[name];
        }

        [PythonAPI("Adds a distributed mass section using command history. The region is a selection or a " +
                   "named region; the selection target is elements.",
                   Arguments = "name: str, region: Selection or Region, mass: float", Returns = "Section")]
        public SectionAPI add_distributed_mass(string name, object region, object mass)
        {
            DistributedMassSection section = new DistributedMassSection(name, null, RegionTypeEnum.Selection, 0,
                                                                        _service.GetIsTwoDimensional());
            section.Mass.Equation = PythonValueConverter.ToEquationString(mass, "mass");
            AddSection(name, null, section, region, "elements");
            return this[name];
        }
        //
        [PythonAPI("Replaces a solid section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, material_name: str = None, " +
                               "region: Selection or Region = None, thickness: float = None",
                   Returns = "Section")]
        public SectionAPI replace_solid(string old_name, string name = null, string material_name = null,
                                        object region = null, object thickness = null)
        {
            SolidSection section = Clone<SolidSection>(old_name, "solid");
            section.Thickness.Equation = ToEquationOrKeep(thickness, section.Thickness, "thickness");
            //
            return Replace(old_name, name, material_name, region, section, "elements");
        }

        [PythonAPI("Replaces a shell section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, material_name: str = None, " +
                               "region: Selection or Region = None, thickness: float = None",
                   Returns = "Section")]
        public SectionAPI replace_shell(string old_name, string name = null, string material_name = null,
                                        object region = null, object thickness = null)
        {
            ShellSection section = Clone<ShellSection>(old_name, "shell");
            section.Thickness.Equation = ToEquationOrKeep(thickness, section.Thickness, "thickness");
            //
            return Replace(old_name, name, material_name, region, section, "elements");
        }

        [PythonAPI("Replaces a membrane section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, material_name: str = None, " +
                               "region: Selection or Region = None, thickness: float = None",
                   Returns = "Section")]
        public SectionAPI replace_membrane(string old_name, string name = null, string material_name = null,
                                           object region = null, object thickness = null)
        {
            MembraneSection section = Clone<MembraneSection>(old_name, "membrane");
            section.Thickness.Equation = ToEquationOrKeep(thickness, section.Thickness, "thickness");
            //
            return Replace(old_name, name, material_name, region, section, "elements");
        }

        [PythonAPI("Replaces a point mass section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "mass: float = None", Returns = "Section")]
        public SectionAPI replace_point_mass(string old_name, string name = null, object region = null,
                                             object mass = null)
        {
            PointMassSection section = Clone<PointMassSection>(old_name, "point mass");
            section.Mass.Equation = ToEquationOrKeep(mass, section.Mass, "mass");
            //
            return Replace(old_name, name, null, region, section, "nodes");
        }

        [PythonAPI("Replaces a distributed mass section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "mass: float = None", Returns = "Section")]
        public SectionAPI replace_distributed_mass(string old_name, string name = null, object region = null,
                                                   object mass = null)
        {
            DistributedMassSection section = Clone<DistributedMassSection>(old_name, "distributed mass");
            section.Mass.Equation = ToEquationOrKeep(mass, section.Mass, "mass");
            //
            return Replace(old_name, name, null, region, section, "elements");
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override Section GetItem(string name)
        {
            return _service.GetSection(name);
        }
        protected override SectionAPI CreateItemAPI(string name)
        {
            return new SectionAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(Section item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Sections(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private void AddSection(string name, string materialName, Section section, object region,
                                string target)
        {
            _service.ValidateNewName(name, null);
            if (materialName != null) _service.ValidateMaterialName(materialName);
            _service.ApplyRegion(section, region, target);
            _service.Add(section);
        }
        private TSection Clone<TSection>(string oldName, string sectionKind) where TSection : Section
        {
            TSection section = _service.Clone(oldName) as TSection;
            if (section == null)
                throw new PythonAPIException("The section '" + oldName + "' is not a " + sectionKind + " section.");
            //
            return section;
        }
        private SectionAPI Replace(string oldName, string name, string materialName, object region,
                                   Section section, string target)
        {
            string newName = name == null ? section.Name : name;
            _service.ValidateNewName(newName, oldName);
            if (materialName != null) _service.ValidateMaterialName(materialName);
            section.Name = newName;
            if (materialName != null) section.MaterialName = materialName;
            if (region != null) _service.ApplyRegion(section, region, target);
            _service.Replace(oldName, section);
            //
            return this[newName];
        }
        private static EquationString ToEquationOrKeep(object value, EquationContainer current, string argumentName)
        {
            if (value == null) return current.Equation;
            //
            return PythonValueConverter.ToEquationString(value, argumentName);
        }
        // A thickness the user could type as an expression is stored as that expression, so a creation
        // carries the same value its replacement can
        private static void ApplyThickness(Section section, object thickness)
        {
            if (thickness == null) return;
            //
            section.Thickness.Equation = PythonValueConverter.ToEquationString(thickness, "thickness");
        }
    }
}
