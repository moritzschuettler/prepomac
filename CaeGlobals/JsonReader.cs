// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CaeGlobals
{
    // The reading half of JsonWriter. A parsed document is a tree of Dictionary<string, object>,
    // List<object>, string, double, bool and null, so a caller reads the fields it needs by name;
    // there is no schema binding and no reflection. It parses the protocol the automation server
    // defines rather than arbitrary documents, so the accepted grammar is plain RFC 8259 with no
    // extensions: no comments, no trailing commas and no unquoted names.
    public class JsonReader
    {
        // Variables                                                                                                                
        private const int MaximumDepth = 64;
        private readonly string _text;
        private int _position;


        // Constructor                                                                                                              
        private JsonReader(string text)
        {
            _text = text == null ? "" : text;
            _position = 0;
        }


        // Static methods                                                                                                           
        public static object Parse(string text)
        {
            JsonReader reader = new JsonReader(text);
            reader.SkipWhiteSpace();
            object value = reader.ReadValue(0);
            reader.SkipWhiteSpace();
            if (reader._position != reader._text.Length) throw reader.Error("Unexpected text after the value");
            return value;
        }
        public static bool TryParse(string text, out object value, out string error)
        {
            try
            {
                value = Parse(text);
                error = null;
                return true;
            }
            catch (CaeException exception)
            {
                value = null;
                error = exception.Message;
                return false;
            }
        }
        // The accessors a caller uses on a parsed document. A missing or mistyped item is the default
        // value, because a protocol reader decides itself which of its fields are required.
        public static Dictionary<string, object> GetObject(object node, string name)
        {
            return GetItem(node, name) as Dictionary<string, object>;
        }
        public static List<object> GetArray(object node, string name)
        {
            return GetItem(node, name) as List<object>;
        }
        public static string GetString(object node, string name, string defaultValue)
        {
            object item = GetItem(node, name);
            if (item is string) return (string)item;
            return defaultValue;
        }
        public static double GetDouble(object node, string name, double defaultValue)
        {
            object item = GetItem(node, name);
            if (item is double) return (double)item;
            return defaultValue;
        }
        public static int GetInt(object node, string name, int defaultValue)
        {
            object item = GetItem(node, name);
            if (item is double)
            {
                double value = (double)item;
                if (value >= int.MinValue && value <= int.MaxValue) return (int)Math.Round(value);
            }
            return defaultValue;
        }
        public static bool GetBool(object node, string name, bool defaultValue)
        {
            object item = GetItem(node, name);
            if (item is bool) return (bool)item;
            return defaultValue;
        }
        public static bool HasItem(object node, string name)
        {
            Dictionary<string, object> items = node as Dictionary<string, object>;
            return items != null && items.ContainsKey(name);
        }
        public static List<string> ToStringList(List<object> values)
        {
            List<string> names = new List<string>();
            if (values == null) return names;
            foreach (object value in values)
            {
                if (value is string) names.Add((string)value);
                else if (value == null) names.Add(null);
                else names.Add(ToText(value));
            }
            return names;
        }
        private static object GetItem(object node, string name)
        {
            Dictionary<string, object> items = node as Dictionary<string, object>;
            if (items == null) return null;
            object item;
            if (!items.TryGetValue(name, out item)) return null;
            return item;
        }
        private static int HexDigit(char character)
        {
            if (character >= '0' && character <= '9') return character - '0';
            else if (character >= 'a' && character <= 'f') return 10 + character - 'a';
            else if (character >= 'A' && character <= 'F') return 10 + character - 'A';
            return -1;
        }
        private static string ToText(object value)
        {
            if (value == null) return "null";
            if (value is bool) return (bool)value ? "true" : "false";
            if (value is double) return ((double)value).ToString("R", CultureInfo.InvariantCulture);
            return value.ToString();
        }


        // Methods                                                                                                                  
        private object ReadValue(int depth)
        {
            if (depth > MaximumDepth) throw Error("The document is nested deeper than " + MaximumDepth + " levels");
            if (_position >= _text.Length) throw Error("A value was expected");
            //
            char character = _text[_position];
            if (character == '{') return ReadObject(depth);
            else if (character == '[') return ReadArray(depth);
            else if (character == '"') return ReadString();
            else if (character == 't') { ReadLiteral("true"); return true; }
            else if (character == 'f') { ReadLiteral("false"); return false; }
            else if (character == 'n') { ReadLiteral("null"); return null; }
            else if (character == '-' || (character >= '0' && character <= '9')) return ReadNumber();
            //
            throw Error("A value was expected");
        }
        private Dictionary<string, object> ReadObject(int depth)
        {
            Dictionary<string, object> items = new Dictionary<string, object>();
            _position++;
            SkipWhiteSpace();
            if (Peek() == '}') { _position++; return items; }
            //
            while (true)
            {
                SkipWhiteSpace();
                if (Peek() != '"') throw Error("A name was expected");
                string name = ReadString();
                SkipWhiteSpace();
                if (Peek() != ':') throw Error("A colon was expected after the name");
                _position++;
                SkipWhiteSpace();
                items[name] = ReadValue(depth + 1);
                SkipWhiteSpace();
                char character = Peek();
                if (character == ',') { _position++; continue; }
                if (character == '}') { _position++; return items; }
                throw Error("A comma or a closing brace was expected");
            }
        }
        private List<object> ReadArray(int depth)
        {
            List<object> values = new List<object>();
            _position++;
            SkipWhiteSpace();
            if (Peek() == ']') { _position++; return values; }
            //
            while (true)
            {
                SkipWhiteSpace();
                values.Add(ReadValue(depth + 1));
                SkipWhiteSpace();
                char character = Peek();
                if (character == ',') { _position++; continue; }
                if (character == ']') { _position++; return values; }
                throw Error("A comma or a closing bracket was expected");
            }
        }
        private string ReadString()
        {
            StringBuilder value = new StringBuilder();
            _position++;
            while (true)
            {
                if (_position >= _text.Length) throw Error("The string is not closed");
                char character = _text[_position++];
                if (character == '"') return value.ToString();
                else if (character < ' ') throw Error("A control character is not allowed in a string");
                else if (character != '\\') value.Append(character);
                else value.Append(ReadEscape());
            }
        }
        private char ReadEscape()
        {
            if (_position >= _text.Length) throw Error("The escape sequence is not complete");
            char character = _text[_position++];
            if (character == '"') return '"';
            else if (character == '\\') return '\\';
            else if (character == '/') return '/';
            else if (character == 'b') return '\b';
            else if (character == 'f') return '\f';
            else if (character == 'n') return '\n';
            else if (character == 'r') return '\r';
            else if (character == 't') return '\t';
            else if (character != 'u') throw Error("Unknown escape sequence \\" + character);
            //
            if (_position + 4 > _text.Length) throw Error("The \\u escape sequence is not complete");
            int code = 0;
            for (int i = 0; i < 4; i++)
            {
                int digit = HexDigit(_text[_position + i]);
                if (digit < 0) throw Error("The \\u escape sequence is not hexadecimal");
                code = 16 * code + digit;
            }
            _position += 4;
            return (char)code;
        }
        private double ReadNumber()
        {
            int start = _position;
            if (Peek() == '-') _position++;
            ReadDigits();
            if (Peek() == '.')
            {
                _position++;
                ReadDigits();
            }
            char exponent = Peek();
            if (exponent == 'e' || exponent == 'E')
            {
                _position++;
                char sign = Peek();
                if (sign == '+' || sign == '-') _position++;
                ReadDigits();
            }
            //
            string text = _text.Substring(start, _position - start);
            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw Error("The number " + text + " cannot be read");
            return value;
        }
        private void ReadDigits()
        {
            int start = _position;
            while (_position < _text.Length && _text[_position] >= '0' && _text[_position] <= '9') _position++;
            if (_position == start) throw Error("A digit was expected");
        }
        private void ReadLiteral(string literal)
        {
            if (_position + literal.Length > _text.Length ||
                string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
            {
                throw Error("The literal " + literal + " was expected");
            }
            _position += literal.Length;
        }
        private char Peek()
        {
            if (_position >= _text.Length) return '\0';
            return _text[_position];
        }
        private void SkipWhiteSpace()
        {
            while (_position < _text.Length)
            {
                char character = _text[_position];
                if (character == ' ' || character == '\t' || character == '\n' || character == '\r') _position++;
                else break;
            }
        }
        private CaeException Error(string message)
        {
            return new CaeException(message + " at position " + _position.ToString(CultureInfo.InvariantCulture) + ".");
        }
    }
}
