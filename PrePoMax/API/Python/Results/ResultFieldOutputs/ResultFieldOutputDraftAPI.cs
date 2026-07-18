// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace PrePoMax.API.Python
{
    public abstract class ResultFieldOutputDraftAPI : BaseAPI
    {
        // Variables
        private readonly string _name;
        private ResultFieldOutputFilterAPI _filter1;
        private ResultFieldOutputFilterAPI _filter2;


        // Properties
        [PythonAPI("The field output name.", Returns = "str")]
        public string name { get { return _name; } }
        [PythonAPI("The optional first field-output filter.", Returns = "FieldOutputFilter or None")]
        public ResultFieldOutputFilterAPI filter1
        {
            get { return _filter1; }
            set { _filter1 = value; }
        }
        [PythonAPI("The optional second field-output filter.", Returns = "FieldOutputFilter or None")]
        public ResultFieldOutputFilterAPI filter2
        {
            get { return _filter2; }
            set { _filter2 = value; }
        }


        // Constructors
        protected ResultFieldOutputDraftAPI(string name)
        {
            _name = name;
        }


        // Methods
        internal ResultFieldOutput CreateBase()
        {
            if (_filter1 == null && _filter2 != null)
                throw new PythonAPIException("The draft filter2 cannot be used without filter1.");
            ResultFieldOutput fieldOutput = CreateFieldOutput();
            if (_filter1 != null) fieldOutput.Filter1 = _filter1.CreateBase();
            if (_filter2 != null) fieldOutput.Filter2 = _filter2.CreateBase();
            return fieldOutput;
        }
        protected abstract ResultFieldOutput CreateFieldOutput();
        public override string ToString()
        {
            return "FieldOutputDraft(name=" + StringListAPI.ToDisplayString(_name) + ")";
        }
    }
}
