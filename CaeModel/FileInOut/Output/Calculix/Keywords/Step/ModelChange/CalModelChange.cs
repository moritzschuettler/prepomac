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
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using System.Data.Common;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal enum  ModelChangeTypeEnum
    {
        ContactPair,
        Element
    }
    [Serializable]
    internal enum ModelChangeParameterEnum
    {
        Add,
        Remove
    }
    [Serializable]
    internal class CalModelChange : CalculixKeyword
    {
        // Variables                                                                                                                
        private ModelChangeTypeEnum _modelChangeType;
        private ModelChangeParameterEnum _modelChangeParameter;
        private string _elementSetName;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalModelChange(ModelChangeTypeEnum modelChangeType, ModelChangeParameterEnum modelChangeParameter,
                              string elementSetName)
        {
            _modelChangeType = modelChangeType;
            _modelChangeParameter = modelChangeParameter;
            _elementSetName = elementSetName;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Model change, Type={0}, {1}{2}", _modelChangeType, _modelChangeParameter, Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return _elementSetName + Environment.NewLine;
        }
    }
}
