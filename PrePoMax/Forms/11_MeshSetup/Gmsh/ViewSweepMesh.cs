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
    public class ViewSweepMesh : ViewGmshSetupItem
    {
        // Variables                                                                                                                
        private SweepMesh _sweepMesh;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Source surfaces")]
        [DescriptionAttribute("Select the source surfaces of the sweep, or select the part and let the source " +
                              "surfaces be found automatically.")]
        [Id(2, 1)]
        public SweepSourceTypeEnum SourceType
        {
            get { return _sweepMesh.SourceType; }
            set { _sweepMesh.SourceType = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Keep imprint edges")]
        [DescriptionAttribute("An imprint that splits one end of a swept prism inside its boundary is carried to " +
                              "the other end, and the mesh follows it there. Keep the carried edges as edges of the " +
                              "mesh, so that the faces they cross stay split along them.")]
        [Id(3, 1)]
        public bool KeepImprintEdges
        {
            get { return _sweepMesh.KeepImprintEdges; }
            set { _sweepMesh.KeepImprintEdges = value; }
        }
        //
        [CategoryAttribute("Mesh smoothing")]
        [OrderedDisplayName(0, 10, "Global smooth steps")]
        [DescriptionAttribute("Enter the number of Laplacian smooth steps for the final global mesh smoothing. " +
                              "The sweep layers are already smooth without it, so leave it at 0 unless the target " +
                              "surface is strongly inclined or curved.")]
        [Id(1, 10)]
        public int NumberOfGlobalSmoothSteps
        {
            get { return _sweepMesh.NumberOfGlobalSmoothSteps; }
            set { _sweepMesh.NumberOfGlobalSmoothSteps = value; }
        }


        // Constructors                                                                                                             
        public ViewSweepMesh(SweepMesh sweepMesh)
        {
            _sweepMesh = sweepMesh;
            SetBase(_sweepMesh);
            //
            _dctd.GetProperty(nameof(AlgorithmMesh3D)).SetIsBrowsable(false);
            //
            _dctd.GetProperty(nameof(ElementSizeType)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(ElementScaleFactor)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(NormalizedLayerSizes)).SetIsBrowsable(false);
            //
            _dctd.GetProperty(nameof(OptimizeFirstOrderSolid)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(OptimizeHighOrder)).SetIsBrowsable(false);
            //
            UpdateVisibility();
        }


        // Methods
        public override void UpdateVisibility()
        {
            base.UpdateVisibility();
        }
        public override MeshSetupItem GetBase()
        {
            return _sweepMesh;
        }
    }
}

