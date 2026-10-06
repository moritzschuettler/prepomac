// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    public class ResultFieldOutputAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(ResultFieldOutputLimit), "limit" },
            { typeof(ResultFieldOutputEnvelope), "envelope" },
            { typeof(ResultFieldOutputEquation), "equation" },
            { typeof(ResultFieldOutputCoordinateSystemTransform), "coordinate_system_transform" }
        };
        protected readonly string _resultName;
        protected readonly string _fieldOutputName;
        private readonly ResultFieldOutputService _service;


        // Properties
        [PythonAPI("The field output name.", Returns = "str")]
        public string name { get { return _fieldOutputName; } }
        [PythonAPI("The field output type, named after the add_* method that makes it without the add_ prefix: " +
                   "limit, envelope, equation or coordinate_system_transform.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
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


        // Static methods
        internal static string GetItemType(ResultFieldOutput fieldOutput)
        {
            string itemType;
            if (ItemTypes.TryGetValue(fieldOutput.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The field output '" + fieldOutput.Name + "' is a " +
                                         fieldOutput.GetType().Name + ", which no add_* method of the " +
                                         "Python API makes.");
        }


        // Methods
        protected ResultFieldOutput Resolve()
        {
            return _service.ResolveFieldOutput(_resultName, _fieldOutputName);
        }
        public override string ToString()
        {
            return "FieldOutput(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", type=" + StringListAPI.ToDisplayString(item_type) + ")";
        }
    }
}
