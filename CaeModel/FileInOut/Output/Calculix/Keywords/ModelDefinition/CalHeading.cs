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
        ModelSpaceEnum _modelSpace;
        static private readonly Regex _hashValueRegex = new Regex(@"\b(Hash:[ \t]*)[^,\r\n]*", RegexOptions.IgnoreCase);
        static private readonly Regex _dateValueRegex = new Regex(@"\b(Date:[ \t]*)[^,\r\n]*", RegexOptions.IgnoreCase);
        static private readonly Regex _modelSpaceValueRegex = new Regex(@"\b(Model space:[ \t]*)[^,\r\n]*",
                                                                        RegexOptions.IgnoreCase);
        static private readonly Regex _hashLineRegex = new Regex(@"^[^\r\n]*\bHash:[^\r\n]*(\r\n|\n|\r)?",
                                                                 RegexOptions.IgnoreCase | RegexOptions.Multiline);
        static private readonly Regex _headingLineRegex = new Regex(@"^[ \t]*\*Heading[^\r\n]*(\r\n|\n|\r)?",
                                                                    RegexOptions.IgnoreCase | RegexOptions.Multiline);


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalHeading(string modelName, string hashName, UnitSystemType unitSystemType, ModelSpaceEnum modelSpace)
        {
            _modelName = modelName;
            _hashName = hashName;
            _unitSystemType = unitSystemType;
            _modelSpace = modelSpace;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Heading{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            return GetDataString(_hashName, DateTime.Now.ToShortDateString()) + GetModelSpaceDataString();
        }
        // The hash of a new model and the date of the day change on their own, so a user edit of the heading
        // is compared and written without them, and the hash of the model is what maps results onto it. The model
        // space line tells the results file of an axisymmetric model apart; it is written like the hash and left out
        // of the comparison, so the edits made before it was written still apply
        public string GetDataStringWithoutGeneratedValues()
        {
            return GetDataString("", "");
        }
        public string ApplyGeneratedValues(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            //
            bool hasModelSpace = _modelSpaceValueRegex.IsMatch(text);
            if (hasModelSpace) text = _modelSpaceValueRegex.Replace(text, match => match.Groups[1].Value + _modelSpace, 1);
            //
            Match line;
            string missingData;
            if (_hashValueRegex.IsMatch(text))
            {
                string date = DateTime.Now.ToShortDateString();
                text = _hashValueRegex.Replace(text, match => match.Groups[1].Value + _hashName, 1);
                text = _dateValueRegex.Replace(text, match => match.Groups[1].Value + date, 1);
                if (hasModelSpace) return text;
                //
                line = _hashLineRegex.Match(text);
                missingData = GetModelSpaceDataString();
            }
            else
            {
                line = _headingLineRegex.Match(text);
                if (!line.Success) return text;
                missingData = hasModelSpace ? GetDataString(_hashName, DateTime.Now.ToShortDateString()) : GetDataString();
            }
            string newLine = line.Groups[1].Length == 0 ? Environment.NewLine : "";
            return text.Insert(line.Index + line.Length, newLine + missingData);
        }
        private string GetDataString(string hashName, string date)
        {
            return string.Format("Hash: {0}, Date: {1}, Unit system: {2}{3}",
                                 hashName, date, _unitSystemType, Environment.NewLine);
        }
        private string GetModelSpaceDataString()
        {
            return string.Format("Model space: {0}{1}", _modelSpace, Environment.NewLine);
        }
    }
}
