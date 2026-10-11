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
        public ReferencePointAPI add(object name, object coordinates, object name_visible = null,
                                     object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.Add(nameText, coordinates, name_visible, color);
            return this[nameText];
        }

        [PythonAPI("Creates a reference point from selected mesh points using command history. The mode is " +
                   "OnPoint, BetweenTwoPoints or CircleCenter, needing 1, 2 or 3 selected points. The " +
                   "coordinates are computed by the model.",
                   Arguments = "name: str, created_from: str, selection: Selection, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint",
                   Example = "pmx.model.reference_points.add_from_selection('RP-1', 'CircleCenter', selection)")]
        public ReferencePointAPI add_from_selection(object name, object created_from, object selection,
                                                    object name_visible = null, object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string createdFrom = PythonValueConverter.ToText(created_from, "created_from", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            _service.AddFromSelection(nameText, createdFrom, selectionValue, name_visible, color);
            return this[nameText];
        }

        [PythonAPI("Creates a reference point from a named region using command history. The mode is " +
                   "CenterOfGravity or BoundingBoxCenter, and the region type is NodeSetName or " +
                   "SurfaceName, which is what the dialog offers. An internal node set or surface, which the " +
                   "dialog does not list, is refused.",
                   Arguments = "name: str, created_from: str, region_type: str, region_name: str, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI add_from_region(object name, object created_from, object region_type,
                                                  object region_name, object name_visible = null,
                                                  object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string createdFrom = PythonValueConverter.ToText(created_from, "created_from", false);
            string regionType = PythonValueConverter.ToText(region_type, "region_type", false);
            string regionName = PythonValueConverter.ToText(region_name, "region_name", false);
            _service.AddFromRegion(nameText, createdFrom, regionType, regionName, name_visible, color);
            return this[nameText];
        }

        [PythonAPI("Replaces an existing reference point using command history. The coordinates that are " +
                   "not given are kept, and giving them turns the point back into a coordinate point.",
                   Arguments = "old_name: str, name: str = None, coordinates: list[float] = None, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI replace(object old_name, object name = null, object coordinates = null,
                                         object name_visible = null, object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            _service.Replace(oldName, nameText, coordinates, name_visible, color);
            return this[nameText == null ? oldName : nameText];
        }

        [PythonAPI("Replaces a reference point by mesh points selected the way add_from_selection does.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "selection: Selection = None, name_visible: bool = None, " +
                               "color: list[int] or str = None", Returns = "ReferencePoint")]
        public ReferencePointAPI replace_from_selection(object old_name, object name = null,
                                                        object created_from = null,
                                                        object selection = null,
                                                        object name_visible = null, object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string createdFrom = PythonValueConverter.ToText(created_from, "created_from", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            _service.ReplaceFromSelection(oldName, nameText, createdFrom, selectionValue, name_visible, color);
            return this[nameText == null ? oldName : nameText];
        }

        [PythonAPI("Replaces a reference point by a named region the way add_from_region does.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "region_type: str = None, region_name: str = None, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ReferencePointAPI replace_from_region(object old_name, object name = null,
                                                     object created_from = null, object region_type = null,
                                                     object region_name = null, object name_visible = null,
                                                     object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string createdFrom = PythonValueConverter.ToText(created_from, "created_from", true);
            string regionType = PythonValueConverter.ToText(region_type, "region_type", true);
            string regionName = PythonValueConverter.ToText(region_name, "region_name", true);
            _service.ReplaceFromRegion(oldName, nameText, createdFrom, regionType, regionName, name_visible,
                                       color);
            return this[nameText == null ? oldName : nameText];
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
