// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The transformations and the exploded view of a view: symmetries and patterns add transformed copies of the base
// actors, and an exploded view moves parts by offsets. Port of the regions Transformations and Exploded view of
// vtkControl.
#include "vtkNativeError.h"
#include "vtkNativeView.h"

#include <vtkActor.h>

namespace vtkn
{
    // AddSymmetry: a mirror about the plane through the point, applied once
    void View::AddSymmetry(int symmetryPlane, const double point[3])
    {
        Require(symmetryPlane >= 0 && symmetryPlane <= 2, "The symmetry plane must be 0, 1 or 2.");
        vtkSmartPointer<vtkTransform> transform = vtkSmartPointer<vtkTransform>::New();
        transform->Translate(point[0], point[1], point[2]);
        transform->Scale(symmetryPlane == 0 ? -1.0 : 1.0, symmetryPlane == 1 ? -1.0 : 1.0,
                         symmetryPlane == 2 ? -1.0 : 1.0);
        transform->Translate(-point[0], -point[1], -point[2]);
        Transforms.push_back({ transform, 2 });
    }

    void View::AddLinearPattern(const double displacement[3], int numOfItems)
    {
        vtkSmartPointer<vtkTransform> transform = vtkSmartPointer<vtkTransform>::New();
        transform->Translate(displacement[0], displacement[1], displacement[2]);
        Transforms.push_back({ transform, numOfItems });
    }

    void View::AddCircularPattern(const double axisPoint[3], const double axisNormal[3], double angle, int numOfItems)
    {
        vtkSmartPointer<vtkTransform> transform = vtkSmartPointer<vtkTransform>::New();
        transform->Translate(axisPoint[0], axisPoint[1], axisPoint[2]);
        transform->RotateWXYZ(angle, axisNormal[0], axisNormal[1], axisNormal[2]);
        transform->Translate(-axisPoint[0], -axisPoint[1], -axisPoint[2]);
        Transforms.push_back({ transform, numOfItems });
    }

    // Port of ApplyTransforms and TransformAllMaxActors: every base actor, the copies of an earlier transformation
    // included, gets numOfItems - 1 copies, each transformed from the one before. A copy is named after the actor
    // it starts from, the transformation and its place in the row (<name>:t<transformation>-<item>), which vtkControl
    // names its own copies by, so that it can send them their directional result values
    void View::ApplyTransforms()
    {
        for (size_t transformIndex = 0; transformIndex < Transforms.size(); transformIndex++)
        {
            const auto& entry = Transforms[transformIndex];
            std::vector<std::pair<std::string, PartActor*>> actors;
            for (const auto& part : Parts) actors.push_back({ part.first, part.second.get() });
            for (const auto& actor : BaseActors) actors.push_back({ actor.first, actor.second.get() });
            for (const auto& actor : actors)
            {
                std::string baseName = actor.first;
                PartActor* baseActor = actor.second;
                for (int i = 0; i < entry.second - 1; i++)
                {
                    std::unique_ptr<PartActor> copy(new PartActor(*baseActor, entry.first));
                    std::string name = actor.first + ":t" + std::to_string(transformIndex) + "-" + std::to_string(i + 1);
                    PartActor* added = copy.get();
                    AddActor(name, std::move(copy));
                    TransformedCopies[baseName].push_back(name);
                    auto source = TransformedCopySources.find(actor.first);
                    TransformedCopySources[name] = source != TransformedCopySources.end() ? source->second : actor.first;
                    baseName = name;
                    baseActor = added;
                }
            }
        }
    }

    void View::RemoveTransformedActors()
    {
        for (const auto& entry : TransformedCopies)
        {
            for (const std::string& name : entry.second)
            {
                PartActor* copy = FindActor(name, VTKN_LAYER_BASE);
                if (copy == nullptr) continue;
                copy->RemoveFrom(Renderer);
                Parts.erase(name);
                BaseActors.erase(name);
            }
        }
        TransformedCopies.clear();
        TransformedCopySources.clear();
    }

    void View::ClearTransforms()
    {
        Transforms.clear();
        TransformedCopies.clear();
        TransformedCopySources.clear();
    }

    // HideTransformedActors and ShowTransformedActors: the copies take the visibility of their base actor
    void View::ShowTransformedActors(bool show)
    {
        for (const auto& entry : TransformedCopies)
        {
            PartActor* baseActor = FindActor(entry.first, VTKN_LAYER_BASE);
            for (const std::string& name : entry.second)
            {
                PartActor* copy = FindActor(name, VTKN_LAYER_BASE);
                if (copy != nullptr) copy->SetVisible(show && baseActor != nullptr && baseActor->IsVisible());
            }
        }
        ApplyEdgesVisibilityAndBackfaceCulling();
    }

    // PreviewExplodedView at its end and RemovePreviewedExplodedView: a part and the actors whose parent it is are
    // moved by its offset
    void View::SetExplodedViewOffsets(const std::map<std::string, std::array<double, 3>>& offsets)
    {
        for (const auto& entry : Parts)
        {
            auto offset = offsets.find(entry.first);
            if (offset == offsets.end()) offset = offsets.find(entry.second->GetParentName());
            if (offset != offsets.end()) entry.second->SetPosition(offset->second.data());
        }
        for (const auto& entry : BaseActors)
        {
            auto offset = offsets.find(entry.first);
            if (offset == offsets.end()) offset = offsets.find(entry.second->GetParentName());
            if (offset != offsets.end()) entry.second->SetPosition(offset->second.data());
        }
        AdjustCameraDistanceAndClipping();
    }
}
