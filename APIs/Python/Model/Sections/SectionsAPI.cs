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
using Python.Runtime;
using System.Collections;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class SectionsAPI : ModelItemsAPI<Section, SectionAPI>
    {
        // Variables
        private const string KeptReferenceNote = " A reference a rename or a remove took away is kept when it is " +
                                                 "not given or is given by the name the section holds, and the " +
                                                 "section stays invalid.";
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

        [PythonAPI("Adds a composite shell section using command history. The region is a selection or a named " +
                   "region; the selection target is elements or parts. Every row of layers is [thickness, " +
                   "material_name, angle_deg]: layer 1 lies on the side opposite to the shell normal and the " +
                   "layers stack along the normal. The angle turns the material axis 1 of the element about the " +
                   "normal, from the material orientation that covers the element, or from the solver default, the " +
                   "projected global x axis. A symmetric layup is completed by its mirror image. As the dialog " +
                   "does, the region must be made of shell parts or shell elements, and CalculiX takes a composite " +
                   "shell of S8R and S6 elements only. The offset is given in total thicknesses. A thickness or an " +
                   "angle may be an equation string.",
                   Arguments = "name: str, region: Selection or Region, layers: list[list], offset: float = 0, " +
                               "symmetric: bool = False",
                   Returns = "Section",
                   Example = "pmx.model.sections.add_composite_shell('Laminate', selection, " +
                             "[[0.25, 'Ply', 0], [0.25, 'Ply', 90]], symmetric=True)")]
        public SectionAPI add_composite_shell(object name, object region, object layers, object offset = null,
                                              object symmetric = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateNewName(nameText, null);
            CompositeShellSection section = new CompositeShellSection(nameText, null, RegionTypeEnum.Selection,
                                                                      ToLayers(layers), _service.GetIsTwoDimensional());
            ApplyOffset(section.Offset, offset);
            if (symmetric != null) section.Symmetric = PythonValueConverter.ToBool(symmetric, "symmetric");
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
        [PythonAPI("Replaces a solid section, keeping every value that is not given." + KeptReferenceNote,
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

        [PythonAPI("Replaces a shell section, keeping every value that is not given." + KeptReferenceNote,
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

        [PythonAPI("Replaces a composite shell section, keeping every value that is not given. layers replaces " +
                   "all layers at once." + KeptReferenceNote,
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "layers: list[list] = None, offset: float = None, symmetric: bool = None",
                   Returns = "Section")]
        public SectionAPI replace_composite_shell(object old_name, object name = null, object region = null,
                                                  object layers = null, object offset = null, object symmetric = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            CompositeShellSection section = Clone<CompositeShellSection>(oldName, "composite_shell");
            if (layers != null) section.Layers = ToLayers(layers);
            PythonModelArgumentCheck.SetEquation(section.Offset, ToEquationOrKeep(offset, section.Offset, "offset"),
                                                 "offset");
            if (symmetric != null) section.Symmetric = PythonValueConverter.ToBool(symmetric, "symmetric");
            //
            return Replace(oldName, nameText, null, region, section, "elements");
        }

        [PythonAPI("Replaces a membrane section, keeping every value that is not given." + KeptReferenceNote,
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

        [PythonAPI("Replaces a point mass section, keeping every value that is not given." + KeptReferenceNote,
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

        [PythonAPI("Replaces a distributed mass section, keeping every value that is not given." +
                   KeptReferenceNote,
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
            if (materialName != null && !_service.IsKeptMissingMaterial(section, materialName))
                section.MaterialName = _service.GetStoredMaterialName(materialName);
            section.Name = newName;
            if (region != null && !_service.IsKeptMissingRegion(section, region))
                _service.ApplyRegion(section, region, target);
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
        // A row is [thickness, material_name, angle_deg]; the material is stored in the model's own spelling
        private List<CompositeLayer> ToLayers(object layers)
        {
            List<object[]> rows = ToRows(layers, "layers");
            if (rows.Count == 0) throw new PythonAPIException("The argument 'layers' must contain at least one layer.");
            //
            List<CompositeLayer> compositeLayers = new List<CompositeLayer>();
            for (int i = 0; i < rows.Count; i++)
            {
                string argumentName = "layers[" + i + "]";
                if (rows[i].Length != 3)
                    throw new PythonAPIException("The argument '" + argumentName + "' must be [thickness, " +
                                                 "material_name, angle_deg].");
                string materialName = rows[i][1] as string;
                if (materialName == null)
                    throw new PythonAPIException("The material of '" + argumentName + "' must be a material name.");
                CompositeLayer layer = new CompositeLayer(_service.GetStoredMaterialName(materialName, argumentName), 0, 0);
                PythonModelArgumentCheck.SetEquation(layer.Thickness,
                                                     PythonValueConverter.ToEquationString(rows[i][0], argumentName),
                                                     argumentName);
                PythonModelArgumentCheck.SetEquation(layer.Angle,
                                                     PythonValueConverter.ToEquationString(rows[i][2], argumentName),
                                                     argumentName);
                compositeLayers.Add(layer);
            }
            return compositeLayers;
        }
        private static List<object[]> ToRows(object value, string argumentName)
        {
            if (value == null || value is string)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a list of rows.");
            //
            List<object[]> rows = new List<object[]>();
            PyObject python = value as PyObject;
            if (python == null)
            {
                IEnumerable enumerable = value as IEnumerable;
                if (enumerable == null)
                    throw new PythonAPIException("The argument '" + argumentName + "' must be a list of rows.");
                foreach (object row in enumerable)
                {
                    IEnumerable items = row as IEnumerable;
                    if (items == null || row is string)
                        throw new PythonAPIException("The argument '" + argumentName + "' must be a list of rows.");
                    List<object> rowItems = new List<object>();
                    foreach (object item in items) rowItems.Add(item);
                    rows.Add(rowItems.ToArray());
                }
                return rows;
            }
            using (Py.GIL())
            {
                if (python.IsNone() || PyString.IsStringType(python))
                    throw new PythonAPIException("The argument '" + argumentName + "' must be a list of rows.");
                using (PyObject builtins = Py.Import("builtins"))
                using (PyObject listFunction = builtins.GetAttr("list"))
                using (PyObject list = listFunction.Invoke(python))
                {
                    for (int i = 0; i < list.Length(); i++)
                    {
                        using (PyObject row = list.GetItem(i))
                        {
                            if (row.IsNone() || PyString.IsStringType(row))
                                throw new PythonAPIException("The argument '" + argumentName + "[" + i + "]' must be " +
                                                             "a list.");
                            using (PyObject items = listFunction.Invoke(row))
                            {
                                object[] values = new object[items.Length()];
                                for (int j = 0; j < values.Length; j++)
                                {
                                    using (PyObject item = items.GetItem(j))
                                        values[j] = item.IsNone() ? null : item.AsManagedObject(typeof(object));
                                }
                                rows.Add(values);
                            }
                        }
                    }
                }
            }
            return rows;
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
