// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Picking, the screen to model step of the Selection region of vtkControl: a point pick names a part and gives a
// point on its surface, an area pick gives the frustum and the parts inside it. Which nodes, elements or surfaces
// those mean is decided by the caller, as the Controller decides it from what vtkControl reported. A pick on a
// section view is resolved to the part it cuts.
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
    // The part a prop belongs to; a transformed copy is reported by the part it was made from
    PartActor* View::FindPart(vtkProp* prop, std::string* name)
    {
        for (const auto& entry : Parts)
        {
            if (!entry.second->Owns(prop)) continue;
            if (name != nullptr)
            {
                auto source = TransformedCopySources.find(entry.first);
                *name = source != TransformedCopySources.end() ? source->second : entry.first;
            }
            return entry.second.get();
        }
        return nullptr;
    }

    // The section actors and skins of the section view that stand for a part, by the part name
    std::vector<std::pair<std::string, PartActor*>> View::GetSectionViewActors()
    {
        std::vector<std::pair<std::string, PartActor*>> actors;
        if (!SectionViewActive) return actors;
        for (const auto* names : { &SectionViewActorNames, &SectionViewSkinActorNames })
        {
            for (const auto& entry : *names)
            {
                if (Parts.count(entry.first) == 0) continue;
                auto actor = BaseActors.find(entry.second);
                if (actor != BaseActors.end()) actors.push_back({ entry.first, actor->second.get() });
            }
        }
        return actors;
    }

    PartActor* View::FindSectionViewActor(vtkProp* prop, std::string* partName, bool* skin)
    {
        if (prop == nullptr || !SectionViewActive) return nullptr;
        for (const auto* names : { &SectionViewActorNames, &SectionViewSkinActorNames })
        {
            for (const auto& entry : *names)
            {
                auto actor = BaseActors.find(entry.second);
                if (actor == BaseActors.end() || !actor->second->Owns(prop) || Parts.count(entry.first) == 0) continue;
                if (partName != nullptr) *partName = entry.first;
                if (skin != nullptr) *skin = names == &SectionViewSkinActorNames;
                return actor->second.get();
            }
        }
        return nullptr;
    }

    // Port of ResolveSectionViewPick: the skin is the part surface itself and the exposed element faces carry the
    // element ids, so nodes, elements and parts are picked on them; a planar cut face only resolves to its part, by
    // the closest point on the part surface
    bool View::ResolveSectionViewPick(PartActor* sectionViewActor, const std::string& partName, bool skin,
                                      const std::set<std::string>* selectableParts, double maxDistance2,
                                      double point[3], int& cellId)
    {
        PartActor* part = Parts[partName].get();
        bool facesWithIds = skin || sectionViewActor->GetCellLocator() != nullptr;
        if (facesWithIds && (skin || SectionViewPartSelection || SectionViewIdSelection))
        {
            cellId = GetGlobalCellIdClosestTo3DPoint(point, part, selectableParts, maxDistance2);
            return cellId != -1;
        }
        if (SectionViewPartSelection && part->GetCellLocator() != nullptr)
        {
            double closestPoint[3];
            vtkIdType localCellId = -1;
            int subId = -1;
            double distance2 = -1;
            part->GetCellLocator()->FindClosestPoint(point, closestPoint, localCellId, subId, distance2);
            if (distance2 >= 0)
            {
                std::copy(closestPoint, closestPoint + 3, point);
                cellId = static_cast<int>(part->GetCellLocator()->GetDataSet()->GetCellData()->GetGlobalIds()
                                              ->GetTuple1(localCellId));
                return true;
            }
        }
        return false;
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
            std::vector<std::pair<std::string, PartActor*>> candidates;
            for (const auto& entry : Parts) candidates.push_back({ entry.first, entry.second.get() });
            if (pass == 1)
            {
                for (const auto& entry : GetSectionViewActors()) candidates.push_back(entry);
            }
            for (const auto& entry : candidates)
            {
                PartActor* part = entry.second;
                if (pass == 0 && part != onlyPart) continue;
                bool shown = part->GetGeometry()->GetVisibility() != 0 && !part->GetGeometryHiddenBySectionView();
                bool visible = selectableParts != nullptr
                                   ? selectableParts->count(entry.first) > 0 && !part->GetGeometryHiddenBySectionView()
                                   : shown;
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
                entry.second->GetGeometry()->SetVisibility(selectableParts->count(entry.first) > 0 &&
                                                           !entry.second->GetGeometryHiddenBySectionView());
            entry.second->SetPickOpacity();
        }
        std::vector<std::pair<std::string, PartActor*>> sectionViewActors = GetSectionViewActors();
        for (const auto& entry : sectionViewActors)
        {
            entry.second->GetGeometry()->PickableOn();
            entry.second->SetPickOpacity();
        }
        double maxDistance2 = limitDistance ? std::pow(GetSelectionPrecision(selectionDistance), 2) : DBL_MAX;
        bool hit = false;
        double p[3] = { 0, 0, 0 };
        std::string name;
        // First pick. vtkControl rendered its renderer only; the render of the window is not shown, or the parts drawn
        // opaque for the pick would flash in a window with translucent parts
        vtkNew<vtkPropPicker> propPicker;
        propPicker->PickFromListOn();
        for (const auto& entry : Parts)
        {
            if (entry.second->GetCellLocator() != nullptr) propPicker->AddPickList(entry.second->GetGeometry());
        }
        for (const auto& entry : sectionViewActors) propPicker->AddPickList(entry.second->GetGeometry());
        PickWithoutAmbientOcclusionPass([&]() {
            int swapBuffers = RenderWindow->GetSwapBuffers();
            RenderWindow->SwapBuffersOff();
            RenderWindow->Render();
            RenderWindow->SetSwapBuffers(swapBuffers);
            propPicker->Pick(x, y, 0, Renderer);
        });
        PartActor* part = FindPart(propPicker->GetActor(), &name);
        bool skin = false;
        PartActor* sectionViewActor = FindSectionViewActor(propPicker->GetActor(), &name, &skin);
        bool sectionViewPick = sectionViewActor != nullptr;
        if (sectionViewPick)
        {
            Renderer->SetDisplayPoint(x, y, Renderer->GetPickedZ());
            Renderer->DisplayToWorld();
            double* world = Renderer->GetWorldPoint();
            for (int i = 0; i < 3; i++) p[i] = world[i] / world[3];
            if (ExactPicking && sectionViewActor->GetCellLocator() != nullptr)
            {
                vtkNew<vtkCellPicker> surfacePicker;
                surfacePicker->SetTolerance(0);
                surfacePicker->PickFromListOn();
                surfacePicker->AddPickList(sectionViewActor->GetGeometry());
                surfacePicker->AddLocator(sectionViewActor->GetCellLocator());
                if (surfacePicker->Pick(x, y, 0, Renderer) != 0) surfacePicker->GetPickPosition(p);
            }
            hit = ResolveSectionViewPick(sectionViewActor, name, skin, selectableParts, maxDistance2, p, cellId);
        }
        else if (part != nullptr)
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
        if (!hit && !sectionViewPick && ExactPicking)
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
            for (const auto& entry : sectionViewActors)
            {
                if (entry.second->GetCellLocator() != nullptr) cellPicker->AddLocator(entry.second->GetCellLocator());
            }
            cellPicker->Pick(x, y, 0, Renderer);
            part = FindPart(cellPicker->GetActor(), &name);
            sectionViewActor = FindSectionViewActor(cellPicker->GetActor(), &name, &skin);
            if (sectionViewActor != nullptr)
            {
                cellPicker->GetPickPosition(p);
                hit = ResolveSectionViewPick(sectionViewActor, name, skin, selectableParts, maxDistance2, p, cellId);
            }
            else if (part != nullptr)
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
            entry.second->ApplyEdgesVisibility(EdgesVisibility, VTKN_LAYER_BASE, SectionViewActive);
        }
        for (const auto& entry : sectionViewActors)
        {
            entry.second->GetGeometry()->PickableOff();
            entry.second->UpdateColor();
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
        // The section actors and the transformed copies are picked by an area, as the parts they stand for
        std::vector<vtkActor*> extraActors;
        for (const auto& entry : GetSectionViewActors()) extraActors.push_back(entry.second->GetGeometry());
        for (const auto& entry : TransformedCopySources)
        {
            auto copy = Parts.find(entry.first);
            if (copy != Parts.end() && Parts[entry.second]->GetGeometry()->GetPickable())
                extraActors.push_back(copy->second->GetGeometry());
        }
        for (vtkActor* actor : extraActors) actor->PickableOn();
        vtkNew<vtkRenderedAreaPicker> areaPicker;
        PickWithoutAmbientOcclusionPass([&]() { areaPicker->AreaPick(x1, y1, x2, y2, Renderer); });
        for (vtkActor* actor : extraActors) actor->PickableOff();
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
            if (FindPart(prop, &name) == nullptr && FindSectionViewActor(prop, &name, nullptr) == nullptr) continue;
            if (std::find(names.begin(), names.end(), name) == names.end()) names.push_back(name);
        }
        // VTK 5.8 picked an area with the OpenGL selection mode, which reports what is drawn in the area also where
        // it is hidden behind other geometry; VTK 9 reports the visible only. A shown part with a point in the
        // frustum (on the kept side of a section plane) is reported too
        for (const auto& entry : Parts)
        {
            PartActor* part = entry.second.get();
            std::string name = entry.first;
            auto source = TransformedCopySources.find(name);
            if (source != TransformedCopySources.end()) name = source->second;
            if (std::find(names.begin(), names.end(), name) != names.end()) continue;
            if (!part->GetGeometry()->GetVisibility() || part->GetGeometryHiddenBySectionView() ||
                (source == TransformedCopySources.end() && part->GetCellLocator() == nullptr)) continue;
            vtkPolyData* polyData = part->GetPolyData();
            if (polyData == nullptr) continue;
            double position[3];
            part->GetGeometry()->GetPosition(position);
            bool inside = false;
            for (vtkIdType i = 0; !inside && i < polyData->GetNumberOfPoints(); i++)
            {
                double point[3];
                polyData->GetPoint(i, point);
                for (int j = 0; j < 3; j++) point[j] += position[j];
                if (SectionPlane != nullptr && SectionPlane->EvaluateFunction(point) < 0) continue;
                inside = frustum->EvaluateFunction(point) <= 0;
            }
            if (inside) names.push_back(name);
        }
        return names;
    }
}
