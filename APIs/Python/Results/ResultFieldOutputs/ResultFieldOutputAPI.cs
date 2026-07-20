// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultFieldOutputAPI : BaseAPI
    {
        // Variables
        protected readonly string _resultName;
        protected readonly string _fieldOutputName;
        private readonly ResultFieldOutputService _service;


        // Properties
        [PythonAPI("The field output name.", Returns = "str")]
        public string name { get { return _fieldOutputName; } }
        [PythonAPI("The field output definition type.", Returns = "str")]
        public string type_name { get { return Resolve().GetType().Name; } }
        [PythonAPI("The first field-output filter, or None when it is disabled.",
                   Returns = "FieldOutputFilter or None")]
        public ResultFieldOutputFilterAPI filter1
        {
            get { return ResultFieldOutputFilterAPI.FromBase(Resolve().Filter1); }
        }
        [PythonAPI("The second field-output filter, or None when it is disabled.",
                   Returns = "FieldOutputFilter or None")]
        public ResultFieldOutputFilterAPI filter2
        {
            get { return ResultFieldOutputFilterAPI.FromBase(Resolve().Filter2); }
        }


        // Constructors
        internal ResultFieldOutputAPI(string resultName, string fieldOutputName, ResultFieldOutputService service)
        {
            _resultName = resultName;
            _fieldOutputName = fieldOutputName;
            _service = service;
        }


        // Methods
        protected ResultFieldOutput Resolve()
        {
            return _service.ResolveFieldOutput(_resultName, _fieldOutputName);
        }
        public override string ToString()
        {
            return "FieldOutput(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", type=" + StringListAPI.ToDisplayString(type_name) + ")";
        }
    }
}
