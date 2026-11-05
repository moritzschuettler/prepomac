// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The selection layer of vtkControl: the highlights of the mouse selection (the PickBy* hover paths and the
// highlight branch of PickByArea) and of the selected items, drawn by a renderer over the parts. A highlight is
// built from the data the Controller returns, as vtkMaxActor builds it (CreateNodalActor, CreatePolyFromData,
// CreateFromDataByVisualizationSurfaceExtraction), and drawn with the property vtkControl gives it.
#include "vtkNativeView.h"
#include "vtkNativeError.h"

#include <vtkCamera.h>
#include <vtkCellArray.h>
#include <vtkCellType.h>
#include <vtkDataSetMapper.h>
#include <vtkExtractEdges.h>
#include <vtkNew.h>
#include <vtkPoints.h>
#include <vtkPolyData.h>
#include <vtkPolyDataSilhouette.h>
#include <vtkProperty.h>
#include <vtkUnstructuredGridGeometryFilter.h>

#include <algorithm>
#include <vector>

namespace vtkn
{
    namespace
    {
        bool IsVolumeCell(int cellType)
        {
            return cellType == VTK_TETRA || cellType == VTK_HEXAHEDRON || cellType == VTK_WEDGE ||
                   cellType == VTK_PYRAMID || cellType == VTK_QUADRATIC_TETRA ||
                   cellType == VTK_QUADRATIC_HEXAHEDRON || cellType == VTK_QUADRATIC_WEDGE ||
                   cellType == VTK_QUADRATIC_PYRAMID;
        }

        vtkSmartPointer<vtkActor> CreateActor(vtkDataSet* data)
        {
            vtkNew<vtkDataSetMapper> mapper;
            mapper->SetInputData(data);
            mapper->ScalarVisibilityOff();
            vtkSmartPointer<vtkActor> actor = vtkSmartPointer<vtkActor>::New();
            actor->SetMapper(mapper);
            actor->PickableOff();
            return actor;
        }
    }

    // The selection renderer of vtkControl: the layer between the overlay and the widgets, with the camera and
    // the lights of the parts; made when the first highlight is drawn
    vtkRenderer* View::GetHighlightRenderer()
    {
        if (HighlightRenderer != nullptr) return HighlightRenderer;
        HighlightRenderer = vtkSmartPointer<vtkRenderer>::New();
        HighlightRenderer->SetLayer(2);
        HighlightRenderer->InteractiveOff();
        HighlightRenderer->SetActiveCamera(Renderer->GetActiveCamera());
        for (const auto& light : Lights) HighlightRenderer->AddLight(light);
        WidgetRenderer->SetLayer(3);
        RenderWindow->SetNumberOfLayers(4);
        RenderWindow->AddRenderer(HighlightRenderer);
        return HighlightRenderer;
    }

    // ApplySelectionFormattingToActor and the property the PickBy* paths set, as the caller computed them
    void View::AddHighlightActor(vtkActor* actor, const vtkn_highlight_style& style)
    {
        vtkProperty* property = actor->GetProperty();
        property->SetColor(style.color[0], style.color[1], style.color[2]);
        property->SetAmbient(style.ambient);
        property->SetDiffuse(style.diffuse);
        property->SetSpecular(style.specular);
        property->SetPointSize(static_cast<float>(style.point_size));
        property->SetLineWidth(static_cast<float>(style.line_width));
        property->SetOpacity(1);
        property->BackfaceCullingOff();
        vtkRenderer* renderer = style.on_geometry ? Renderer.GetPointer() : GetHighlightRenderer();
        renderer->AddActor(actor);
        Highlights.push_back({ actor, renderer, style.current != 0 });
    }

    void View::AddHighlight(const vtkn_mesh& mesh, const vtkn_highlight_style& style)
    {
        CheckMesh(mesh, "highlight");
        vtkNew<vtkPoints> points;
        points->SetNumberOfPoints(mesh.point_count);
        for (int i = 0; i < mesh.point_count; i++) points->SetPoint(i, mesh.coordinates + 3 * i);
        vtkNew<vtkUnstructuredGrid> grid;
        grid->SetPoints(points);
        bool hasVolumeCells = false;
        if (style.points || mesh.cell_count == 0)
        {
            // CreateNodalActor: a vertex per node
            grid->Allocate(mesh.point_count);
            for (vtkIdType i = 0; i < mesh.point_count; i++) grid->InsertNextCell(VTK_VERTEX, 1, &i);
        }
        else
        {
            grid->Allocate(mesh.cell_count);
            std::vector<vtkIdType> ids;
            for (int i = 0, cell = 0; i < mesh.cells_length; cell++)
            {
                int count = mesh.cells[i];
                ids.assign(mesh.cells + i + 1, mesh.cells + i + 1 + count);
                grid->InsertNextCell(mesh.cell_types[cell], count, ids.data());
                hasVolumeCells = hasVolumeCells || IsVolumeCell(mesh.cell_types[cell]);
                i += count + 1;
            }
        }
        // The visualization surface of volume cells: their outer faces
        vtkSmartPointer<vtkDataSet> surface = grid.GetPointer();
        if (hasVolumeCells)
        {
            vtkNew<vtkUnstructuredGridGeometryFilter> filter;
            filter->SetInputData(grid);
            filter->Update();
            surface = filter->GetOutput();
        }
        vtkSmartPointer<vtkActor> actor = CreateActor(surface);
        AddHighlightActor(actor, style);
        // The element edges keep the look of the part's element edges and are drawn on the same layer
        if (style.element_edges && !style.points)
        {
            vtkNew<vtkExtractEdges> edges;
            edges->SetInputData(surface);
            edges->Update();
            vtkSmartPointer<vtkActor> edgesActor = CreateActor(edges->GetOutput());
            vtkProperty* property = edgesActor->GetProperty();
            property->SetColor(0, 0, 0);
            property->SetLighting(false);
            property->SetLineWidth(1);
            vtkRenderer* renderer = style.on_geometry ? Renderer.GetPointer() : GetHighlightRenderer();
            renderer->AddActor(edgesActor);
            Highlights.push_back({ edgesActor, renderer, style.current != 0 });
        }
    }

    // GetHighlightActorFromActorName: a copy of the part's model edges, or its silhouette from the current camera
    void View::AddPartHighlight(const std::string& name, const vtkn_highlight_style& style)
    {
        auto found = Parts.find(name);
        if (found == Parts.end()) throw Error(VTKN_ERROR_NOT_FOUND, "No part is named '" + name + "'.");
        PartActor* part = found->second.get();
        vtkDataSet* modelEdges = part->GetModelEdges() != nullptr ? part->GetModelEdges()->GetMapper()->GetInput()
                                                                  : nullptr;
        vtkSmartPointer<vtkActor> actor;
        if (modelEdges != nullptr && modelEdges->GetNumberOfPoints() > 0) actor = CreateActor(modelEdges);
        else
        {
            vtkPolyData* surface = vtkPolyData::SafeDownCast(part->GetGeometry()->GetMapper()->GetInput());
            Require(surface != nullptr, "The part has no surface.");
            vtkNew<vtkPolyDataSilhouette> silhouette;
            silhouette->SetInputData(surface);
            silhouette->SetCamera(Renderer->GetActiveCamera());
            silhouette->SetDirectionToCameraVector();
            silhouette->SetBorderEdges(1);
            silhouette->SetEnableFeatureAngle(1);
            silhouette->SetFeatureAngle(30);
            silhouette->Update();
            vtkNew<vtkPolyData> copy;
            copy->DeepCopy(silhouette->GetOutput());
            actor = CreateActor(copy);
        }
        AddHighlightActor(actor, style);
    }

    void View::ClearHighlights(bool currentOnly)
    {
        auto removed = std::remove_if(Highlights.begin(), Highlights.end(), [currentOnly](const Highlight& highlight) {
            if (currentOnly && !highlight.Current) return false;
            highlight.Renderer->RemoveActor(highlight.Actor);
            return true;
        });
        Highlights.erase(removed, Highlights.end());
    }
}
