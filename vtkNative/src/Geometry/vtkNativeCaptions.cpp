// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Captions, labels and the in-place updates of overlay annotations. Port of AddCaptionActor, AddLabelActor (both
// overloads), AddActorCaption, SetAllCaptionPositions, ApplySelectionFormattingToCaptionActor,
// UpdateOverlayCaptionActorPosition and the point updates of UpdateMeasurementAnnotation and UpdateOverlayPointsActor.
#include "vtkNativeError.h"
#include "vtkNativeView.h"

#include <vtkCommand.h>
#include <vtkCoordinate.h>
#include <vtkLabeledDataMapper.h>
#include <vtkMath.h>
#include <vtkNew.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkPolyData.h>
#include <vtkStringArray.h>
#include <vtkTextMapper.h>
#include <vtkTextProperty.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    namespace
    {
        // The text of a caption from its world point, in pixels
        const double CaptionPaddingX = 3;
        const double CaptionPaddingY = 3;

        // CreateNewTextProperty of vtkControl: the line offset follows the default size, not the scaled one
        void FormatText(vtkTextProperty* property, int fontSize)
        {
            Require(fontSize > 0, "The font size must be positive.");
            property->SetColor(0, 0, 0);
            property->SetFontFamilyToArial();
            property->SetFontSize(fontSize);
            property->SetLineOffset(-std::round(16 / 7.0));
            property->SetLineSpacing(1.5);
        }
    }

    vtkRenderer* View::GetCaptionRenderer(vtkn_layer layer)
    {
        if (layer == VTKN_LAYER_OVERLAY) return OverlayRenderer;
        if (layer == VTKN_LAYER_SELECTION) return SelectionRenderer;
        return Renderer;
    }

    // AddActorCaption: a base or overlay caption replaces the one of its name, a selection caption is added to the
    // others and formatted with the highlight color. The offsets of the captions follow the camera at every render
    void View::AddCaptionEntry(CaptionEntry entry)
    {
        if (entry.Layer != VTKN_LAYER_SELECTION)
        {
            std::set<std::string> names = { entry.Name };
            RemoveCaptions(entry.Layer, &names);
        }
        entry.Actor->UseBoundsOff();
        GetCaptionRenderer(entry.Layer)->AddViewProp(entry.Actor);
        if (entry.Layer == VTKN_LAYER_SELECTION) ApplyCaptionSelectionFormatting(entry);
        Captions.push_back(entry);
        if (CaptionCommand == nullptr)
        {
            CaptionCommand = vtkSmartPointer<vtkCallbackCommand>::New();
            CaptionCommand->SetClientData(this);
            CaptionCommand->SetCallback([](vtkObject*, unsigned long, void* clientData, void*) {
                static_cast<View*>(clientData)->UpdateCaptionPositions();
            });
            Renderer->AddObserver(vtkCommand::StartEvent, CaptionCommand);
        }
        UpdateCaptionPositions();
    }

    // vtkCaptionActor2D without leader and border, as vtkControl sets it up, is its text at the padding of the
    // caption box above its world point. VTK 9 draws that text as a texture, which places it a pixel higher than
    // VTK 5.8 did, so the text is drawn by a text mapper as the labels are
    void View::AddCaption(const std::string& name, const std::string& caption, const double position[3],
                          const double* offset, int fontSize, vtkn_layer layer, bool useSecondaryHighlightColor)
    {
        vtkNew<vtkTextMapper> textMapper;
        FormatText(textMapper->GetTextProperty(), fontSize);
        textMapper->SetInput(caption.c_str());
        vtkNew<vtkCoordinate> anchor;
        anchor->SetCoordinateSystemToWorld();
        anchor->SetValue(position[0], position[1], position[2]);
        vtkNew<vtkActor2D> captionActor;
        captionActor->SetMapper(textMapper);
        captionActor->PickableOff();
        captionActor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
        captionActor->GetPositionCoordinate()->SetReferenceCoordinate(anchor);
        captionActor->GetPositionCoordinate()->SetValue(CaptionPaddingX, CaptionPaddingY);
        //
        CaptionEntry entry = {};
        entry.Name = name;
        entry.Actor = captionActor;
        entry.Anchor = anchor;
        entry.TextMapper = textMapper;
        entry.Layer = layer;
        std::copy(position, position + 3, entry.Position);
        entry.HasOffset = offset != nullptr;
        if (offset != nullptr) std::copy(offset, offset + 3, entry.Offset);
        entry.UseSecondaryHighlightColor = useSecondaryHighlightColor;
        AddCaptionEntry(entry);
    }

    // Both overloads of AddLabelActor: the integer labels are formatted as "%d" by the caller
    void View::AddLabels(const std::string& name, const double* positions, int count,
                         const std::vector<std::string>& labels, const double color[3], int fontSize, vtkn_layer layer)
    {
        Require(count > 0 && positions != nullptr && static_cast<int>(labels.size()) == count,
                "The label positions and values must have the same non-zero length.");
        vtkNew<vtkPoints> points;
        vtkNew<vtkStringArray> labelData;
        labelData->SetName("Labels");
        labelData->SetNumberOfComponents(1);
        for (int i = 0; i < count; i++)
        {
            points->InsertNextPoint(positions + 3 * i);
            labelData->InsertNextValue(labels[i]);
        }
        vtkNew<vtkPolyData> polyData;
        polyData->SetPoints(points);
        polyData->GetPointData()->AddArray(labelData);
        //
        vtkNew<vtkTextProperty> textProperty;
        FormatText(textProperty, fontSize);
        textProperty->SetColor(color[0], color[1], color[2]);
        vtkNew<vtkLabeledDataMapper> mapper;
        mapper->SetInputData(polyData);
        mapper->SetLabelModeToLabelFieldData();
        mapper->SetFieldDataName("Labels");
        mapper->SetLabelFormat("%s");
        mapper->SetLabelTextProperty(textProperty);
        mapper->CoordinateSystemWorld();
        vtkNew<vtkActor2D> labelActor;
        labelActor->SetMapper(mapper);
        labelActor->PickableOff();
        //
        CaptionEntry entry = {};
        entry.Name = name;
        entry.Actor = labelActor;
        entry.Layer = layer;
        std::copy(positions, positions + 3, entry.Position);
        AddCaptionEntry(entry);
    }

    void View::RemoveCaptions(vtkn_layer layer, const std::set<std::string>* names)
    {
        auto removed = std::remove_if(Captions.begin(), Captions.end(), [&](const CaptionEntry& entry) {
            return entry.Layer == layer && (names == nullptr || names->count(entry.Name) > 0);
        });
        for (auto entry = removed; entry != Captions.end(); ++entry)
            GetCaptionRenderer(entry->Layer)->RemoveViewProp(entry->Actor);
        Captions.erase(removed, Captions.end());
    }

    void View::ApplyCaptionSelectionFormatting(CaptionEntry& entry)
    {
        if (entry.TextMapper == nullptr) return;
        const double* color = entry.UseSecondaryHighlightColor ? Highlight.Secondary : Highlight.Primary;
        entry.TextMapper->GetTextProperty()->SetColor(color[0], color[1], color[2]);
    }

    // SetAllCaptionPositions: an offset is in pixels, turned into world units at the focal plane
    void View::UpdateCaptionPositions()
    {
        bool anyOffset = false;
        for (const CaptionEntry& entry : Captions) anyOffset |= entry.HasOffset;
        if (!anyOffset) return;
        const double display1[2] = { 0, 0 };
        const double display2[2] = { 1, 0 };
        double world1[3];
        double world2[3];
        DisplayToWorld(display1, world1);
        DisplayToWorld(display2, world2);
        double size = std::sqrt(vtkMath::Distance2BetweenPoints(world1, world2));
        for (const CaptionEntry& entry : Captions)
        {
            if (!entry.HasOffset || entry.Anchor == nullptr) continue;
            entry.Anchor->SetValue(entry.Position[0] + size * entry.Offset[0], entry.Position[1] + size * entry.Offset[1],
                                   entry.Position[2] + size * entry.Offset[2]);
        }
    }

    // UpdateOverlayCaptionActorPosition
    void View::SetCaptionPosition(const std::string& name, const double position[3])
    {
        for (CaptionEntry& entry : Captions)
        {
            if (entry.Layer != VTKN_LAYER_OVERLAY || entry.Name != name || entry.Anchor == nullptr) continue;
            std::copy(position, position + 3, entry.Position);
            entry.Anchor->SetValue(position[0], position[1], position[2]);
        }
    }

    bool View::SetCaptionVisible(const std::string& name, vtkn_layer layer, bool visible)
    {
        bool found = false;
        for (CaptionEntry& entry : Captions)
        {
            if (entry.Layer != layer || entry.Name != name) continue;
            entry.Actor->SetVisibility(visible);
            found = true;
        }
        return found;
    }

    // UpdateMeasurementActor and the arrows of UpdateMeasurementAnnotation: the points move, the cells stay
    void View::SetActorPoints(const std::string& name, vtkn_layer layer, const double* coordinates, int count,
                              const double* normals)
    {
        PartActor* actor = FindActor(name, layer);
        if (actor == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "There is no actor named " + name + ".");
        actor->SetPoints(coordinates, count, normals);
    }
}
