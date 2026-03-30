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

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class AbqElement : CalElement
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqElement(CalElement calElement)
            : base(calElement)
        {
            ConvertElementTypeToAbaqus();
        }
        public AbqElement(string elementType, string elementSetName, List<FeElement> elements,
                          ConvertPyramidsToEnum convertPyramidsTo)
            : base(elementType, elementSetName, elements, convertPyramidsTo)
        {
            ConvertElementTypeToAbaqus();
        }

        // Methods                                                                                                                  
        private void ConvertElementTypeToAbaqus()
        {
            if (_elementType == "B31R") _elementType = "B31";
            else if (_elementType == "B32R") _elementType = "B32";
            else if (_elementType == "S6") _elementType = "STRI65";
            else if (_elementType == "S8") _elementType = "S8R";
            else if (_elementType == "C3D10T") _elementType = "C3D10";
        }

    }
}
