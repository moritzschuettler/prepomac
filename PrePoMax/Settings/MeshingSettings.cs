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
using System.ComponentModel;
using CaeGlobals;
using System.IO;
using DynamicTypeDescriptor;
using System.Drawing;
using CaeMesh;

namespace PrePoMax
{
    [Serializable]
    public class MeshingSettings : ISettings
    {
        // Variables                                                                                                                
        private MeshingParameters _meshingParameters;


        // Properties                                                                                                               
        public MeshingParameters MeshingParameters { get { return _meshingParameters; } set { _meshingParameters = value; } }


        // Constructors                                                                                                             
        public MeshingSettings()
        {
            _meshingParameters = new MeshingParameters("Settings");
        }
        public MeshingSettings(MeshingParameters meshingParameters)
        {
            _meshingParameters = meshingParameters;
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _meshingParameters.Reset();
        }
        
    }
}
