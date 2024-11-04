using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeMesh.Meshing;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewSweepMesh : ViewGmshSetupItem
    {
        // Variables                                                                                                                
        private SweepMesh _sweepMesh;


        // Properties                                                                                                               
        [CategoryAttribute("Mesh smoothing")]
        [OrderedDisplayName(0, 10, "Layer smooth steps")]
        [DescriptionAttribute("Enter the number of Laplacian smooth steps for the generation of the next sweep layers.")]
        [Id(1, 10)]
        public int NumberOfLayerSmoothSteps
        {
            get { return _sweepMesh.NumberOfLayerSmoothSteps; }
            set { _sweepMesh.NumberOfLayerSmoothSteps = value; }
        }
        [CategoryAttribute("Mesh smoothing")]
        [OrderedDisplayName(1, 10, "Global smooth steps")]
        [DescriptionAttribute("Enter the number of Laplacian smooth steps for the final global mesh smoothing.")]
        [Id(2, 10)]
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
        public override MeshSetupItem GetBase()
        {
            return _sweepMesh;
        }
    }
}

