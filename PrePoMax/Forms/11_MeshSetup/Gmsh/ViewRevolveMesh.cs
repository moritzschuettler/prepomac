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
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewRevolveMesh : ViewGmshSetupItem
    {
        // Variables                                                                                                                
        private RevolveMesh _revolveMesh;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Source surfaces")]
        [DescriptionAttribute("Select the source surfaces of the revolution, or select the part and let the source " +
                              "surfaces be found automatically.")]
        [Id(2, 1)]
        public RevolveSourceTypeEnum SourceType
        {
            get { return _revolveMesh.SourceType; }
            set { _revolveMesh.SourceType = value; }
        }
        //

        [CategoryAttribute("Full revolution")]
        [OrderedDisplayName(0, 10, "Create 360° model")]
        [DescriptionAttribute("Create a full 360° model by repeating the revolved mesh around the axis. " +
                              "Available when the revolution angle divides 360° into an integer number of sectors " +
                              "(e.g. 180°, 120°, 90°, 60°, 45°).")]
        [Id(1, 10)]
        public bool CreateFull360Model
        {
            get { return _revolveMesh.CreateFull360Model; }
            set { _revolveMesh.CreateFull360Model = value; }
        }

        // Constructors                                                                                                             
        public ViewRevolveMesh(RevolveMesh revolveMesh)
        {
            _revolveMesh = revolveMesh;
            SetBase(_revolveMesh);
            //
            _dctd.GetProperty(nameof(AlgorithmMesh3D)).SetIsBrowsable(false);
            //
            _dctd.GetProperty(nameof(OptimizeFirstOrderShell)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(OptimizeFirstOrderSolid)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(OptimizeHighOrder)).SetIsBrowsable(false);
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(CreateFull360Model));
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public override MeshSetupItem GetBase()
        {
            return _revolveMesh;
        }
        public override void UpdateVisibility()
        {
            base.UpdateVisibility();
            // The revolution angle is determined while validating the selected source faces. The full 360 degree
            // option is offered whenever that angle divides 360 degrees into an integer number of equal sectors.
            _dctd.GetProperty(nameof(CreateFull360Model)).SetIsBrowsable(_revolveMesh.CanCreateFull360Model);
        }
    }
}

