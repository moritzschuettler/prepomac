// PrePoMax for macOS - NetGenMesher: BREP_VISUALIZATION, the triangulation of a CAD shape that PrePoMax shows and
// selects on, with the geometry metadata of its solids, shells, faces, edges and vertices (VIS file format 2).
//
//   BREP_VISUALIZATION <brep> <vis> <linear deflection> <relative 0/1> <angular deflection in rad>
#include "Common.h"

#include <BRepAdaptor_Curve.hxx>
#include <BRepAdaptor_Surface.hxx>
#include <BRepBndLib.hxx>
#include <BRepGProp.hxx>
#include <BRepLProp_SLProps.hxx>
#include <BRepMesh_IncrementalMesh.hxx>
#include <BRepTools.hxx>
#include <BRep_Tool.hxx>
#include <Bnd_Box.hxx>
#include <GProp_GProps.hxx>
#include <GeomAbs_CurveType.hxx>
#include <GeomAbs_SurfaceType.hxx>
#include <Poly_PolygonOnTriangulation.hxx>
#include <Poly_Triangulation.hxx>
#include <ShapeAnalysis_Shell.hxx>
#include <TopExp.hxx>
#include <TopExp_Explorer.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Edge.hxx>
#include <TopoDS_Face.hxx>
#include <TopoDS_Vertex.hxx>
#include <gp_Circ.hxx>
#include <gp_Cone.hxx>
#include <gp_Cylinder.hxx>
#include <gp_Elips.hxx>
#include <gp_Lin.hxx>
#include <gp_Pln.hxx>
#include <gp_Sphere.hxx>
#include <gp_Torus.hxx>

#include <cmath>
#include <fstream>
#include <set>
#include <sstream>

namespace ngm
{
    namespace
    {
        const char* FaceTypeName(GeomAbs_SurfaceType type)
        {
            switch (type)
            {
                case GeomAbs_Plane: return "Plane";
                case GeomAbs_Cylinder: return "Cylinder";
                case GeomAbs_Cone: return "Cone";
                case GeomAbs_Sphere: return "Sphere";
                case GeomAbs_Torus: return "Torus";
                case GeomAbs_BezierSurface: return "BezierSurface";
                case GeomAbs_BSplineSurface: return "BSplineSurface";
                case GeomAbs_SurfaceOfRevolution: return "SurfaceOfRevolution";
                case GeomAbs_SurfaceOfExtrusion: return "SurfaceOfExtrusion";
                case GeomAbs_OffsetSurface: return "OffsetSurface";
                default: return "OtherSurface";
            }
        }
        const char* CurveTypeName(GeomAbs_CurveType type)
        {
            switch (type)
            {
                case GeomAbs_Line: return "Line";
                case GeomAbs_Circle: return "Circle";
                case GeomAbs_Ellipse: return "Ellipse";
                case GeomAbs_Hyperbola: return "Hyperbola";
                case GeomAbs_Parabola: return "Parabola";
                case GeomAbs_BezierCurve: return "BezierCurve";
                case GeomAbs_BSplineCurve: return "BSplineCurve";
                case GeomAbs_OffsetCurve: return "OffsetCurve";
                default: return "OtherCurve";
            }
        }
        const char* OrientationName(TopAbs_Orientation orientation)
        {
            switch (orientation)
            {
                case TopAbs_FORWARD: return "Forward";
                case TopAbs_REVERSED: return "Reversed";
                case TopAbs_INTERNAL: return "Internal";
                default: return "External";
            }
        }

        // One entity of the metadata: "<Label> number: <id>" and "<Label> <property>: <values>"
        class EntityWriter
        {
        public:
            EntityWriter(std::ostream& out, const std::string& label, int id) : _out(out), _label(label)
            {
                _out << label << " number: " << id << "\n";
            }
            void Text(const std::string& name, const std::string& value)
            {
                _out << _label << " " << name << ": " << value << "\n";
            }
            void Numbers(const std::string& name, const std::vector<double>& values)
            {
                if (values.empty()) return;
                _out << _label << " " << name << ":";
                for (double value : values) _out << " " << Num(value);
                _out << "\n";
            }
            void Number(const std::string& name, double value) { Numbers(name, { value }); }
            void Ids(const std::string& name, const std::vector<int>& ids)
            {
                if (ids.empty()) return;
                _out << _label << " " << name << ":";
                for (int id : ids) _out << " " << id;
                _out << "\n";
            }
        private:
            std::ostream& _out;
            std::string _label;
        };

        std::vector<double> BoundingBox(const TopoDS_Shape& shape)
        {
            Bnd_Box box;
            BRepBndLib::AddOptimal(shape, box, false, false);
            if (box.IsVoid()) return {};
            double xmin, ymin, zmin, xmax, ymax, zmax;
            box.Get(xmin, ymin, zmin, xmax, ymax, zmax);
            return { xmin, xmax, ymin, ymax, zmin, zmax };
        }
        std::vector<double> Point(const gp_Pnt& p) { return { p.X(), p.Y(), p.Z() }; }
        std::vector<double> Axis(const gp_Pnt& p, const gp_Dir& d) { return { p.X(), p.Y(), p.Z(), d.X(), d.Y(), d.Z() }; }

        std::vector<int> SubIds(const TopoDS_Shape& shape, TopAbs_ShapeEnum type, const TopTools_IndexedMapOfShape& map)
        {
            std::vector<int> ids;
            TopTools_IndexedMapOfShape sub;
            TopExp::MapShapes(shape, type, sub);
            for (int i = 1; i <= sub.Extent(); i++)
            {
                int id = map.FindIndex(sub(i));
                if (id > 0) ids.push_back(id);
            }
            return ids;
        }

        void WriteSolid(std::ostream& out, const TopoDS_Shape& solid, int id, const ShapeMaps& maps)
        {
            EntityWriter w(out, "Solid", id);
            GProp_GProps volume;
            BRepGProp::VolumeProperties(solid, volume);
            GProp_GProps surface;
            BRepGProp::SurfaceProperties(solid, surface);
            w.Number("volume", std::fabs(volume.Mass()));
            w.Number("surface_area", surface.Mass());
            w.Numbers("center_of_mass", Point(volume.CentreOfMass()));
            gp_Mat inertia = volume.MatrixOfInertia();
            double sign = volume.Mass() < 0 ? -1 : 1;
            std::vector<double> values;
            for (int r = 1; r <= 3; r++)
                for (int c = 1; c <= 3; c++) values.push_back(sign * inertia(r, c));
            w.Numbers("inertia_at_center_of_mass", values);
            w.Numbers("bounding_box", BoundingBox(solid));
            w.Ids("face_ids", SubIds(solid, TopAbs_FACE, maps.faces));
            w.Ids("shell_ids", SubIds(solid, TopAbs_SHELL, maps.shells));
        }
        void WriteShell(std::ostream& out, const TopoDS_Shape& shell, int id, const ShapeMaps& maps)
        {
            EntityWriter w(out, "Shell", id);
            GProp_GProps surface;
            BRepGProp::SurfaceProperties(shell, surface);
            w.Number("area", surface.Mass());
            w.Numbers("centroid", Point(surface.CentreOfMass()));
            w.Numbers("bounding_box", BoundingBox(shell));
            w.Number("closed", BRep_Tool::IsClosed(shell) ? 1 : 0);
            w.Ids("face_ids", SubIds(shell, TopAbs_FACE, maps.faces));
        }
        // The side of a cylinder, a cone or a sphere that holds the material: the outside of a shaft is convex, the
        // wall of a hole concave
        std::string Convexity(const TopoDS_Face& face, const gp_Pnt& axisPoint, const gp_Dir& axisDirection)
        {
            BRepAdaptor_Surface surface(face);
            double u = 0.5 * (surface.FirstUParameter() + surface.LastUParameter());
            double v = 0.5 * (surface.FirstVParameter() + surface.LastVParameter());
            BRepLProp_SLProps props(surface, u, v, 1, 1e-7);
            if (!props.IsNormalDefined()) return "";
            gp_Dir normal = props.Normal();
            if (face.Orientation() == TopAbs_REVERSED) normal.Reverse();
            gp_Vec radial(axisPoint, props.Value());
            gp_Vec along(axisDirection);
            radial -= along * radial.Dot(along);
            if (radial.Magnitude() < 1e-12) return "";
            return radial.Dot(gp_Vec(normal)) > 0 ? "Convex" : "Concave";
        }
        void WriteFace(std::ostream& out, const TopoDS_Face& face, int id, const ShapeMaps& maps)
        {
            EntityWriter w(out, "Face", id);
            BRepAdaptor_Surface surface(face, true);
            GeomAbs_SurfaceType type = surface.GetType();
            w.Text("type", FaceTypeName(type));
            w.Text("orientation", OrientationName(face.Orientation()));
            GProp_GProps props;
            BRepGProp::SurfaceProperties(face, props);
            w.Number("area", props.Mass());
            w.Numbers("centroid", Point(props.CentreOfMass()));
            w.Numbers("bounding_box", BoundingBox(face));
            switch (type)
            {
                case GeomAbs_Plane:
                    {
                        gp_Pln plane = surface.Plane();
                        gp_Dir normal = plane.Axis().Direction();
                        if (face.Orientation() == TopAbs_REVERSED) normal.Reverse();
                        // The point of the plane closest to the middle of the face
                        gp_Pnt c = props.CentreOfMass();
                        gp_Vec d(plane.Location(), c);
                        gp_Pnt onPlane = c.Translated(-gp_Vec(normal) * d.Dot(gp_Vec(normal)));
                        w.Numbers("axis", Axis(onPlane, normal));
                        break;
                    }
                case GeomAbs_Cylinder:
                    {
                        gp_Cylinder cylinder = surface.Cylinder();
                        w.Numbers("axis", Axis(cylinder.Location(), cylinder.Axis().Direction()));
                        w.Number("radius", cylinder.Radius());
                        std::string convexity = Convexity(face, cylinder.Location(), cylinder.Axis().Direction());
                        if (!convexity.empty()) w.Text("convexity", convexity);
                        break;
                    }
                case GeomAbs_Cone:
                    {
                        gp_Cone cone = surface.Cone();
                        w.Numbers("axis", Axis(cone.Location(), cone.Axis().Direction()));
                        w.Number("reference_radius", cone.RefRadius());
                        w.Number("semi_angle_rad", cone.SemiAngle());
                        w.Numbers("apex", Point(cone.Apex()));
                        std::string convexity = Convexity(face, cone.Location(), cone.Axis().Direction());
                        if (!convexity.empty()) w.Text("convexity", convexity);
                        break;
                    }
                case GeomAbs_Sphere:
                    {
                        gp_Sphere sphere = surface.Sphere();
                        w.Numbers("center", Point(sphere.Location()));
                        w.Number("radius", sphere.Radius());
                        w.Numbers("axis", Axis(sphere.Location(), sphere.Position().Direction()));
                        break;
                    }
                case GeomAbs_Torus:
                    {
                        gp_Torus torus = surface.Torus();
                        w.Numbers("axis", Axis(torus.Location(), torus.Axis().Direction()));
                        w.Numbers("center", Point(torus.Location()));
                        w.Number("major_radius", torus.MajorRadius());
                        w.Number("minor_radius", torus.MinorRadius());
                        break;
                    }
                case GeomAbs_SurfaceOfExtrusion:
                    {
                        gp_Dir direction = surface.Direction();
                        w.Numbers("extrusion_direction", { direction.X(), direction.Y(), direction.Z() });
                        break;
                    }
                case GeomAbs_SurfaceOfRevolution:
                    {
                        gp_Ax1 axis = surface.AxeOfRevolution();
                        w.Numbers("axis", Axis(axis.Location(), axis.Direction()));
                        break;
                    }
                default:
                    break;
            }
            w.Ids("edge_ids", SubIds(face, TopAbs_EDGE, maps.edges));
        }
        void WriteEdge(std::ostream& out, const TopoDS_Edge& edge, int id, const ShapeMaps& maps,
                       const TopTools_IndexedDataMapOfShapeListOfShape& edgeFaces, bool internal)
        {
            EntityWriter w(out, "Edge", id);
            if (BRep_Tool::Degenerated(edge))
            {
                w.Text("type", "OtherCurve");
                w.Number("degenerated", 1);
                return;
            }
            BRepAdaptor_Curve curve(edge);
            GeomAbs_CurveType type = curve.GetType();
            w.Text("type", CurveTypeName(type));
            if (internal) w.Text("orientation", "Internal");
            GProp_GProps props;
            BRepGProp::LinearProperties(edge, props);
            w.Number("length", props.Mass());
            w.Numbers("centroid", Point(props.CentreOfMass()));
            double first = curve.FirstParameter(), last = curve.LastParameter();
            w.Numbers("midpoint", Point(curve.Value(0.5 * (first + last))));
            w.Numbers("bounding_box", BoundingBox(edge));
            TopoDS_Vertex v1, v2;
            TopExp::Vertices(edge, v1, v2);
            bool closed = !v1.IsNull() && v1.IsSame(v2);
            w.Number("closed", closed ? 1 : 0);
            switch (type)
            {
                case GeomAbs_Line:
                    {
                        gp_Lin line = curve.Line();
                        w.Numbers("axis", Axis(curve.Value(first), line.Direction()));
                        break;
                    }
                case GeomAbs_Circle:
                    {
                        gp_Circ circle = curve.Circle();
                        w.Numbers("axis", Axis(circle.Location(), circle.Axis().Direction()));
                        w.Numbers("center", Point(circle.Location()));
                        w.Number("radius", circle.Radius());
                        double sweep = std::fabs(last - first);
                        if (closed && std::fabs(sweep - 2 * M_PI) < 1e-9) sweep = 2 * M_PI;
                        w.Number("sweep_angle_rad", sweep);
                        break;
                    }
                case GeomAbs_Ellipse:
                    {
                        gp_Elips ellipse = curve.Ellipse();
                        w.Numbers("axis", Axis(ellipse.Location(), ellipse.Axis().Direction()));
                        w.Numbers("center", Point(ellipse.Location()));
                        w.Number("major_radius", ellipse.MajorRadius());
                        w.Number("minor_radius", ellipse.MinorRadius());
                        break;
                    }
                default:
                    break;
            }
            std::vector<int> faceIds;
            int index = edgeFaces.FindIndex(edge);
            if (index > 0)
            {
                std::set<int> unique;
                for (TopTools_ListIteratorOfListOfShape it(edgeFaces(index)); it.More(); it.Next())
                {
                    int faceId = maps.faces.FindIndex(it.Value());
                    if (faceId > 0 && unique.insert(faceId).second) faceIds.push_back(faceId);
                }
            }
            w.Ids("face_ids", faceIds);
            std::vector<int> vertexIds;
            if (!v1.IsNull()) vertexIds.push_back(maps.vertices.FindIndex(v1));
            if (!v2.IsNull() && !closed) vertexIds.push_back(maps.vertices.FindIndex(v2));
            w.Ids("vertex_ids", vertexIds);
        }

        void WriteFaceMesh(std::ostream& out, const TopoDS_Face& face, int faceId, const ShapeMaps& maps)
        {
            TopLoc_Location location;
            Handle(Poly_Triangulation) triangulation = BRep_Tool::Triangulation(face, location);
            if (triangulation.IsNull()) return;
            gp_Trsf transform = location.Transformation();
            //
            out << "**********\n";
            out << "Face mesh number: " << faceId << "\n";
            out << "**********\n";
            out << "Number of nodes: " << triangulation->NbNodes() << "\n";
            for (int i = 1; i <= triangulation->NbNodes(); i++)
            {
                gp_Pnt p = triangulation->Node(i).Transformed(transform);
                out << i << " " << Num(p.X()) << " " << Num(p.Y()) << " " << Num(p.Z()) << "\n";
            }
            out << "**********\n";
            // The triangles in the direction of the surface; PrePoMax turns those of a reversed face around
            out << "Number of triangles: " << triangulation->NbTriangles() << "\n";
            for (int i = 1; i <= triangulation->NbTriangles(); i++)
            {
                int n1, n2, n3;
                triangulation->Triangle(i).Get(n1, n2, n3);
                out << i << " " << n1 << " " << n2 << " " << n3 << "\n";
            }
            out << "**********\n";
            // The edges of the face as polylines over the nodes of the face
            std::vector<std::string> polylines;
            for (TopExp_Explorer ex(face, TopAbs_EDGE); ex.More(); ex.Next())
            {
                TopoDS_Edge edge = TopoDS::Edge(ex.Current());
                if (BRep_Tool::Degenerated(edge)) continue;
                int edgeId = maps.edges.FindIndex(edge);
                if (edgeId <= 0) continue;
                Handle(Poly_PolygonOnTriangulation) polygon = BRep_Tool::PolygonOnTriangulation(edge, triangulation, location);
                if (polygon.IsNull()) continue;
                std::ostringstream line;
                line << "Edge polyline: " << edgeId;
                for (int i = 1; i <= polygon->NbNodes(); i++) line << " " << polygon->Node(i);
                polylines.push_back(line.str());
            }
            out << "Number of edge polylines: " << polylines.size() << "\n";
            for (const std::string& line : polylines) out << line << "\n";
        }
    }

    int Visualization(const std::vector<std::string>& args)
    {
        if (args.size() < 2)
        {
            Error("BREP_VISUALIZATION needs a BREP file and a VIS file name.");
            return 1;
        }
        double linearDeflection = args.size() > 2 ? std::stod(args[2]) : 0.01;
        bool relative = args.size() > 3 ? std::stoi(args[3]) != 0 : true;
        double angularDeflection = args.size() > 4 ? std::stod(args[4]) : 0.5;
        //
        TopoDS_Shape shape;
        if (!ReadShape(args[0], shape)) return 1;
        //
        BRepTools::Clean(shape);
        BRepMesh_IncrementalMesh mesher(shape, linearDeflection, relative, angularDeflection, true);
        if (!mesher.IsDone())
        {
            Error("The visualization of " + args[0] + " could not be created.");
            return 1;
        }
        //
        ShapeMaps maps(shape);
        TopTools_IndexedDataMapOfShapeListOfShape edgeFaces;
        TopExp::MapShapesAndAncestors(shape, TopAbs_EDGE, TopAbs_FACE, edgeFaces);
        // Edges that lie inside a face
        std::set<int> internalEdges;
        for (int i = 1; i <= maps.faces.Extent(); i++)
        {
            for (TopExp_Explorer ex(maps.faces(i), TopAbs_EDGE); ex.More(); ex.Next())
            {
                if (ex.Current().Orientation() == TopAbs_INTERNAL)
                    internalEdges.insert(maps.edges.FindIndex(ex.Current()));
            }
        }
        //
        std::ofstream out(args[1]);
        if (!out)
        {
            Error("The file " + args[1] + " could not be written.");
            return 1;
        }
        out << "VIS file format: 2\n";
        out << "Coordinate system: Global Cartesian\n";
        out << "Length unit: Model\n";
        out << "Geometry\n";
        for (int i = 1; i <= maps.solids.Extent(); i++) WriteSolid(out, maps.solids(i), i, maps);
        for (int i = 1; i <= maps.shells.Extent(); i++) WriteShell(out, maps.shells(i), i, maps);
        for (int i = 1; i <= maps.faces.Extent(); i++) WriteFace(out, TopoDS::Face(maps.faces(i)), i, maps);
        for (int i = 1; i <= maps.edges.Extent(); i++)
            WriteEdge(out, TopoDS::Edge(maps.edges(i)), i, maps, edgeFaces, internalEdges.count(i) > 0);
        for (int i = 1; i <= maps.vertices.Extent(); i++)
        {
            EntityWriter w(out, "Vertex", i);
            w.Numbers("coordinates", Point(BRep_Tool::Pnt(TopoDS::Vertex(maps.vertices(i)))));
        }
        out << "Visualization\n";
        out << "**********\n";
        out << "Number of visualization vertices: " << maps.vertices.Extent() << "\n";
        for (int i = 1; i <= maps.vertices.Extent(); i++)
        {
            gp_Pnt p = BRep_Tool::Pnt(TopoDS::Vertex(maps.vertices(i)));
            out << i << " " << Num(p.X()) << " " << Num(p.Y()) << " " << Num(p.Z()) << "\n";
        }
        for (int i = 1; i <= maps.faces.Extent(); i++) WriteFaceMesh(out, TopoDS::Face(maps.faces(i)), i, maps);
        out << "**********\n";
        out.close();
        //
        Log("Visualization: " + std::to_string(maps.solids.Extent()) + " solids, " +
            std::to_string(maps.faces.Extent()) + " faces, " + std::to_string(maps.edges.Extent()) + " edges.");
        return 0;
    }
}
