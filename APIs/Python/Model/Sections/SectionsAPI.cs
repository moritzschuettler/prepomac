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
                   "region; the selection target is elements or parts, as the dialog picks them. As the " +
                   "dialog does, a region that is not made of solid parts or solid elements - in a 2D model, " +
                   "of the parts of the mesh - is refused, on an add and on the section a replace ends with. " +
                   "The thickness is used only in a 2D model, so " +
                   "it is refused in a 3D one; it must be larger than 0, and a 2D section that is not given " +
                   "one gets 1.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, " +
                               "thickness: float = None",
                   Returns = "Section",
                   Example = "pmx.model.sections.add_solid('Sec-1', 'Steel', selection)")]
        public SectionAPI add_solid(object name, object material_name, object region, object thickness = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string materialNameText = PythonValueConverter.ToText(material_name, "material_name", false);
            _service.ValidateNewName(nameText, null);
            string materialName = _service.GetStoredMaterialName(materialNameText);
            SolidSection section = new SolidSection(nameText, materialName, null, RegionTypeEnum.Selection, 1,
                                                    _service.GetIsTwoDimensional());
            ApplySolidThickness(section, thickness);
            AddSection(section, region, "elements");
            return this[nameText];
        }

        [PythonAPI("Adds a shell section using command history. The region is a selection or a named " +
                   "region; the selection target is elements or parts. As the dialog does, a region that is " +
                   "not made of shell parts or shell elements is refused, on an add and on the section a " +
                   "replace ends with, and a 2D model takes the section on a selection only. The offset of the " +
                   "shell mid surface from " +
                   "the selected geometry is given in shell thicknesses. The thickness must be larger than 0.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, " +
                               "thickness: float = 1, offset: float = 0",
                   Returns = "Section")]
        public SectionAPI add_shell(object name, object material_name, object region, object thickness = null,
                                    object offset = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string materialNameText = PythonValueConverter.ToText(material_name, "material_name", false);
            _service.ValidateNewName(nameText, null);
            string materialName = _service.GetStoredMaterialName(materialNameText);
            ShellSection section = new ShellSection(nameText, materialName, null, RegionTypeEnum.Selection, 1,
                                                    _service.GetIsTwoDimensional());
            ApplyThickness(section, thickness);
            ApplyOffset(section.Offset, offset);
            AddSection(section, region, "elements");
            return this[nameText];
        }

        [PythonAPI("Adds a membrane section using command history. The region is a selection or a named " +
                   "region; the selection target is elements or parts. As the dialog does, a region that is " +
                   "not made of shell parts or shell elements is refused, on an add and on the section a " +
                   "replace ends with, and a 2D model takes the section on a selection only. The offset of the " +
                   "membrane mid surface from " +
                   "the selected geometry is given in membrane thicknesses. The thickness must be larger " +
                   "than 0.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, " +
                               "thickness: float = 1, offset: float = 0",
                   Returns = "Section")]
        public SectionAPI add_membrane(object name, object material_name, object region,
                                       object thickness = null, object offset = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string materialNameText = PythonValueConverter.ToText(material_name, "material_name", false);
            _service.ValidateNewName(nameText, null);
            string materialName = _service.GetStoredMaterialName(materialNameText);
            MembraneSection section = new MembraneSection(nameText, materialName, null, RegionTypeEnum.Selection, 1,
                                                          _service.GetIsTwoDimensional());
            ApplyThickness(section, thickness);
            ApplyOffset(section.Offset, offset);
            AddSection(section, region, "elements");
            return this[nameText];
        }

        [PythonAPI("Adds a point mass section using command history. The region is a selection or a named " +
                   "region; the selection target is nodes. As the dialog does, a 2D model takes the section on " +
                   "a selection only. The mass must be zero or larger.",
                   Arguments = "name: str, region: Selection or Region, mass: float", Returns = "Section")]
        public SectionAPI add_point_mass(object name, object region, object mass)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateNewName(nameText, null);
            PointMassSection section = new PointMassSection(nameText, null, RegionTypeEnum.Selection, 0,
                                                            _service.GetIsTwoDimensional());
            PythonModelArgumentCheck.SetEquation(section.Mass, ToMass(mass, section.Mass), "mass");
            AddSection(section, region, "nodes");
            return this[nameText];
        }

        [PythonAPI("Adds a distributed mass section using command history. The region is a selection or a " +
                   "named region; the selection target is surfaces. The mass must be zero or larger.",
                   Arguments = "name: str, region: Selection or Region, mass: float", Returns = "Section")]
        public SectionAPI add_distributed_mass(object name, object region, object mass)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateNewName(nameText, null);
            DistributedMassSection section = new DistributedMassSection(nameText, null, RegionTypeEnum.Selection, 0,
                                                                        _service.GetIsTwoDimensional());
            PythonModelArgumentCheck.SetEquation(section.Mass, ToMass(mass, section.Mass), "mass");
            AddSection(section, region, "surfaces");
            return this[nameText];
        }
        //
        [PythonAPI("Replaces a solid section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, material_name: str = None, " +
                               "region: Selection or Region = None, thickness: float = None",
                   Returns = "Section")]
        public SectionAPI replace_solid(object old_name, object name = null, object material_name = null,
                                        object region = null, object thickness = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string materialName = PythonValueConverter.ToText(material_name, "material_name", true);
            SolidSection section = Clone<SolidSection>(oldName, "solid");
            ApplySolidThickness(section, thickness);
            //
            return Replace(oldName, nameText, materialName, region, section, "elements");
        }

        [PythonAPI("Replaces a shell section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, material_name: str = None, " +
                               "region: Selection or Region = None, thickness: float = None, " +
                               "offset: float = None",
                   Returns = "Section")]
        public SectionAPI replace_shell(object old_name, object name = null, object material_name = null,
                                        object region = null, object thickness = null, object offset = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string materialName = PythonValueConverter.ToText(material_name, "material_name", true);
            ShellSection section = Clone<ShellSection>(oldName, "shell");
            ApplyThickness(section, thickness);
            PythonModelArgumentCheck.SetEquation(section.Offset, ToEquationOrKeep(offset, section.Offset, "offset"),
                                                 "offset");
            //
            return Replace(oldName, nameText, materialName, region, section, "elements");
        }

        [PythonAPI("Replaces a membrane section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, material_name: str = None, " +
                               "region: Selection or Region = None, thickness: float = None, " +
                               "offset: float = None",
                   Returns = "Section")]
        public SectionAPI replace_membrane(object old_name, object name = null, object material_name = null,
                                           object region = null, object thickness = null, object offset = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string materialName = PythonValueConverter.ToText(material_name, "material_name", true);
            MembraneSection section = Clone<MembraneSection>(oldName, "membrane");
            ApplyThickness(section, thickness);
            PythonModelArgumentCheck.SetEquation(section.Offset, ToEquationOrKeep(offset, section.Offset, "offset"),
                                                 "offset");
            //
            return Replace(oldName, nameText, materialName, region, section, "elements");
        }

        [PythonAPI("Replaces a point mass section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "mass: float = None", Returns = "Section")]
        public SectionAPI replace_point_mass(object old_name, object name = null, object region = null,
                                             object mass = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            PointMassSection section = Clone<PointMassSection>(oldName, "point_mass");
            if (mass != null) PythonModelArgumentCheck.SetEquation(section.Mass, ToMass(mass, section.Mass), "mass");
            //
            return Replace(oldName, nameText, null, region, section, "nodes");
        }

        [PythonAPI("Replaces a distributed mass section, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "mass: float = None", Returns = "Section")]
        public SectionAPI replace_distributed_mass(object old_name, object name = null, object region = null,
                                                   object mass = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            DistributedMassSection section = Clone<DistributedMassSection>(oldName, "distributed_mass");
            if (mass != null) PythonModelArgumentCheck.SetEquation(section.Mass, ToMass(mass, section.Mass), "mass");
            //
            return Replace(oldName, nameText, null, region, section, "surfaces");
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
        private void AddSection(Section section, object region, string target)
        {
            _service.ApplyRegion(section, region, target);
            _service.Add(section);
        }
        // The type cannot change on a replace, the same way the dialog only edits the type it opened
        private TSection Clone<TSection>(string oldName, string itemType) where TSection : Section
        {
            Section section = _service.Clone(oldName);
            if (section.GetType() != typeof(TSection))
                throw new PythonAPIException("The section '" + oldName + "' is made by add_" +
                                             SectionAPI.GetItemType(section) + ", so replace_" + itemType +
                                             " cannot replace it. Remove it and add the new type instead.");
            //
            return (TSection)section;
        }
        private SectionAPI Replace(string oldName, string name, string materialName, object region,
                                   Section section, string target)
        {
            string newName = name == null ? section.Name : name;
            _service.ValidateNewName(newName, oldName);
            string storedMaterialName = materialName == null ? null : _service.GetStoredMaterialName(materialName);
            section.Name = newName;
            if (storedMaterialName != null) section.MaterialName = storedMaterialName;
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
            EquationString equation = PythonValueConverter.ToEquationString(thickness, "thickness");
            // The model refuses a thickness that is not positive with a message that names no argument
            if (!(PythonModelArgumentCheck.Evaluate(section.Thickness, equation, "thickness") > 0))
                throw new PythonAPIException("The argument 'thickness' must be larger than 0.");
            PythonModelArgumentCheck.SetEquation(section.Thickness, equation, "thickness");
        }
        // The solver input carries the thickness of a solid section only in a 2D model
        private static void ApplySolidThickness(SolidSection section, object thickness)
        {
            if (thickness != null && !section.TwoD)
                throw new PythonAPIException("The argument 'thickness' applies to a solid section only in a 2D " +
                                             "model; a 3D solid section has no thickness.");
            ApplyThickness(section, thickness);
        }
        private static EquationString ToMass(object mass, EquationContainer current)
        {
            EquationString equation = PythonValueConverter.ToEquationString(mass, "mass");
            if (PythonModelArgumentCheck.Evaluate(current, equation, "mass") < 0)
                throw new PythonAPIException("The argument 'mass' must be zero or larger.");
            //
            return equation;
        }
        // The offset only exists on the section types that have a mid surface, so it is applied through
        // the container rather than through the section
        private static void ApplyOffset(EquationContainer sectionOffset, object offset)
        {
            if (offset == null) return;
            //
            PythonModelArgumentCheck.SetEquation(sectionOffset, offset, "offset");
        }
    }
}
