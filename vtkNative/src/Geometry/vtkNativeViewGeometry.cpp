// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The geometry operations of a view: the actors of the three 3D layers and what vtkControl does with them. Port of
// the regions Add geometry (AddCells, AddPoints, AddActor*), Highlight geometry, Hide/Show geometry and Clear, and of
// the edges visibility of all layers.
#include "vtkNativeColorSpectrum.h"
#include "vtkNativeError.h"
#include "vtkNativeSymbols.h"
#include "vtkNativeView.h"

#include <vtkCamera.h>
#include <vtkCellArray.h>
#include <vtkDoubleArray.h>
#include <vtkFloatArray.h>
#include <vtkGlyph3D.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkNew.h>
#include <vtkObjectFactory.h>
#include <vtkPolyDataSilhouette.h>
#include <vtkProperty.h>
#include <vtkWeakPointer.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    namespace
    {
        // Glyphs that keep their size on the screen. vtkDistanceToCamera gives every point the scale of that size,
        // but it is modified by every move of the camera, so the glyphs and the silhouette edges made of them were
        // built again for every frame of a turning view. In the parallel projection of the views that scale is the
        // same for every point and changes only with the zoom and the height of the view, so it is the scale factor
        // of the glyphs here, checked whenever the pipeline asks for the modification time
        class ScreenSizeGlyph : public vtkGlyph3D
        {
        public:
            static ScreenSizeGlyph* New();
            vtkTypeMacro(ScreenSizeGlyph, vtkGlyph3D);

            void SetScreenSize(vtkRenderer* renderer, double screenSize, double sourceScaleFactor)
            {
                Renderer = renderer;
                ScreenSize = screenSize;
                SourceScaleFactor = sourceScaleFactor;
                this->SetScaleModeToDataScalingOff();
                this->ScalingOn();
            }

            vtkMTimeType GetMTime() override
            {
                if (Renderer != nullptr)
                {
                    // The scale of vtkDistanceToCamera in a parallel projection
                    double size = 1;
                    int height = Renderer->GetSize()[1];
                    if (height > 0) size = 2.0 * (Renderer->GetActiveCamera()->GetParallelScale() / height) * ScreenSize;
                    double scaleFactor = SourceScaleFactor * size;
                    if (scaleFactor != this->ScaleFactor) this->SetScaleFactor(scaleFactor);
                }
                return this->Superclass::GetMTime();
            }

        protected:
            ScreenSizeGlyph() = default;
            ~ScreenSizeGlyph() override = default;

        private:
            vtkWeakPointer<vtkRenderer> Renderer;
            double ScreenSize = 1;
            double SourceScaleFactor = 1;
        };

        vtkStandardNewMacro(ScreenSizeGlyph);
    }

    // vtkControl draws the selection layer between the overlay and the widgets: four layers, widgets on top
    void View::InitializeGeometry()
    {
        SelectionRenderer = vtkSmartPointer<vtkRenderer>::New();
        SelectionRenderer->SetLayer(2);
        SelectionRenderer->InteractiveOff();
        SelectionRenderer->SetActiveCamera(Renderer->GetActiveCamera());
        SelectionRenderer->UseOITOff();
        SelectionRenderer->UseDepthPeelingOff();
        for (const auto& light : Lights) SelectionRenderer->AddLight(light);
        WidgetRenderer->SetLayer(3);
        RenderWindow->SetNumberOfLayers(4);
        RenderWindow->AddRenderer(SelectionRenderer);
        CoorSysSymbols.reset(new CoorSysSymbolSet());
        CoorSysSymbols->SetRenderer(OverlayRenderer);
        CoorSysSelectionSymbols.reset(new CoorSysSymbolSet());
        CoorSysSelectionSymbols->SetOverrideColor(Highlight.Primary);
        CoorSysSelectionSymbols->SetRenderer(SelectionRenderer);
        MaterialAxesSelectionSymbols.reset(new CoorSysSymbolSet());
        MaterialAxesSelectionSymbols->SetRenderer(SelectionRenderer);
    }

    // AddCoordinateSystemSymbol and AddMaterialAxesSymbol
    void View::AddCoordinateSystemSymbol(const double origin[3], const double axisDirections[3][3],
                                         const std::array<std::string, 3>& labels, double symbolSize, vtkn_layer layer,
                                         bool materialAxes)
    {
        CoorSysSymbolSet* symbols = materialAxes ? MaterialAxesSelectionSymbols.get()
                                                 : layer == VTKN_LAYER_SELECTION ? CoorSysSelectionSymbols.get()
                                                                                 : CoorSysSymbols.get();
        symbols->AddSymbol(origin, axisDirections, labels, symbolSize);
    }

    vtkRenderer* View::GetLayerRenderer(const PartActor& actor)
    {
        vtkn_layer layer = static_cast<vtkn_layer>(actor.GetStyle().layer);
        if (layer == VTKN_LAYER_OVERLAY) return OverlayRenderer;
        if (layer == VTKN_LAYER_SELECTION && !actor.GetStyle().draw_on_geometry) return SelectionRenderer;
        return Renderer;
    }

    void View::AddActor(const std::string& name, const std::string& parentName, const vtkn_part_style& style,
                        const vtkn_mesh& geometry, const vtkn_mesh* modelEdges, const vtkn_mesh* modelEdgesToDraw)
    {
        Require(style.layer >= VTKN_LAYER_BASE && style.layer <= VTKN_LAYER_OVERLAY,
                "The layer of the style is not one of vtkn_layer.");
        std::unique_ptr<PartActor> actor(new PartActor(style, geometry, modelEdges, modelEdgesToDraw));
        actor->SetParentName(parentName);
        // The surfaces drawn on the parts, such as those of the constraints and the loads, come in thousands. A
        // highlighted surface that is drawn among them is drawn the way they are, since it lies on one of them
        if (!actor->IsAPart() && style.layer != VTKN_LAYER_OVERLAY && GetLayerRenderer(*actor) == Renderer)
        {
            for (vtkActor* drawn : { actor->GetGeometry(), actor->GetElementEdges(), actor->GetModelEdges() })
                ActorBatches::Allow(drawn);
        }
        AddActor(name, std::move(actor));
    }

    // Port of AddActor, AddActorGeometry and AddActorEdges: the actor goes to the renderer of its layer, the
    // selection layer formats it with the highlight colors, the edges mode formats it, and the camera distance and
    // clipping range follow the new bounds
    void View::AddActor(const std::string& name, std::unique_ptr<PartActor> actor)
    {
        Require(!name.empty(), "The actor name must not be empty.");
        PartActor* added = actor.get();
        vtkn_layer layer = actor->GetLayer();
        if (layer == VTKN_LAYER_BASE)
        {
            RemoveActor(name);
            auto entry = BaseActors.find(name);
            if (entry != BaseActors.end())
            {
                entry->second->RemoveFrom(Renderer);
                BaseActors.erase(entry);
            }
            actor->AddTo(Renderer);
            if (actor->IsAPart()) Parts[name] = std::move(actor);
            else BaseActors[name] = std::move(actor);
        }
        else if (layer == VTKN_LAYER_OVERLAY)
        {
            auto entry = OverlayParts.find(name);
            if (entry != OverlayParts.end()) entry->second->RemoveFrom(OverlayRenderer);
            actor->AddTo(OverlayRenderer);
            OverlayParts[name] = std::move(actor);
        }
        else
        {
            actor->AddTo(GetLayerRenderer(*actor));
            actor->GetGeometry()->PickableOff();
            actor->ApplySelectionFormatting(Highlight);
            SelectedParts.push_back(std::move(actor));
        }
        if (added->IsAPart()) ApplyEdgesVisibilityAndBackfaceCulling();
        else ApplyEdgesVisibility(*added, layer, PartsDrawElementEdges());
        AdjustCameraAfterAdd();
    }

    // A surface drawn on the parts, such as a surface of a load, kept its own mesh lines in VTK 5.8, where they were
    // half a pixel wide and were drawn over the lines of the part beneath. The one pixel lines of both came out
    // twice as dark and hard there; the lines of the parts alone are drawn over every face, unless the exploded view
    // moved the parts away from the surface
    void View::ApplyEdgesVisibility(PartActor& actor, vtkn_layer layer, bool partsDrawElementEdges)
    {
        actor.SetElementEdgesDrawnByParts(partsDrawElementEdges && layer != VTKN_LAYER_OVERLAY && !actor.IsAPart() &&
                                          actor.GetStyle().draw_on_geometry != 0);
        actor.ApplyEdgesVisibility(EdgesVisibility, layer, SectionViewActive);
    }

    bool View::PartsDrawElementEdges()
    {
        bool drawn = false;
        for (const auto& entry : Parts)
        {
            const double* position = entry.second->GetGeometry()->GetPosition();
            if (position[0] != 0 || position[1] != 0 || position[2] != 0) return false;
            const vtkn_part_style& style = entry.second->GetStyle();
            if (style.visible != 0 && style.can_have_element_edges != 0) drawn = true;
        }
        return drawn;
    }

    std::vector<PartActor*> View::GetBaseActors()
    {
        std::vector<PartActor*> actors;
        for (const auto& entry : Parts) actors.push_back(entry.second.get());
        for (const auto& entry : BaseActors) actors.push_back(entry.second.get());
        return actors;
    }

    PartActor* View::FindActor(const std::string& name, vtkn_layer layer)
    {
        if (layer == VTKN_LAYER_BASE)
        {
            auto entry = Parts.find(name);
            if (entry != Parts.end()) return entry->second.get();
            entry = BaseActors.find(name);
            return entry != BaseActors.end() ? entry->second.get() : nullptr;
        }
        if (layer == VTKN_LAYER_OVERLAY)
        {
            auto entry = OverlayParts.find(name);
            return entry != OverlayParts.end() ? entry->second.get() : nullptr;
        }
        return nullptr;
    }

    void View::SetActorVisible(const std::string& name, vtkn_layer layer, bool visible)
    {
        PartActor* actor = FindActor(name, layer);
        if (actor == nullptr && SetCaptionVisible(name, layer, visible)) return;
        if (actor == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "There is no actor named " + name + ".");
        actor->SetVisible(visible);
    }

    namespace
    {
        template <typename T>
        bool RenameKey(std::map<std::string, T>& map, const std::string& oldName, const std::string& newName)
        {
            auto entry = map.find(oldName);
            if (entry == map.end()) return false;
            T value = std::move(entry->second);
            map.erase(entry);
            map[newName] = std::move(value);
            return true;
        }
    }

    // The renaming part of UpdateActor: every table that knows the actor by its name
    void View::RenameActor(const std::string& oldName, const std::string& newName)
    {
        if (oldName == newName) return;
        if (!RenameKey(Parts, oldName, newName) && !RenameKey(BaseActors, oldName, newName) &&
            !RenameKey(OverlayParts, oldName, newName)) return;
        for (auto* actors : { &Parts, &BaseActors, &OverlayParts })
            for (auto& entry : *actors)
                if (entry.second->GetParentName() == oldName) entry.second->SetParentName(newName);
        RenameKey(SectionViewActorNames, oldName, newName);
        RenameKey(SectionViewSkinActorNames, oldName, newName);
        RenameKey(TransformedCopies, oldName, newName);
        for (auto& entry : TransformedCopySources)
            if (entry.second == oldName) entry.second = newName;
        RenameKey(VolumeGrids, oldName, newName);
        RenameKey(Actors, oldName, newName);
    }

    // Port of UpdateActorColor: the copies and the section view were made with the color of the actor
    void View::SetActorColor(const std::string& name, vtkn_layer layer, const double* rgba)
    {
        PartActor* actor = FindActor(name, layer);
        if (actor == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "There is no actor named " + name + ".");
        actor->SetColor(rgba);
        if (layer != VTKN_LAYER_BASE) return;
        for (const auto& entry : TransformedCopySources)
        {
            if (entry.second != name) continue;
            PartActor* copy = FindActor(entry.first, VTKN_LAYER_BASE);
            if (copy != nullptr) copy->SetColor(rgba);
        }
        if (SectionViewActive) CreateSectionView(SectionView);
    }

    // Port of HighlightActor, GetHighlightActorFromActorName and GetCopyOfModelEdgesActor: the model edges the part
    // owns, or when it has none its silhouette and free edges seen from the camera, drawn in the selection layer by
    // an actor with the defaults of a new vtkMaxActor
    void View::HighlightActor(const std::string& name)
    {
        PartActor* part = FindActor(name, VTKN_LAYER_BASE);
        if (part == nullptr || part->GetModelEdges() == nullptr) return;
        vtkNew<vtkPolyData> modelEdges;
        modelEdges->DeepCopy(part->GetModelEdgesForHighlight());
        vtkn_part_style style = DefaultStyle();
        style.layer = VTKN_LAYER_SELECTION;
        style.color[3] = 1;
        std::unique_ptr<PartActor> highlight;
        if (modelEdges->GetNumberOfPoints() > 0) highlight.reset(new PartActor(style, modelEdges));
        else
        {
            vtkNew<vtkPolyDataSilhouette> silhouette;
            silhouette->SetInputData(part->GetPolyData());
            silhouette->SetCamera(Renderer->GetActiveCamera());
            silhouette->SetDirectionToCameraVector();
            silhouette->SetBorderEdges(1);
            silhouette->SetEnableFeatureAngle(1);
            silhouette->SetFeatureAngle(30);
            highlight.reset(new PartActor(style, silhouette->GetOutputPort()));
        }
        highlight->GetGeometry()->PickableOff();
        AddActor("highlight", std::move(highlight));
    }

    // Port of HideShowActors: the animation frames and the undeformed shape of a part follow it by their name, the
    // other actors by their name or their parent's; an overlay actor that is named is shown either way
    void View::HideShowActors(const std::set<std::string>& names, bool visible)
    {
        const std::string splitters[2] = { "_animation-frame-", "_undeformed" };
        std::vector<std::pair<std::string, PartActor*>> baseActors;
        for (const auto& entry : Parts) baseActors.push_back({ entry.first, entry.second.get() });
        for (const auto& entry : BaseActors) baseActors.push_back({ entry.first, entry.second.get() });
        for (const auto& entry : baseActors)
        {
            std::string baseName;
            for (const std::string& splitter : splitters)
            {
                size_t position = entry.first.find(splitter);
                if (position != std::string::npos && entry.first.find(splitter, position + 1) == std::string::npos)
                {
                    baseName = entry.first.substr(0, position);
                    break;
                }
            }
            if (!baseName.empty())
            {
                if (names.count(baseName) > 0) entry.second->SetVisible(visible);
            }
            else if (names.count(entry.first) > 0 ||
                     (!entry.second->GetParentName().empty() && names.count(entry.second->GetParentName()) > 0))
                HideShowActor(entry.second, visible);
        }
        for (const std::string& name : names)
        {
            auto entry = OverlayParts.find(name);
            if (entry != OverlayParts.end()) entry->second->SetVisible(true);
        }
        ApplyEdgesVisibilityAndBackfaceCulling();
        AdjustCameraDistanceAndClipping();
    }

    void View::HideShowActor(PartActor* actor, bool visible)
    {
        actor->SetVisible(visible);
        for (const auto& entry : TransformedCopies)
        {
            if (FindActor(entry.first, VTKN_LAYER_BASE) != actor) continue;
            for (const std::string& name : entry.second)
            {
                PartActor* copy = FindActor(name, VTKN_LAYER_BASE);
                if (copy != nullptr) copy->SetVisible(visible);
            }
        }
        if (!SectionViewActive) return;
        for (const auto& entry : SectionViewActorNames)
        {
            if (FindActor(entry.first, VTKN_LAYER_BASE) != actor) continue;
            PartActor* sectionViewActor = FindActor(entry.second, VTKN_LAYER_BASE);
            if (sectionViewActor != nullptr) sectionViewActor->SetVisible(visible);
        }
    }

    void View::UpdateResultRepresentationVisibility(const std::set<std::string>& partNames, bool visible)
    {
        for (PartActor* actor : GetBaseActors())
        {
            const std::string& parentName = actor->GetParentName();
            if (parentName.empty() || partNames.count(parentName) == 0) continue;
            bool actorVisible = visible;
            PartActor* parent = FindActor(parentName, VTKN_LAYER_BASE);
            if (actorVisible && parent != nullptr) actorVisible = parent->IsVisible();
            HideShowActor(actor, actorVisible);
        }
    }

    // Port of Clear, ClearButKeepParts, ClearSelection and ClearOverlay
    void View::Clear(vtkn_clear what)
    {
        Require(what >= VTKN_CLEAR_ALL && what <= VTKN_CLEAR_OVERLAY, "The clear mode is not one of vtkn_clear.");
        if (what == VTKN_CLEAR_ALL)
        {
            RemoveSectionView();
            ClearTransforms();
            for (PartActor* actor : GetBaseActors()) actor->RemoveFrom(Renderer);
            Parts.clear();
            BaseActors.clear();
            RemoveCaptions(VTKN_LAYER_BASE, nullptr);
        }
        else if (what == VTKN_CLEAR_BUT_KEEP_PARTS)
        {
            std::vector<std::string> removed;
            for (const auto& entry : BaseActors)
            {
                PartActor* parent = entry.second->GetParentName().empty() ? nullptr
                                                                          : FindActor(entry.second->GetParentName(),
                                                                                      VTKN_LAYER_BASE);
                bool isPartChild = parent != nullptr && parent->IsAPart();
                if (!isPartChild) removed.push_back(entry.first);
            }
            for (const std::string& name : removed)
            {
                BaseActors[name]->RemoveFrom(Renderer);
                BaseActors.erase(name);
            }
            RemoveCaptions(VTKN_LAYER_BASE, nullptr);
        }
        if (what != VTKN_CLEAR_OVERLAY)
        {
            for (PartActor* actor : GetBaseActors()) actor->UpdateColor();
            for (const auto& actor : SelectedParts) actor->RemoveFrom(GetLayerRenderer(*actor));
            SelectedParts.clear();
            RemoveCaptions(VTKN_LAYER_SELECTION, nullptr);
            CoorSysSelectionSymbols->Clear();
            MaterialAxesSelectionSymbols->Clear();
        }
        if (what != VTKN_CLEAR_SELECTION)
        {
            for (const auto& entry : OverlayParts) entry.second->RemoveFrom(OverlayRenderer);
            OverlayParts.clear();
            RemoveCaptions(VTKN_LAYER_OVERLAY, nullptr);
            CoorSysSymbols->Clear();
        }
    }

    void View::RemoveOverlayActors(const std::set<std::string>& names)
    {
        for (const std::string& name : names)
        {
            auto entry = OverlayParts.find(name);
            if (entry == OverlayParts.end()) continue;
            entry->second->RemoveFrom(OverlayRenderer);
            OverlayParts.erase(entry);
        }
        RemoveCaptions(VTKN_LAYER_OVERLAY, &names);
    }

    // Port of SetHighlightColor: the actors of the selection layer are formatted again
    void View::SetHighlightColors(const double* primary, const double* secondary)
    {
        Require(primary != nullptr && secondary != nullptr, "The highlight colors must not be null.");
        std::copy(primary, primary + 3, Highlight.Primary);
        std::copy(secondary, secondary + 3, Highlight.Secondary);
        for (const auto& actor : SelectedParts) actor->ApplySelectionFormatting(Highlight);
        for (CaptionEntry& entry : Captions)
        {
            if (entry.Layer == VTKN_LAYER_SELECTION) ApplyCaptionSelectionFormatting(entry);
        }
        CoorSysSelectionSymbols->SetOverrideColor(Highlight.Primary);
    }

    // Port of the AddOriented* methods, AddSphereActor and AddSymbolEdges: the symbol source is glyphed onto the
    // points along their normals, scaled by the distance to the camera so that it keeps its size on the screen, and
    // formatted as a symbol; its silhouette edges are an actor of their own
    void View::AddSymbolActor(const std::string& name, const vtkn_part_style& style, const double* points,
                              int pointCount, const double* normals, int normalCount,
                              const vtkn_symbol_parameters& parameters)
    {
        Require(pointCount >= 0 && normalCount >= 0, "The point and normal counts must not be negative.");
        Require(pointCount == 0 || points != nullptr, "The points must not be null.");
        Require(normalCount == 0 || normals != nullptr, "The normals must not be null.");
        Require(style.layer >= VTKN_LAYER_BASE && style.layer <= VTKN_LAYER_OVERLAY,
                "The layer of the style is not one of vtkn_layer.");
        SymbolSource symbol = CreateSymbolSource(parameters);
        //
        vtkNew<vtkPoints> pointData;
        for (int i = 0; i < pointCount; i++) pointData->InsertNextPoint(points + 3 * i);
        vtkNew<vtkPolyData> polyData;
        polyData->SetPoints(pointData);
        if (normals != nullptr)
        {
            vtkNew<vtkDoubleArray> normalArray;
            normalArray->SetNumberOfComponents(3);
            for (int i = 0; i < normalCount; i++) normalArray->InsertNextTuple(normals + 3 * i);
            polyData->GetPointData()->SetNormals(normalArray);
        }
        double relativeSize = parameters.kind == VTKN_SYMBOL_ARROW ? parameters.relative_size : 1;
        vtkSmartPointer<ScreenSizeGlyph> glyph = vtkSmartPointer<ScreenSizeGlyph>::New();
        glyph->SetSourceData(symbol.Source);
        glyph->SetInputData(polyData);
        glyph->SetVectorModeToUseNormal();
        glyph->SetScreenSize(Renderer, parameters.symbol_size * relativeSize, symbol.ScaleFactor);
        glyph->OrientOn();
        // ApplySymbolFormattingToActor
        vtkn_part_style symbolStyle = style;
        symbolStyle.backface_culling = 1;
        symbolStyle.ambient = 0.6;
        symbolStyle.diffuse = 0.6;
        std::unique_ptr<PartActor> actor(new PartActor(symbolStyle, glyph->GetOutputPort()));
        actor->SetSymbolInput(polyData);
        actor->GetGeometry()->GetProperty()->SetRepresentationToSurface();
        if (symbol.Flat) actor->GetGeometry()->GetProperty()->SetInterpolationToFlat();
        AddActor(name, std::move(actor));
        if (!parameters.draw_edges) return;
        //
        vtkNew<vtkPolyDataSilhouette> silhouette;
        silhouette->SetInputConnection(glyph->GetOutputPort());
        silhouette->SetCamera(Renderer->GetActiveCamera());
        silhouette->SetEnableFeatureAngle(1);
        silhouette->SetFeatureAngle(60);
        vtkn_part_style edgesStyle = style;
        edgesStyle.pickable = 0;
        const double* color = style.has_backface_color ? style.backface_color : style.color;
        for (int i = 0; i < 3; i++)
        {
            int channel = static_cast<int>(color[i] * 255 + 0.5);
            edgesStyle.color[i] = static_cast<int>(channel + (255 - channel) * 0.2) / 255.0;
        }
        edgesStyle.color[3] = 1;
        edgesStyle.line_width = 1.5;
        std::unique_ptr<PartActor> edges(new PartActor(edgesStyle, silhouette->GetOutputPort()));
        AddActor(name + ":silhouette", std::move(edges));
    }

    // Port of the poly data of AddVectorFieldActor, AddTensorFieldActor and CreateAnimatedGlyphPolyData: float points,
    // the lines of the arrows and the vertices of the points, drawn by vtkMaxActor(data, mapper). The points are
    // flat round dots, as the nodes of G1
    void View::AddGlyphActor(const std::string& name, const std::string& parentName, const vtkn_part_style& style,
                             const float* coordinates, int pointCount, const float* values, const int32_t* lines,
                             int lineCount, const int32_t* vertices, int vertexCount,
                             const vtkn_extreme_nodes* extremes)
    {
        Require(pointCount >= 0 && lineCount >= 0 && vertexCount >= 0, "The counts must not be negative.");
        Require(pointCount == 0 || (coordinates != nullptr && values != nullptr),
                "The coordinates and values must not be null.");
        Require(lineCount == 0 || lines != nullptr, "The lines must not be null.");
        Require(vertexCount == 0 || vertices != nullptr, "The vertices must not be null.");
        Require(style.layer == VTKN_LAYER_BASE, "A glyph actor is drawn in the base layer.");
        vtkNew<vtkFloatArray> coordinateArray;
        coordinateArray->SetNumberOfComponents(3);
        coordinateArray->SetNumberOfTuples(pointCount);
        if (pointCount > 0)
            std::copy(coordinates, coordinates + 3 * static_cast<size_t>(pointCount), coordinateArray->GetPointer(0));
        vtkNew<vtkPoints> points;
        points->SetData(coordinateArray);
        vtkNew<vtkCellArray> lineCells;
        for (int i = 0; i < lineCount; i++)
        {
            vtkIdType ids[2] = { lines[2 * i], lines[2 * i + 1] };
            Require(ids[0] >= 0 && ids[0] < pointCount && ids[1] >= 0 && ids[1] < pointCount,
                    "A line point index is out of range.");
            lineCells->InsertNextCell(2, ids);
        }
        vtkNew<vtkCellArray> vertexCells;
        for (int i = 0; i < vertexCount; i++)
        {
            vtkIdType id = vertices[i];
            Require(id >= 0 && id < pointCount, "A vertex point index is out of range.");
            vertexCells->InsertNextCell(1, &id);
        }
        vtkNew<vtkPolyData> polyData;
        polyData->SetPoints(points);
        polyData->SetLines(lineCells);
        polyData->SetVerts(vertexCells);
        //
        std::unique_ptr<PartActor> actor(new PartActor(style, polyData));
        actor->SetParentName(parentName);
        actor->GetGeometry()->GetProperty()->RenderPointsAsSpheresOn();
        actor->DrawPointsAsFlatRoundDots();
        if (pointCount > 0) actor->SetPointScalars(values, pointCount, extremes);
        AddActor(name, std::move(actor));
    }

    // The actor part of vtkControl.SetAnimationFrame; vtkControl removes and applies the section view and the
    // transformations around it, and those calls reach this view too
    void View::SetAnimationFrame(int frameNumber)
    {
        for (PartActor* actor : GetBaseActors())
            if (actor->IsAnimated()) actor->SetAnimationFrame(frameNumber);
        AdjustCameraDistanceAndClipping();
    }

    // Port of UpdateScalarFormatting without its widgets: the extreme nodes of the visible actors with color
    // contours give the range, the lookup table of the spectrum over it colors them, unknown values white
    void View::UpdateScalarFormatting(const vtkn_color_spectrum& spectrum, bool keepScalarBarVisible,
                                      bool useAllFrames, double allFramesMin, double allFramesMax)
    {
        const vtkn_extreme_nodes* minNode = nullptr;
        const vtkn_extreme_nodes* maxNode = nullptr;
        const vtkn_extreme_nodes* fallbackMinNode = nullptr;
        const vtkn_extreme_nodes* fallbackMaxNode = nullptr;
        std::vector<PartActor*> actors = GetBaseActors();
        for (PartActor* actor : actors)
        {
            if (!actor->HasExtremes()) continue;
            const vtkn_extreme_nodes& extremes = actor->GetExtremes();
            if (!std::isnan(extremes.min_value) &&
                (fallbackMinNode == nullptr || extremes.min_value < fallbackMinNode->min_value))
                fallbackMinNode = &extremes;
            if (!std::isnan(extremes.max_value) &&
                (fallbackMaxNode == nullptr || extremes.max_value > fallbackMaxNode->max_value))
                fallbackMaxNode = &extremes;
            actor->ApplyScalarVisibility();
            if (!actor->IsVisible() || !actor->GetColorContours()) continue;
            if (minNode == nullptr || maxNode == nullptr)
            {
                minNode = &extremes;
                maxNode = &extremes;
            }
            else
            {
                if (!std::isnan(extremes.min_value) && extremes.min_value < minNode->min_value) minNode = &extremes;
                if (!std::isnan(extremes.max_value) && extremes.max_value > maxNode->max_value) maxNode = &extremes;
            }
        }
        if (minNode == nullptr || maxNode == nullptr)
        {
            if (!keepScalarBarVisible || fallbackMinNode == nullptr || fallbackMaxNode == nullptr)
            {
                HasScalarRange = false;
                return;
            }
            minNode = fallbackMinNode;
            maxNode = fallbackMaxNode;
        }
        double minValue = minNode->min_value;
        double maxValue = maxNode->max_value;
        if (spectrum.min_max_type == VTKN_SPECTRUM_MIN_MAX_AUTOMATIC && useAllFrames)
        {
            minValue = allFramesMin;
            maxValue = allFramesMax;
        }
        vtkSmartPointer<vtkLookupTable> lookupTable = CreateActorLookupTable(spectrum, minValue, maxValue);
        double range[2];
        lookupTable->GetTableRange(range);
        vtkNew<vtkLookupTable> lookup;
        lookup->DeepCopy(lookupTable);
        lookup->SetNanColor(1, 1, 1, 1);
        for (PartActor* actor : actors)
        {
            if (actor->GetColorContours() && actor->IsVisible() && actor->HasPointScalars())
                actor->SetResultLookupTable(lookup, range);
        }
        HasScalarRange = true;
        ScalarRange[0] = range[0];
        ScalarRange[1] = range[1];
    }

    bool View::GetScalarRange(double range[2])
    {
        range[0] = ScalarRange[0];
        range[1] = ScalarRange[1];
        return HasScalarRange;
    }

    // Port of ApplyEdgesVisibilityAndBackfaceCulling: the base and selection layers follow the edges mode, the
    // overlay layer keeps its formatting
    void View::ApplyEdgesVisibilityAndBackfaceCulling()
    {
        bool partsDrawElementEdges = PartsDrawElementEdges();
        for (PartActor* actor : GetBaseActors()) ApplyEdgesVisibility(*actor, VTKN_LAYER_BASE, partsDrawElementEdges);
        for (const auto& actor : SelectedParts) ApplyEdgesVisibility(*actor, VTKN_LAYER_SELECTION, partsDrawElementEdges);
    }
}
