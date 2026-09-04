// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CaeGlobals
{
    // A minimal write-only JSON emitter. It exists so that machine readable output does not require a
    // JSON package; reading JSON is intentionally not supported.
    public class JsonWriter
    {
        // Variables
        private readonly StringBuilder _text;
        private int _depth;
        private bool _isFirstItem;


        // Constructor
        public JsonWriter()
        {
            _text = new StringBuilder();
            _depth = 0;
            _isFirstItem = true;
        }


        // Methods
        public void BeginObject()
        {
            WriteItemStart();
            _text.Append('{');
            BeginScope();
        }
        public void BeginObject(string name)
        {
            WriteItemStart();
            WriteName(name);
            _text.Append('{');
            BeginScope();
        }
        public void EndObject()
        {
            EndScope('}');
        }
        public void BeginArray(string name)
        {
            WriteItemStart();
            WriteName(name);
            _text.Append('[');
            BeginScope();
        }
        public void EndArray()
        {
            EndScope(']');
        }
        public void WriteValue(string name, string value)
        {
            WriteItemStart();
            WriteName(name);
            WriteString(value);
        }
        public void WriteValue(string name, int value)
        {
            WriteItemStart();
            WriteName(name);
            _text.Append(value.ToString(CultureInfo.InvariantCulture));
        }
        public void WriteValue(string name, double value)
        {
            WriteItemStart();
            WriteName(name);
            WriteNumber(value);
        }
        public void WriteValue(string name, bool value)
        {
            WriteItemStart();
            WriteName(name);
            _text.Append(value ? "true" : "false");
        }
        public void WriteValue(string value)
        {
            WriteItemStart();
            WriteString(value);
        }
        public void SaveToFile(string fileName)
        {
            File.WriteAllText(fileName, ToString(), new UTF8Encoding(false));
        }
        public override string ToString()
        {
            return _text.ToString();
        }
        //
        private void BeginScope()
        {
            _depth++;
            _isFirstItem = true;
        }
        private void EndScope(char closingCharacter)
        {
            bool isEmpty = _isFirstItem;
            _depth--;
            if (!isEmpty) WriteNewLineAndIndent();
            _text.Append(closingCharacter);
            _isFirstItem = false;
        }
        private void WriteItemStart()
        {
            if (!_isFirstItem) _text.Append(',');
            if (_depth > 0) WriteNewLineAndIndent();
            _isFirstItem = false;
        }
        private void WriteNewLineAndIndent()
        {
            _text.Append('\n');
            _text.Append(' ', 2 * _depth);
        }
        private void WriteName(string name)
        {
            WriteString(name);
            _text.Append(": ");
        }
        private void WriteNumber(double value)
        {
            // JSON has no representation for these
            if (double.IsNaN(value) || double.IsInfinity(value)) _text.Append("null");
            else _text.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }
        private void WriteString(string value)
        {
            if (value == null)
            {
                _text.Append("null");
                return;
            }
            //
            _text.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '"') _text.Append("\\\"");
                else if (character == '\\') _text.Append("\\\\");
                else if (character == '\n') _text.Append("\\n");
                else if (character == '\r') _text.Append("\\r");
                else if (character == '\t') _text.Append("\\t");
                else if (character < ' ') _text.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                else _text.Append(character);
            }
            _text.Append('"');
        }
    }
}
