// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    public class DistributionsAPI : ModelItemsAPI<Distribution, DistributionAPI>
    {
        // Variables
        private readonly DistributionService _service;


        // Constructors
        internal DistributionsAPI(DistributionService service)
            : base("distribution")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a distribution defined by an equation using command history. Give one equation " +
                   "string for the magnitude, or three for the three directions. The coordinate system, Global " +
                   "by default, is the one x, y and z are measured in; its name is matched regardless of case.",
                   Arguments = "name: str, equation: str or list[str], coordinate_system_name: str = None",
                   Returns = "Distribution",
                   Example = "pmx.model.distributions.add_from_equation('D-1', 'x + y')")]
        public DistributionAPI add_from_equation(object name, object equation, object coordinate_system_name = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.Add(_service.BuildFromEquation(nameText, equation, coordinate_system_name));
            return this[nameText];
        }

        [PythonAPI("Adds a distribution read from a point cloud file using command history. The " +
                   "interpolator type is ClosestPoint, Gauss or Shepard, and the scale and the translate " +
                   "vectors place the cloud on the model. The coordinate system is Global by default, and its " +
                   "name is matched regardless of case.",
                   Arguments = "name: str, file_name: str, interpolator_type: str = None, " +
                               "interpolator_radius: float = None, scale: list[float] = None, " +
                               "translate: list[float] = None, coordinate_system_name: str = None",
                   Returns = "Distribution")]
        public DistributionAPI add_from_file(object name, object file_name, object interpolator_type = null,
                                             object interpolator_radius = null, object scale = null,
                                             object translate = null, object coordinate_system_name = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            string interpolatorType = PythonValueConverter.ToText(interpolator_type, "interpolator_type", true);
            _service.Add(_service.BuildFromFile(nameText, fileName, interpolatorType, interpolator_radius, scale,
                                                translate, coordinate_system_name));
            return this[nameText];
        }

        [PythonAPI("Replaces an existing equation based distribution using command history, keeping every " +
                   "value that is not given. A distribution read from a file is refused; remove it and add " +
                   "the equation based one instead.",
                   Arguments = "old_name: str, name: str = None, equation: str or list[str] = None, " +
                               "coordinate_system_name: str = None",
                   Returns = "Distribution")]
        public DistributionAPI replace_from_equation(object old_name, object name = null, object equation = null,
                                                     object coordinate_system_name = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            Distribution item = _service.BuildReplacementFromEquation(oldName, nameText, equation,
                                                                       coordinate_system_name);
            _service.Replace(oldName, item);
            //
            return this[item.Name];
        }
        [PythonAPI("Replaces a file based distribution using command history, keeping every value that is not " +
                   "given. An equation based distribution is refused; remove it and add the file based one " +
                   "instead.",
                   Arguments = "old_name: str, name: str = None, file_name: str = None, " +
                               "interpolator_type: str = None, interpolator_radius: float = None, " +
                               "scale: list[float] = None, translate: list[float] = None, " +
                               "coordinate_system_name: str = None",
                   Returns = "Distribution")]
        public DistributionAPI replace_from_file(object old_name, object name = null, object file_name = null,
                                                 object interpolator_type = null, object interpolator_radius = null,
                                                 object scale = null, object translate = null,
                                                 object coordinate_system_name = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string fileName = PythonValueConverter.ToText(file_name, "file_name", true);
            string interpolatorType = PythonValueConverter.ToText(interpolator_type, "interpolator_type", true);
            Distribution item = _service.BuildReplacementFromFile(oldName, nameText, fileName, interpolatorType,
                                                                   interpolator_radius, scale, translate,
                                                                   coordinate_system_name);
            _service.Replace(oldName, item);
            //
            return this[item.Name];
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override Distribution GetItem(string name)
        {
            return _service.GetDistribution(name);
        }
        protected override DistributionAPI CreateItemAPI(string name)
        {
            return new DistributionAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(Distribution item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Distributions(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
