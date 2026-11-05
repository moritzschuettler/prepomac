// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativePartActor.h"
#include "vtkNativeError.h"

#include <vtkCellArray.h>
#include <vtkCellData.h>
#include <vtkCellType.h>
#include <vtkDataSetMapper.h>
#include <vtkDoubleArray.h>
#include <vtkIdTypeArray.h>
#include <vtkNew.h>
#include <vtkOpenGLShaderProperty.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkPolyDataNormals.h>
#include <vtkProperty.h>

#include <algorithm>
#include <array>
#include <cstring>
#include <set>
#include <utility>
#include <vector>

namespace vtkn
{
    void CheckMesh(const vtkn_mesh& mesh, const char* meshName)
    {
        std::string name(meshName);
        if (mesh.point_count < 0 || mesh.cells_length < 0 || mesh.cell_count < 0)
            throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The counts of the " + name + " must not be negative.");
        if (mesh.point_count > 0 && mesh.coordinates == nullptr)
            throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The " + name + " has points but no coordinates.");
        if (mesh.cell_count > 0 && (mesh.cells == nullptr || mesh.cell_types == nullptr))
            throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The " + name + " has cells but no cell data.");
        int cellsRead = 0;
        for (int i = 0; i < mesh.cells_length; cellsRead++)
        {
            int count = mesh.cells[i];
            if (count <= 0 || i + count >= mesh.cells_length)
                throw Error(VTKN_ERROR_INVALID_ARGUMENT, "A cell of the " + name + " ends inside the cell array.");
            for (int j = 1; j <= count; j++)
            {
                int id = mesh.cells[i + j];
                if (id < 0 || id >= mesh.point_count)
                    throw Error(VTKN_ERROR_INVALID_ARGUMENT, "A cell of the " + name + " refers to a missing point.");
            }
            i += count + 1;
        }
        if (cellsRead != mesh.cell_count)
            throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The cells of the " + name + " do not match its cell types.");
    }

    namespace
    {
        double DistanceSquared(const double* coordinates, int a, int b)
        {
            double dx = coordinates[3 * b] - coordinates[3 * a];
            double dy = coordinates[3 * b + 1] - coordinates[3 * a + 1];
            double dz = coordinates[3 * b + 2] - coordinates[3 * a + 2];
            return dx * dx + dy * dy + dz * dz;
        }

        void InsertTriangle(vtkCellArray* polys, std::vector<int>& polyCellIds, int cellId, int a, int b, int c)
        {
            vtkIdType ids[3] = { a, b, c };
            polys->InsertNextCell(3, ids);
            polyCellIds.push_back(cellId);
        }

        // VTK 9 lights every fragment with the direction from that fragment to the eye, also under the parallel
        // projection PrePoMax uses, while VTK 5.8 lit with a viewer at infinity. With the camera just outside the
        // model, as PrePoMax places it, the local viewer moves the specular highlight across a flat face, so the
        // view direction is put back to the one of a parallel projection
        void UseInfiniteViewer(vtkActor* actor)
        {
            vtkOpenGLShaderProperty* shaderProperty = vtkOpenGLShaderProperty::SafeDownCast(actor->GetShaderProperty());
            if (shaderProperty == nullptr) return;
            shaderProperty->AddFragmentShaderReplacement("normalize(-vertexVC.xyz)", false, "vec3(0.0, 0.0, 1.0)", true);
        }

        // VTK 9 draws every actor with an opacity below one in the translucent pass, which renders without
        // multisampling and tests the depth against a copy of the buffer, so the edges come out jagged and broken.
        // Forced opaque, they keep their alpha but are blended in the multisampled opaque pass
        void SetEdgeLine(vtkActor* actor, double width, double opacity)
        {
            vtkProperty* property = actor->GetProperty();
            property->SetColor(0, 0, 0);
            property->SetLighting(false);
            property->SetLineWidth(static_cast<float>(width));
            property->SetOpacity(opacity);
            actor->SetForceOpaque(true);
        }

        vtkSmartPointer<vtkIdTypeArray> ToIdArray(const std::vector<int>& values)
        {
            vtkSmartPointer<vtkIdTypeArray> array = vtkSmartPointer<vtkIdTypeArray>::New();
            array->SetNumberOfValues(static_cast<vtkIdType>(values.size()));
            for (size_t i = 0; i < values.size(); i++) array->SetValue(static_cast<vtkIdType>(i), values[i]);
            return array;
        }
    }

    // VTK 9 numbers the cells of a poly data vertices first, then lines, then polygons, whatever order they were
    // inserted in (VTK 5.8 kept the insertion order), so the cell ids are collected per kind and joined that way
    void BuildPolyData(const vtkn_mesh& mesh, bool extractEdges, vtkSmartPointer<vtkPolyData>& polyActor,
                       vtkSmartPointer<vtkPolyData>& polyEdges)
    {
        polyActor = vtkSmartPointer<vtkPolyData>::New();
        polyEdges = vtkSmartPointer<vtkPolyData>::New();
        //
        vtkNew<vtkDoubleArray> coordinates;
        coordinates->SetNumberOfComponents(3);
        coordinates->SetNumberOfTuples(mesh.point_count);
        if (mesh.point_count > 0)
            std::memcpy(coordinates->GetPointer(0), mesh.coordinates, sizeof(double) * 3 * static_cast<size_t>(mesh.point_count));
        vtkNew<vtkPoints> points;
        points->SetData(coordinates);
        polyActor->SetPoints(points);
        polyEdges->SetPoints(points);
        //
        vtkNew<vtkCellArray> verts;
        vtkNew<vtkCellArray> lines;
        vtkNew<vtkCellArray> polys;
        std::vector<int> vertCellIds;
        std::vector<int> lineCellIds;
        std::vector<int> polyCellIds;
        std::set<int> uniqueVertexIds;
        std::set<std::pair<int, int>> uniqueEdgeCells;
        std::set<std::array<int, 3>> uniquePolyEdgeCells;
        //
        const int32_t* cell = mesh.cells;
        for (int i = 0; i < mesh.cell_count; i++)
        {
            int count = cell[0];
            const int32_t* n = cell + 1;
            int cellType = mesh.cell_types[i];
            int cellId = mesh.cell_ids != nullptr ? mesh.cell_ids[i] : i;
            cell += count + 1;
            //
            bool isVertex = cellType == VTK_VERTEX;
            bool isEdge = cellType == VTK_LINE || cellType == VTK_QUADRATIC_EDGE;
            bool isSurface = cellType == VTK_TRIANGLE || cellType == VTK_QUAD || cellType == VTK_QUADRATIC_TRIANGLE ||
                             cellType == VTK_QUADRATIC_QUAD;
            // Cell edges
            if (extractEdges)
            {
                if (isEdge) uniqueVertexIds.insert(n, n + count);
                else if (cellType == VTK_TRIANGLE || cellType == VTK_QUAD)
                {
                    int corners = cellType == VTK_TRIANGLE ? 3 : 4;
                    for (int j = 0; j < corners; j++)
                    {
                        int id1 = n[j];
                        int id2 = n[(j + 1) % corners];
                        uniqueEdgeCells.insert(std::make_pair(std::min(id1, id2), std::max(id1, id2)));
                    }
                }
                else if (cellType == VTK_QUADRATIC_TRIANGLE || cellType == VTK_QUADRATIC_QUAD)
                {
                    int corners = cellType == VTK_QUADRATIC_TRIANGLE ? 3 : 4;
                    for (int j = 0; j < corners; j++)
                    {
                        int id1 = n[j];
                        int id2 = n[j + corners];
                        int id3 = n[(j + 1) % corners];
                        if (id1 < id3) uniquePolyEdgeCells.insert({ id1, id2, id3 });
                        else uniquePolyEdgeCells.insert({ id3, id2, id1 });
                    }
                }
            }
            // Cells
            if (isVertex)
            {
                vtkIdType id = n[0];
                verts->InsertNextCell(1, &id);
                vertCellIds.push_back(cellId);
            }
            else if (isEdge)
            {
                if (cellType == VTK_LINE)
                {
                    vtkIdType ids[2] = { n[0], n[1] };
                    lines->InsertNextCell(2, ids);
                    lineCellIds.push_back(cellId);
                }
                else
                {
                    vtkIdType first[2] = { n[0], n[2] };
                    vtkIdType second[2] = { n[2], n[1] };
                    lines->InsertNextCell(2, first);
                    lines->InsertNextCell(2, second);
                    lineCellIds.push_back(cellId);
                    lineCellIds.push_back(cellId);
                }
            }
            else if (isSurface)
            {
                if (cellType == VTK_TRIANGLE) InsertTriangle(polys, polyCellIds, cellId, n[0], n[1], n[2]);
                else if (cellType == VTK_QUAD)
                {
                    InsertTriangle(polys, polyCellIds, cellId, n[0], n[1], n[2]);
                    InsertTriangle(polys, polyCellIds, cellId, n[0], n[2], n[3]);
                }
                else if (cellType == VTK_QUADRATIC_TRIANGLE)
                {
                    InsertTriangle(polys, polyCellIds, cellId, n[0], n[3], n[5]);
                    InsertTriangle(polys, polyCellIds, cellId, n[3], n[4], n[5]);
                    InsertTriangle(polys, polyCellIds, cellId, n[3], n[1], n[4]);
                    InsertTriangle(polys, polyCellIds, cellId, n[5], n[4], n[2]);
                }
                else
                {
                    // The quadrilateral is split along its shorter middle line
                    double d1 = DistanceSquared(mesh.coordinates, n[4], n[6]);
                    double d2 = DistanceSquared(mesh.coordinates, n[5], n[7]);
                    if (d1 < d2)
                    {
                        InsertTriangle(polys, polyCellIds, cellId, n[7], n[0], n[4]);
                        InsertTriangle(polys, polyCellIds, cellId, n[7], n[4], n[6]);
                        InsertTriangle(polys, polyCellIds, cellId, n[7], n[6], n[3]);
                        InsertTriangle(polys, polyCellIds, cellId, n[5], n[4], n[1]);
                        InsertTriangle(polys, polyCellIds, cellId, n[5], n[6], n[4]);
                        InsertTriangle(polys, polyCellIds, cellId, n[5], n[2], n[6]);
                    }
                    else
                    {
                        InsertTriangle(polys, polyCellIds, cellId, n[4], n[7], n[0]);
                        InsertTriangle(polys, polyCellIds, cellId, n[4], n[5], n[7]);
                        InsertTriangle(polys, polyCellIds, cellId, n[4], n[1], n[5]);
                        InsertTriangle(polys, polyCellIds, cellId, n[6], n[5], n[2]);
                        InsertTriangle(polys, polyCellIds, cellId, n[6], n[7], n[5]);
                        InsertTriangle(polys, polyCellIds, cellId, n[6], n[3], n[7]);
                    }
                }
            }
            else throw Error(VTKN_ERROR_INVALID_ARGUMENT, "A cell type of the part is not supported: " +
                                                          std::to_string(cellType));
        }
        polyActor->SetVerts(verts);
        polyActor->SetLines(lines);
        polyActor->SetPolys(polys);
        //
        if (extractEdges)
        {
            vtkNew<vtkCellArray> edgeVerts;
            vtkNew<vtkCellArray> edgeLines;
            for (int vertexId : uniqueVertexIds)
            {
                vtkIdType id = vertexId;
                edgeVerts->InsertNextCell(1, &id);
            }
            for (const auto& edge : uniqueEdgeCells)
            {
                vtkIdType ids[2] = { edge.first, edge.second };
                edgeLines->InsertNextCell(2, ids);
            }
            for (const auto& edge : uniquePolyEdgeCells)
            {
                vtkIdType ids[3] = { edge[0], edge[1], edge[2] };
                edgeLines->InsertNextCell(3, ids);
            }
            polyEdges->SetVerts(edgeVerts);
            polyEdges->SetLines(edgeLines);
        }
        // Global ids, which the picking of slice C reads
        if (mesh.point_ids != nullptr)
        {
            std::vector<int> pointIds(mesh.point_ids, mesh.point_ids + mesh.point_count);
            vtkSmartPointer<vtkIdTypeArray> ids = ToIdArray(pointIds);
            polyActor->GetPointData()->SetGlobalIds(ids);
        }
        if (mesh.cell_ids != nullptr)
        {
            std::vector<int> cellIds;
            cellIds.reserve(vertCellIds.size() + lineCellIds.size() + polyCellIds.size());
            cellIds.insert(cellIds.end(), vertCellIds.begin(), vertCellIds.end());
            cellIds.insert(cellIds.end(), lineCellIds.begin(), lineCellIds.end());
            cellIds.insert(cellIds.end(), polyCellIds.begin(), polyCellIds.end());
            polyActor->GetCellData()->SetGlobalIds(ToIdArray(cellIds));
        }
    }

    PartActor::PartActor(const vtkn_part_style& style, const vtkn_mesh& geometry, const vtkn_mesh* modelEdges,
                         const vtkn_mesh* modelEdgesToDraw)
        : Style(style)
    {
        CheckMesh(geometry, "part geometry");
        if (modelEdges != nullptr) CheckMesh(*modelEdges, "model edges");
        if (modelEdgesToDraw != nullptr) CheckMesh(*modelEdgesToDraw, "model edges to draw");
        //
        vtkSmartPointer<vtkPolyData> polyActor;
        vtkSmartPointer<vtkPolyData> polyEdges;
        BuildPolyData(geometry, true, polyActor, polyEdges);
        // Smooth shading: point normals without splitting, which would change the topology
        vtkNew<vtkPolyDataNormals> normalGenerator;
        normalGenerator->SetInputData(polyActor);
        normalGenerator->SplittingOff();
        normalGenerator->ComputePointNormalsOn();
        normalGenerator->ComputeCellNormalsOff();
        normalGenerator->Update();
        polyActor->GetPointData()->SetNormals(normalGenerator->GetOutput()->GetPointData()->GetNormals());
        //
        vtkNew<vtkDataSetMapper> mapper;
        mapper->SetInputData(polyActor);
        mapper->ScalarVisibilityOff();
        Geometry = vtkSmartPointer<vtkActor>::New();
        Geometry->SetMapper(mapper);
        // A part is picked when it is pickable and its cells carry the element ids
        bool pickable = style.pickable != 0 && polyActor->GetCellData()->GetGlobalIds() != nullptr;
        Geometry->SetPickable(pickable ? 1 : 0);
        if (pickable)
        {
            CellLocator = vtkSmartPointer<vtkCellLocator>::New();
            CellLocator->SetDataSet(polyActor);
        }
        UseInfiniteViewer(Geometry);
        // Element edges
        if (style.can_have_element_edges)
        {
            vtkNew<vtkDataSetMapper> edgesMapper;
            edgesMapper->SetInputData(polyEdges);
            ElementEdges = vtkSmartPointer<vtkActor>::New();
            ElementEdges->SetMapper(edgesMapper);
            ElementEdges->PickableOff();
        }
        // Model edges; those another part draws are left out of the drawn ones
        if (modelEdges != nullptr)
        {
            vtkSmartPointer<vtkPolyData> polyModelEdges;
            vtkSmartPointer<vtkPolyData> unused;
            BuildPolyData(modelEdgesToDraw != nullptr ? *modelEdgesToDraw : *modelEdges, false, polyModelEdges, unused);
            vtkNew<vtkDataSetMapper> modelEdgesMapper;
            modelEdgesMapper->SetInputData(polyModelEdges);
            ModelEdges = vtkSmartPointer<vtkActor>::New();
            ModelEdges->SetMapper(modelEdgesMapper);
            ModelEdges->PickableOff();
            // A part without element edges, such as a geometry part, shows its model edges in the element
            // edges mode
            if (!style.can_have_element_edges)
            {
                vtkNew<vtkDataSetMapper> elementEdgesMapper;
                elementEdgesMapper->SetInputData(polyModelEdges);
                ElementEdges = vtkSmartPointer<vtkActor>::New();
                ElementEdges->SetMapper(elementEdgesMapper);
                ElementEdges->PickableOff();
            }
        }
        //
        Geometry->GetProperty()->SetLineWidth(static_cast<float>(style.line_width));
        Geometry->GetProperty()->SetPointSize(static_cast<float>(style.node_size));
        if (ModelEdges != nullptr) ModelEdges->GetProperty()->SetPointSize(static_cast<float>(style.node_size));
        if (ElementEdges != nullptr) ElementEdges->GetProperty()->SetPointSize(static_cast<float>(style.node_size));
        //
        UpdateColor();
        Geometry->GetProperty()->SetLighting(style.lighting != 0);
    }

    bool PartActor::Owns(vtkProp* prop) const
    {
        return prop != nullptr && (prop == Geometry.GetPointer() || prop == ElementEdges.GetPointer() ||
                                   prop == ModelEdges.GetPointer());
    }

    void PartActor::SetPickOpacity()
    {
        if (Geometry->GetProperty()->GetOpacity() != 1) Geometry->GetProperty()->SetOpacity(1);
    }

    void PartActor::AddTo(vtkRenderer* renderer)
    {
        renderer->AddActor(Geometry);
        if (ElementEdges != nullptr) renderer->AddActor(ElementEdges);
        if (ModelEdges != nullptr) renderer->AddActor(ModelEdges);
    }

    void PartActor::RemoveFrom(vtkRenderer* renderer)
    {
        renderer->RemoveActor(Geometry);
        if (ElementEdges != nullptr) renderer->RemoveActor(ElementEdges);
        if (ModelEdges != nullptr) renderer->RemoveActor(ModelEdges);
    }

    void PartActor::SetGlobalLighting(double ambient, double diffuse)
    {
        if (!Style.use_global_lighting) return;
        Style.ambient = ambient;
        Style.diffuse = diffuse;
        UpdateColor();
    }

    // Port of vtkMaxActor.UpdateColor without the color table and the result contours
    void PartActor::UpdateColor()
    {
        const double* color = Style.color;
        double opacity = color[3];
        vtkProperty* property = Geometry->GetProperty();
        property->SetAmbient(Style.ambient);
        property->SetDiffuse(Style.diffuse);
        property->SetOpacity(opacity);
        property->SetColor(color[0], color[1], color[2]);
        property->SetSpecular(0.6);
        property->SetSpecularPower(100);
        //
        if (ModelEdges != nullptr) SetEdgeLine(ModelEdges, 0.8, opacity * 0.7);
        if (ElementEdges != nullptr) SetEdgeLine(ElementEdges, 0.5, opacity * 0.4);
        if (Style.has_backface_color && !Style.backface_culling)
        {
            vtkNew<vtkProperty> backface;
            backface->SetAmbient(Style.ambient);
            backface->SetDiffuse(Style.diffuse);
            backface->SetOpacity(Style.backface_color[3]);
            backface->SetColor(Style.backface_color[0], Style.backface_color[1], Style.backface_color[2]);
            backface->SetSpecular(0.6);
            backface->SetSpecularPower(100);
            Geometry->SetBackfaceProperty(backface);
        }
    }

    // Port of ApplyEdgeVisibilityAndBackfaceCullingToActor, ...ToActorEdges and ...ToModelEdges for the base layer
    void PartActor::ApplyEdgesVisibility(vtkn_edges_visibility edgesVisibility)
    {
        bool visible = Style.visible != 0;
        vtkProperty* property = Geometry->GetProperty();
        if (!Style.use_edge_visibility) Geometry->SetVisibility(visible);
        else
        {
            if (edgesVisibility == VTKN_EDGES_ELEMENT_EDGES) property->SetInterpolationToFlat();
            else property->SetInterpolationToPhong();
            //
            if (edgesVisibility == VTKN_EDGES_WIREFRAME) Geometry->VisibilityOff();
            else
            {
                property->SetRepresentationToSurface();
                Geometry->SetVisibility(visible);
                property->SetBackfaceCulling(Style.backface_culling != 0);
            }
        }
        //
        if (ModelEdges != nullptr)
        {
            ModelEdges->SetVisibility(visible && (edgesVisibility == VTKN_EDGES_MODEL_EDGES ||
                                                  edgesVisibility == VTKN_EDGES_WIREFRAME));
            ModelEdges->GetProperty()->BackfaceCullingOn();
        }
        if (ElementEdges != nullptr)
        {
            ElementEdges->SetVisibility(visible && edgesVisibility == VTKN_EDGES_ELEMENT_EDGES);
            ElementEdges->GetProperty()->BackfaceCullingOn();
        }
    }
}
