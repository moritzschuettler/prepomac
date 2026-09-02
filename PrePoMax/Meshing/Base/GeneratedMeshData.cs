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
    }
}
