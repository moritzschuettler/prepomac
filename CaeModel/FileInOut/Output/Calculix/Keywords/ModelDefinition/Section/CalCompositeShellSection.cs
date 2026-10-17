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
using System.Text;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalCompositeShellSection : CalculixKeyword
    {
        // Variables                                                                                                                
        private CompositeShellSection _section;
        private string _elementSetName;
        private string[] _layerOrientationNames;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalCompositeShellSection(CompositeShellSection section, string elementSetName, string[] layerOrientationNames)
        {
            _section = section;
            _elementSetName = elementSetName;
            _layerOrientationNames = layerOrientationNames;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Shell section, Elset={0}, Composite", _elementSetName);
            if (!double.IsNaN(_section.Offset.Value)) sb.AppendFormat(", Offset={0}", _section.Offset.Value.ToCalculiX16String());
            sb.AppendLine();
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            List<CompositeLayer> layers = _section.GetSolverLayers();
            for (int i = 0; i < layers.Count; i++)
            {
                sb.Append(layers[i].Thickness.Value.ToCalculiX16String()).Append(", , ").Append(layers[i].MaterialName);
                if (_layerOrientationNames != null && _layerOrientationNames[i] != null)
                    sb.Append(", ").Append(_layerOrientationNames[i]);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
