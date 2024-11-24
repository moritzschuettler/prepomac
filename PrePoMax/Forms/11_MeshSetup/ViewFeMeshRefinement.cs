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
    public class ViewFeMeshRefinement : ViewMeshSetupItem
    {
        // Variables                                                                                                                
        private FeMeshRefinement _meshRefinement;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _meshRefinement.Name; } set { _meshRefinement.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the mesh setup item.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [CategoryAttribute("Mesh size")]
        [OrderedDisplayName(1, 10, "Element size")]
        [DescriptionAttribute("Element size can only be used to reduce the local element size, but the global limit of " +
                              "the minimum element size is kept.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double MeshSize { get { return _meshRefinement.MeshSize; } set { _meshRefinement.MeshSize = value; } }


        // Constructors                                                                                                             
        public ViewFeMeshRefinement(FeMeshRefinement meshRefinement)
        {
            _meshRefinement = meshRefinement;                               // 1 command
            _dctd = ProviderInstaller.Install(this);                        // 2 command
            InitializeRegion();                                             // 3 command
        }


        // Methods                                                                                                                  
        public override MeshSetupItem GetBase()
        {
            return _meshRefinement;
        }
    }
}
