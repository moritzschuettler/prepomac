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
        [PythonAPI("Highlights a Python selection in its stored view. This replaces the current interactive selection.",
                   Arguments = "selection: Selection", Returns = "None")]
        public void highlight(object selection)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            _service.Highlight(selectionValue);
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
