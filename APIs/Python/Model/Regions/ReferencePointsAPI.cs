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
                   "CenterOfGravity or BoundingBoxCenter, and the region type is NodeSetName or " +
                   "SurfaceName, which is what the dialog offers.",
                   Arguments = "name: str, created_from: str, region_type: str, region_name: str",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI add_from_region(string name, string created_from, string region_type,
                                                  string region_name)
        {
            _service.AddFromRegion(name, created_from, region_type, region_name);
            return this[name];
        }

        [PythonAPI("Replaces an existing reference point using command history. The coordinates that are " +
                   "not given are kept, and giving them turns the point back into a coordinate point.",
                   Arguments = "old_name: str, name: str = None, coordinates: list[float] = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI replace(string old_name, string name = null, object coordinates = null)
        {
            _service.Replace(old_name, name, coordinates);
            return this[name == null ? old_name : name];
        }

        [PythonAPI("Replaces a reference point by mesh points selected the way add_from_selection does.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "selection: Selection = None", Returns = "ReferencePoint")]
        public ReferencePointAPI replace_from_selection(string old_name, string name = null,
                                                        string created_from = null,
                                                        SelectionAPI selection = null)
        {
            _service.ReplaceFromSelection(old_name, name, created_from, selection);
            return this[name == null ? old_name : name];
        }

        [PythonAPI("Replaces a reference point by a named region the way add_from_region does.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "region_type: str = None, region_name: str = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI replace_from_region(string old_name, string name = null,
                                                     string created_from = null, string region_type = null,
                                                     string region_name = null)
        {
            _service.ReplaceFromRegion(old_name, name, created_from, region_type, region_name);
            return this[name == null ? old_name : name];
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
