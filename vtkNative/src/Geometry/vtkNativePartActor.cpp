// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativePartActor.h"
#include "vtkNativeError.h"

#include <vtkCellArray.h>
#include <vtkCellData.h>
#include <vtkCellType.h>
#include <vtkCellTypes.h>
#include <vtkDataSetMapper.h>
#include <vtkDoubleArray.h>
#include <vtkIdTypeArray.h>
#include <vtkNew.h>
#include <vtkOpenGLShaderProperty.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkPolyDataMapper.h>
#include <vtkPolyDataNormals.h>
#include <vtkProperty.h>
#include <vtkReverseSense.h>
#include <vtkTransformFilter.h>
#include <vtkTransformPolyDataFilter.h>
#include <vtkUnsignedCharArray.h>

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
                       vtkSmartPointer<vtkPolyData>& polyEdges, std::vector<int>* sourceCells)
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
        std::vector<int> vertSources;
        std::vector<int> lineSources;
        std::vector<int> polySources;
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
                vertSources.push_back(i);
            }
            else if (isEdge)
            {
                if (cellType == VTK_LINE)
                {
                    vtkIdType ids[2] = { n[0], n[1] };
                    lines->InsertNextCell(2, ids);
                    lineCellIds.push_back(cellId);
                    lineSources.push_back(i);
                }
                else
                {
                    vtkIdType first[2] = { n[0], n[2] };
                    vtkIdType second[2] = { n[2], n[1] };
                    lines->InsertNextCell(2, first);
                    lines->InsertNextCell(2, second);
                    lineCellIds.push_back(cellId);
                    lineCellIds.push_back(cellId);
                    lineSources.push_back(i);
                    lineSources.push_back(i);
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
            if (isSurface) polySources.resize(polyCellIds.size(), i);
            if (!isVertex && !isEdge && !isSurface)
                throw Error(VTKN_ERROR_INVALID_ARGUMENT, "A cell type of the part is not supported: " +
                                                          std::to_string(cellType));
        }
        polyActor->SetVerts(verts);
        polyActor->SetLines(lines);
        polyActor->SetPolys(polys);
        if (sourceCells != nullptr)
        {
            sourceCells->clear();
            sourceCells->insert(sourceCells->end(), vertSources.begin(), vertSources.end());
            sourceCells->insert(sourceCells->end(), lineSources.begin(), lineSources.end());
            sourceCells->insert(sourceCells->end(), polySources.begin(), polySources.end());
        }
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


    vtkn_part_style DefaultStyle()
    {
        vtkn_part_style style = {};
        style.ambient = 0.5;
        style.diffuse = 0.5;
        style.line_width = 1;
        style.node_size = 1;
        style.backface_culling = 1;
        style.lighting = 1;
        style.use_global_lighting = 1;
        style.use_edge_visibility = 1;
        style.visible = 1;
        style.layer = VTKN_LAYER_BASE;
        style.representation = VTKN_REPRESENTATION_UNKNOWN;
        style.section_view_possible = 1;
        return style;
    }

    PartActor::PartActor(const vtkn_part_style& style, const vtkn_mesh& geometry, const vtkn_mesh* modelEdges,
                         const vtkn_mesh* modelEdgesToDraw)
        : Style(style)
    {
        CheckMesh(geometry, "part geometry");
        if (modelEdges != nullptr) CheckMesh(*modelEdges, "model edges");
        if (modelEdgesToDraw != nullptr) CheckMesh(*modelEdgesToDraw, "model edges to draw");
        // vtkMaxActor(data, false, createNodalActor): a geometry without cells is drawn as its nodes
        bool nodal = style.nodal || (geometry.cell_count == 0 && geometry.point_count > 0);
        if (nodal) CreateNodal(geometry);
        else CreateFromMesh(geometry, modelEdges, modelEdgesToDraw);
        ApplyStyleSizes();
        UpdateColor();
        // VTK 5.8 drew smoothed points as flat round dots; VTK 9 has no point smoothing and draws squares, so the
        // nodes are drawn as sphere imposters without lighting, which are flat round dots
        Geometry->GetProperty()->SetLighting(style.lighting != 0 && !nodal);
    }

    PartActor::PartActor(const vtkn_part_style& style, vtkPolyData* polyData) : Style(style)
    {
        vtkNew<vtkDataSetMapper> mapper;
        mapper->SetInputData(polyData);
        CreateFromMapper(mapper);
    }

    PartActor::PartActor(const vtkn_part_style& style, vtkAlgorithmOutput* output) : Style(style)
    {
        vtkNew<vtkPolyDataMapper> mapper;
        mapper->SetInputConnection(output);
        mapper->ScalarVisibilityOff();
        CreateFromMapper(mapper);
    }

    PartActor::PartActor(const vtkn_part_style& style, vtkPolyData* faces, vtkPolyData* elementEdges,
                         vtkPolyData* modelEdges, const PartActor& part)
        : Style(style)
    {
        vtkNew<vtkPolyDataMapper> mapper;
        mapper->SetInputData(faces);
        Geometry = vtkSmartPointer<vtkActor>::New();
        Geometry->SetMapper(mapper);
        Geometry->PickableOff();
        UseInfiniteViewer(Geometry);
        vtkDataArray* scalars = faces->GetPointData()->GetArray("scalars");
        PointScalars = vtkFloatArray::SafeDownCast(scalars);
        HasExtremeNodes = part.HasExtremeNodes;
        Extremes = part.Extremes;
        vtkPolyData* edges[2] = { elementEdges, modelEdges };
        vtkSmartPointer<vtkActor>* actors[2] = { &ElementEdges, &ModelEdges };
        for (int i = 0; i < 2; i++)
        {
            vtkNew<vtkPolyDataMapper> edgesMapper;
            edgesMapper->SetInputData(edges[i]);
            *actors[i] = vtkSmartPointer<vtkActor>::New();
            (*actors[i])->SetMapper(edgesMapper);
            (*actors[i])->PickableOff();
        }
        ApplyStyleSizes();
        UpdateColor();
        Geometry->GetProperty()->SetLighting(style.lighting != 0);
    }

    namespace
    {
        // Port of TransformActor: the transformed data of an actor, its cells reversed by a mirror so that their
        // front faces stay outside
        vtkSmartPointer<vtkActor> TransformActor(vtkActor* source, vtkTransform* transform, bool polyDataMapper)
        {
            vtkPolyData* polyData = vtkPolyData::SafeDownCast(source->GetMapper()->GetInput());
            vtkNew<vtkTransformPolyDataFilter> transformFilter;
            transformFilter->SetInputData(polyData);
            transformFilter->SetTransform(transform);
            transformFilter->Update();
            vtkSmartPointer<vtkPolyData> output = vtkSmartPointer<vtkPolyData>::New();
            double scale[3];
            transform->GetScale(scale);
            if (scale[0] < 0 || scale[1] < 0 || scale[2] < 0)
            {
                vtkNew<vtkReverseSense> reverseSense;
                reverseSense->SetInputConnection(transformFilter->GetOutputPort());
                reverseSense->ReverseNormalsOff();
                reverseSense->ReverseCellsOn();
                reverseSense->Update();
                output->DeepCopy(reverseSense->GetOutput());
            }
            else output->DeepCopy(transformFilter->GetOutput());
            vtkSmartPointer<vtkMapper> mapper;
            if (polyDataMapper) mapper = vtkSmartPointer<vtkPolyDataMapper>::New();
            else mapper = vtkSmartPointer<vtkDataSetMapper>::New();
            mapper->SetInputDataObject(output);
            mapper->SetScalarVisibility(source->GetMapper()->GetScalarVisibility());
            vtkSmartPointer<vtkActor> actor = vtkSmartPointer<vtkActor>::New();
            actor->SetMapper(mapper);
            actor->GetProperty()->DeepCopy(source->GetProperty());
            actor->SetVisibility(source->GetVisibility());
            actor->PickableOff();
            return actor;
        }
    }

    PartActor::PartActor(const PartActor& source, vtkTransform* transform)
        : Style(source.Style), ParentName(source.ParentName), SourceCells(source.SourceCells),
          ColorTable(source.ColorTable), Extremes(source.Extremes), HasExtremeNodes(source.HasExtremeNodes),
          HatchIndex(source.HatchIndex), HasVolumeCells(source.HasVolumeCells)
    {
        std::copy(source.SectionColor, source.SectionColor + 4, SectionColor);
        Style.pickable = 0;
        Geometry = TransformActor(source.Geometry, transform, vtkPolyDataMapper::SafeDownCast(source.Geometry->GetMapper()) != nullptr);
        UseInfiniteViewer(Geometry);
        if (source.ElementEdges != nullptr) ElementEdges = TransformActor(source.ElementEdges, transform, false);
        if (source.ModelEdges != nullptr) ModelEdges = TransformActor(source.ModelEdges, transform, false);
        vtkPolyData* polyData = vtkPolyData::SafeDownCast(Geometry->GetMapper()->GetInput());
        if (source.PointScalars != nullptr && polyData != nullptr)
            PointScalars = vtkFloatArray::SafeDownCast(polyData->GetPointData()->GetArray("scalars"));
        if (source.VolumeGrid != nullptr)
        {
            vtkNew<vtkTransformFilter> gridTransform;
            gridTransform->SetInputData(source.VolumeGrid);
            gridTransform->SetTransform(transform);
            gridTransform->Update();
            VolumeGrid = vtkSmartPointer<vtkUnstructuredGrid>::New();
            VolumeGrid->DeepCopy(gridTransform->GetOutput());
        }
        UpdateColor();
    }

    namespace
    {
        vtkSmartPointer<vtkPoints> CreateFloatPoints(const float* coordinates, int count)
        {
            if (coordinates == nullptr) return nullptr;
            vtkNew<vtkFloatArray> data;
            data->SetNumberOfComponents(3);
            data->SetNumberOfTuples(count);
            std::memcpy(data->GetPointer(0), coordinates, sizeof(float) * 3 * static_cast<size_t>(count));
            vtkSmartPointer<vtkPoints> points = vtkSmartPointer<vtkPoints>::New();
            points->SetData(data);
            return points;
        }
    }

    // Port of the frame part of InitializeAnimation: the frame's points, with the normals of the surface at them,
    // its values and extreme nodes
    void PartActor::AddAnimationFrame(const float* coordinates, int pointCount, const float* values, int valueCount,
                                      const vtkn_extreme_nodes* extremes, const float* modelEdgeCoordinates,
                                      int modelEdgePointCount)
    {
        vtkPolyData* polyData = vtkPolyData::SafeDownCast(Geometry->GetMapper()->GetInput());
        Require(polyData != nullptr, "The actor has no points to animate.");
        Require(coordinates == nullptr || pointCount == polyData->GetNumberOfPoints(),
                "There must be one frame point per point of the actor.");
        AnimationFrame frame;
        frame.Points = CreateFloatPoints(coordinates, pointCount);
        frame.ModelEdgesPoints = CreateFloatPoints(modelEdgeCoordinates, modelEdgePointCount);
        if (values != nullptr)
        {
            frame.Values = vtkSmartPointer<vtkFloatArray>::New();
            frame.Values->SetName("scalars");
            frame.Values->SetNumberOfValues(valueCount);
            for (int i = 0; i < valueCount; i++) frame.Values->SetValue(i, values[i]);
        }
        if (extremes != nullptr)
        {
            frame.HasExtremes = true;
            frame.Extremes = *extremes;
        }
        if (frame.Points != nullptr && polyData->GetPointData()->GetNormals() != nullptr)
        {
            vtkNew<vtkPolyData> framePolyData;
            framePolyData->ShallowCopy(polyData);
            framePolyData->SetPoints(frame.Points);
            vtkNew<vtkPolyDataNormals> normalGenerator;
            normalGenerator->SetInputData(framePolyData);
            normalGenerator->SplittingOff();
            normalGenerator->ComputePointNormalsOn();
            normalGenerator->ComputeCellNormalsOff();
            normalGenerator->Update();
            frame.PointNormals = normalGenerator->GetOutput()->GetPointData()->GetNormals();
        }
        AnimationFrames.push_back(frame);
        if (AnimationFrames.size() == 1) SetAnimationFrame(0);
    }

    // Port of vtkMaxActor.SetAnimationFrame without the volume grid of the frame
    void PartActor::SetAnimationFrame(int frameNumber)
    {
        if (frameNumber < 0 || frameNumber >= static_cast<int>(AnimationFrames.size()))
            throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The animation frame cannot be set.");
        const AnimationFrame& frame = AnimationFrames[frameNumber];
        // VTK 9 computes the bounds of a data set again only when its points changed after the last computation;
        // the points of a frame are older, so they are marked modified when they are shown
        if (frame.Points != nullptr) frame.Points->Modified();
        if (frame.ModelEdgesPoints != nullptr) frame.ModelEdgesPoints->Modified();
        vtkPolyData* polyData = vtkPolyData::SafeDownCast(Geometry->GetMapper()->GetInput());
        if (frame.Points != nullptr)
        {
            polyData->SetPoints(frame.Points);
            if (ElementEdges != nullptr)
            {
                vtkPolyData* edges = vtkPolyData::SafeDownCast(ElementEdges->GetMapper()->GetInput());
                if (edges != nullptr && edges->GetNumberOfPoints() == frame.Points->GetNumberOfPoints())
                    edges->SetPoints(frame.Points);
            }
            polyData->GetPointData()->SetNormals(frame.PointNormals);
        }
        if (frame.ModelEdgesPoints != nullptr && ModelEdges != nullptr)
        {
            vtkPolyData* modelEdges = vtkPolyData::SafeDownCast(ModelEdges->GetMapper()->GetInput());
            if (modelEdges != nullptr) modelEdges->SetPoints(frame.ModelEdgesPoints);
        }
        if (frame.Values != nullptr)
        {
            polyData->GetPointData()->SetScalars(frame.Values);
            PointScalars = frame.Values;
        }
        if (frame.HasExtremes)
        {
            Extremes = frame.Extremes;
            HasExtremeNodes = true;
        }
        if (CellLocator != nullptr) CellLocator->Modified();
    }

    void PartActor::SetPosition(const double position[3])
    {
        Geometry->SetPosition(position[0], position[1], position[2]);
        if (ModelEdges != nullptr) ModelEdges->SetPosition(position[0], position[1], position[2]);
        if (ElementEdges != nullptr) ElementEdges->SetPosition(position[0], position[1], position[2]);
    }

    vtkSmartPointer<vtkUnstructuredGrid> BuildUnstructuredGrid(const vtkn_mesh& mesh)
    {
        CheckMesh(mesh, "volume grid");
        vtkNew<vtkDoubleArray> coordinates;
        coordinates->SetNumberOfComponents(3);
        coordinates->SetNumberOfTuples(mesh.point_count);
        if (mesh.point_count > 0)
            std::memcpy(coordinates->GetPointer(0), mesh.coordinates, sizeof(double) * 3 * static_cast<size_t>(mesh.point_count));
        vtkNew<vtkPoints> points;
        points->SetData(coordinates);
        vtkNew<vtkCellArray> cells;
        vtkNew<vtkUnsignedCharArray> types;
        const int32_t* cell = mesh.cells;
        for (int i = 0; i < mesh.cell_count; i++)
        {
            std::vector<vtkIdType> ids(cell + 1, cell + 1 + cell[0]);
            cells->InsertNextCell(static_cast<vtkIdType>(ids.size()), ids.data());
            types->InsertNextValue(mesh.cell_types[i]);
            cell += cell[0] + 1;
        }
        vtkSmartPointer<vtkUnstructuredGrid> grid = vtkSmartPointer<vtkUnstructuredGrid>::New();
        grid->SetPoints(points);
        grid->SetCells(types, cells);
        return grid;
    }

    void PartActor::AddClippingPlane(vtkPlane* plane)
    {
        Geometry->GetMapper()->AddClippingPlane(plane);
        if (ElementEdges != nullptr) ElementEdges->GetMapper()->AddClippingPlane(plane);
        if (ModelEdges != nullptr) ModelEdges->GetMapper()->AddClippingPlane(plane);
        if (Style.representation == VTKN_REPRESENTATION_SOLID_AS_SHELL) Geometry->GetProperty()->BackfaceCullingOff();
    }

    void PartActor::RemoveAllClippingPlanes()
    {
        Geometry->GetMapper()->RemoveAllClippingPlanes();
        if (ElementEdges != nullptr) ElementEdges->GetMapper()->RemoveAllClippingPlanes();
        if (ModelEdges != nullptr) ModelEdges->GetMapper()->RemoveAllClippingPlanes();
    }

    void PartActor::SetSectionViewData(vtkUnstructuredGrid* volumeGrid, const double* sectionColor, int hatchIndex,
                                       bool hasVolumeCells)
    {
        VolumeGrid = volumeGrid;
        std::copy(sectionColor, sectionColor + 4, SectionColor);
        HatchIndex = hatchIndex;
        HasVolumeCells = hasVolumeCells;
    }

    // AddSectionViewHatch: black lines in the plane, formatted with the model edges by UpdateColor
    void PartActor::SetSectionViewHatch(vtkPolyData* hatch, vtkRenderer* renderer)
    {
        vtkNew<vtkPolyDataMapper> mapper;
        mapper->SetInputData(hatch);
        mapper->ScalarVisibilityOff();
        SectionViewHatch = vtkSmartPointer<vtkActor>::New();
        SectionViewHatch->SetMapper(mapper);
        SectionViewHatch->PickableOff();
        renderer->AddActor(SectionViewHatch);
        UpdateColor();
    }

    // vtkMaxActor(data, mapper): no edges and no locator, the drawing fields of the data
    void PartActor::CreateFromMapper(vtkMapper* mapper)
    {
        Geometry = vtkSmartPointer<vtkActor>::New();
        Geometry->SetMapper(mapper);
        Geometry->SetPickable(Style.pickable != 0);
        Geometry->GetProperty()->SetLineWidth(static_cast<float>(Style.line_width));
        Geometry->GetProperty()->SetPointSize(static_cast<float>(Style.node_size));
        UseInfiniteViewer(Geometry);
        UpdateColor();
        Geometry->GetProperty()->SetLighting(Style.lighting != 0);
    }

    // Port of CreateNodalActor. The picking of nodes goes through the parts, so a nodal actor is only pickable when
    // its data says so; ActiViz left it pickable without a locator
    void PartActor::CreateNodal(const vtkn_mesh& geometry)
    {
        vtkNew<vtkDoubleArray> coordinates;
        coordinates->SetNumberOfComponents(3);
        coordinates->SetNumberOfTuples(geometry.point_count);
        if (geometry.point_count > 0)
            std::memcpy(coordinates->GetPointer(0), geometry.coordinates,
                        sizeof(double) * 3 * static_cast<size_t>(geometry.point_count));
        vtkNew<vtkPoints> points;
        points->SetData(coordinates);
        vtkNew<vtkCellArray> vertices;
        for (vtkIdType i = 0; i < geometry.point_count; i++) vertices->InsertNextCell(1, &i);
        vtkNew<vtkPolyData> pointsPolyData;
        pointsPolyData->SetPoints(points);
        pointsPolyData->SetVerts(vertices);
        if (geometry.point_ids != nullptr)
        {
            std::vector<int> pointIds(geometry.point_ids, geometry.point_ids + geometry.point_count);
            pointsPolyData->GetPointData()->SetGlobalIds(ToIdArray(pointIds));
        }
        vtkNew<vtkPolyDataMapper> mapper;
        mapper->SetInputData(pointsPolyData);
        mapper->ScalarVisibilityOff();
        Geometry = vtkSmartPointer<vtkActor>::New();
        Geometry->SetMapper(mapper);
        Geometry->SetPickable(Style.pickable != 0);
        Geometry->GetProperty()->RenderPointsAsSpheresOn();
        UseInfiniteViewer(Geometry);
    }

    void PartActor::CreateFromMesh(const vtkn_mesh& geometry, const vtkn_mesh* modelEdges,
                                   const vtkn_mesh* modelEdgesToDraw)
    {
        vtkSmartPointer<vtkPolyData> polyActor;
        vtkSmartPointer<vtkPolyData> polyEdges;
        BuildPolyData(geometry, true, polyActor, polyEdges, &SourceCells);
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
        // A surface that is not a part, such as the faces of a constraint, covers faces of a part exactly. VTK 5.8
        // drew the later of two equal depths, VTK 9 mixes them, so such a surface is pulled a step in front of the
        // parts (the polygon offset of the view is 2, 1) and stays behind the edge lines
        if (!Style.is_a_part) mapper->SetRelativeCoincidentTopologyPolygonOffsetParameters(-1, -1);
        Geometry = vtkSmartPointer<vtkActor>::New();
        Geometry->SetMapper(mapper);
        // A part is picked when it is pickable and its cells carry the element ids
        bool pickable = Style.pickable != 0 && polyActor->GetCellData()->GetGlobalIds() != nullptr;
        Geometry->SetPickable(pickable ? 1 : 0);
        if (pickable)
        {
            CellLocator = vtkSmartPointer<vtkCellLocator>::New();
            CellLocator->SetDataSet(polyActor);
        }
        UseInfiniteViewer(Geometry);
        // Element edges
        if (Style.can_have_element_edges)
        {
            vtkNew<vtkDataSetMapper> edgesMapper;
            edgesMapper->SetInputData(polyEdges);
            ElementEdges = vtkSmartPointer<vtkActor>::New();
            ElementEdges->SetMapper(edgesMapper);
            ElementEdges->PickableOff();
        }
        // Model edges; those another part draws are left out of the drawn ones, and the part is highlighted by the
        // ones it owns
        if (modelEdges != nullptr)
        {
            vtkSmartPointer<vtkPolyData> polyModelEdges;
            vtkSmartPointer<vtkPolyData> unused;
            BuildPolyData(*modelEdges, false, polyModelEdges, unused);
            if (modelEdgesToDraw != nullptr)
            {
                ModelEdgesForHighlight = polyModelEdges;
                BuildPolyData(*modelEdgesToDraw, false, polyModelEdges, unused);
            }
            vtkNew<vtkDataSetMapper> modelEdgesMapper;
            modelEdgesMapper->SetInputData(polyModelEdges);
            ModelEdges = vtkSmartPointer<vtkActor>::New();
            ModelEdges->SetMapper(modelEdgesMapper);
            ModelEdges->PickableOff();
            // A part without element edges, such as a geometry part, shows its model edges in the element
            // edges mode
            if (!Style.can_have_element_edges)
            {
                vtkNew<vtkDataSetMapper> elementEdgesMapper;
                elementEdgesMapper->SetInputData(polyModelEdges);
                ElementEdges = vtkSmartPointer<vtkActor>::New();
                ElementEdges->SetMapper(elementEdgesMapper);
                ElementEdges->PickableOff();
            }
        }
    }

    void PartActor::ApplyStyleSizes()
    {
        Geometry->GetProperty()->SetLineWidth(static_cast<float>(Style.line_width));
        Geometry->GetProperty()->SetPointSize(static_cast<float>(Style.node_size));
        if (ModelEdges != nullptr) ModelEdges->GetProperty()->SetPointSize(static_cast<float>(Style.node_size));
        if (ElementEdges != nullptr) ElementEdges->GetProperty()->SetPointSize(static_cast<float>(Style.node_size));
    }

    vtkPolyData* PartActor::GetPolyData()
    {
        vtkMapper* mapper = Geometry->GetMapper();
        if (mapper == nullptr) return nullptr;
        if (mapper->GetInputAlgorithm() != nullptr && mapper->GetNumberOfInputConnections(0) > 0)
            mapper->GetInputAlgorithm()->Update();
        return vtkPolyData::SafeDownCast(mapper->GetInput());
    }

    vtkPolyData* PartActor::GetModelEdgesForHighlight()
    {
        if (ModelEdgesForHighlight != nullptr) return ModelEdgesForHighlight;
        if (ModelEdges == nullptr) return nullptr;
        return vtkPolyData::SafeDownCast(ModelEdges->GetMapper()->GetInput());
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
        if (SectionViewHatch != nullptr) renderer->RemoveActor(SectionViewHatch);
        renderer->RemoveActor(Geometry);
        if (ElementEdges != nullptr) renderer->RemoveActor(ElementEdges);
        if (ModelEdges != nullptr) renderer->RemoveActor(ModelEdges);
    }

    void PartActor::SetVisible(bool visible)
    {
        Style.visible = visible ? 1 : 0;
        Geometry->SetVisibility(visible);
        if (SectionViewHatch != nullptr) SectionViewHatch->SetVisibility(visible);
    }

    void PartActor::SetGlobalLighting(double ambient, double diffuse)
    {
        if (!Style.use_global_lighting) return;
        Style.ambient = ambient;
        Style.diffuse = diffuse;
        UpdateColor();
    }

    // Port of vtkMaxActor.UpdateColor without the color table
    void PartActor::UpdateColor()
    {
        const double* color = Style.color;
        double opacity = color[3];
        vtkProperty* property = Geometry->GetProperty();
        if (!ColorTable.empty())
        {
            // The color table replaces the ambient material color and not the ambient component, so the ambient
            // component is baked into the color table
            double diffuse = std::min(Style.diffuse, 0.999);
            int count = static_cast<int>(ColorTable.size());
            vtkNew<vtkLookupTable> cellLut;
            cellLut->SetNumberOfTableValues(count + 1);
            cellLut->Build();
            cellLut->SetTableValue(0, Style.ambient, Style.ambient, Style.ambient, 1);
            for (int i = 0; i < count; i++)
                cellLut->SetTableValue(i + 1, Style.ambient * ColorTable[i][0], Style.ambient * ColorTable[i][1],
                                       Style.ambient * ColorTable[i][2], 1);
            Geometry->GetMapper()->SetLookupTable(cellLut);
            Geometry->GetMapper()->SetScalarRange(-1, count - 1);
            property->SetAmbient(1);
            property->SetDiffuse(diffuse);
            property->SetDiffuseColor(color[0], color[1], color[2]);
            property->SetOpacity(opacity);
            property->SetSpecularColor(color[0], color[1], color[2]);
            property->SetSpecular(0.6);
            property->SetSpecularPower(100);
        }
        else
        {
            property->SetAmbient(Style.ambient);
            property->SetDiffuse(Style.diffuse);
            property->SetOpacity(opacity);
            if (!Style.color_contours)
            {
                property->SetColor(color[0], color[1], color[2]);
                property->SetSpecular(0.6);
                property->SetSpecularPower(100);
            }
            else
            {
                property->SetAmbientColor(1, 1, 1);
                // VTK 5.8 lit the colors of the results with the default global ambient light of OpenGL (0.2) on
                // top of the ambient of the actor; VTK 9 has no global ambient light
                property->SetAmbient(Style.ambient * 1.2);
            }
        }
        //
        if (ModelEdges != nullptr) SetEdgeLine(ModelEdges, 0.8, opacity * 0.7);
        if (SectionViewHatch != nullptr) SetEdgeLine(SectionViewHatch, 0.8, opacity * 0.7);
        if (ElementEdges != nullptr) SetEdgeLine(ElementEdges, 0.5, opacity * 0.4);
        if (Style.has_backface_color && !Style.backface_culling)
        {
            vtkNew<vtkProperty> backface;
            backface->SetAmbient(Style.ambient);
            backface->SetDiffuse(Style.diffuse);
            backface->SetOpacity(Style.backface_color[3]);
            if (!Style.color_contours)
            {
                backface->SetColor(Style.backface_color[0], Style.backface_color[1], Style.backface_color[2]);
                backface->SetSpecular(0.6);
                backface->SetSpecularPower(100);
            }
            else backface->SetAmbientColor(1, 1, 1);
            Geometry->SetBackfaceProperty(backface);
        }
    }

    void PartActor::SetPointScalars(const float* values, int count, const vtkn_extreme_nodes* extremes)
    {
        vtkPolyData* polyData = vtkPolyData::SafeDownCast(Geometry->GetMapper()->GetInput());
        Require(polyData != nullptr, "The actor has no point data for scalars.");
        if (values == nullptr)
        {
            polyData->GetPointData()->RemoveArray("scalars");
            PointScalars = nullptr;
            HasExtremeNodes = false;
            Extremes = {};
            Geometry->GetMapper()->SetInterpolateScalarsBeforeMapping(0);
            return;
        }
        Require(count == polyData->GetNumberOfPoints(), "There must be one scalar value per point of the actor.");
        PointScalars = vtkSmartPointer<vtkFloatArray>::New();
        PointScalars->SetName("scalars");
        PointScalars->SetNumberOfValues(count);
        for (int i = 0; i < count; i++) PointScalars->SetValue(i, values[i]);
        polyData->GetPointData()->SetScalars(PointScalars);
        HasExtremeNodes = extremes != nullptr;
        if (extremes != nullptr) Extremes = *extremes;
    }

    // Port of the cell scalars of CreatePolyFromData and of the color table of vtkMaxActor: each triangle, line
    // and vertex takes the value of the cell it came from
    void PartActor::SetCellColors(const float* values, int count, const double* rgb, int rgbCount)
    {
        vtkPolyData* polyData = vtkPolyData::SafeDownCast(Geometry->GetMapper()->GetInput());
        Require(polyData != nullptr, "The actor has no cell data for colors.");
        if (values != nullptr)
        {
            CellScalars = vtkSmartPointer<vtkFloatArray>::New();
            CellScalars->SetNumberOfValues(static_cast<vtkIdType>(SourceCells.size()));
            for (size_t i = 0; i < SourceCells.size(); i++)
            {
                int source = SourceCells[i];
                Require(source < count, "There must be one value per cell of the actor.");
                CellScalars->SetValue(static_cast<vtkIdType>(i), values[source]);
            }
            polyData->GetCellData()->SetScalars(CellScalars);
        }
        ColorTable.clear();
        for (int i = 0; i < rgbCount; i++) ColorTable.push_back({ rgb[3 * i], rgb[3 * i + 1], rgb[3 * i + 2] });
        UpdateColor();
    }

    // The actor part of UpdateScalarFormatting: with color contours the result values are the active scalars and
    // are interpolated before mapping; without them no point scalars are active and the actor takes its colors
    void PartActor::ApplyScalarVisibility()
    {
        vtkPolyData* polyData = vtkPolyData::SafeDownCast(Geometry->GetMapper()->GetInput());
        if (polyData == nullptr) return;
        vtkMapper* mapper = Geometry->GetMapper();
        if (Style.color_contours)
        {
            if (PointScalars != nullptr) polyData->GetPointData()->SetActiveScalars("scalars");
            if (mapper->GetInterpolateScalarsBeforeMapping() != 1) mapper->SetInterpolateScalarsBeforeMapping(1);
        }
        else
        {
            polyData->GetPointData()->SetActiveScalars("none");
            if (mapper->GetInterpolateScalarsBeforeMapping() != 0) mapper->SetInterpolateScalarsBeforeMapping(0);
            UpdateColor();
        }
    }

    void PartActor::SetResultLookupTable(vtkLookupTable* lookupTable, const double range[2])
    {
        vtkMapper* mapper = Geometry->GetMapper();
        mapper->SetScalarRange(range[0], range[1]);
        mapper->SetLookupTable(lookupTable);
    }

    void PartActor::ApplyEdgesVisibility(vtkn_edges_visibility edgesVisibility)
    {
        ApplyEdgesVisibility(edgesVisibility, VTKN_LAYER_BASE, false);
    }

    // Port of ApplyEdgeVisibilityAndBackfaceCullingToActor, ...ToActorEdges and ...ToModelEdges. The overlay layer
    // keeps the formatting of its geometry
    void PartActor::ApplyEdgesVisibility(vtkn_edges_visibility edgesVisibility, vtkn_layer layer,
                                         bool sectionViewActive)
    {
        bool visible = Style.visible != 0 && !GeometryHiddenBySectionView;
        if (layer != VTKN_LAYER_OVERLAY)
        {
            vtkProperty* property = Geometry->GetProperty();
            if (!Style.use_edge_visibility) Geometry->SetVisibility(visible);
            else
            {
                if (edgesVisibility == VTKN_EDGES_ELEMENT_EDGES) property->SetInterpolationToFlat();
                else property->SetInterpolationToPhong();
                //
                if (edgesVisibility == VTKN_EDGES_WIREFRAME && layer == VTKN_LAYER_BASE) Geometry->VisibilityOff();
                else
                {
                    property->SetRepresentationToSurface();
                    Geometry->SetVisibility(visible);
                    property->SetBackfaceCulling(Style.backface_culling != 0);
                    if (Style.representation == VTKN_REPRESENTATION_SOLID_AS_SHELL && sectionViewActive)
                        property->BackfaceCullingOff();
                }
            }
        }
        // The edges of an actor outside the base layer follow the edges mode only
        bool edgesVisible = layer != VTKN_LAYER_BASE || visible;
        if (ModelEdges != nullptr)
        {
            ModelEdges->SetVisibility(edgesVisible && (edgesVisibility == VTKN_EDGES_MODEL_EDGES ||
                                                       edgesVisibility == VTKN_EDGES_WIREFRAME));
            ModelEdges->GetProperty()->BackfaceCullingOn();
        }
        if (ElementEdges != nullptr)
        {
            ElementEdges->SetVisibility(edgesVisible && edgesVisibility == VTKN_EDGES_ELEMENT_EDGES);
            ElementEdges->GetProperty()->BackfaceCullingOn();
        }
    }

    // Port of ApplySelectionFormattingToActor: nodes and edges in the full highlight color, surfaces darker and
    // lit, every point larger and a line of the default width wider
    void PartActor::ApplySelectionFormatting(const HighlightColors& colors)
    {
        if (Style.keep_own_color) return;
        vtkPolyData* polyData = GetPolyData();
        bool onlyEdges = true;
        bool onlyNodes = true;
        if (polyData != nullptr)
        {
            vtkNew<vtkCellTypes> types;
            polyData->GetDistinctCellTypes(types);
            for (vtkIdType i = 0; i < types->GetNumberOfTypes(); i++)
            {
                unsigned char cellType = types->GetCellType(i);
                if (cellType != VTK_LINE && cellType != VTK_QUADRATIC_EDGE) onlyEdges = false;
                if (cellType != VTK_VERTEX) onlyNodes = false;
            }
        }
        double k = onlyNodes || onlyEdges ? 1 : 0.7;
        double lighting = onlyNodes || onlyEdges ? 1 : 0.6;
        vtkProperty* property = Geometry->GetProperty();
        property->SetPointSize(property->GetPointSize() + 2);
        const double* color = Style.use_secondary_highlight_color ? colors.Secondary : colors.Primary;
        property->SetColor(color[0] * k, color[1] * k, color[2] * k);
        property->SetAmbient(lighting);
        property->SetDiffuse(lighting);
        if (property->GetLineWidth() == 1) property->SetLineWidth(2.2f);
        property->SetOpacity(1);
        Geometry->PickableOff();
    }
}
