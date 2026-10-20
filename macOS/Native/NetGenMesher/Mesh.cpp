// PrePoMax for macOS - NetGenMesher: meshing of CAD shapes with gmsh (its 3D algorithm is the one of Netgen) into the
// Netgen VOL format PrePoMax reads. The surfaces and edges of the mesh carry the face and edge numbers of the shape,
// the same numbers the visualization (BREP_VISUALIZATION) uses.
//
//   BREP_MESH <brep> <vol> <mesh parameters> <local mesh sizes>
//   BREP_EDGE_MESH <brep> <vol> <mesh parameters> <local mesh sizes>
#include "Common.h"

#include <BRepAdaptor_Surface.hxx>
#include <BRepGProp.hxx>
#include <BRepLProp_SLProps.hxx>
#include <BRep_Tool.hxx>
#include <GProp_GProps.hxx>
#include <GeomAPI_ProjectPointOnSurf.hxx>
#include <TopExp.hxx>
#include <TopoDS.hxx>

#include <gmsh.h>

#include <algorithm>
#include <cmath>
#include <fstream>
#include <limits>
#include <map>
#include <set>
#include <sstream>

namespace ngm
{
    namespace
    {
        struct LocalSizes
        {
            std::vector<std::array<double, 4>> points;          // x y z h
            std::vector<std::array<double, 7>> lines;           // x1 y1 z1 x2 y2 z2 h
        };
        LocalSizes ReadLocalSizes(const std::string& fileName)
        {
            LocalSizes sizes;
            std::ifstream file(fileName);
            if (!file) return sizes;
            int n = 0;
            if (file >> n)
            {
                for (int i = 0; i < n; i++)
                {
                    std::array<double, 4> p;
                    if (!(file >> p[0] >> p[1] >> p[2] >> p[3])) return sizes;
                    sizes.points.push_back(p);
                }
            }
            if (file >> n)
            {
                for (int i = 0; i < n; i++)
                {
                    std::array<double, 7> l;
                    if (!(file >> l[0] >> l[1] >> l[2] >> l[3] >> l[4] >> l[5] >> l[6])) return sizes;
                    sizes.lines.push_back(l);
                }
            }
            return sizes;
        }
        double DistanceToSegment(const double* p, const double* a, const double* b)
        {
            double ab[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double ap[3] = { p[0] - a[0], p[1] - a[1], p[2] - a[2] };
            double l2 = ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2];
            double t = l2 > 0 ? (ab[0] * ap[0] + ab[1] * ap[1] + ab[2] * ap[2]) / l2 : 0;
            t = std::max(0.0, std::min(1.0, t));
            double d[3] = { ap[0] - t * ab[0], ap[1] - t * ab[1], ap[2] - t * ab[2] };
            return std::sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]);
        }

        struct EntityProps { double mass; gp_Pnt center; };
        EntityProps OccProps(const TopoDS_Shape& shape, int dim)
        {
            GProp_GProps props;
            if (dim == 2) BRepGProp::SurfaceProperties(shape, props);
            else BRepGProp::LinearProperties(shape, props);
            return { props.Mass(), props.CentreOfMass() };
        }
        // The gmsh entities of a dimension matched to the shapes of the map by their centres of mass and sizes
        std::map<int, int> MatchEntities(int dim, const TopTools_IndexedMapOfShape& map)
        {
            std::map<int, int> tagToId;
            gmsh::vectorpair entities;
            gmsh::model::getEntities(entities, dim);
            std::vector<EntityProps> occ;
            double scale = 0;
            for (int i = 1; i <= map.Extent(); i++)
            {
                bool degenerated = dim == 1 && BRep_Tool::Degenerated(TopoDS::Edge(map(i)));
                occ.push_back(degenerated ? EntityProps{ -1, gp_Pnt() } : OccProps(map(i), dim));
                scale = std::max(scale, std::fabs(occ.back().mass));
            }
            std::set<int> used;
            for (auto& entity : entities)
            {
                double mass = 0;
                std::vector<double> center(3, 0.0);
                try
                {
                    gmsh::model::occ::getMass(dim, entity.second, mass);
                    gmsh::model::occ::getCenterOfMass(dim, entity.second, center[0], center[1], center[2]);
                }
                catch (...) { continue; }
                gp_Pnt c(center[0], center[1], center[2]);
                int best = -1;
                double bestScore = std::numeric_limits<double>::max();
                for (size_t i = 0; i < occ.size(); i++)
                {
                    if (occ[i].mass < 0) continue;
                    double score = occ[i].center.Distance(c) + std::fabs(occ[i].mass - mass) / std::max(1e-12, scale);
                    if (used.count((int)i + 1)) score += 1e6;
                    if (score < bestScore) { bestScore = score; best = (int)i + 1; }
                }
                if (best > 0)
                {
                    tagToId[entity.second] = best;
                    used.insert(best);
                }
            }
            return tagToId;
        }

        // gmsh element node orders (equal to Abaqus) turned into the Netgen orders PrePoMax reads back
        std::vector<size_t> SurfaceOrder(int type, const size_t* n)
        {
            switch (type)
            {
                case 2: return { n[0], n[1], n[2] };                                       // tri3
                case 3: return { n[0], n[1], n[2], n[3] };                                 // quad4
                case 9: return { n[0], n[1], n[2], n[4], n[5], n[3] };                     // tri6
                case 16: return { n[0], n[1], n[2], n[3], n[4], n[7], n[5], n[6] };        // quad8
                default: return {};
            }
        }
        std::vector<size_t> VolumeOrder(int type, const size_t* n)
        {
            switch (type)
            {
                case 4: return { n[0], n[2], n[1], n[3] };                                 // tet4
                case 11:                                                                   // tet10: gmsh 8 = (2,3), 9 = (1,3)
                    {
                        size_t a[10] = { n[0], n[1], n[2], n[3], n[4], n[5], n[6], n[7], n[9], n[8] };
                        return { a[0], a[2], a[1], a[3], a[6], a[4], a[7], a[5], a[9], a[8] };
                    }
                case 6: return { n[0], n[2], n[1], n[3], n[5], n[4] };                     // prism6
                default: return {};
            }
        }
        int NodesOfType(int type)
        {
            switch (type)
            {
                case 1: return 2; case 8: return 3;
                case 2: return 3; case 3: return 4; case 9: return 6; case 16: return 8; case 10: return 9;
                case 4: return 4; case 11: return 10; case 6: return 6; case 5: return 8; case 7: return 5;
                default: return 0;
            }
        }

        int MeshBrep(const std::vector<std::string>& args, bool onlyEdges)
        {
            if (args.size() < 3)
            {
                Error("The mesh command needs a BREP file, a VOL file and a mesh parameters file.");
                return 1;
            }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            ShapeMaps maps(shape);
            std::map<std::string, std::string> parameters = ReadParameterFile(args[2]);
            LocalSizes local = args.size() > 3 ? ReadLocalSizes(args[3]) : LocalSizes();
            //
            double maxh = GetDouble(parameters, "maxh", 1e10);
            double minh = GetDouble(parameters, "minh", 0);
            double grading = GetDouble(parameters, "grading", 0.3);
            double elementsPerCurve = GetDouble(parameters, "elementspercurve", 2);
            bool secondOrder = GetInt(parameters, "second_order", 0) != 0;
            bool quadDominated = GetInt(parameters, "quad_dominated", 0) != 0;
            int optimize3D = GetInt(parameters, "optsteps_3d", 3);
            int optimize2D = GetInt(parameters, "optsteps_2d", 3);
            int threads = GetInt(parameters, "parallel_meshing_threads", 1);
            if (maxh <= 0) maxh = 1e10;
            //
            gmsh::initialize();
            int status = 0;
            try
            {
                gmsh::option::setNumber("General.Terminal", 1);
                gmsh::option::setNumber("General.Verbosity", 3);
                gmsh::option::setNumber("General.NumThreads", std::max(1, threads));
                gmsh::option::setNumber("Geometry.OCCParallel", 1);
                gmsh::model::add("NetGenMesher");
                gmsh::vectorpair imported;
                gmsh::model::occ::importShapesNativePointer(&shape, imported, false);
                gmsh::model::occ::synchronize();
                //
                gmsh::option::setNumber("Mesh.MeshSizeMax", maxh);
                gmsh::option::setNumber("Mesh.MeshSizeMin", minh);
                gmsh::option::setNumber("Mesh.MeshSizeFromCurvature", std::max(0.0, 2 * M_PI * elementsPerCurve));
                gmsh::option::setNumber("Mesh.MeshSizeExtendFromBoundary", 1);
                gmsh::option::setNumber("Mesh.Algorithm", quadDominated ? 8 : 6);
                gmsh::option::setNumber("Mesh.Algorithm3D", 4);                 // Frontal: the Netgen mesher
                gmsh::option::setNumber("Mesh.OptimizeNetgen", optimize3D > 0 ? 1 : 0);
                gmsh::option::setNumber("Mesh.Optimize", 1);
                gmsh::option::setNumber("Mesh.Smoothing", std::max(1, optimize2D));
                if (quadDominated)
                {
                    gmsh::option::setNumber("Mesh.RecombineAll", 1);
                    gmsh::option::setNumber("Mesh.RecombinationAlgorithm", 1);
                }
                if (secondOrder)
                {
                    gmsh::option::setNumber("Mesh.ElementOrder", 2);
                    gmsh::option::setNumber("Mesh.SecondOrderIncomplete", 1);
                }
                // The local sizes, spread with the grading of the mesh
                if (!local.points.empty() || !local.lines.empty())
                {
                    double rate = std::max(0.1, grading);
                    gmsh::model::mesh::setSizeCallback([local, rate, maxh](int, int, double x, double y, double z, double lc) {
                        double p[3] = { x, y, z };
                        double h = std::min(lc, maxh);
                        for (auto& q : local.points)
                        {
                            double d = std::sqrt((x - q[0]) * (x - q[0]) + (y - q[1]) * (y - q[1]) + (z - q[2]) * (z - q[2]));
                            h = std::min(h, q[3] + rate * d);
                        }
                        for (auto& l : local.lines)
                        {
                            double d = DistanceToSegment(p, &l[0], &l[3]);
                            h = std::min(h, l[6] + rate * d);
                        }
                        return h;
                    });
                }
                int dimension = onlyEdges ? 1 : (maps.solids.Extent() > 0 ? 3 : 2);
                Log("Meshing " + std::string(dimension == 3 ? "solid" : dimension == 2 ? "surface" : "edge") + " geometry...");
                gmsh::model::mesh::generate(dimension);
                if (secondOrder && !onlyEdges) gmsh::model::mesh::setOrder(2);
                //
                std::map<int, int> faceIds = MatchEntities(2, maps.faces);
                std::map<int, int> edgeIds = MatchEntities(1, maps.edges);
                // Nodes
                std::vector<size_t> nodeTags;
                std::vector<double> coordinates, parametric;
                gmsh::model::mesh::getNodes(nodeTags, coordinates, parametric, -1, -1, false, false);
                std::map<size_t, size_t> nodeIndex;
                for (size_t i = 0; i < nodeTags.size(); i++) nodeIndex[nodeTags[i]] = i + 1;
                auto index = [&](size_t tag) { return nodeIndex[tag]; };
                //
                std::ostringstream surface, volume, edges;
                size_t numSurface = 0, numVolume = 0, numEdges = 0;
                // Surface elements, turned to the side of their face
                gmsh::vectorpair faces;
                gmsh::model::getEntities(faces, 2);
                for (auto& face : faces)
                {
                    int faceId = faceIds.count(face.second) ? faceIds[face.second] : face.second;
                    std::vector<int> types;
                    std::vector<std::vector<size_t>> tags, nodes;
                    gmsh::model::mesh::getElements(types, tags, nodes, 2, face.second);
                    bool flip = false;
                    if (!onlyEdges && faceId >= 1 && faceId <= maps.faces.Extent())
                    {
                        // The normal of the first element against the normal of the face
                        for (size_t t = 0; t < types.size() && !flip; t++)
                        {
                            int nn = NodesOfType(types[t]);
                            if (nn == 0 || nodes[t].size() < (size_t)nn) continue;
                            const size_t* n = &nodes[t][0];
                            double c[3][3];
                            for (int k = 0; k < 3; k++)
                            {
                                size_t i = nodeIndex[n[k]] - 1;
                                c[k][0] = coordinates[3 * i]; c[k][1] = coordinates[3 * i + 1]; c[k][2] = coordinates[3 * i + 2];
                            }
                            gp_Vec e1(c[1][0] - c[0][0], c[1][1] - c[0][1], c[1][2] - c[0][2]);
                            gp_Vec e2(c[2][0] - c[0][0], c[2][1] - c[0][1], c[2][2] - c[0][2]);
                            gp_Vec normal = e1.Crossed(e2);
                            gp_Pnt centre((c[0][0] + c[1][0] + c[2][0]) / 3, (c[0][1] + c[1][1] + c[2][1]) / 3,
                                          (c[0][2] + c[1][2] + c[2][2]) / 3);
                            TopoDS_Face occFace = TopoDS::Face(maps.faces(faceId));
                            Handle(Geom_Surface) geomSurface = BRep_Tool::Surface(occFace);
                            GeomAPI_ProjectPointOnSurf projector(centre, geomSurface);
                            if (projector.NbPoints() == 0) break;
                            double u, v;
                            projector.LowerDistanceParameters(u, v);
                            BRepAdaptor_Surface adaptor(occFace, false);
                            BRepLProp_SLProps props(adaptor, u, v, 1, 1e-9);
                            if (!props.IsNormalDefined()) break;
                            gp_Dir faceNormal = props.Normal();
                            if (occFace.Orientation() == TopAbs_REVERSED) faceNormal.Reverse();
                            flip = normal.Dot(gp_Vec(faceNormal)) < 0;
                            break;
                        }
                    }
                    for (size_t t = 0; t < types.size(); t++)
                    {
                        int nn = NodesOfType(types[t]);
                        if (nn == 0) continue;
                        for (size_t e = 0; e + nn <= nodes[t].size(); e += nn)
                        {
                            std::vector<size_t> a(nodes[t].begin() + e, nodes[t].begin() + e + nn);
                            if (flip)
                            {
                                // The same element seen from the other side, in the gmsh (Abaqus) order
                                std::vector<size_t> o = a;
                                if (nn == 3) a = { o[0], o[2], o[1] };
                                else if (nn == 4) a = { o[0], o[3], o[2], o[1] };
                                else if (nn == 6) a = { o[0], o[2], o[1], o[5], o[4], o[3] };
                                else if (nn == 8) a = { o[0], o[3], o[2], o[1], o[7], o[6], o[5], o[4] };
                            }
                            std::vector<size_t> order = SurfaceOrder(types[t], a.data());
                            if (order.empty()) continue;
                            surface << faceId << " " << faceId << " 1 0 " << order.size();
                            for (size_t node : order) surface << " " << index(node);
                            surface << "\n";
                            numSurface++;
                        }
                    }
                }
                // Volume elements
                gmsh::vectorpair solids;
                gmsh::model::getEntities(solids, 3);
                int material = 0;
                for (auto& solid : solids)
                {
                    material++;
                    std::vector<int> types;
                    std::vector<std::vector<size_t>> tags, nodes;
                    gmsh::model::mesh::getElements(types, tags, nodes, 3, solid.second);
                    for (size_t t = 0; t < types.size(); t++)
                    {
                        int nn = NodesOfType(types[t]);
                        std::vector<size_t> probe = nn > 0 && nodes[t].size() >= (size_t)nn ?
                                                    VolumeOrder(types[t], &nodes[t][0]) : std::vector<size_t>();
                        if (probe.empty())
                        {
                            if (!nodes[t].empty()) Log("Warning: volume elements of an unsupported type were skipped.");
                            continue;
                        }
                        for (size_t e = 0; e + nn <= nodes[t].size(); e += nn)
                        {
                            std::vector<size_t> order = VolumeOrder(types[t], &nodes[t][e]);
                            volume << material << " " << order.size();
                            for (size_t node : order) volume << " " << index(node);
                            volume << "\n";
                            numVolume++;
                        }
                    }
                }
                // Edge segments with the parameters of their nodes along the edge
                gmsh::vectorpair curves;
                gmsh::model::getEntities(curves, 1);
                TopTools_IndexedDataMapOfShapeListOfShape edgeFaces;
                TopExp::MapShapesAndAncestors(shape, TopAbs_EDGE, TopAbs_FACE, edgeFaces);
                for (auto& curve : curves)
                {
                    int edgeId = edgeIds.count(curve.second) ? edgeIds[curve.second] : curve.second;
                    int faceId = 0;
                    if (edgeId >= 1 && edgeId <= maps.edges.Extent())
                    {
                        int k = edgeFaces.FindIndex(maps.edges(edgeId));
                        if (k > 0 && !edgeFaces(k).IsEmpty()) faceId = maps.faces.FindIndex(edgeFaces(k).First());
                    }
                    std::vector<size_t> curveNodes;
                    std::vector<double> curveCoordinates, curveParameters;
                    gmsh::model::mesh::getNodes(curveNodes, curveCoordinates, curveParameters, 1, curve.second, true, true);
                    std::map<size_t, double> parameter;
                    for (size_t i = 0; i < curveNodes.size() && i < curveParameters.size(); i++)
                        parameter[curveNodes[i]] = curveParameters[i];
                    std::vector<int> types;
                    std::vector<std::vector<size_t>> tags, nodes;
                    gmsh::model::mesh::getElements(types, tags, nodes, 1, curve.second);
                    for (size_t t = 0; t < types.size(); t++)
                    {
                        int nn = NodesOfType(types[t]);
                        if (nn == 0) continue;
                        for (size_t e = 0; e + nn <= nodes[t].size(); e += nn)
                        {
                            size_t a = nodes[t][e], b = nodes[t][e + 1];
                            edges << faceId << " 0 " << index(a) << " " << index(b) << " -1 -1 0 0 " << edgeId << " "
                                  << Num(parameter.count(a) ? parameter[a] : 0) << " " << edgeId << " "
                                  << Num(parameter.count(b) ? parameter[b] : 0) << "\n";
                            numEdges++;
                        }
                    }
                }
                //
                std::ofstream out(args[1]);
                out << "mesh3d\n";
                out << "dimension\n3\n";
                out << "geomtype\n11\n";
                out << "\n# surfnr    bcnr   domin  domout      np      p1      p2      p3\n";
                out << "surfaceelements\n" << numSurface << "\n" << surface.str();
                out << "\n#  matnr      np      p1      p2      p3      p4\n";
                out << "volumeelements\n" << numVolume << "\n" << volume.str();
                out << "\n# surfid  0   p1   p2   trignum1    trignum2   domin/surfnr1    domout/surfnr2   ednr1   dist1   ednr2   dist2\n";
                out << "edgesegmentsgi2\n" << numEdges << "\n" << edges.str();
                out << "\n#          X             Y             Z\n";
                out << "points\n" << nodeTags.size() << "\n";
                for (size_t i = 0; i < nodeTags.size(); i++)
                    out << Num(coordinates[3 * i]) << " " << Num(coordinates[3 * i + 1]) << " " << Num(coordinates[3 * i + 2]) << "\n";
                out << "\nendmesh\n";
                out.close();
                Log("Mesh: " + std::to_string(nodeTags.size()) + " nodes, " + std::to_string(numVolume) +
                    " volume elements, " + std::to_string(numSurface) + " surface elements.");
                if (!onlyEdges && numSurface == 0 && numVolume == 0)
                {
                    Error("The mesher created no elements.");
                    status = 1;
                }
            }
            catch (const std::exception& ex)
            {
                Error(std::string("Meshing failed: ") + ex.what());
                status = 1;
            }
            catch (...)
            {
                Error("Meshing failed.");
                status = 1;
            }
            gmsh::finalize();
            return status;
        }
    }

    void RegisterMeshCommands()
    {
        RegisterCommand("BREP_MESH", [](auto& a) { return MeshBrep(a, false); });
        RegisterCommand("BREP_EDGE_MESH", [](auto& a) { return MeshBrep(a, true); });
    }
}
