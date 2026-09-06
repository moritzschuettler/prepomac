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
    // One user edit of a generated CalculiX keyword. The signature is the fingerprint of the generated
    // keyword the edit was made against; it is read only, because the application computes it and uses
    // it to drop an edit whose generated keyword has changed.
    public class KeywordOverrideAPI : BaseAPI
    {
        // Variables
        private readonly int[] _indices;
        private readonly string _data;
        private readonly bool _disabled;
        private readonly bool _suppressed;
        private readonly string _originalSignature;


        // Properties
        [PythonAPI("The index path of the overridden keyword inside the generated keyword tree.", Returns = "list[int]")]
        public int[] indices { get { return (int[])_indices.Clone(); } }
        [PythonAPI("The replacement keyword text.", Returns = "str")]
        public string data { get { return _data; } }
        [PythonAPI("Whether the generated keyword is written as a comment.", Returns = "bool")]
        public bool disabled { get { return _disabled; } }
        [PythonAPI("Whether the override is suppressed by the command history.", Returns = "bool")]
        public bool suppressed { get { return _suppressed; } }
        [PythonAPI("The fingerprint of the generated keyword the override was made against.", Returns = "str")]
        public string original_signature { get { return _originalSignature; } }


        // Constructors
        internal KeywordOverrideAPI(int[] indices, string data, bool disabled, bool suppressed, string originalSignature)
        {
            _indices = indices == null ? new int[0] : (int[])indices.Clone();
            _data = data;
            _disabled = disabled;
            _suppressed = suppressed;
            _originalSignature = originalSignature;
        }


        // Methods
        public override string ToString()
        {
            return "KeywordOverride(indices=[" + string.Join(", ", _indices) + "], disabled=" + _disabled +
                   ", data=" + StringListAPI.ToDisplayString(_data) + ")";
        }
    }
}
