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
        private const string DisplayNote = "The name visibility and the symbol colour are part of the " +
                                           "stored definition and keep their defaults when they are not given.";
        private readonly ReferencePointService _service;


        // Constructors
        internal ReferencePointsAPI(ReferencePointService service)
            : base("reference point")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates a reference point at the given coordinates using command history. " + DisplayNote,
                   Arguments = "name: str, coordinates: list[float], name_visible: bool = None, " +
                               "color: list[int] or str = None", Returns = "ReferencePoint",
                   Example = "pmx.model.reference_points.add('RP-1', [0, 0, 100])")]
        public ReferencePointAPI add(string name, object coordinates, object name_visible = null,
                                     object color = null)
        {
            _service.Add(name, coordinates, name_visible, color);
            return this[name];
        }

        [PythonAPI("Creates a reference point from selected mesh points using command history. The mode is " +
                   "OnPoint, BetweenTwoPoints or CircleCenter, needing 1, 2 or 3 selected points. The " +
                   "coordinates are computed by the model.",
                   Arguments = "name: str, created_from: str, selection: Selection, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint",
                   Example = "pmx.model.reference_points.add_from_selection('RP-1', 'CircleCenter', selection)")]
        public ReferencePointAPI add_from_selection(string name, string created_from, SelectionAPI selection,
                                                    object name_visible = null, object color = null)
        {
            _service.AddFromSelection(name, created_from, selection, name_visible, color);
            return this[name];
        }

        [PythonAPI("Creates a reference point from a named region using command history. The mode is " +
                   "CenterOfGravity or BoundingBoxCenter, and the region type is NodeSetName or " +
                   "SurfaceName, which is what the dialog offers.",
                   Arguments = "name: str, created_from: str, region_type: str, region_name: str, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI add_from_region(string name, string created_from, string region_type,
                                                  string region_name, object name_visible = null,
                                                  object color = null)
        {
            _service.AddFromRegion(name, created_from, region_type, region_name, name_visible, color);
            return this[name];
        }

        [PythonAPI("Replaces an existing reference point using command history. The coordinates that are " +
                   "not given are kept, and giving them turns the point back into a coordinate point.",
                   Arguments = "old_name: str, name: str = None, coordinates: list[float] = None, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI replace(string old_name, string name = null, object coordinates = null,
                                         object name_visible = null, object color = null)
        {
            _service.Replace(old_name, name, coordinates, name_visible, color);
            return this[name == null ? old_name : name];
        }

        [PythonAPI("Replaces a reference point by mesh points selected the way add_from_selection does.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "selection: Selection = None, name_visible: bool = None, " +
                               "color: list[int] or str = None", Returns = "ReferencePoint")]
        public ReferencePointAPI replace_from_selection(string old_name, string name = null,
                                                        string created_from = null,
                                                        SelectionAPI selection = null,
                                                        object name_visible = null, object color = null)
        {
            _service.ReplaceFromSelection(old_name, name, created_from, selection, name_visible, color);
            return this[name == null ? old_name : name];
        }

        [PythonAPI("Replaces a reference point by a named region the way add_from_region does.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "region_type: str = None, region_name: str = None, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI replace_from_region(string old_name, string name = null,
                                                     string created_from = null, string region_type = null,
                                                     string region_name = null, object name_visible = null,
                                                     object color = null)
        {
            _service.ReplaceFromRegion(old_name, name, created_from, region_type, region_name, name_visible,
                                       color);
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
