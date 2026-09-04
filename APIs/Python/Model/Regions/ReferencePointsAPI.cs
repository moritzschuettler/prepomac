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
    public class ReferencePointsAPI : ModelItemsAPI<FeReferencePoint, ReferencePointAPI>
    {
        // Variables
        private readonly ReferencePointService _service;


        // Constructors
        internal ReferencePointsAPI(ReferencePointService service)
            : base("reference point")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates a reference point at the given coordinates using command history.",
                   Arguments = "name: str, coordinates: list[float]", Returns = "ReferencePoint",
                   Example = "pmx.model.reference_points.add('RP-1', [0, 0, 100])")]
        public ReferencePointAPI add(string name, object coordinates)
        {
            _service.Add(name, coordinates);
            return this[name];
        }

        [PythonAPI("Creates a reference point from selected mesh points using command history. The mode is " +
                   "OnPoint, BetweenTwoPoints or CircleCenter, needing 1, 2 or 3 selected points. The " +
                   "coordinates are computed by the model.",
                   Arguments = "name: str, created_from: str, selection: Selection", Returns = "ReferencePoint",
                   Example = "pmx.model.reference_points.add_from_selection('RP-1', 'CircleCenter', selection)")]
        public ReferencePointAPI add_from_selection(string name, string created_from, SelectionAPI selection)
        {
            _service.AddFromSelection(name, created_from, selection);
            return this[name];
        }

        [PythonAPI("Creates a reference point from a named region using command history. The mode is " +
                   "CenterOfGravity or BoundingBoxCenter, and the region type is NodeSetName, SurfaceName " +
                   "or PartName.",
                   Arguments = "name: str, created_from: str, region_type: str, region_name: str",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI add_from_region(string name, string created_from, string region_type,
                                                  string region_name)
        {
            _service.AddFromRegion(name, created_from, region_type, region_name);
            return this[name];
        }

        [PythonAPI("Replaces an existing reference point using command history.",
                   Arguments = "old_name: str, name: str, coordinates: list[float]", Returns = "ReferencePoint")]
        public ReferencePointAPI replace(string old_name, string name, object coordinates)
        {
            _service.Replace(old_name, name, coordinates);
            return this[name];
        }

        [PythonAPI("Hides the named reference points using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the named reference points using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            _service.Show(ResolveNames(names, "names"));
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override FeReferencePoint GetItem(string name)
        {
            return _service.GetReferencePoint(name);
        }
        protected override ReferencePointAPI CreateItemAPI(string name)
        {
            return new ReferencePointAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(FeReferencePoint item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "ReferencePoints(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
