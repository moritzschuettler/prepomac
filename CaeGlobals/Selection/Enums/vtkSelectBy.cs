using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeGlobals
{
    public enum vtkSelectBy
    {
        // General
        Off,
        Default,        
        // Mesh based
        Id,
        Node,
        Element,
        Edge,
        Surface,
        EdgeAngle,
        SurfaceAngle,
        Part,
        // Geometry based
        Geometry,
        GeometryVertex,
        GeometryEdge,
        GeometrySurface,
        GeometryEdgeAngle,
        GeometrySurfaceAngle,
        GeometryPart,
        //
        Widget
    }
    // Has extension: IsGeometryBased
}
