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
        private int _parallelMeshingThreads;


        // Properties                                                                                                               
        public MeshingParameters MeshingParameters { get { return _meshingParameters; } set { _meshingParameters = value; } }
        public int ParallelMeshingThreads
        {
            get { return _parallelMeshingThreads; }
            set
            {
                if (value < 0) throw new Exception("The number of parallel meshing threads must be larger or equal to 0.");
                _parallelMeshingThreads = value;
            }
        }


        // Constructors                                                                                                             
        public MeshingSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
            if (_meshingParameters == null) _meshingParameters = new MeshingParameters("Settings");
        }
        public void Reset()
        {
            if (_meshingParameters == null) CheckValues();
            else _meshingParameters.Reset();
            _parallelMeshingThreads = 1;
        }
    }
}
