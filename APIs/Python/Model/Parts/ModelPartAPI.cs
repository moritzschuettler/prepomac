// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    public class ModelPartAPI : PartAPI
    {
        // Variables
        private readonly string _partName;
        private readonly ModelPartService _service;


        // Constructors
        internal ModelPartAPI(string partName, ModelPartService service)
        {
            _partName = partName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the IDs of the inverted or degenerate solid elements of the part: those with a " +
                   "scaled Jacobian at a corner that is zero or negative. Meshing keeps such elements, reports " +
                   "them in the output window and highlights them, so a script finds them here.",
                   Returns = "IntList",
                   Example = "bad = pmx.model.parts['Solid_part-1'].get_inverted_element_ids()")]
        public IntListAPI get_inverted_element_ids()
        {
            return new IntListAPI(InvertedElements.GetElementIds(ResolveMesh(), ResolvePart()));
        }
        [PythonAPI("Returns the mass of the part from the densities of the materials of its sections, as the " +
                   "Query dialog computes it, or None when no element has a section with a density. A shell has " +
                   "the mass of its thickness and a composite shell the mass of all its layers.",
                   Returns = "float or None",
                   Example = "mass = pmx.model.parts['Plate'].get_mass()")]
        public double? get_mass()
        {
            double mass;
            string[] warnings;
            if (_service.TryGetPartMass(_partName, out mass, out warnings)) return mass;
            return null;
        }
        [PythonAPI("Returns the warnings of the mass computation of the part: elements without a section, without " +
                   "a density or of an unsupported type, which are excluded from the mass, and the approximations " +
                   "made.", Returns = "list[str]")]
        public object get_mass_warnings()
        {
            double mass;
            string[] warnings;
            _service.TryGetPartMass(_partName, out mass, out warnings);
            return PythonListConverter.ToPythonList(warnings);
        }
        protected override BasePart ResolvePart()
        {
            return _service.ResolvePart(_partName);
        }
        protected override FeMesh ResolveMesh()
        {
            return _service.ResolveMesh();
        }
    }
}
