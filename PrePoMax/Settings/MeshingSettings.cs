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
using System.Runtime.Serialization;
using DynamicTypeDescriptor;
using System.Drawing;
using CaeMesh;

namespace PrePoMax
{
    [Serializable]
    public class MeshingSettings : ISettings
    {
        // Variables                                                                                                                
        public const int MinMesherTimeLimit = 1;
        public const int MaxMesherTimeLimit = 10080;
        public const int DefaultMesherTimeLimit = 30;
        private MeshingParameters _meshingParameters;
        private int _parallelMeshingThreads;
        private int _mesherTimeLimit;


        // Properties                                                                                                               
        public MeshingParameters MeshingParameters { get { return _meshingParameters; } set { _meshingParameters = value; } }
        public int ParallelMeshingThreads
        {
            get { return _parallelMeshingThreads; }
            set
            {
                if (value < 1) throw new Exception("The number of parallel meshing threads must be larger than 0.");
                _parallelMeshingThreads = value;
            }
        }
        // In minutes, for one run of a mesher executable
        public int MesherTimeLimit
        {
            get { return _mesherTimeLimit; }
            set { _mesherTimeLimit = Math.Min(MaxMesherTimeLimit, Math.Max(MinMesherTimeLimit, value)); }
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
        // The time limit was added after the settings file format was already in use, and deserialization does
        // not run the constructor, so a file written by an older version leaves it at zero
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_mesherTimeLimit < MinMesherTimeLimit) _mesherTimeLimit = DefaultMesherTimeLimit;
        }
        public void Reset()
        {
            if (_meshingParameters == null) CheckValues();
            else _meshingParameters.Reset();
            _parallelMeshingThreads = 1;
            _mesherTimeLimit = DefaultMesherTimeLimit;
        }
    }
}
