// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeResults
{
    // A tensor field turned into the material axes of the shell elements: axes 1 and 2 of the material orientation, or
    // of the ply of a composite shell, and axis 3 along the shell normal
    [Serializable]
    public class ResultFieldOutputMaterialAxes : ResultFieldOutput
    {
        // Variables
        private string _fieldName;
        private string[] _componentNames;


        // Properties
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }


        // Constructor
        public ResultFieldOutputMaterialAxes(string name, string fieldName)
            : base(name)
        {
            _fieldName = fieldName;
            _componentNames = null;
        }


        // Methods
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
        public void SetComponentNames(string[] componentNames)
        {
            _componentNames = componentNames;
        }
        public override string[] GetComponentNames()
        {
            return _componentNames;
        }
    }
}
