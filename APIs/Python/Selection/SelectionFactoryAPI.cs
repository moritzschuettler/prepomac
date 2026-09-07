// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    public class SelectionFactoryAPI : BaseAPI
    {
        // Variables
        private readonly SelectionService _service;


        // Constructors
        internal SelectionFactoryAPI(SelectionService service)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates an ID-based selection draft. Possible view values are 'geometry', 'femodel', and " +
                   "'results'. Possible target values are 'nodes', 'elements', 'surfaces', 'edges', 'parts', " +
                   "'geometry', and 'geometry_surfaces'. Possible source values are 'nodes', 'elements', " +
                   "'vertices', 'edges', 'surfaces', and 'parts'. Edge and surface IDs are 1-based Query IDs " +
                   "grouped by part ID.",
                   Arguments = "view: str, target: str, source: str, ids: list[int] or dict[int, list[int]]",
                   Returns = "Selection")]
        public SelectionAPI create_by_ids(string view, string target, string source, object ids)
        {
            return _service.CreateByIds(view, target, source, ids);
        }
        [PythonAPI("Highlights a Python selection in its stored view. This replaces the current interactive selection.",
                   Arguments = "selection: Selection", Returns = "None")]
        public void highlight(SelectionAPI selection)
        {
            _service.Highlight(selection);
        }
        [PythonAPI("Clears the current interactive selection and its highlight.", Returns = "None")]
        public void clear_highlight()
        {
            _service.ClearHighlight();
        }
        public override string ToString()
        {
            return "SelectionFactory(views=['geometry', 'femodel', 'results'])";
        }
    }
}
