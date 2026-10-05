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
                   "string for the magnitude, or three for the three directions.",
                   Arguments = "name: str, equation: str or list[str]", Returns = "Distribution",
                   Example = "pmx.model.distributions.add_from_equation('D-1', 'x + y')")]
        public DistributionAPI add_from_equation(string name, object equation)
        {
            _service.Add(_service.BuildFromEquation(name, equation));
            return this[name];
        }

        [PythonAPI("Adds a distribution read from a point cloud file using command history. The " +
                   "interpolator type is ClosestPoint, Gauss or Shepard, and the scale and the translate " +
                   "vectors place the cloud on the model.",
                   Arguments = "name: str, file_name: str, interpolator_type: str = None, " +
                               "interpolator_radius: float = None, scale: list[float] = None, " +
                               "translate: list[float] = None", Returns = "Distribution")]
        public DistributionAPI add_from_file(string name, string file_name, string interpolator_type = null,
                                             object interpolator_radius = null, object scale = null,
                                             object translate = null)
        {
            _service.Add(_service.BuildFromFile(name, file_name, interpolator_type, interpolator_radius, scale,
                                                translate));
            return this[name];
        }

        [PythonAPI("Replaces an existing distribution with an equation based one using command history, " +
                   "keeping every value that is not given. The equation may be left out only when the " +
                   "distribution is already equation based.",
                   Arguments = "old_name: str, name: str = None, equation: str or list[str] = None",
                   Returns = "Distribution")]
        public DistributionAPI replace_from_equation(string old_name, string name = null, object equation = null)
        {
            Distribution item = _service.BuildReplacementFromEquation(old_name, name, equation);
            _service.Replace(old_name, item);
            //
            return this[item.Name];
        }
        [PythonAPI("Replaces a file based distribution, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, file_name: str = None, " +
                               "interpolator_type: str = None, interpolator_radius: float = None, " +
                               "scale: list[float] = None, translate: list[float] = None",
                   Returns = "Distribution")]
        public DistributionAPI replace_from_file(string old_name, string name = null, string file_name = null,
                                                 string interpolator_type = null, object interpolator_radius = null,
                                                 object scale = null, object translate = null)
        {
            Distribution item = _service.BuildReplacementFromFile(old_name, name, file_name, interpolator_type,
                                                                   interpolator_radius, scale, translate);
            _service.Replace(old_name, item);
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
