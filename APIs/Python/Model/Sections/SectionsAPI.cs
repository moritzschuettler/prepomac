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
        [PythonAPI("Adds a solid section using command history. The region is a selection or a named region; the selection target is elements.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, thickness: float = 1",
                   Returns = "Section",
                   Example = "pmx.model.sections.add_solid('Sec-1', 'Steel', selection)")]
        public SectionAPI add_solid(string name, string material_name, object region, object thickness = null)
        {
            SolidSection section = new SolidSection(name, material_name, null, RegionTypeEnum.Selection,
                                                    ToThickness(thickness), _service.GetIsTwoDimensional());
            AddSection(name, material_name, section, region, "elements");
            return this[name];
        }

        [PythonAPI("Adds a shell section using command history. The region is a selection or a named region; the selection target is elements.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, thickness: float = 1",
                   Returns = "Section")]
        public SectionAPI add_shell(string name, string material_name, object region, object thickness = null)
        {
            ShellSection section = new ShellSection(name, material_name, null, RegionTypeEnum.Selection,
                                                    ToThickness(thickness), _service.GetIsTwoDimensional());
            AddSection(name, material_name, section, region, "elements");
            return this[name];
        }

        [PythonAPI("Adds a membrane section using command history. The region is a selection or a named region; the selection target is elements.",
                   Arguments = "name: str, material_name: str, region: Selection or Region, thickness: float = 1",
                   Returns = "Section")]
        public SectionAPI add_membrane(string name, string material_name, object region,
                                       object thickness = null)
        {
            MembraneSection section = new MembraneSection(name, material_name, null, RegionTypeEnum.Selection,
                                                          ToThickness(thickness), _service.GetIsTwoDimensional());
            AddSection(name, material_name, section, region, "elements");
            return this[name];
        }

        [PythonAPI("Adds a point mass section using command history. The region is a selection or a named region; the selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, mass: float", Returns = "Section")]
        public SectionAPI add_point_mass(string name, object region, object mass)
        {
            PointMassSection section = new PointMassSection(name, null, RegionTypeEnum.Selection,
                                                            PythonValueConverter.ToDouble(mass, "mass"),
                                                            _service.GetIsTwoDimensional());
            AddSection(name, null, section, region, "nodes");
            return this[name];
        }

        [PythonAPI("Adds a distributed mass section using command history. The region is a selection or a named region; the selection target is elements.",
                   Arguments = "name: str, region: Selection or Region, mass: float", Returns = "Section")]
        public SectionAPI add_distributed_mass(string name, object region, object mass)
        {
            DistributedMassSection section = new DistributedMassSection(name, null, RegionTypeEnum.Selection,
                                                                        PythonValueConverter.ToDouble(mass, "mass"),
                                                                        _service.GetIsTwoDimensional());
            AddSection(name, null, section, region, "elements");
            return this[name];
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
        private static double ToThickness(object thickness)
        {
            if (thickness == null) return 1;
            //
            return PythonValueConverter.ToDouble(thickness, "thickness");
        }
    }
}
