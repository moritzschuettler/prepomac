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
    public class ViewThickenShellMesh : ViewMeshSetupItem
    {
        // Variables                                                                                                                
        private ThickenShellMesh _thickenShellMesh;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _thickenShellMesh.Name; } set { _thickenShellMesh.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the mesh setup item.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [CategoryAttribute("Mesh size")]
        [OrderedDisplayName(1, 10, "Thickness")]
        [DescriptionAttribute("Enter teh thickness of the resulting solid mesh.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double Thickness { get { return _thickenShellMesh.Thickness; } set { _thickenShellMesh.Thickness = value; } }
        //
        [CategoryAttribute("Mesh size")]
        [OrderedDisplayName(2, 10, "Number of layers")]
        [DescriptionAttribute("Enter the number of finite element layers of the resulting solid mesh.")]
        [Id(2, 3)]
        public int NumberOfLayers
        {
            get { return _thickenShellMesh.NumberOfLayers; }
            set { _thickenShellMesh.NumberOfLayers = value; }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(0, 10, "Offset")]
        [DescriptionAttribute("Enter the offset of the resulting solid mesh.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 4)]
        public double Offset { get { return _thickenShellMesh.Offset; } set { _thickenShellMesh.Offset = value; } }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(1, 10, "Keep model edges")]
        [DescriptionAttribute("If model edges are not kept the thicken direction is averaged from neighboring faces.")]
        [Id(2, 4)]
        public bool KeepModelEdges
        {
            get { return _thickenShellMesh.KeepModelEdges; }
            set { _thickenShellMesh.KeepModelEdges = value; }
        }


        // Constructors                                                                                                             
        public ViewThickenShellMesh(ThickenShellMesh thickenShellMesh)
        {
            _thickenShellMesh = thickenShellMesh;               // 1 command
            _dctd = ProviderInstaller.Install(this);            // 2 command
            InitializeRegion();                                 // 3 command
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(KeepModelEdges));
        }


        // Methods                                                                                                                  
        public override MeshSetupItem GetBase()
        {
            return _thickenShellMesh;
        }



    }
}
