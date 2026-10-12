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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CaeGlobals;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalHeading : CalculixKeyword
    {
        // Variables                                                                                                                
        string _modelName;
        string _hashName;
        UnitSystemType _unitSystemType;
        static private readonly Regex _hashValueRegex = new Regex(@"\b(Hash:[ \t]*)[^,\r\n]*", RegexOptions.IgnoreCase);
        static private readonly Regex _dateValueRegex = new Regex(@"\b(Date:[ \t]*)[^,\r\n]*", RegexOptions.IgnoreCase);
        static private readonly Regex _headingLineRegex = new Regex(@"^[ \t]*\*Heading[^\r\n]*(\r\n|\n|\r)?",
                                                                    RegexOptions.IgnoreCase | RegexOptions.Multiline);


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalHeading(string modelName, string hashName, UnitSystemType unitSystemType)
        {
            _modelName = modelName;
            _hashName = hashName;
            _unitSystemType = unitSystemType;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Heading{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            return GetDataString(_hashName, DateTime.Now.ToShortDateString());
        }
        // The hash of a new model and the date of the day change on their own, so a user edit of the heading
        // is compared and written without them, and the hash of the model is what maps results onto it
        public string GetDataStringWithoutGeneratedValues()
        {
            return GetDataString("", "");
        }
        public string ApplyGeneratedValues(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            //
            if (_hashValueRegex.IsMatch(text))
            {
                string date = DateTime.Now.ToShortDateString();
                text = _hashValueRegex.Replace(text, match => match.Groups[1].Value + _hashName, 1);
                return _dateValueRegex.Replace(text, match => match.Groups[1].Value + date, 1);
            }
            //
            Match headingLine = _headingLineRegex.Match(text);
            if (!headingLine.Success) return text;
            string newLine = headingLine.Groups[1].Length == 0 ? Environment.NewLine : "";
            return text.Insert(headingLine.Index + headingLine.Length, newLine + GetDataString());
        }
        private string GetDataString(string hashName, string date)
        {
            return string.Format("Hash: {0}, Date: {1}, Unit system: {2}{3}",
                                 hashName, date, _unitSystemType, Environment.NewLine);
        }
    }
}
