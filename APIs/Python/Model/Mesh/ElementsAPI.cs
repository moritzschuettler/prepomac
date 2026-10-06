// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python.Model
{
    public class ElementsAPI : BaseAPI
    {
        // Variables
        private readonly MeshEditService _service;


        // Properties
        [PythonAPI("The number of elements in the model mesh.", Returns = "int")]
        public int count { get { return _service.GetElementCount(); } }


        // Constructors
        internal ElementsAPI(MeshEditService service)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Renumbers the model elements using command history.", Arguments = "start_id: int",
                   Returns = "None")]
        public void renumber(int start_id)
        {
            _service.RenumberElements(start_id);
        }

        [PythonAPI("Returns a copied bulk mesh snapshot ordered by ID.", Returns = "ElementData")]
        public ElementDataAPI get_data()
        {
            return _service.GetElementData();
        }
        [PythonAPI("Returns all model mesh IDs in ascending order.", Returns = "IntList")]
        public IntListAPI get_ids()
        {
            return get_data().ids;
        }
        public override string ToString()
        {
            return "Elements(count=" + count + ")";
        }
    }
}
