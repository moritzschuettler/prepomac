// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewSurfaceExtraction
    {
        // Variables                                                                                                                
        public const string RegionSelectionName = "RegionSelection";
        //
        private SurfaceExtraction _surfaceExtraction;
        private DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Surfaces")]
        [DescriptionAttribute("Select the surfaces to extract. Hold the Shift key to select more than one " +
                              "surface. The surfaces of one part become one new shell part and the part they " +
                              "were taken from is left as it was, a sub-part of a compound included.")]
        [Id(1, 1)]
        public string RegionSelection
        {
            get
            {
                int count = _surfaceExtraction.CreationIds == null ? 0 : _surfaceExtraction.CreationIds.Length;
                if (count <= 0) return "Empty";
                else if (count == 1) return "1 surface";
                else return count + " surfaces";
            }
            set { }
        }
        //
        [CategoryAttribute("Options")]
        [OrderedDisplayName(0, 10, "Offset")]
        [DescriptionAttribute("Enter the distance the extracted surfaces are moved along their own normals. " +
                              "A positive value moves them out of the body they were taken from and a negative " +
                              "value into it, so half the wall thickness entered as a negative value on an outer " +
                              "surface gives a mid-surface. Leave it at zero to extract the surfaces as they are.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 2)]
        public double Offset { get { return _surfaceExtraction.Offset; } set { _surfaceExtraction.Offset = value; } }


        // Constructors                                                                                                             
        public ViewSurfaceExtraction(SurfaceExtraction surfaceExtraction)
        {
            _surfaceExtraction = surfaceExtraction;             // 1 command
            _dctd = ProviderInstaller.Install(this);            // 2 command
        }


        // Methods                                                                                                                  
        public SurfaceExtraction GetBase()
        {
            return _surfaceExtraction;
        }
    }
}
