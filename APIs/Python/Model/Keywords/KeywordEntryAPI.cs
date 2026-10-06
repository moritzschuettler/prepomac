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
    // One CalculiX keyword addressed by the index path of the generated keyword tree. The same shape
    // describes a generated keyword and a user keyword, because a user keyword is inserted at the
    // generated path the user picked in the keyword editor.
    public class KeywordEntryAPI : BaseAPI
    {
        // Variables
        private readonly int[] _indices;
        private readonly string _data;


        // Properties
        [PythonAPI("The index path of the keyword inside the generated keyword tree.", Returns = "IntList")]
        public IntListAPI indices { get { return new IntListAPI(_indices); } }
        [PythonAPI("The complete keyword text.", Returns = "str")]
        public string data { get { return _data; } }


        // Constructors
        internal KeywordEntryAPI(int[] indices, string data)
        {
            _indices = (int[])indices.Clone();
            _data = data;
        }


        // Methods
        public override string ToString()
        {
            return "KeywordEntry(indices=[" + string.Join(", ", _indices) + "], data=" +
                   StringListAPI.ToDisplayString(_data) + ")";
        }
    }
}
