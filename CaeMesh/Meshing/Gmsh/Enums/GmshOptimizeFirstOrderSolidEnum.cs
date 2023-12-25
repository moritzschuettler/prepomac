using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh.Meshing
{
    /* Optimization of a linear mesh
    Netgen
    HighOrder
    HighOrderElastic
    HighOrderFastCurving
    Laplace2D
    Relocate2D
    Relocate3D
    QuadQuasiStructured
    UntangleMeshGeometry
    */
    [Serializable]
    public enum GmshOptimizeFirstOrderSolidEnum
    {
        [StandardValue("None", DisplayName = "None")]
        None,
        [StandardValue("Gmsh", DisplayName = "Gmsh")]
        Gmsh,
        [StandardValue("Netgen", DisplayName = "Netgen")]
        Netgen,
        [StandardValue("Relocate3D", DisplayName = "Relocate 3D")]
        Relocate3D,
    }
}
