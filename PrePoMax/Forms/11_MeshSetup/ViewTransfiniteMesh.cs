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
    public class ViewTransfiniteMesh : ViewGmshSetupItem
    {
        // Variables                                                                                                                
        private TransfiniteMesh _transfiniteMesh;


        // Properties                                                                                                               
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(1, 10, "Recombine")]
        [DescriptionAttribute("Apply recombination of triangles into quads.")]
        [Id(1, 4)]
        public bool Recombine
        {
            get { return AlgorithmRecombine != GmshAlgorithmRecombineEnum.None; }
            set
            {
                if (value) _transfiniteMesh.AlgorithmRecombine = GmshAlgorithmRecombineEnum.Simple;
                else _transfiniteMesh.AlgorithmRecombine = GmshAlgorithmRecombineEnum.None;
            }
        }
        //
        [CategoryAttribute("Experimental for pyramids")]
        [OrderedDisplayName(0, 10, "Allow pyramid elements")]
        [DescriptionAttribute("Select yes to allow the creation of pyramid elements.")]
        [Id(1, 5)]
        public bool AllowPrismElements
        {
            get { return _transfiniteMesh.AllowPyramidElements; }
            set { _transfiniteMesh.AllowPyramidElements = value; }
        }
        //


        // Constructors                                                                                                             
        public ViewTransfiniteMesh(TransfiniteMesh transfiniteMesh)
        {
            _transfiniteMesh = transfiniteMesh;
            SetBase(_transfiniteMesh);
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(Recombine));
            _dctd.RenameBooleanPropertyToYesNo(nameof(AllowPrismElements));
            //
            _dctd.GetProperty(nameof(AlgorithmMesh2D)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(AlgorithmMesh3D)).SetIsBrowsable(false);
            //
            _dctd.GetProperty(nameof(AlgorithmRecombine)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(RecombineMinQuality)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(TransfiniteThreeSided)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(TransfiniteFourSided)).SetIsBrowsable(false);
            //
            _dctd.GetProperty(nameof(OptimizeFirstOrderShell)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(OptimizeFirstOrderSolid)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(OptimizeHighOrder)).SetIsBrowsable(false);
            //
            _dctd.GetProperty(nameof(ElementSizeType)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(ElementScaleFactor)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(NormalizedLayerSizes)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(NumOfElementsPerLayer)).SetIsBrowsable(false);
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public override MeshSetupItem GetBase()
        {
            return _transfiniteMesh;
        }
    }
}

