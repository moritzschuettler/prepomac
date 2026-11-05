// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Picking, the screen to model step of the Selection region of vtkControl: a point pick names a part and gives a
// point on its surface, an area pick gives the frustum and the parts inside it. Which nodes, elements or surfaces
// those mean is decided by the caller, as the Controller decides it from what vtkControl reported. Section views
// are not ported.
#include "vtkNativeView.h"
#include "vtkNativeError.h"

#include <vtkCamera.h>
#include <vtkCellData.h>
#include <vtkCellPicker.h>
#include <vtkDataArray.h>
#include <vtkNew.h>
#include <vtkPlane.h>
#include <vtkPlanes.h>
#include <vtkProp3DCollection.h>
#include <vtkPropPicker.h>
#include <vtkRenderedAreaPicker.h>

#include <algorithm>
#include <cfloat>
#include <cmath>

namespace vtkn
{
    PartActor* View::FindPart(vtkProp* prop, std::string* name)
    {
        for (const auto& entry : Parts)
        {
            if (!entry.second->Owns(prop)) continue;
            if (name != nullptr) *name = entry.first;
            return entry.second.get();
        }
        return nullptr;
    }

    // GetSelectionPrecision: the selection distance in pixels as a model length at the focal plane
    double View::GetSelectionPrecision(int selectionDistance)
    {
        double display1[2] = { 0, 0 };
        double display2[2] = { static_cast<double>(selectionDistance), 0 };
        double world1[3];
        double world2[3];
        DisplayToWorld(display1, world1);
        DisplayToWorld(display2, world2);
        return std::sqrt(std::pow(world1[0] - world2[0], 2) + std::pow(world1[1] - world2[1], 2) +
                         std::pow(world1[2] - world2[2], 2));
    }

    // Port of the two GetGlobalCellIdClosestTo3DPoint overloads: the closest surface point of the picked part,
    // else of every visible part, which a user pick accepts only within the selection precision
    int View::GetGlobalCellIdClosestTo3DPoint(double point[3], PartActor* onlyPart,
                                              const std::set<std::string>* selectableParts, double maxDistance2)
    {
        for (int pass = onlyPart != nullptr ? 0 : 1; pass < 2; pass++)
        {
            int globalCellId = -1;
            double minDistance = DBL_MAX;
            double pointOut[3] = { 0, 0, 0 };
            for (const auto& entry : Parts)
            {
                PartActor* part = entry.second.get();
                if (pass == 0 && part != onlyPart) continue;
                bool visible = selectableParts != nullptr ? selectableParts->count(entry.first) > 0
                                                          : part->GetGeometry()->GetVisibility() != 0;
                vtkCellLocator* locator = part->GetCellLocator();
                if (visible && locator != nullptr)
                {
                    double closestPoint[3];
                    vtkIdType localCellId = -1;
                    int subId = -1;
                    double distance2 = -1;
                    locator->FindClosestPoint(point, closestPoint, localCellId, subId, distance2);
                    if (distance2 >= 0 && distance2 < minDistance)
                    {
                        minDistance = distance2;
                        vtkDataArray* globalIds = locator->GetDataSet()->GetCellData()->GetGlobalIds();
                        globalCellId = static_cast<int>(globalIds->GetTuple1(localCellId));
                        std::copy(closestPoint, closestPoint + 3, pointOut);
                    }
                }
                if (minDistance == 0) break;
            }
            if (globalCellId != -1 && minDistance <= maxDistance2)
            {
                std::copy(pointOut, pointOut + 3, point);
                return globalCellId;
            }
        }
        return -1;
    }

    // Port of GetPickPoint: the hardware pick of the pickable parts, all drawn opaque, names the part and its
    // depth gives the point, which is moved onto the closest surface cell; where the exact pixel misses, a cell
    // pick within the selection distance in pixels takes over
    bool View::PickPoint(int x, int y, int selectionDistance, bool limitDistance,
                         const std::set<std::string>* selectableParts, double point[3], double direction[3],
                         std::string& partName, int& cellId)
    {
        Require(selectionDistance >= 1, "The selection distance must be at least one pixel.");
        Renderer->GetActiveCamera()->GetDirectionOfProjection(direction);
        partName.clear();
        cellId = -1;
        if (Widgets.IsPositionOverWidget(x, y)) return false;
        //
        for (const auto& entry : Parts)
        {
            if (selectableParts != nullptr)
                entry.second->GetGeometry()->SetVisibility(selectableParts->count(entry.first) > 0);
            entry.second->SetPickOpacity();
        }
        double maxDistance2 = limitDistance ? std::pow(GetSelectionPrecision(selectionDistance), 2) : DBL_MAX;
        bool hit = false;
        double p[3] = { 0, 0, 0 };
        std::string name;
        // First pick
        RenderWindow->Render();
        vtkNew<vtkPropPicker> propPicker;
        propPicker->PickFromListOn();
        for (const auto& entry : Parts)
        {
            if (entry.second->GetCellLocator() != nullptr) propPicker->AddPickList(entry.second->GetGeometry());
        }
        propPicker->Pick(x, y, 0, Renderer);
        PartActor* part = FindPart(propPicker->GetActor(), &name);
        if (part != nullptr)
        {
            // vtkControl rebuilt the point from the depth of the pick; the depth VTK 9 reports from its hardware
            // selection misses the surface by pixels, so the ray of the pixel is intersected with the picked part
            vtkNew<vtkCellPicker> surfacePicker;
            surfacePicker->SetTolerance(0);
            surfacePicker->PickFromListOn();
            surfacePicker->AddPickList(part->GetGeometry());
            surfacePicker->AddLocator(part->GetCellLocator());
            if (ExactPicking && surfacePicker->Pick(x, y, 0, Renderer) != 0) surfacePicker->GetPickPosition(p);
            else
            {
                Renderer->SetDisplayPoint(x, y, Renderer->GetPickedZ());
                Renderer->DisplayToWorld();
                double* world = Renderer->GetWorldPoint();
                p[0] = world[0] / world[3];
                p[1] = world[1] / world[3];
                p[2] = world[2] / world[3];
            }
            cellId = GetGlobalCellIdClosestTo3DPoint(p, part, selectableParts, maxDistance2);
            hit = cellId != -1;
        }
        // Within the selection distance of the pixel
        if (!hit && ExactPicking)
        {
            int* windowSize = RenderWindow->GetSize();
            double windowDiagonal = std::sqrt(static_cast<double>(windowSize[0]) * windowSize[0] +
                                              static_cast<double>(windowSize[1]) * windowSize[1]);
            vtkNew<vtkCellPicker> cellPicker;
            cellPicker->SetTolerance(windowDiagonal > 0 ? selectionDistance / windowDiagonal : 0);
            for (const auto& entry : Parts)
            {
                if (entry.second->GetCellLocator() != nullptr) cellPicker->AddLocator(entry.second->GetCellLocator());
            }
            cellPicker->Pick(x, y, 0, Renderer);
            part = FindPart(cellPicker->GetActor(), &name);
            if (part != nullptr)
            {
                cellPicker->GetPickPosition(p);
                cellId = GetGlobalCellIdClosestTo3DPoint(p, part, selectableParts, maxDistance2);
                hit = cellId != -1;
            }
        }
        // The colors, the opacity and the visibility come back
        for (const auto& entry : Parts)
        {
            entry.second->UpdateColor();
            entry.second->ApplyEdgesVisibility(EdgesVisibility);
        }
        if (!hit)
        {
            cellId = -1;
            return false;
        }
        std::copy(p, p + 3, point);
        partName = name;
        return true;
    }

    // The frustum part of PickByArea: the six planes as origin and normal, and the parts whose geometry was
    // rendered inside the box, each once
    std::vector<std::string> View::PickArea(int x1, int y1, int x2, int y2, double planes36[36])
    {
        vtkNew<vtkRenderedAreaPicker> areaPicker;
        areaPicker->AreaPick(x1, y1, x2, y2, Renderer);
        vtkPlanes* frustum = areaPicker->GetFrustum();
        Require(frustum != nullptr && frustum->GetNumberOfPlanes() == 6, "The area pick returned no frustum.");
        for (int i = 0; i < 6; i++)
        {
            vtkPlane* plane = frustum->GetPlane(i);
            plane->GetOrigin(planes36 + 6 * i);
            plane->GetNormal(planes36 + 6 * i + 3);
        }
        std::vector<std::string> names;
        vtkProp3DCollection* props = areaPicker->GetProp3Ds();
        props->InitTraversal();
        for (vtkProp3D* prop = props->GetNextProp3D(); prop != nullptr; prop = props->GetNextProp3D())
        {
            std::string name;
            if (FindPart(prop, &name) == nullptr) continue;
            if (std::find(names.begin(), names.end(), name) == names.end()) names.push_back(name);
        }
        return names;
    }
}
