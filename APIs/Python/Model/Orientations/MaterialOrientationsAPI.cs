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
using CaeModel;

namespace APIs.Python.Model
{
    public class MaterialOrientationsAPI : ModelItemsAPI<MaterialOrientation, MaterialOrientationAPI>
    {
        // Variables
        private const string KeptReferenceNote = " A region or a coordinate system a rename or a remove took away is " +
                                                 "kept when it is not given or is given by the name the orientation " +
                                                 "holds, and the orientation stays invalid.";
        private readonly MaterialOrientationService _service;


        // Constructors
        internal MaterialOrientationsAPI(MaterialOrientationService service)
            : base("material orientation")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a material orientation of shell elements using command history. The region is a selection " +
                   "or a named region of shell parts or shell elements; the selection target is elements or parts. " +
                   "The reference_axis of the coordinate system - X, Y or Z, defaulting to X - is projected onto " +
                   "every shell element at the point CalculiX uses, the centre of a quadrilateral and the first " +
                   "node of a triangle, and becomes the material axis 1; axis 3 is the shell normal. An axis closer " +
                   "than 0.1 degrees to the normal is replaced by the z axis of the coordinate system, or by its x " +
                   "axis when it is the z axis. rotation_angle_deg turns axis 1 about the normal. The coordinate " +
                   "system defaults to Global. Where regions overlap, the later active orientation applies, as a " +
                   "later section does. symbol_density is the density of the spatially " +
                   "distributed material axes symbols as a percentage of the full density, which does not depend " +
                   "on the mesh size, from 0 to 100; it defaults to 50.",
                   Arguments = "name: str, region: Selection or Region, coordinate_system_name: str = None, " +
                               "reference_axis: str = None, rotation_angle_deg: float = 0, " +
                               "symbol_density: int = 50",
                   Returns = "MaterialOrientation",
                   Example = "pmx.model.material_orientations.add('Ori-1', pmx.create.region('PartName', 'Plate'), " +
                             "reference_axis='X', rotation_angle_deg=30)")]
        public MaterialOrientationAPI add(object name, object region, object coordinate_system_name = null,
                                          object reference_axis = null, object rotation_angle_deg = null,
                                          object symbol_density = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateNewName(nameText, null);
            MaterialOrientation materialOrientation = new MaterialOrientation(nameText, null, RegionTypeEnum.Selection);
            _service.ApplyCoordinateSystemName(materialOrientation, coordinate_system_name);
            ApplyReferenceAxis(materialOrientation, reference_axis);
            ApplyRotationAngle(materialOrientation, rotation_angle_deg);
            ApplySymbolDensity(materialOrientation, symbol_density);
            _service.ApplyRegion(materialOrientation, region);
            _service.Add(materialOrientation);
            return this[nameText];
        }

        [PythonAPI("Replaces a material orientation, keeping every value that is not given." + KeptReferenceNote,
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "coordinate_system_name: str = None, reference_axis: str = None, " +
                               "rotation_angle_deg: float = None, symbol_density: int = None",
                   Returns = "MaterialOrientation")]
        public MaterialOrientationAPI replace(object old_name, object name = null, object region = null,
                                              object coordinate_system_name = null, object reference_axis = null,
                                              object rotation_angle_deg = null, object symbol_density = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            MaterialOrientation materialOrientation = _service.Clone(oldName);
            string newName = nameText == null ? materialOrientation.Name : nameText;
            _service.ValidateNewName(newName, oldName);
            materialOrientation.Name = newName;
            if (!_service.IsKeptMissingCoordinateSystem(materialOrientation, coordinate_system_name))
                _service.ApplyCoordinateSystemName(materialOrientation, coordinate_system_name);
            ApplyReferenceAxis(materialOrientation, reference_axis);
            ApplyRotationAngle(materialOrientation, rotation_angle_deg);
            ApplySymbolDensity(materialOrientation, symbol_density);
            if (region != null && !_service.IsKeptMissingRegion(materialOrientation, region))
                _service.ApplyRegion(materialOrientation, region);
            _service.Replace(oldName, materialOrientation);
            //
            return this[newName];
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override MaterialOrientation GetItem(string name)
        {
            return _service.GetMaterialOrientation(name);
        }
        protected override MaterialOrientationAPI CreateItemAPI(string name)
        {
            return new MaterialOrientationAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void SetItemsActive(MaterialOrientation[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(MaterialOrientation item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "MaterialOrientations(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private static void ApplyReferenceAxis(MaterialOrientation materialOrientation, object referenceAxis)
        {
            if (referenceAxis == null) return;
            //
            string text = PythonValueConverter.ToText(referenceAxis, "reference_axis", false);
            materialOrientation.ReferenceAxis = PythonValueConverter.ToEnum<MaterialOrientationAxisEnum>(text,
                                                                                                       "reference_axis");
        }
        private static void ApplyRotationAngle(MaterialOrientation materialOrientation, object rotationAngleDeg)
        {
            if (rotationAngleDeg == null) return;
            //
            PythonModelArgumentCheck.SetEquation(materialOrientation.RotationAngle, rotationAngleDeg, "rotation_angle_deg");
        }
        // The dialog takes a percentage, so a value outside 0 to 100 is refused rather than clamped as the model does
        private static void ApplySymbolDensity(MaterialOrientation materialOrientation, object symbolDensity)
        {
            if (symbolDensity == null) return;
            //
            int density = PythonValueConverter.ToInt(symbolDensity, "symbol_density");
            if (density < 0 || density > 100)
                throw new PythonAPIException("The argument 'symbol_density' must be a percentage from 0 to 100.");
            materialOrientation.SymbolDensity = density;
        }
    }
}
