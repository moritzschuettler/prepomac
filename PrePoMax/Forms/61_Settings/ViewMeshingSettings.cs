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
using CaeMesh;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewMeshingSettings : Forms.ViewMeshingParameters, IViewSettings, IReset
    {
        // Variables                                                                                                                
        MeshingSettings _meshingSettings;


        // Properties                                                                                                               
        [CategoryAttribute("Parallelization")]
        [OrderedDisplayName(0, 10, "Number of threads")]
        [DescriptionAttribute("Set the number of threads used for Netgen meshing. Select Auto to use the automatic " +
                              "worker count, 1 for serial meshing, or a larger value for an explicit worker limit.")]
        [TypeConverter(typeof(ParallelMeshingThreadsConverter))]
        [Id(1, 7)]
        public int ParallelMeshingThreads
        {
            get { return _meshingSettings.ParallelMeshingThreads; }
            set { _meshingSettings.ParallelMeshingThreads = value; }
        }


        // Constructors                                                                                                             
        public ViewMeshingSettings(MeshingSettings meshingSettings)
        {
            _meshingSettings = meshingSettings;
            _settingsView = true;
            //
            SetBase(_meshingSettings.MeshingParameters);
            //
            _dctd.GetProperty(nameof(AdvancedView)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(Name)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(MaxH)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(MinH)).SetIsBrowsable(false);
        }


        // Methods                                                                                                                  
        new public ISettings GetBase()
        {
            return _meshingSettings;
        }
        public void Reset()
        {
            _meshingSettings.Reset();
            UpdateVisibility(); // apply settings view
        }
    }
}
