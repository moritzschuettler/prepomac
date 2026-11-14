// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The object behind a vtkn_view handle: one render window with the layered renderers of vtkControl (the base layer
// and the overlay layer so far), its interactor when it is embedded in a parent window, the named actors and parts,
// and the camera operations of the Views region of vtkControl.
#pragma once

#include "vtkNative.h"
#include "vtkNativeCoorSysSymbols.h"
#include "vtkNativePartActor.h"
#include "vtkNativeScalarBar.h"
#include "vtkNativeWidget.h"
#include "vtkNativeInteractorStyle.h"
#include "vtkNativeWidgetSet.h"

#include <vtkActor.h>
#include <vtkActor2D.h>
#include <vtkCallbackCommand.h>
#include <vtkCamera.h>
#include <vtkCoordinate.h>
#include <vtkLight.h>
#include <vtkPlane.h>
#include <vtkProperty.h>
#include <vtkTransform.h>
#include <vtkRenderPass.h>
#include <vtkRenderWindow.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkRenderer.h>
#include <vtkSSAOPass.h>
#include <vtkSmartPointer.h>
#include <vtkTextMapper.h>
#include <vtkUnstructuredGrid.h>

#include <array>
#include <functional>
#include <map>
#include <memory>
#include <set>
#include <string>
#include <vector>

struct vtkn_view_s
{
};

namespace vtkn
{
    class View : public vtkn_view_s
    {
    public:
        explicit View(bool offScreen);
        ~View();
        View(const View&) = delete;
        View& operator=(const View&) = delete;

        static View* FromHandle(vtkn_view handle);

        void SetParentWindow(void* parentWindow);
        void SetSize(int width, int height);
        void GetSize(int& width, int& height);
        void SetEventCallback(vtkn_event_callback callback, void* userData);
        void SetBackground(bool gradient, const double* top, const double* bottom);
        void SetSmoothing(bool pointSmoothing, bool lineSmoothing);
        void Render();
        std::string GetRenderInfo();

        void GetCamera(double* values10);
        void SetCamera(const double* values10);
        void ResetCamera();

        // Views (vtkNativeViewCamera.cpp)
        void SetViewParameters(const double* values10);
        void SetStandardView(vtkn_orientation orientation, bool zoomToFit);
        void SetAxisView(const double* axis, bool zoomToFit);
        void SetVerticalAxisView(const double* axis, bool zoomToFit);
        void ZoomToFit();
        void SetZoomFactor(double parallelScale);
        void BoxZoom(int x1, int y1, int x2, int y2);
        void AdjustCameraDistanceAndClipping();
        void SetCameraAdjustmentSuspended(bool suspended);

        void AddMeshActor(const std::string& name, const double* points, int pointCount, const int32_t* cells,
                          int cellsLength, const uint8_t* cellTypes, int cellCount, const double* color,
                          bool edgesVisible);
        void AddPartActor(const std::string& name, const vtkn_part_style& style, const vtkn_mesh& geometry,
                          const vtkn_mesh* modelEdges, const vtkn_mesh* modelEdgesToDraw);
        void RemoveActor(const std::string& name);
        void ClearActors();
        std::string GetActorNames();
        void SetEdgesVisibility(vtkn_edges_visibility edgesVisibility);
        void SetLighting(double ambient, double diffuse);
        // Geometry (Geometry/vtkNativeViewGeometry.cpp)
        void AddActor(const std::string& name, const std::string& parentName, const vtkn_part_style& style,
                      const vtkn_mesh& geometry, const vtkn_mesh* modelEdges, const vtkn_mesh* modelEdgesToDraw);
        void AddActor(const std::string& name, std::unique_ptr<PartActor> actor);
        PartActor* FindActor(const std::string& name, vtkn_layer layer);
        void SetActorVisible(const std::string& name, vtkn_layer layer, bool visible);
        void SetActorColor(const std::string& name, vtkn_layer layer, const double* rgba);
        void RenameActor(const std::string& oldName, const std::string& newName);
        void HighlightActor(const std::string& name);
        void HideShowActors(const std::set<std::string>& names, bool visible);
        void UpdateResultRepresentationVisibility(const std::set<std::string>& partNames, bool visible);
        void Clear(vtkn_clear what);
        void RemoveOverlayActors(const std::set<std::string>& names);
        void SetHighlightColors(const double* primary, const double* secondary);
        void ApplyEdgesVisibilityAndBackfaceCulling();
        void UpdateScalarFormatting(const vtkn_color_spectrum& spectrum, bool keepScalarBarVisible,
                                    bool useAllFrames, double allFramesMin, double allFramesMax);
        bool GetScalarRange(double range[2]);
        bool GetVisibleBounds(double bounds[6]) { return ComputeVisiblePropBounds(bounds); }
        // Transformations and exploded view (Geometry/vtkNativeTransformations.cpp)
        void AddSymmetry(int symmetryPlane, const double point[3]);
        void AddLinearPattern(const double displacement[3], int numOfItems);
        void AddCircularPattern(const double axisPoint[3], const double axisNormal[3], double angle, int numOfItems);
        void ApplyTransforms();
        void RemoveTransformedActors();
        void ClearTransforms();
        void ShowTransformedActors(bool show);
        void SetExplodedViewOffsets(const std::map<std::string, std::array<double, 3>>& offsets);
        void SetAnimationFrame(int frameNumber);
        void RenderToPng(const std::string& fileName, bool transparentBackground, int magnification);
        void CreateSectionView(const vtkn_section_view& sectionView);
        void RemoveSectionView();
        void AddCaption(const std::string& name, const std::string& caption, const double position[3],
                        const double* offset, int fontSize, vtkn_layer layer, bool useSecondaryHighlightColor);
        void AddLabels(const std::string& name, const double* positions, int count, const std::vector<std::string>& labels,
                       const double color[3], int fontSize, vtkn_layer layer);
        void SetCaptionPosition(const std::string& name, const double position[3]);
        void AddCoordinateSystemSymbol(const double origin[3], const double axisDirections[3][3],
                                       const std::array<std::string, 3>& labels, double symbolSize, vtkn_layer layer,
                                       bool materialAxes);
        bool SetCaptionVisible(const std::string& name, vtkn_layer layer, bool visible);
        void SetActorPoints(const std::string& name, vtkn_layer layer, const double* coordinates, int count,
                            const double* normals);
        void AddSymbolActor(const std::string& name, const vtkn_part_style& style, const double* points,
                            int pointCount, const double* normals, int normalCount,
                            const vtkn_symbol_parameters& parameters);
        void AddGlyphActor(const std::string& name, const std::string& parentName, const vtkn_part_style& style,
                           const float* coordinates, int pointCount, const float* values, const int32_t* lines,
                           int lineCount, const int32_t* vertices, int vertexCount,
                           const vtkn_extreme_nodes* extremes);

        std::vector<uint8_t> ReadPixels(int magnification, int& width, int& height);
        void WritePng(const std::string& fileName, int magnification);
        // Ambient occlusion (Geometry/vtkNativeAmbientOcclusion.cpp)
        void SetAmbientOcclusion(bool enabled, double relativeRadius, int kernelSize);

        // Picking (vtkNativeViewPick.cpp)
        bool PickPoint(int x, int y, int selectionDistance, bool limitDistance,
                       const std::set<std::string>* selectableParts, double point[3], double direction[3],
                       std::string& partName, int& cellId);
        std::vector<std::string> PickArea(int x1, int y1, int x2, int y2, double planes36[36]);
        void SetExactPicking(bool exact) { ExactPicking = exact; }
        // What the select mode lets a pick on a section view resolve to (ResolveSectionViewPick)
        void SetSectionViewPickMode(bool partSelection, bool nodeOrElementSelection)
        {
            SectionViewPartSelection = partSelection;
            SectionViewIdSelection = nodeOrElementSelection;
        }
        // Id resolution (Selection/vtkNativeViewSelection.cpp)
        void GetClosestCell(double point[3], double maxDistance, const std::set<std::string>* selectableParts,
                            vtkn_closest_cell& cell);
        void GetClosestCellsOnParts(const double point[3], const std::vector<std::string>& names, double* distances,
                                    double* normals);
        double GetSelectionPrecisionInModel(int selectionDistance);
        void GetPartBounds(const std::string& name, double bounds[6]);
        void SetVolumeGrid(const std::string& name, const vtkn_mesh* grid);
        void GetIdsInsideFrustum(const std::string& name, const double* planes, int planeCount,
                                 std::vector<int>& pointIds, std::vector<int>& cellIds);
        // Selection layer (Selection/vtkNativeViewHighlight.cpp)
        void AddHighlight(const vtkn_mesh& mesh, const vtkn_highlight_style& style);
        void AddPartHighlight(const std::string& name, const vtkn_highlight_style& style);
        void ClearHighlights(bool currentOnly);

        // Widgets
        bool HandleMouseEvent(int eventId, int x, int y);
        ScalarBar* CreateScalarBar();
        void DestroyScalarBar(ScalarBar* bar);
        // vtkNativeViewWidgets.cpp
        Widget* CreateWidget(vtkn_widget_kind kind);
        void DestroyWidget(Widget* widget);
        void SetWidgetHighlight(bool enabled, bool localEnabled);
        double GetWidgetId(BorderWidget* widget) const;
        // vtkNativeViewInteraction.cpp
        InteractorStyle* GetInteractorStyle();
        void HandleInput(int inputEvent, int x, int y, bool ctrl, bool shift, int keyCode, const char* keySym);

    private:
        static void OnVtkEvent(vtkObject* caller, unsigned long eventId, void* clientData, void* callData);
        // vtkNativeViewPick.cpp
        PartActor* FindPart(vtkProp* prop, std::string* name);
        // The section actor or skin a prop belongs to and the part it cuts (GetSectionViewParentActor)
        PartActor* FindSectionViewActor(vtkProp* prop, std::string* partName, bool* skin);
        std::vector<std::pair<std::string, PartActor*>> GetSectionViewActors();
        bool ResolveSectionViewPick(PartActor* sectionViewActor, const std::string& partName, bool skin,
                                    const std::set<std::string>* selectableParts, double maxDistance2, double point[3],
                                    int& cellId);
        int GetGlobalCellIdClosestTo3DPoint(double point[3], PartActor* onlyPart,
                                            const std::set<std::string>* selectableParts, double maxDistance2);
        double GetSelectionPrecision(int selectionDistance);
        static void OnWidgetEvent(vtkObject* caller, unsigned long eventId, void* clientData, void* callData);
        void RaiseEvent(int32_t eventId, const double* values, int32_t count);
        // vtkNativeViewCamera.cpp
        void SetFrontBackView(bool front);
        void SetTopBottomView(bool top);
        void SetLeftRightView(bool left);
        void SetIsometricView(bool positive, bool zoomToFit);
        void SetNormalView();
        void SetVerticalView();
        void ResetCameraToBounds();
        void FinishViewChange();
        void ResetClippingRange();
        void AdjustCameraDistance();
        bool ComputeVisiblePropBounds(double bounds[6]);
        void GetViewRotationPivot(double pivot[3]);
        void KeepZoomAndRotationPivot(const double pivot[3], const double pivotDisplay[2], double distance);
        void GetClosestUpView(double up[3]);
        void WorldToDisplay(const double world[3], double display[2]);
        void DisplayToWorld(const double display[2], double world[3]);

        void AdjustCameraAfterAdd();
        // Ambient occlusion (Geometry/vtkNativeAmbientOcclusion.cpp): the passes of the base layer, and the line
        // widths and point sizes of its properties as they are and as the supersampling draws them
        struct SupersampledSizes
        {
            float LineWidth;
            float PointSize;
            float AppliedLineWidth;
            float AppliedPointSize;
        };
        void CreateAmbientOcclusionPass();
        void PrepareAmbientOcclusion();
        void UpdateAmbientOcclusionRadius();
        void ApplySupersampledPixelSizes();
        void PickWithoutAmbientOcclusionPass(const std::function<void()>& pick);
        std::vector<uint8_t> ReadPixelsInOverlappingTiles(int magnification, int& width, int& height);
        bool AmbientOcclusion = false;
        double AmbientOcclusionRelativeRadius = 0.05;
        int AmbientOcclusionKernelSize = 64;
        vtkSmartPointer<vtkRenderPass> AmbientOcclusionPass;
        vtkSmartPointer<vtkSSAOPass> SsaoPass;
        std::map<vtkProperty*, SupersampledSizes> SupersampledPixelSizes;

        bool OffScreen;
        bool Embedded = false;
        bool CameraAdjustmentSuspended = false;
        bool CameraAdjustmentPending = false;
        vtkn_edges_visibility EdgesVisibility = VTKN_EDGES_ELEMENT_EDGES;
        vtkSmartPointer<vtkRenderWindow> RenderWindow;
        vtkSmartPointer<vtkRenderer> Renderer;
        vtkSmartPointer<vtkRenderer> OverlayRenderer;
        vtkSmartPointer<vtkRenderer> WidgetRenderer;
        vtkSmartPointer<vtkRenderWindowInteractor> Interactor;
        vtkSmartPointer<vtkCallbackCommand> EventCommand;
        vtkSmartPointer<vtkCallbackCommand> InteractorCommand;
        vtkSmartPointer<vtkCallbackCommand> WidgetCommand;
        WidgetSet Widgets;
        std::vector<std::unique_ptr<ScalarBar>> ScalarBars;
        std::vector<std::unique_ptr<Widget>> AreaWidgets;
        vtkSmartPointer<InteractorStyle> Style;
        vtkSmartPointer<vtkRenderWindowInteractor> ScriptInteractor;
        friend class InteractorStyle;
        int NextWidgetId = 1;
        std::vector<vtkSmartPointer<vtkLight>> Lights;
        std::map<std::string, vtkSmartPointer<vtkActor>> Actors;
        std::map<std::string, std::unique_ptr<PartActor>> Parts;
        vtkn_event_callback Callback = nullptr;
        void* UserData = nullptr;
        // Geometry: the selection layer and the actors of the overlay and selection layers, beside the base layer
        // Parts; the selection renderer draws between the overlay and the widgets, as in vtkControl
        void InitializeGeometry();
        void HideShowActor(PartActor* actor, bool visible);
        vtkRenderer* GetLayerRenderer(const PartActor& actor);
        void ApplyEdgesVisibility(PartActor& actor, vtkn_layer layer, bool partsDrawElementEdges);
        bool PartsDrawElementEdges();
        vtkSmartPointer<vtkRenderer> SelectionRenderer;
        // The base layer actors that are not parts (symbols, constraint faces, nodes): Parts holds only what
        // picking and the parts of the checks consider
        std::map<std::string, std::unique_ptr<PartActor>> BaseActors;
        std::vector<PartActor*> GetBaseActors();
        std::map<std::string, std::unique_ptr<PartActor>> OverlayParts;
        std::vector<std::unique_ptr<PartActor>> SelectedParts;
        HighlightColors Highlight;
        // Section view (Geometry/vtkNativeSectionView.cpp)
        void ClearSectionViewActors();
        void AddSectionViewActors(const std::string& name, PartActor* actor);
        vtkn_part_style GetSectionViewStyle(PartActor* actor);
        std::unique_ptr<PartActor> GetSectionViewActorFromSolid(PartActor* actor);
        std::unique_ptr<PartActor> GetSectionViewActorFromClosedSurface(PartActor* actor);
        void AddSectionViewActorsElementWise(const std::string& name, PartActor* actor);
        void AddSectionViewSkinActorsToSurfaceActors();
        void AddSectionViewSkinActor(const std::string& name, PartActor* actor, std::unique_ptr<PartActor> skinActor);
        vtkSmartPointer<vtkPlane> SectionPlane;
        vtkn_section_view SectionView = {};
        std::map<std::string, std::string> SectionViewActorNames;
        // The element-wise section: the skins that replace the cut parts and the surfaces on them, and the elements
        // the cut keeps
        std::map<std::string, std::string> SectionViewSkinActorNames;
        std::set<int> SectionViewKeptElementIds;
        std::vector<std::pair<vtkSmartPointer<vtkTransform>, int>> Transforms;
        std::map<std::string, std::vector<std::string>> TransformedCopies;
        // The part a transformed copy was made from, by the copy name
        std::map<std::string, std::string> TransformedCopySources;
        bool HasScalarRange = false;
        double ScalarRange[2] = { 0, 1 };
        bool SectionViewActive = false;
        // The selection area: the volume grids of the parts, and the highlights drawn into the selection layer
        struct HighlightEntry
        {
            vtkSmartPointer<vtkActor> Actor;
            vtkRenderer* Renderer;
            bool Current;
        };
        vtkRenderer* GetHighlightRenderer();
        void AddHighlightActor(vtkActor* actor, const vtkn_highlight_style& style);
        std::map<std::string, vtkSmartPointer<vtkUnstructuredGrid>> VolumeGrids;
        std::vector<HighlightEntry> Highlights;
        bool ExactPicking = true;
        bool SectionViewPartSelection = true;
        bool SectionViewIdSelection = true;
        // Captions and labels (Geometry/vtkNativeCaptions.cpp): the 2D text of every layer, at world positions that
        // keep their screen offset when the camera changes
        struct CaptionEntry
        {
            std::string Name;
            vtkSmartPointer<vtkActor2D> Actor;
            vtkn_layer Layer;
            double Position[3];
            bool HasOffset;
            double Offset[3];
            bool UseSecondaryHighlightColor;
            // A caption: its world point and its text; null for labels
            vtkSmartPointer<vtkCoordinate> Anchor;
            vtkSmartPointer<vtkTextMapper> TextMapper;
        };
        vtkRenderer* GetCaptionRenderer(vtkn_layer layer);
        void AddCaptionEntry(CaptionEntry entry);
        void RemoveCaptions(vtkn_layer layer, const std::set<std::string>* names);
        void ApplyCaptionSelectionFormatting(CaptionEntry& entry);
        void UpdateCaptionPositions();
        std::vector<CaptionEntry> Captions;
        vtkSmartPointer<vtkCallbackCommand> CaptionCommand;
        // The coordinate systems of the model in the overlay layer, and in the selection layer the selected ones in the
        // highlight color and the material axes in their own colors
        std::unique_ptr<CoorSysSymbolSet> CoorSysSymbols;
        std::unique_ptr<CoorSysSymbolSet> CoorSysSelectionSymbols;
        std::unique_ptr<CoorSysSymbolSet> MaterialAxesSelectionSymbols;
    };
}
