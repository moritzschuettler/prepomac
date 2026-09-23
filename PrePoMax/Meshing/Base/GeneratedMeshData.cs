using CaeMesh;
using System;
using FileInOut.Output;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    internal class GeneratedMeshData
    {
        public GeometryPart GeometryPart;
        public string MeshFileName;
        public bool? ConvertToSecondOrder;
        public bool SmoothShaded;
        public FeMesh Mesh;
        public string[] RelatedPartNames;
        public QuadWildInputFileInfo QuadWildInputFileInfo;
        public bool RetainWorkDirectory;
        // QuadWild debug visualization: the prefix of the traced files (null when it is off), whether the mesh is
        // the traced triangle mesh of a preview, and the patch material of every element
        public string QuadWildDebugTracePrefix;
        public bool QuadWildTriangularPreview;
        public Dictionary<int, string> ElementIdMaterialName;
    }
}
