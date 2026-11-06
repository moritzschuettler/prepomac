// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The section view of a view: the parts are clipped by the section plane and each part with a volume grid gets a
// section actor, the cut of its volume grid or the cap of its closed surface, with the cut element edges, the
// outline and optionally a hatch; the element-wise section keeps whole elements instead. Port of the Section view
// region of vtkControl and of SectionViewCap and SectionViewHatch.
#include "vtkNativeError.h"
#include "vtkNativeView.h"

#include <vtkCellArray.h>
#include <vtkCellData.h>
#include <vtkCleanPolyData.h>
#include <vtkClipClosedSurface.h>
#include <vtkCutter.h>
#include <vtkDataArray.h>
#include <vtkDataSetSurfaceFilter.h>
#include <vtkDelaunay2D.h>
#include <vtkExtractCells.h>
#include <vtkExtractEdges.h>
#include <vtkExtractGeometry.h>
#include <vtkFeatureEdges.h>
#include <vtkGeometryFilter.h>
#include <vtkGenericCell.h>
#include <vtkIdList.h>
#include <vtkMapper.h>
#include <vtkNew.h>
#include <vtkPlaneCollection.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkStripper.h>
#include <vtkUnstructuredGridGeometryFilter.h>
#include <vtkUnstructuredGrid.h>

#include <algorithm>
#include <array>
#include <cfloat>
#include <cmath>
#include <cstdint>
#include <map>
#include <queue>
#include <set>
#include <vector>

namespace vtkn
{
    namespace
    {
        int64_t GetEdgeKey(vtkIdType first, vtkIdType second)
        {
            return first < second ? (static_cast<int64_t>(first) << 32) + second
                                  : (static_cast<int64_t>(second) << 32) + first;
        }

        // SectionViewCap.GetPlaneAxes: any direction in the plane, and the one completing a right handed system
        void GetPlaneAxes(vtkPlane* plane, double firstAxis[3], double secondAxis[3])
        {
            double normal[3];
            plane->GetNormal(normal);
            double length = std::sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            for (double& value : normal) value /= length;
            double axis[3] = { 1, 0, 0 };
            if (std::abs(normal[0]) >= 0.9) axis[0] = 0, axis[1] = 1;
            double projection = axis[0] * normal[0] + axis[1] * normal[1] + axis[2] * normal[2];
            for (int i = 0; i < 3; i++) axis[i] -= projection * normal[i];
            length = std::sqrt(axis[0] * axis[0] + axis[1] * axis[1] + axis[2] * axis[2]);
            for (int i = 0; i < 3; i++) firstAxis[i] = axis[i] / length;
            secondAxis[0] = normal[1] * firstAxis[2] - normal[2] * firstAxis[1];
            secondAxis[1] = normal[2] * firstAxis[0] - normal[0] * firstAxis[2];
            secondAxis[2] = normal[0] * firstAxis[1] - normal[1] * firstAxis[0];
        }

        double GetSignedArea(const std::vector<std::array<double, 2>>& points, const vtkIdType* triangle)
        {
            const auto& first = points[triangle[0]];
            const auto& second = points[triangle[1]];
            const auto& third = points[triangle[2]];
            return 0.5 * ((second[0] - first[0]) * (third[1] - first[1]) -
                          (third[0] - first[0]) * (second[1] - first[1]));
        }

        // SectionViewCap.GetTrianglesInsideTheCut: crossing a loop edge turns the inside into the outside
        bool GetTrianglesInsideTheCut(const std::vector<std::array<vtkIdType, 3>>& triangles,
                                      const std::set<int64_t>& loopEdges, std::vector<int>& insideTheCut)
        {
            std::map<int64_t, std::vector<int>> edgeTriangles;
            for (int i = 0; i < static_cast<int>(triangles.size()); i++)
                for (int j = 0; j < 3; j++)
                    edgeTriangles[GetEdgeKey(triangles[i][j], triangles[i][(j + 1) % 3])].push_back(i);
            insideTheCut.assign(triangles.size(), -1);
            std::queue<int> queue;
            for (const auto& entry : edgeTriangles)
            {
                if (entry.second.size() != 1 || insideTheCut[entry.second[0]] >= 0) continue;
                insideTheCut[entry.second[0]] = loopEdges.count(entry.first) > 0 ? 1 : 0;
                queue.push(entry.second[0]);
            }
            while (!queue.empty())
            {
                int triangleId = queue.front();
                queue.pop();
                for (int j = 0; j < 3; j++)
                {
                    int64_t edgeKey = GetEdgeKey(triangles[triangleId][j], triangles[triangleId][(j + 1) % 3]);
                    int neighbourSide = loopEdges.count(edgeKey) > 0 ? 1 - insideTheCut[triangleId]
                                                                     : insideTheCut[triangleId];
                    for (int neighbour : edgeTriangles[edgeKey])
                    {
                        if (neighbour == triangleId) continue;
                        if (insideTheCut[neighbour] >= 0)
                        {
                            if (insideTheCut[neighbour] != neighbourSide) return false;
                        }
                        else
                        {
                            insideTheCut[neighbour] = neighbourSide;
                            queue.push(neighbour);
                        }
                    }
                }
            }
            for (int side : insideTheCut) if (side < 0) return false;
            return true;
        }

        // Port of SectionViewCap.GetCap: the area the closed loops of the cut enclose, triangulated on the plane
        vtkSmartPointer<vtkPolyData> GetCap(vtkPolyData* closedSurface, vtkPlane* plane)
        {
            vtkNew<vtkCutter> cutter;
            cutter->SetCutFunction(plane);
            cutter->SetInputData(closedSurface);
            cutter->GenerateCutScalarsOff();
            vtkNew<vtkCleanPolyData> clean;
            clean->SetInputConnection(cutter->GetOutputPort());
            clean->PointMergingOn();
            clean->ConvertLinesToPointsOff();
            clean->ConvertPolysToLinesOff();
            clean->ConvertStripsToPolysOff();
            clean->Update();
            vtkPolyData* cut = clean->GetOutput();
            vtkIdType numOfPoints = cut->GetNumberOfPoints();
            if (numOfPoints < 3) return nullptr;
            //
            double origin[3];
            double firstAxis[3];
            double secondAxis[3];
            plane->GetOrigin(origin);
            GetPlaneAxes(plane, firstAxis, secondAxis);
            std::vector<std::array<double, 2>> planePoints(numOfPoints);
            for (vtkIdType i = 0; i < numOfPoints; i++)
            {
                double point[3];
                cut->GetPoint(i, point);
                double d[3] = { point[0] - origin[0], point[1] - origin[1], point[2] - origin[2] };
                planePoints[i] = { d[0] * firstAxis[0] + d[1] * firstAxis[1] + d[2] * firstAxis[2],
                                   d[0] * secondAxis[0] + d[1] * secondAxis[1] + d[2] * secondAxis[2] };
            }
            std::vector<int> pointDegrees(numOfPoints, 0);
            std::set<int64_t> loopEdges;
            vtkNew<vtkCellArray> loopLines;
            double loopArea = 0;
            vtkNew<vtkIdList> cellPoints;
            for (vtkIdType i = 0; i < cut->GetNumberOfCells(); i++)
            {
                cut->GetCellPoints(i, cellPoints);
                for (vtkIdType j = 0; j + 1 < cellPoints->GetNumberOfIds(); j++)
                {
                    vtkIdType firstId = cellPoints->GetId(j);
                    vtkIdType secondId = cellPoints->GetId(j + 1);
                    if (firstId == secondId || !loopEdges.insert(GetEdgeKey(firstId, secondId)).second) continue;
                    pointDegrees[firstId]++;
                    pointDegrees[secondId]++;
                    loopArea += 0.5 * (planePoints[firstId][0] * planePoints[secondId][1] -
                                       planePoints[secondId][0] * planePoints[firstId][1]);
                    vtkIdType ids[2] = { firstId, secondId };
                    loopLines->InsertNextCell(2, ids);
                }
            }
            for (int degree : pointDegrees) if (degree != 0 && degree != 2) return nullptr;
            loopArea = std::abs(loopArea);
            if (loopArea <= 0) return nullptr;
            //
            vtkNew<vtkPoints> flatPoints;
            flatPoints->SetNumberOfPoints(numOfPoints);
            for (vtkIdType i = 0; i < numOfPoints; i++) flatPoints->SetPoint(i, planePoints[i][0], planePoints[i][1], 0);
            vtkNew<vtkPolyData> flatCut;
            flatCut->SetPoints(flatPoints);
            vtkNew<vtkPolyData> loops;
            loops->SetPoints(flatPoints);
            loops->SetLines(loopLines);
            vtkNew<vtkDelaunay2D> delaunay;
            delaunay->SetInputData(flatCut);
            delaunay->SetSourceData(loops);
            delaunay->SetProjectionPlaneMode(0);
            delaunay->SetTolerance(0);
            delaunay->BoundingTriangulationOff();
            delaunay->Update();
            vtkPolyData* triangulated = delaunay->GetOutput();
            vtkIdType numOfTriangles = triangulated->GetNumberOfPolys();
            if (numOfTriangles == 0) return nullptr;
            std::vector<std::array<vtkIdType, 3>> triangles(numOfTriangles);
            for (vtkIdType i = 0; i < numOfTriangles; i++)
            {
                triangulated->GetCellPoints(i, cellPoints);
                triangles[i] = { cellPoints->GetId(0), cellPoints->GetId(1), cellPoints->GetId(2) };
            }
            std::vector<int> insideTheCut;
            if (!GetTrianglesInsideTheCut(triangles, loopEdges, insideTheCut)) return nullptr;
            //
            vtkNew<vtkCellArray> capCells;
            double capArea = 0;
            for (vtkIdType i = 0; i < numOfTriangles; i++)
            {
                if (insideTheCut[i] != 1) continue;
                double triangleArea = GetSignedArea(planePoints, triangles[i].data());
                capArea += std::abs(triangleArea);
                // The section face of the remaining part faces away from it, opposite to the plane normal
                vtkIdType ids[3] = { triangles[i][0], triangleArea > 0 ? triangles[i][2] : triangles[i][1],
                                     triangleArea > 0 ? triangles[i][1] : triangles[i][2] };
                capCells->InsertNextCell(3, ids);
            }
            if (std::abs(capArea - loopArea) > 0.001 * loopArea) return nullptr;
            vtkSmartPointer<vtkPolyData> cap = vtkSmartPointer<vtkPolyData>::New();
            cap->SetPoints(cut->GetPoints());
            cap->SetPolys(capCells);
            return cap;
        }

        bool IsClosedSurface(vtkPolyData* surface)
        {
            vtkNew<vtkFeatureEdges> featureEdges;
            featureEdges->SetInputData(surface);
            featureEdges->BoundaryEdgesOn();
            featureEdges->FeatureEdgesOff();
            featureEdges->ManifoldEdgesOff();
            featureEdges->NonManifoldEdgesOn();
            featureEdges->SetColoring(0);
            featureEdges->Update();
            return featureEdges->GetOutput()->GetNumberOfCells() == 0;
        }

        // Port of GetSectionCap: the triangulated loops, else the faces vtkClipClosedSurface makes on the plane
        vtkSmartPointer<vtkPolyData> GetSectionCap(vtkPolyData* surface, vtkPlane* plane)
        {
            vtkSmartPointer<vtkPolyData> cap = GetCap(surface, plane);
            if (cap != nullptr) return cap;
            vtkNew<vtkPlaneCollection> planes;
            planes->AddItem(plane);
            vtkNew<vtkClipClosedSurface> clipper;
            clipper->SetInputData(surface);
            clipper->SetClippingPlanes(planes);
            clipper->GenerateFacesOn();
            clipper->GenerateOutlineOff();
            clipper->SetScalarModeToLabels();
            clipper->Update();
            vtkPolyData* clipped = clipper->GetOutput();
            vtkDataArray* labels = clipped->GetCellData()->GetScalars();
            vtkNew<vtkCellArray> capCells;
            vtkNew<vtkIdList> cellPoints;
            for (vtkIdType i = 0; labels != nullptr && i < clipped->GetNumberOfCells(); i++)
            {
                if (labels->GetTuple1(i) != 1) continue;
                clipped->GetCellPoints(i, cellPoints);
                capCells->InsertNextCell(cellPoints);
            }
            cap = vtkSmartPointer<vtkPolyData>::New();
            cap->SetPoints(clipped->GetPoints());
            cap->SetPolys(capCells);
            return cap;
        }

        // Port of SectionViewHatch.GetHatch: parallel lines across the section faces, counted from the plane origin;
        // neighbouring sections alternate the direction and every second pair the spacing
        vtkSmartPointer<vtkPolyData> GetHatch(vtkPolyData* sectionFaces, vtkPlane* plane, double spacing,
                                              int hatchIndex)
        {
            const int maxNumberOfLines = 2000;
            if (sectionFaces == nullptr || sectionFaces->GetNumberOfCells() == 0 || spacing <= 0) return nullptr;
            double angle = (hatchIndex % 2 == 0 ? 45 : 135) * 3.14159265358979323846 / 180;
            spacing *= 1 + 0.5 * ((hatchIndex / 2) % 3);
            double firstAxis[3];
            double secondAxis[3];
            GetPlaneAxes(plane, firstAxis, secondAxis);
            double lineNormal[3];
            for (int i = 0; i < 3; i++) lineNormal[i] = -std::sin(angle) * firstAxis[i] + std::cos(angle) * secondAxis[i];
            double origin[3];
            plane->GetOrigin(origin);
            double minDistance = DBL_MAX;
            double maxDistance = -DBL_MAX;
            for (vtkIdType i = 0; i < sectionFaces->GetNumberOfPoints(); i++)
            {
                double point[3];
                sectionFaces->GetPoint(i, point);
                double distance = (point[0] - origin[0]) * lineNormal[0] + (point[1] - origin[1]) * lineNormal[1] +
                                  (point[2] - origin[2]) * lineNormal[2];
                minDistance = std::min(minDistance, distance);
                maxDistance = std::max(maxDistance, distance);
            }
            int firstLine = static_cast<int>(std::ceil(minDistance / spacing));
            int lastLine = static_cast<int>(std::floor(maxDistance / spacing));
            int numOfLines = lastLine - firstLine + 1;
            if (numOfLines <= 0 || numOfLines > maxNumberOfLines) return nullptr;
            vtkNew<vtkPlane> linePlane;
            linePlane->SetOrigin(origin);
            linePlane->SetNormal(lineNormal);
            vtkNew<vtkCutter> cutter;
            cutter->SetCutFunction(linePlane);
            cutter->SetInputData(sectionFaces);
            cutter->GenerateCutScalarsOff();
            cutter->SetNumberOfContours(numOfLines);
            for (int i = 0; i < numOfLines; i++) cutter->SetValue(i, (firstLine + i) * spacing);
            cutter->Update();
            vtkSmartPointer<vtkPolyData> hatch = vtkSmartPointer<vtkPolyData>::New();
            hatch->SetPoints(cutter->GetOutput()->GetPoints());
            hatch->SetLines(cutter->GetOutput()->GetLines());
            return hatch;
        }

        vtkSmartPointer<vtkPolyData> Copy(vtkPolyData* polyData, bool keepScalars)
        {
            vtkSmartPointer<vtkPolyData> copy = vtkSmartPointer<vtkPolyData>::New();
            copy->DeepCopy(polyData);
            if (!keepScalars) copy->GetPointData()->RemoveArray("scalars");
            return copy;
        }

        // The element-wise section (GetSectionViewActorsElementWise and its helpers)
        typedef std::vector<vtkIdType> FaceNodeKey;

        // GetPlaneSideOfBounds: 1 when the box is all on the visible side, -1 when all behind, else 0
        int GetPlaneSideOfBounds(vtkPlane* plane, const double bounds[6])
        {
            bool positive = false;
            bool negative = false;
            for (int i = 0; i < 8; i++)
            {
                double value = plane->EvaluateFunction(bounds[i & 1], bounds[2 + ((i >> 1) & 1)], bounds[4 + ((i >> 2) & 1)]);
                if (value > 0) positive = true;
                else negative = true;
            }
            if (positive && !negative) return 1;
            if (negative && !positive) return -1;
            return 0;
        }

        vtkDataArray* GetNodeIds(vtkDataSet* data) { return data->GetPointData()->GetArray(SectionViewNodeIdsName); }
        vtkDataArray* GetElementIds(vtkDataSet* data) { return data->GetCellData()->GetArray(SectionViewElementIdsName); }

        FaceNodeKey GetFaceNodeKey(vtkIdType count, const vtkIdType* pointIds, vtkDataArray* nodeIds)
        {
            FaceNodeKey key(count);
            for (vtkIdType i = 0; i < count; i++) key[i] = static_cast<vtkIdType>(nodeIds->GetTuple1(pointIds[i]));
            std::sort(key.begin(), key.end());
            return key;
        }

        // Unlike vtkGeometryFilter this filter keeps the quadratic faces as quadratic cells
        vtkSmartPointer<vtkUnstructuredGrid> GetBoundary(vtkUnstructuredGrid* grid)
        {
            vtkNew<vtkUnstructuredGridGeometryFilter> geometryFilter;
            geometryFilter->SetInputData(grid);
            geometryFilter->Update();
            return vtkUnstructuredGrid::SafeDownCast(geometryFilter->GetOutput());
        }

        std::set<FaceNodeKey> GetFaceNodeKeys(vtkUnstructuredGrid* faces)
        {
            std::set<FaceNodeKey> keys;
            vtkDataArray* nodeIds = GetNodeIds(faces);
            vtkNew<vtkIdList> pointIds;
            for (vtkIdType i = 0; i < faces->GetNumberOfCells(); i++)
            {
                faces->GetCellPoints(i, pointIds);
                keys.insert(GetFaceNodeKey(pointIds->GetNumberOfIds(), pointIds->GetPointer(0), nodeIds));
            }
            return keys;
        }

        // LabelSectionFaces: a face of the remaining elements that is not on the part surface was exposed by the cut
        std::vector<int> LabelSectionFaces(vtkUnstructuredGrid* boundary, const std::set<FaceNodeKey>& outerFaces)
        {
            vtkDataArray* nodeIds = GetNodeIds(boundary);
            std::vector<int> faceTypes(boundary->GetNumberOfCells());
            vtkNew<vtkIdList> pointIds;
            for (vtkIdType i = 0; i < boundary->GetNumberOfCells(); i++)
            {
                boundary->GetCellPoints(i, pointIds);
                FaceNodeKey key = GetFaceNodeKey(pointIds->GetNumberOfIds(), pointIds->GetPointer(0), nodeIds);
                faceTypes[i] = outerFaces.count(key) > 0 ? 0 : 1;
            }
            return faceTypes;
        }

        // ExtractSectionFaces: the faces named by their ids, with the ids as global ids
        vtkSmartPointer<vtkUnstructuredGrid> ExtractSectionFaces(vtkUnstructuredGrid* boundary,
                                                                 const std::vector<int>& faceTypes, int faceType)
        {
            vtkNew<vtkIdList> cellIds;
            for (size_t i = 0; i < faceTypes.size(); i++)
                if (faceTypes[i] == faceType) cellIds->InsertNextId(static_cast<vtkIdType>(i));
            if (cellIds->GetNumberOfIds() == 0) return vtkSmartPointer<vtkUnstructuredGrid>::New();
            vtkNew<vtkExtractCells> extract;
            extract->SetInputData(boundary);
            extract->SetCellList(cellIds);
            extract->Update();
            vtkSmartPointer<vtkUnstructuredGrid> faces = vtkUnstructuredGrid::SafeDownCast(extract->GetOutput());
            return faces;
        }

        // GetFaceEdges: the corner pairs of a face, with the mid node of a quadratic edge last
        std::vector<std::vector<int>> GetFaceEdges(int cellType, int numOfPoints)
        {
            if (cellType == VTK_TRIANGLE) return { { 0, 1 }, { 1, 2 }, { 2, 0 } };
            if (cellType == VTK_QUAD) return { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 } };
            if (cellType == VTK_QUADRATIC_TRIANGLE) return { { 0, 1, 3 }, { 1, 2, 4 }, { 2, 0, 5 } };
            if (cellType == VTK_QUADRATIC_QUAD) return { { 0, 1, 4 }, { 1, 2, 5 }, { 2, 3, 6 }, { 3, 0, 7 } };
            std::vector<std::vector<int>> edges;
            if (cellType == VTK_POLYGON)
                for (int i = 0; i < numOfPoints; i++) edges.push_back({ i, (i + 1) % numOfPoints });
            return edges;
        }

        // GetSectionOutline: the edges of the exposed faces that belong to a single exposed face
        vtkSmartPointer<vtkUnstructuredGrid> GetSectionOutline(vtkUnstructuredGrid* boundary,
                                                               const std::vector<int>& faceTypes)
        {
            std::map<int64_t, std::vector<vtkIdType>> outlineEdges;
            std::set<int64_t> sharedEdges;
            vtkNew<vtkIdList> pointIds;
            for (vtkIdType i = 0; i < boundary->GetNumberOfCells(); i++)
            {
                if (faceTypes[i] != 1) continue;
                boundary->GetCellPoints(i, pointIds);
                for (const std::vector<int>& edge : GetFaceEdges(boundary->GetCellType(i),
                                                                 static_cast<int>(pointIds->GetNumberOfIds())))
                {
                    int64_t key = GetEdgeKey(pointIds->GetId(edge[0]), pointIds->GetId(edge[1]));
                    if (outlineEdges.count(key) > 0) sharedEdges.insert(key);
                    else
                    {
                        std::vector<vtkIdType> edgePointIds;
                        for (int index : edge) edgePointIds.push_back(pointIds->GetId(index));
                        outlineEdges[key] = edgePointIds;
                    }
                }
            }
            vtkSmartPointer<vtkUnstructuredGrid> outline = vtkSmartPointer<vtkUnstructuredGrid>::New();
            outline->SetPoints(boundary->GetPoints());
            outline->Allocate(static_cast<vtkIdType>(outlineEdges.size()));
            for (const auto& entry : outlineEdges)
            {
                if (sharedEdges.count(entry.first) > 0) continue;
                const std::vector<vtkIdType>& ids = entry.second;
                outline->InsertNextCell(ids.size() == 2 ? VTK_LINE : VTK_QUADRATIC_EDGE,
                                        static_cast<vtkIdType>(ids.size()), ids.data());
            }
            return outline;
        }

        // The surface vtkDataSetMapper draws for a grid
        vtkSmartPointer<vtkPolyData> GetSurface(vtkDataSet* data)
        {
            vtkNew<vtkDataSetSurfaceFilter> surfaceFilter;
            surfaceFilter->SetInputData(data);
            surfaceFilter->Update();
            return surfaceFilter->GetOutput();
        }

        // GetSurfaceOfKeptElements: the triangles of the surface of a kept element, and the element edges along them
        vtkSmartPointer<vtkPolyData> GetSurfaceOfKeptElements(vtkPolyData* surface, vtkPolyData* edges,
                                                              const std::set<int>& keptElementIds,
                                                              vtkSmartPointer<vtkPolyData>& croppedEdges)
        {
            vtkDataArray* elementIds = surface->GetCellData()->GetGlobalIds();
            vtkSmartPointer<vtkPolyData> cropped = vtkSmartPointer<vtkPolyData>::New();
            cropped->SetPoints(surface->GetPoints());
            cropped->GetPointData()->ShallowCopy(surface->GetPointData());
            vtkNew<vtkCellArray> polys;
            vtkNew<vtkIdTypeArray> croppedElementIds;
            std::set<int64_t> keptEdges;
            vtkIdType cellId = surface->GetNumberOfVerts() + surface->GetNumberOfLines();
            vtkCellArray* surfacePolys = surface->GetPolys();
            vtkNew<vtkIdList> pointIds;
            for (vtkIdType i = 0; i < surfacePolys->GetNumberOfCells(); i++, cellId++)
            {
                surfacePolys->GetCellAtId(i, pointIds);
                if (cellId >= elementIds->GetNumberOfTuples()) continue;
                int elementId = static_cast<int>(elementIds->GetTuple1(cellId));
                if (keptElementIds.count(elementId) == 0) continue;
                polys->InsertNextCell(pointIds);
                croppedElementIds->InsertNextValue(elementId);
                vtkIdType count = pointIds->GetNumberOfIds();
                for (vtkIdType j = 0; j < count; j++)
                    keptEdges.insert(GetEdgeKey(pointIds->GetId(j), pointIds->GetId((j + 1) % count)));
            }
            cropped->SetPolys(polys);
            if (croppedElementIds->GetNumberOfTuples() > 0) cropped->GetCellData()->SetGlobalIds(croppedElementIds);
            //
            croppedEdges = vtkSmartPointer<vtkPolyData>::New();
            if (edges != nullptr)
            {
                croppedEdges->SetPoints(edges->GetPoints());
                vtkNew<vtkCellArray> lines;
                vtkCellArray* edgeLines = edges->GetLines();
                for (vtkIdType i = 0; i < edgeLines->GetNumberOfCells(); i++)
                {
                    edgeLines->GetCellAtId(i, pointIds);
                    vtkIdType count = pointIds->GetNumberOfIds();
                    bool keep = count >= 2;
                    for (vtkIdType j = 0; keep && j < count - 1; j++)
                        keep = keptEdges.count(GetEdgeKey(pointIds->GetId(j), pointIds->GetId(j + 1))) > 0;
                    if (keep) lines->InsertNextCell(pointIds);
                }
                croppedEdges->SetLines(lines);
            }
            return cropped;
        }

        // GetModelEdgesOfSkin: a model edge line remains when it is a half edge of a remaining face
        vtkSmartPointer<vtkPolyData> GetModelEdgesOfSkin(vtkPolyData* modelEdges, vtkUnstructuredGrid* skinFaces)
        {
            vtkSmartPointer<vtkPolyData> cropped = vtkSmartPointer<vtkPolyData>::New();
            if (modelEdges == nullptr || modelEdges->GetPointData()->GetGlobalIds() == nullptr) return cropped;
            std::set<int64_t> skinEdges;
            vtkDataArray* faceNodeIds = GetNodeIds(skinFaces);
            vtkNew<vtkIdList> pointIds;
            for (vtkIdType i = 0; faceNodeIds != nullptr && i < skinFaces->GetNumberOfCells(); i++)
            {
                skinFaces->GetCellPoints(i, pointIds);
                auto node = [&](int index) { return static_cast<vtkIdType>(faceNodeIds->GetTuple1(pointIds->GetId(index))); };
                for (const std::vector<int>& edge : GetFaceEdges(skinFaces->GetCellType(i),
                                                                 static_cast<int>(pointIds->GetNumberOfIds())))
                {
                    if (edge.size() == 2) skinEdges.insert(GetEdgeKey(node(edge[0]), node(edge[1])));
                    else
                    {
                        skinEdges.insert(GetEdgeKey(node(edge[0]), node(edge[2])));
                        skinEdges.insert(GetEdgeKey(node(edge[2]), node(edge[1])));
                    }
                }
            }
            vtkDataArray* edgeNodeIds = modelEdges->GetPointData()->GetGlobalIds();
            cropped->SetPoints(modelEdges->GetPoints());
            vtkNew<vtkCellArray> lines;
            vtkCellArray* edgeLines = modelEdges->GetLines();
            for (vtkIdType i = 0; i < edgeLines->GetNumberOfCells(); i++)
            {
                edgeLines->GetCellAtId(i, pointIds);
                if (pointIds->GetNumberOfIds() != 2) continue;
                vtkIdType first = static_cast<vtkIdType>(edgeNodeIds->GetTuple1(pointIds->GetId(0)));
                vtkIdType second = static_cast<vtkIdType>(edgeNodeIds->GetTuple1(pointIds->GetId(1)));
                if (skinEdges.count(GetEdgeKey(first, second)) > 0) lines->InsertNextCell(pointIds);
            }
            cropped->SetLines(lines);
            return cropped;
        }

        vtkPolyData* GetInput(vtkActor* actor)
        {
            return actor == nullptr ? nullptr : vtkPolyData::SafeDownCast(actor->GetMapper()->GetInput());
        }

        // The style of a skin: that of the actor it replaces, not a part and not picked
        vtkn_part_style GetSkinStyle(PartActor* actor)
        {
            vtkn_part_style style = actor->GetStyle();
            style.is_a_part = 0;
            style.pickable = 0;
            return style;
        }
    }

    // Port of CreateSectionView and UpdateSectionView: the plane object stays, so the clipped mappers follow it
    void View::CreateSectionView(const vtkn_section_view& sectionView)
    {
        bool update = SectionViewActive;
        if (update) ClearSectionViewActors();
        else
        {
            SectionPlane = vtkSmartPointer<vtkPlane>::New();
            for (PartActor* actor : GetBaseActors())
                if (actor->GetStyle().section_view_possible) actor->AddClippingPlane(SectionPlane);
        }
        SectionPlane->SetOrigin(sectionView.point[0], sectionView.point[1], sectionView.point[2]);
        SectionPlane->SetNormal(sectionView.normal[0], sectionView.normal[1], sectionView.normal[2]);
        SectionView = sectionView;
        SectionViewActive = true;
        std::vector<std::pair<std::string, PartActor*>> actors;
        for (const auto& entry : Parts) actors.push_back({ entry.first, entry.second.get() });
        for (const auto& entry : BaseActors) actors.push_back({ entry.first, entry.second.get() });
        SectionViewKeptElementIds.clear();
        for (const auto& entry : actors)
            if (entry.second->GetStyle().section_view_possible) AddSectionViewActors(entry.first, entry.second);
        if (SectionView.element_wise) AddSectionViewSkinActorsToSurfaceActors();
        ApplyEdgesVisibilityAndBackfaceCulling();
        AdjustCameraDistanceAndClipping();
    }

    void View::RemoveSectionView()
    {
        if (!SectionViewActive) return;
        for (PartActor* actor : GetBaseActors()) actor->RemoveAllClippingPlanes();
        ClearSectionViewActors();
        SectionViewActive = false;
        SectionPlane = nullptr;
        ApplyEdgesVisibilityAndBackfaceCulling();
        AdjustCameraDistanceAndClipping();
    }

    void View::ClearSectionViewActors()
    {
        for (const auto& entry : SectionViewActorNames)
        {
            auto actor = BaseActors.find(entry.second);
            if (actor == BaseActors.end()) continue;
            actor->second->RemoveFrom(Renderer);
            BaseActors.erase(actor);
        }
        SectionViewActorNames.clear();
        for (const auto& entry : SectionViewSkinActorNames)
        {
            auto actor = BaseActors.find(entry.second);
            if (actor == BaseActors.end()) continue;
            actor->second->RemoveFrom(Renderer);
            BaseActors.erase(actor);
        }
        SectionViewSkinActorNames.clear();
        for (PartActor* actor : GetBaseActors()) actor->SetGeometryHiddenBySectionView(false);
    }

    // Port of AddSectionViewActors and GetSectionViewActor
    void View::AddSectionViewActors(const std::string& name, PartActor* actor)
    {
        vtkUnstructuredGrid* volumeGrid = actor->GetVolumeGrid();
        if (volumeGrid == nullptr) return;
        int representation = actor->GetStyle().representation;
        std::unique_ptr<PartActor> sectionViewActor;
        if (representation == VTKN_REPRESENTATION_SOLID || representation == VTKN_REPRESENTATION_SHELL)
        {
            if (representation == VTKN_REPRESENTATION_SOLID && !actor->GetHasVolumeCells())
                sectionViewActor = GetSectionViewActorFromClosedSurface(actor);
            else if (SectionView.element_wise)
            {
                AddSectionViewActorsElementWise(name, actor);
                return;
            }
            else sectionViewActor = GetSectionViewActorFromSolid(actor);
        }
        else if (representation == VTKN_REPRESENTATION_SOLID_AS_SHELL)
            sectionViewActor = GetSectionViewActorFromClosedSurface(actor);
        if (sectionViewActor == nullptr) return;
        //
        PartActor* added = sectionViewActor.get();
        std::string sectionName = name + ":sectionView:1";
        AddActor(sectionName, std::move(sectionViewActor));
        SectionViewActorNames[name] = sectionName;
        if (SectionView.hatch)
        {
            vtkSmartPointer<vtkPolyData> hatch = GetHatch(added->GetPolyData(), SectionPlane, SectionView.hatch_spacing,
                                                          actor->GetHatchIndex());
            if (hatch != nullptr) added->SetSectionViewHatch(hatch, Renderer);
        }
    }

    // The style of a section actor: the part's, seen from both sides, in the section color, not picked
    vtkn_part_style View::GetSectionViewStyle(PartActor* actor)
    {
        vtkn_part_style style = actor->GetStyle();
        style.backface_culling = 0;
        style.pickable = 0;
        style.is_a_part = 0;
        std::copy(actor->GetSectionColor(), actor->GetSectionColor() + 4, style.color);
        return style;
    }

    // Port of GetSectionViewActorFromSolid: the cut of the volume grid, the element edges cut from the faces of the
    // cells on the plane (the plane moved a little when it lies on the faces), and the outline as model edges
    std::unique_ptr<PartActor> View::GetSectionViewActorFromSolid(PartActor* actor)
    {
        vtkNew<vtkUnstructuredGrid> grid;
        grid->DeepCopy(actor->GetVolumeGrid());
        vtkNew<vtkCutter> cutter;
        cutter->SetCutFunction(SectionPlane);
        cutter->SetInputData(grid);
        cutter->GenerateCutScalarsOff();
        vtkNew<vtkGeometryFilter> geometryFilter;
        geometryFilter->SetInputConnection(cutter->GetOutputPort());
        geometryFilter->Update();
        vtkSmartPointer<vtkPolyData> polyData = Copy(geometryFilter->GetOutput(), true);
        // The faces of the cells on the plane, each once, cut by the plane
        vtkNew<vtkExtractGeometry> extract;
        extract->SetImplicitFunction(SectionPlane);
        extract->ExtractBoundaryCellsOn();
        extract->ExtractOnlyBoundaryCellsOn();
        extract->SetInputData(grid);
        extract->Update();
        vtkUnstructuredGrid* extracted = vtkUnstructuredGrid::SafeDownCast(extract->GetOutput());
        vtkNew<vtkUnstructuredGrid> cellGrid;
        cellGrid->SetPoints(extracted->GetPoints());
        std::set<std::vector<vtkIdType>> faces;
        vtkNew<vtkGenericCell> cell;
        for (vtkIdType i = 0; i < extracted->GetNumberOfCells(); i++)
        {
            extracted->GetCell(i, cell);
            for (int j = 0; j < cell->GetNumberOfFaces(); j++)
            {
                vtkCell* cellFace = cell->GetFace(j);
                vtkIdList* facePointIds = cellFace->GetPointIds();
                std::vector<vtkIdType> key(facePointIds->GetPointer(0),
                                           facePointIds->GetPointer(0) + facePointIds->GetNumberOfIds());
                std::sort(key.begin(), key.end());
                if (faces.insert(key).second) cellGrid->InsertNextCell(cellFace->GetCellType(), facePointIds);
            }
        }
        vtkNew<vtkPlane> cutPlane;
        double origin[3];
        double normal[3];
        SectionPlane->GetOrigin(origin);
        SectionPlane->GetNormal(normal);
        cutPlane->SetOrigin(origin);
        cutPlane->SetNormal(normal);
        vtkNew<vtkCutter> edgeCutter;
        edgeCutter->SetCutFunction(cutPlane);
        edgeCutter->SetInputData(cellGrid);
        edgeCutter->GenerateCutScalarsOff();
        edgeCutter->Update();
        // A plane parallel to the faces may find no section: it is moved forward and backward and the found edges
        // are moved back onto the plane
        double bounds[6];
        double size[3] = { 0, 0, 0 };
        if (ComputeVisiblePropBounds(bounds))
            for (int i = 0; i < 3; i++) size[i] = bounds[2 * i + 1] - bounds[2 * i];
        double v[3] = { size[0] * 0.01 * normal[0], size[1] * 0.01 * normal[1], size[2] * 0.01 * normal[2] };
        if (cellGrid->GetNumberOfCells() > 0 && edgeCutter->GetOutput()->GetNumberOfCells() == 0)
        {
            int count = 0;
            int step = 0;
            while (count++ <= 4)
            {
                if (edgeCutter->GetOutput()->GetNumberOfCells() == 0)
                {
                    step = static_cast<int>(std::pow(-1, count) * (count + 1) / 2);
                    cutPlane->SetOrigin(origin[0] + step * v[0], origin[1] + step * v[1], origin[2] + step * v[2]);
                    edgeCutter->Update();
                }
                else
                {
                    vtkPoints* points = edgeCutter->GetOutput()->GetPoints();
                    for (vtkIdType i = 0; i < points->GetNumberOfPoints(); i++)
                    {
                        double coor[3];
                        points->GetPoint(i, coor);
                        points->SetPoint(i, coor[0] - step * v[0], coor[1] - step * v[1], coor[2] - step * v[2]);
                    }
                    break;
                }
            }
        }
        vtkSmartPointer<vtkPolyData> edges = Copy(edgeCutter->GetOutput(), false);
        vtkNew<vtkFeatureEdges> featureEdges;
        featureEdges->SetInputData(polyData);
        featureEdges->BoundaryEdgesOn();
        featureEdges->FeatureEdgesOff();
        featureEdges->ManifoldEdgesOff();
        featureEdges->NonManifoldEdgesOff();
        featureEdges->SetColoring(0);
        featureEdges->Update();
        vtkSmartPointer<vtkPolyData> modelEdges = Copy(featureEdges->GetOutput(), false);
        return std::unique_ptr<PartActor>(new PartActor(GetSectionViewStyle(actor), polyData, edges, modelEdges,
                                                        *actor));
    }

    // Port of GetSectionViewActorFromClosedSurface: the cap of a closed surface, its cut outline as edges
    std::unique_ptr<PartActor> View::GetSectionViewActorFromClosedSurface(PartActor* actor)
    {
        vtkNew<vtkGeometryFilter> geometryFilter;
        geometryFilter->SetInputData(actor->GetVolumeGrid());
        geometryFilter->Update();
        vtkPolyData* surface = geometryFilter->GetOutput();
        vtkNew<vtkCutter> cutter;
        cutter->SetCutFunction(SectionPlane);
        cutter->SetInputData(actor->GetPolyData());
        cutter->GenerateCutScalarsOff();
        vtkNew<vtkStripper> cutStrips;
        cutStrips->SetInputConnection(cutter->GetOutputPort());
        cutStrips->Update();
        vtkSmartPointer<vtkPolyData> polyData = IsClosedSurface(surface) ? GetSectionCap(surface, SectionPlane)
                                                                         : vtkSmartPointer<vtkPolyData>::New();
        vtkSmartPointer<vtkPolyData> edges = Copy(cutStrips->GetOutput(), false);
        return std::unique_ptr<PartActor>(new PartActor(GetSectionViewStyle(actor), polyData, edges, edges, *actor));
    }

    // Port of GetSectionViewActorsElementWise and the element-wise part of AddSectionViewActors: the whole elements on
    // the visible side of the plane, the faces the cut exposed as the section actor with its outline, and the part
    // surface of the remaining elements as the skin that replaces the part
    void View::AddSectionViewActorsElementWise(const std::string& name, PartActor* actor)
    {
        vtkUnstructuredGrid* grid = actor->GetVolumeGrid();
        vtkDataArray* gridElementIds = GetElementIds(grid);
        if (GetNodeIds(grid) == nullptr || gridElementIds == nullptr) return;
        vtkPolyData* surface = actor->GetPolyData();
        vtkPolyData* elementEdges = GetInput(actor->GetElementEdges());
        vtkPolyData* modelEdges = GetInput(actor->GetModelEdges());
        // A part the plane does not reach is either untouched or gone
        int side = GetPlaneSideOfBounds(SectionPlane, grid->GetBounds());
        if (side > 0)
        {
            for (vtkIdType i = 0; i < gridElementIds->GetNumberOfTuples(); i++)
                SectionViewKeptElementIds.insert(static_cast<int>(gridElementIds->GetTuple1(i)));
            return;
        }
        if (side < 0)
        {
            vtkNew<vtkPolyData> empty;
            vtkNew<vtkUnstructuredGrid> noFaces;
            vtkSmartPointer<vtkPolyData> skinModelEdges = GetModelEdgesOfSkin(modelEdges, noFaces);
            std::unique_ptr<PartActor> skinActor(new PartActor(GetSkinStyle(actor), empty, empty, skinModelEdges,
                                                               *actor));
            AddSectionViewSkinActor(name, actor, std::move(skinActor));
            return;
        }
        std::set<FaceNodeKey> outerFaces = GetFaceNodeKeys(GetBoundary(grid));
        // Whole elements on the visible side of the plane
        vtkNew<vtkExtractGeometry> extract;
        extract->SetImplicitFunction(SectionPlane);
        extract->ExtractInsideOff();
        extract->ExtractBoundaryCellsOff();
        extract->SetInputData(grid);
        extract->Update();
        vtkUnstructuredGrid* kept = vtkUnstructuredGrid::SafeDownCast(extract->GetOutput());
        std::set<int> keptElementIds;
        vtkDataArray* keptIds = GetElementIds(kept);
        for (vtkIdType i = 0; keptIds != nullptr && i < keptIds->GetNumberOfTuples(); i++)
            keptElementIds.insert(static_cast<int>(keptIds->GetTuple1(i)));
        SectionViewKeptElementIds.insert(keptElementIds.begin(), keptElementIds.end());
        // Their boundary: the part surface that remains and the element faces the cut exposed
        vtkSmartPointer<vtkUnstructuredGrid> boundary = GetBoundary(kept);
        std::vector<int> faceTypes = LabelSectionFaces(boundary, outerFaces);
        vtkSmartPointer<vtkUnstructuredGrid> exposedFaces = ExtractSectionFaces(boundary, faceTypes, 1);
        vtkSmartPointer<vtkUnstructuredGrid> skinFaces = ExtractSectionFaces(boundary, faceTypes, 0);
        //
        vtkSmartPointer<vtkPolyData> faces = GetSurface(exposedFaces);
        vtkNew<vtkExtractEdges> extractEdges;
        extractEdges->SetInputData(faces);
        extractEdges->Update();
        vtkSmartPointer<vtkPolyData> faceEdges = Copy(extractEdges->GetOutput(), false);
        vtkSmartPointer<vtkPolyData> outline = GetSurface(GetSectionOutline(boundary, faceTypes));
        outline->GetPointData()->RemoveArray("scalars");
        std::unique_ptr<PartActor> sectionViewActor(new PartActor(GetSectionViewStyle(actor), faces, faceEdges, outline,
                                                                   *actor));
        std::string sectionName = name + ":sectionView:1";
        AddActor(sectionName, std::move(sectionViewActor));
        SectionViewActorNames[name] = sectionName;
        // The skin is cut from the part surface itself, so that it and the surfaces drawn on the part are the same
        // polygons
        vtkSmartPointer<vtkPolyData> skinEdges;
        vtkSmartPointer<vtkPolyData> skin;
        if (surface != nullptr && surface->GetNumberOfPolys() > 0 && surface->GetCellData()->GetGlobalIds() != nullptr)
            skin = GetSurfaceOfKeptElements(surface, elementEdges, keptElementIds, skinEdges);
        else
        {
            skin = GetSurface(skinFaces);
            vtkNew<vtkExtractEdges> extractSkinEdges;
            extractSkinEdges->SetInputData(skin);
            extractSkinEdges->Update();
            skinEdges = Copy(extractSkinEdges->GetOutput(), false);
        }
        std::unique_ptr<PartActor> skinActor(new PartActor(GetSkinStyle(actor), skin, skinEdges,
                                                           GetModelEdgesOfSkin(modelEdges, skinFaces), *actor));
        AddSectionViewSkinActor(name, actor, std::move(skinActor));
    }

    // The element-wise section draws the remaining surface itself, so the geometry it replaces is hidden
    void View::AddSectionViewSkinActor(const std::string& name, PartActor* actor, std::unique_ptr<PartActor> skinActor)
    {
        std::string skinName = name + ":sectionView:2";
        AddActor(skinName, std::move(skinActor));
        SectionViewSkinActorNames[name] = skinName;
        actor->SetGeometryHiddenBySectionView(true);
    }

    // Port of AddSectionViewSkinActorsToSurfaceActors: the surfaces drawn on the parts (loads, contact regions...)
    // keep the faces of the remaining elements
    void View::AddSectionViewSkinActorsToSurfaceActors()
    {
        std::vector<std::pair<std::string, PartActor*>> actors;
        for (const auto& entry : BaseActors) actors.push_back({ entry.first, entry.second.get() });
        for (const auto& entry : actors)
        {
            PartActor* actor = entry.second;
            const vtkn_part_style& style = actor->GetStyle();
            if (style.is_a_part || !style.section_view_possible || actor->GetVolumeGrid() != nullptr ||
                SectionViewSkinActorNames.count(entry.first) > 0 || entry.first.find(":sectionView:") != std::string::npos)
                continue;
            vtkPolyData* surface = actor->GetPolyData();
            if (surface == nullptr || surface->GetNumberOfPolys() == 0 || surface->GetCellData()->GetGlobalIds() == nullptr)
                continue;
            vtkSmartPointer<vtkPolyData> croppedEdges;
            vtkSmartPointer<vtkPolyData> cropped = GetSurfaceOfKeptElements(surface, GetInput(actor->GetElementEdges()),
                                                                            SectionViewKeptElementIds, croppedEdges);
            vtkNew<vtkPolyData> noModelEdges;
            std::unique_ptr<PartActor> skinActor(new PartActor(GetSkinStyle(actor), cropped, croppedEdges, noModelEdges,
                                                               *actor));
            AddSectionViewSkinActor(entry.first, actor, std::move(skinActor));
        }
    }
}
