// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace DynamicTypeDescriptor
{
    [Serializable]
    [PmxType(1017)]
    public class MultiChoiceContainer
    {
        // Variables                                                                                                                
        [PmxKey(0)] private MultiChoiceEnum _multiChoice;
        [PmxKey(1)] Dictionary<MultiChoiceEnum, string[]> _enumData;
        [PmxKey(2)] Dictionary<MultiChoiceEnum, string> _enumNames;


        // Properties                                                                                                               
        public MultiChoiceEnum MultiChoice { get { return _multiChoice; } set { _multiChoice = value; } }
        public string[] Names
        {
            get
            {
                List<string> names = new List<string>();
                foreach (var entry in _enumNames)
                {
                    if (_multiChoice.HasFlag(entry.Key)) names.Add(entry.Value);
                }
                return names.ToArray();
            }
        }
        public Dictionary<MultiChoiceEnum, string[]> EnumData { get { return _enumData; } }


        // Constructors                                                                                                             
        public MultiChoiceContainer(string[] valueNames, string[] selectedNames)
        {
            if (valueNames == null || selectedNames == null) throw new NotSupportedException();
            //
            MultiChoiceEnum[] enumValues = Enum.GetValues(typeof(MultiChoiceEnum)).Cast<MultiChoiceEnum>().ToArray();
            _enumData = new Dictionary<MultiChoiceEnum, string[]>();
            _enumNames = new Dictionary<MultiChoiceEnum, string>();
            for (int i = 0; i < valueNames.Length; i++)
            {
                _enumData.Add(enumValues[i], new string[] { valueNames[i], valueNames[i] });
                _enumNames.Add(enumValues[i], valueNames[i]);
            }
            // Select
            if (selectedNames.Length > 0)
            {
                _multiChoice = 0;
                HashSet<string> namesHash = new HashSet<string>(selectedNames);
                foreach (var entry in _enumNames)
                {
                    if (namesHash.Contains(entry.Value)) _multiChoice |= entry.Key;
                }
            }
            else throw new NotSupportedException();
        }


        // Methods                                                                                                                  
        
    }
}
