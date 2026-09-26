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
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(1, 10, "2D meshing algorithm")]
        [DescriptionAttribute("Select the algorithm for the surface meshing. QuadWild meshes the source surfaces with " +
                              "quadrilaterals and chooses the number of elements on every source edge whose count is " +
                              "not fixed by an edge mesh size, by the layers of another sweep block or by a " +
                              "neighbouring part; gmsh meshes the side surfaces.")]
        [Id(1, 3)]
        public SweepAlgorithmMesh2DEnum SweepAlgorithmMesh2D
        {
            get { return _sweepMesh.SweepAlgorithmMesh2D; }
            set { _sweepMesh.SweepAlgorithmMesh2D = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(7, 10, "Flow profile")]
        [DescriptionAttribute("Aligned uses flow.txt. No alignment uses flow_noalign.txt and relaxes singularity alignment.")]
        [Id(8, 3)]
        public QuadWildFlowProfileEnum QuadWildFlowProfile
        {
            get { return _sweepMesh.QuadWildFlowProfile; }
            set { _sweepMesh.QuadWildFlowProfile = value; }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(8, 10, "Alpha")]
        [DescriptionAttribute("Controls the QuadWild trade-off between regularity and isometry. Values near 0 favor " +
                              "regularity; larger values allow more singularities.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(9, 3)]
        public double QuadWildAlpha
        {
            get { return _sweepMesh.QuadWildAlpha; }
            set { _sweepMesh.QuadWildAlpha = value; }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(9, 10, "Smooth")]
        [DescriptionAttribute("Use the smoothed QuadWild quadrangulation. Set to No to use the unsmoothed one, which " +
                              "has the same topology.")]
        [Id(10, 3)]
        public bool QuadWildSmooth
        {
            get { return _sweepMesh.QuadWildSmooth; }
            set { _sweepMesh.QuadWildSmooth = value; }
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
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(0, 10, "Fallback to gmsh")]
        [DescriptionAttribute("Mesh a source surface with gmsh when QuadWild cannot mesh it. Set to No to have the " +
                              "meshing fail instead, so that the surfaces QuadWild cannot mesh are found.")]
        [Id(1, 11)]
        public bool QuadWildFallbackToGmsh
        {
            get { return _sweepMesh.QuadWildFallbackToGmsh; }
            set { _sweepMesh.QuadWildFallbackToGmsh = value; }
        }


        // Constructors                                                                                                             
        public ViewSweepMesh(SweepMesh sweepMesh)
        {
            _sweepMesh = sweepMesh;
            SetBase(_sweepMesh);
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(KeepImprintEdges));
            _dctd.RenameBooleanPropertyToYesNo(nameof(QuadWildSmooth));
            _dctd.RenameBooleanPropertyToYesNo(nameof(QuadWildFallbackToGmsh));
            //
            _dctd.GetProperty(nameof(AlgorithmMesh2D)).SetIsBrowsable(false);
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
            // QuadWild takes the place of the gmsh options for the surfaces: the recombination, the transfinite faces
            // and the optimisation
            bool quadWild = _sweepMesh.SourceMesher == SweepSourceMesherEnum.QuadWild;
            _dctd.GetProperty(nameof(AlgorithmRecombine)).SetIsBrowsable(!quadWild);
            _dctd.GetProperty(nameof(TransfiniteThreeSided)).SetIsBrowsable(!quadWild);
            _dctd.GetProperty(nameof(TransfiniteFourSided)).SetIsBrowsable(!quadWild);
            _dctd.GetProperty(nameof(OptimizeFirstOrderShell)).SetIsBrowsable(!quadWild);
            //
            base.UpdateVisibility();
            //
            if (quadWild)
            {
                _dctd.GetProperty(nameof(RecombineMinQuality)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(TransfiniteAngleDeg)).SetIsBrowsable(false);
            }
            _dctd.GetProperty(nameof(QuadWildFlowProfile)).SetIsBrowsable(quadWild);
            _dctd.GetProperty(nameof(QuadWildAlpha)).SetIsBrowsable(quadWild);
            _dctd.GetProperty(nameof(QuadWildSmooth)).SetIsBrowsable(quadWild);
            _dctd.GetProperty(nameof(QuadWildFallbackToGmsh)).SetIsBrowsable(quadWild);
        }
        public override MeshSetupItem GetBase()
        {
            return _sweepMesh;
        }
    }
}

