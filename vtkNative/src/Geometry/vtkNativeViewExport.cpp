// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// What vtkControl read back from the ActiViz actors: the drawn actors of the 3MF export (GetVtkMaxActorData), the
// actors the element-wise section view hides (GeometryHiddenBySectionView), the crop of a part (CropWithCylinder and
// CropWithCube of vtkMaxActor) and the anchor of an annotation that follows an animation (TryCreateAnimationArrowAnchor).
#include "vtkNativeError.h"
#include "vtkNativeGuard.h"
#include "vtkNativePartActor.h"
#include "vtkNativeView.h"

#include <vtkBox.h>
#include <vtkCellArray.h>
#include <vtkCellLocator.h>
#include <vtkClipPolyData.h>
#include <vtkCylinder.h>
#include <vtkDataArray.h>
#include <vtkDecimatePro.h>
#include <vtkGenericCell.h>
#include <vtkIdList.h>
#include <vtkMapper.h>
#include <vtkNew.h>
#include <vtkPointData.h>
#include <vtkPolyData.h>
#include <vtkSTLWriter.h>
#include <vtkTransform.h>
#include <vtkUnstructuredGrid.h>

#include <vector>

namespace vtkn
{
    namespace
    {
        vtkPolyData* GetInput(vtkActor* actor)
        {
            if (actor == nullptr || actor->GetMapper() == nullptr) return nullptr;
            vtkMapper* mapper = actor->GetMapper();
            if (mapper->GetInputAlgorithm() != nullptr && mapper->GetNumberOfInputConnections(0) > 0)
                mapper->GetInputAlgorithm()->Update();
            return vtkPolyData::SafeDownCast(mapper->GetInput());
        }

        void ReadPoints(vtkPolyData* polyData, std::vector<double>& coordinates)
        {
            vtkIdType count = polyData->GetNumberOfPoints();
            coordinates.resize(3 * static_cast<size_t>(count));
            for (vtkIdType i = 0; i < count; i++) polyData->GetPoint(i, &coordinates[3 * static_cast<size_t>(i)]);
        }

        // The edges of GetVtkMaxActorData: a line cell is a segment, a cell of three points two segments
        void ReadEdges(vtkActor* actor, std::vector<double>& coordinates, std::vector<int32_t>& edges)
        {
            vtkPolyData* polyData = GetInput(actor);
            if (polyData == nullptr || actor->GetVisibility() == 0) return;
            ReadPoints(polyData, coordinates);
            vtkNew<vtkIdList> ids;
            for (vtkIdType i = 0; i < polyData->GetNumberOfCells(); i++)
            {
                polyData->GetCellPoints(i, ids);
                vtkIdType count = ids->GetNumberOfIds();
                if (count == 2 || count == 3)
                {
                    edges.push_back(static_cast<int32_t>(ids->GetId(0)));
                    edges.push_back(static_cast<int32_t>(ids->GetId(1)));
                }
                if (count == 3)
                {
                    edges.push_back(static_cast<int32_t>(ids->GetId(1)));
                    edges.push_back(static_cast<int32_t>(ids->GetId(2)));
                }
            }
        }
    }

    // Port of GetVtkMaxActorData: a wire draws its lines as model edges in its own color; the faces are the polygons
    // of the geometry with their values when the actor is colored by them
    int View::PrepareExport()
    {
        ExportActors.clear();
        std::vector<std::pair<std::string, PartActor*>> actors;
        for (const auto& entry : Parts) actors.push_back({ entry.first, entry.second.get() });
        for (const auto& entry : BaseActors) actors.push_back({ entry.first, entry.second.get() });
        for (const auto& entry : OverlayParts) actors.push_back({ entry.first, entry.second.get() });
        for (const auto& entry : actors)
        {
            PartActor* actor = entry.second;
            const vtkn_part_style& style = actor->GetStyle();
            ExportActor exported;
            exported.Name = entry.first;
            for (int i = 0; i < 4; i++) exported.Color[i] = style.color[i];
            exported.Wire = style.representation == VTKN_REPRESENTATION_WIRE;
            exported.ColorContours = style.color_contours != 0;
            if (exported.Wire) ReadEdges(actor->GetGeometry(), exported.ModelEdgeCoordinates, exported.ModelEdges);
            else
            {
                vtkActor* geometry = actor->GetGeometry();
                vtkPolyData* polyData = GetInput(geometry);
                if (polyData != nullptr && geometry->GetVisibility() != 0)
                {
                    ReadPoints(polyData, exported.Coordinates);
                    vtkCellArray* polys = polyData->GetPolys();
                    vtkNew<vtkIdList> ids;
                    for (polys->InitTraversal(); polys->GetNextCell(ids) != 0;)
                    {
                        if (ids->GetNumberOfIds() != 3) continue;
                        for (int i = 0; i < 3; i++) exported.Triangles.push_back(static_cast<int32_t>(ids->GetId(i)));
                    }
                    vtkDataArray* scalars = polyData->GetPointData()->GetArray("scalars");
                    if (exported.ColorContours && scalars != nullptr)
                    {
                        exported.Values.resize(static_cast<size_t>(scalars->GetNumberOfTuples()));
                        for (vtkIdType i = 0; i < scalars->GetNumberOfTuples(); i++)
                            exported.Values[static_cast<size_t>(i)] = static_cast<float>(scalars->GetTuple1(i));
                    }
                }
                ReadEdges(actor->GetElementEdges(), exported.ElementEdgeCoordinates, exported.ElementEdges);
                ReadEdges(actor->GetModelEdges(), exported.ModelEdgeCoordinates, exported.ModelEdges);
            }
            ExportActors.push_back(std::move(exported));
        }
        return static_cast<int>(ExportActors.size());
    }

    void View::GetExportActor(int index, vtkn_export_actor& actor)
    {
        Require(index >= 0 && index < static_cast<int>(ExportActors.size()), "The export actor index is out of range.");
        const ExportActor& exported = ExportActors[static_cast<size_t>(index)];
        actor = {};
        actor.name = exported.Name.c_str();
        for (int i = 0; i < 4; i++) actor.color[i] = exported.Color[i];
        actor.wire = exported.Wire ? 1 : 0;
        actor.color_contours = exported.ColorContours ? 1 : 0;
        if (!exported.Coordinates.empty())
        {
            actor.coordinates3 = exported.Coordinates.data();
            actor.point_count = static_cast<int32_t>(exported.Coordinates.size() / 3);
            actor.triangles3 = exported.Triangles.empty() ? nullptr : exported.Triangles.data();
            actor.triangle_count = static_cast<int32_t>(exported.Triangles.size() / 3);
            actor.values = exported.Values.empty() ? nullptr : exported.Values.data();
        }
        if (!exported.ElementEdgeCoordinates.empty())
        {
            actor.element_edge_coordinates3 = exported.ElementEdgeCoordinates.data();
            actor.element_edge_point_count = static_cast<int32_t>(exported.ElementEdgeCoordinates.size() / 3);
            actor.element_edges2 = exported.ElementEdges.empty() ? nullptr : exported.ElementEdges.data();
            actor.element_edge_count = static_cast<int32_t>(exported.ElementEdges.size() / 2);
        }
        if (!exported.ModelEdgeCoordinates.empty())
        {
            actor.model_edge_coordinates3 = exported.ModelEdgeCoordinates.data();
            actor.model_edge_point_count = static_cast<int32_t>(exported.ModelEdgeCoordinates.size() / 3);
            actor.model_edges2 = exported.ModelEdges.empty() ? nullptr : exported.ModelEdges.data();
            actor.model_edge_count = static_cast<int32_t>(exported.ModelEdges.size() / 2);
        }
    }

    std::string View::GetSectionViewHiddenActors()
    {
        std::string names;
        for (const auto* actors : { &Parts, &BaseActors })
        {
            for (const auto& entry : *actors)
            {
                if (!entry.second->GetGeometryHiddenBySectionView()) continue;
                names += entry.first;
                names.push_back('\0');
            }
        }
        return names;
    }

    // Port of CropWithCylinder and CropWithCube of vtkMaxActor
    void View::CropPartToStl(const std::string& name, int shape, const double* parameters, const std::string& fileName)
    {
        auto found = Parts.find(name);
        if (found == Parts.end()) throw Error(VTKN_ERROR_NOT_FOUND, "No part is named '" + name + "'.");
        vtkNew<vtkClipPolyData> clipper;
        clipper->SetInputData(found->second->GetPolyData());
        vtkNew<vtkTransform> transform;
        vtkNew<vtkCylinder> cylinder;
        vtkNew<vtkBox> box;
        if (shape == 0)
        {
            transform->Identity();
            transform->RotateX(90);
            cylinder->SetTransform(transform);
            cylinder->SetRadius(parameters[0]);
            clipper->SetClipFunction(cylinder);
        }
        else if (shape == 1)
        {
            double half = parameters[3] / 2;
            box->SetBounds(parameters[0] - half, parameters[0] + half, parameters[1] - half, parameters[1] + half,
                           parameters[2] - half, parameters[2] + half);
            clipper->SetClipFunction(box);
        }
        else throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The crop shape is not 0 (a cylinder) or 1 (a cube).");
        clipper->GenerateClippedOutputOn();
        clipper->GenerateClipScalarsOn();
        clipper->SetValue(0);
        vtkNew<vtkDecimatePro> decimate;
        decimate->SetInputConnection(clipper->GetClippedOutputPort());
        decimate->SetTargetReduction(0.05);
        decimate->PreserveTopologyOn();
        vtkNew<vtkSTLWriter> writer;
        writer->SetFileName(fileName.c_str());
        writer->SetInputConnection(decimate->GetOutputPort());
        if (writer->Write() != 1)
            throw Error(VTKN_ERROR_VTK, "The cropped part could not be written to '" + fileName + "'.");
    }
}

using namespace vtkn;

extern "C"
{
    vtkn_status VTKN_CALL vtkn_view_get_section_view_hidden_actors(vtkn_view view, char* buffer, int32_t capacity,
                                                                    int32_t* required)
    {
        return GuardVtk([&]() {
            return CopyOut(View::FromHandleOfActors(view)->GetSectionViewHiddenActors(), buffer, capacity, required);
        });
    }

    vtkn_status VTKN_CALL vtkn_view_crop_part_to_stl(vtkn_view view, const char* name, int32_t shape,
                                                      const double* parameters, const char* file_name)
    {
        return GuardVtk([&]() {
            Require(parameters != nullptr, "The argument 'parameters' must not be null.");
            View::FromHandleOfActors(view)->CropPartToStl(ToName(name, "name"), shape, parameters,
                                                          ToName(file_name, "file_name"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_prepare_export(vtkn_view view, int32_t* actor_count)
    {
        return GuardVtk([&]() {
            Require(actor_count != nullptr, "The argument 'actor_count' must not be null.");
            *actor_count = View::FromHandleOfActors(view)->PrepareExport();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_export_actor(vtkn_view view, int32_t index, vtkn_export_actor* actor)
    {
        return GuardVtk([&]() {
            Require(actor != nullptr, "The argument 'actor' must not be null.");
            View::FromHandleOfActors(view)->GetExportActor(index, *actor);
            return VTKN_OK;
        });
    }

    // Port of TryCreateAnimationArrowAnchor of vtkMaxActor: the cell that holds the point, else the closest one, and
    // the weights of its points
    vtkn_status VTKN_CALL vtkn_locate_point_in_mesh(const vtkn_mesh* mesh, const double* point3, int32_t* point_indices,
                                                     double* weights, int32_t capacity, int32_t* count,
                                                     double* closest3, int32_t* found)
    {
        return GuardVtk([&]() {
            Require(mesh != nullptr && point3 != nullptr && point_indices != nullptr && weights != nullptr &&
                    count != nullptr && closest3 != nullptr && found != nullptr, "An argument must not be null.");
            *count = 0;
            *found = 0;
            vtkSmartPointer<vtkUnstructuredGrid> grid = BuildUnstructuredGrid(*mesh);
            if (grid->GetNumberOfCells() == 0) return VTKN_OK;
            vtkNew<vtkCellLocator> locator;
            locator->SetDataSet(grid);
            locator->BuildLocator();
            vtkNew<vtkGenericCell> cell;
            double point[3] = { point3[0], point3[1], point3[2] };
            double evaluationPoint[3] = { point3[0], point3[1], point3[2] };
            vtkIdType cellId = locator->FindCell(point);
            int subId = 0;
            double distance2 = 0;
            if (cellId >= 0) grid->GetCell(cellId, cell);
            else
            {
                locator->FindClosestPoint(point, evaluationPoint, cell, cellId, subId, distance2);
                if (cellId < 0) return VTKN_OK;
            }
            int pointCount = static_cast<int>(cell->GetNumberOfPoints());
            if (pointCount == 0) return VTKN_OK;
            if (pointCount > capacity) return VTKN_ERROR_BUFFER_TOO_SMALL;
            std::vector<double> cellWeights(static_cast<size_t>(pointCount));
            double closest[3];
            double parametric[3];
            if (cell->EvaluatePosition(evaluationPoint, closest, subId, parametric, distance2, cellWeights.data()) < 0)
                return VTKN_OK;
            for (int i = 0; i < 3; i++) closest3[i] = 0;
            for (int i = 0; i < pointCount; i++)
            {
                vtkIdType pointId = cell->GetPointId(i);
                point_indices[i] = static_cast<int32_t>(pointId);
                weights[i] = cellWeights[static_cast<size_t>(i)];
                double cellPoint[3];
                grid->GetPoint(pointId, cellPoint);
                for (int j = 0; j < 3; j++) closest3[j] += weights[i] * cellPoint[j];
            }
            *count = pointCount;
            *found = 1;
            return VTKN_OK;
        });
    }
}
