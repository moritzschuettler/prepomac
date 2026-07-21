// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Linq;
using System.Text;

namespace FileInOut.Output.Calculix
{
    /// <summary>
    /// A user edit of one generated CalculiX keyword. The index path addresses the generated keyword
    /// tree before user keywords are inserted. The original signature prevents an edit from being
    /// applied to a different generated keyword after the model has changed.
    /// </summary>
    [Serializable]
    public class CalculixKeywordOverride
    {
        private int[] _indices;
        private string _originalSignature;
        private string _data;
        private bool _disabled;

        public int[] Indices { get { return _indices == null ? null : (int[])_indices.Clone(); } }
        public string OriginalSignature { get { return _originalSignature; } }
        public string Data { get { return _data; } set { _data = value; } }
        public bool Disabled { get { return _disabled; } set { _disabled = value; } }

        public CalculixKeywordOverride(int[] indices, string originalSignature)
        {
            _indices = indices == null ? null : (int[])indices.Clone();
            _originalSignature = originalSignature;
        }

        public bool HasSameIndices(int[] indices)
        {
            if (_indices == null || indices == null) return _indices == indices;
            return _indices.SequenceEqual(indices);
        }
    }

    /// <summary>
    /// A transient rendered keyword used only while writing. It keeps the original child keyword list
    /// so one generated fragment can be overridden independently.
    /// </summary>
    [Serializable]
    internal class CalKeywordOverride : CalculixKeyword
    {
        private string _originalData;
        private string _replacementData;
        private bool _disabled;

        public CalKeywordOverride(CalculixKeyword originalKeyword, string originalData,
                                  string replacementData, bool disabled)
        {
            _originalData = originalData;
            _replacementData = replacementData;
            _disabled = disabled;
            foreach (var childKeyword in originalKeyword.Keywords) _keywords.Add(childKeyword);
        }

        public override string GetKeywordString()
        {
            string data = _replacementData ?? _originalData;
            if (_disabled) return Comment(data);
            return AppendNewLine(data);
        }
        public override string GetDataString()
        {
            return "";
        }

        private static string AppendNewLine(string data)
        {
            if (string.IsNullOrEmpty(data)) return "";
            if (data.EndsWith("\r") || data.EndsWith("\n")) return data;
            return data + Environment.NewLine;
        }
        private static string Comment(string data)
        {
            if (string.IsNullOrEmpty(data)) return "** Disabled by CalculiX keyword editor" + Environment.NewLine;

            string[] lines = data.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            StringBuilder sb = new StringBuilder();
            int count = lines.Length;
            if (count > 0 && lines[count - 1].Length == 0) count--;
            for (int i = 0; i < count; i++) sb.Append("**").AppendLine(lines[i]);
            return sb.ToString();
        }
    }
}
