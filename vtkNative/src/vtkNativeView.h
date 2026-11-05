// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The object behind a vtkn_view handle: one render window with the layered renderers of vtkControl (the base layer
// and the overlay layer so far), its interactor when it is embedded in a parent window, the named actors and parts,
// and the camera operations of the Views region of vtkControl.
#pragma once

#include "vtkNative.h"
#include "vtkNativePartActor.h"
#include "vtkNativeScalarBar.h"
#include "vtkNativeWidget.h"
#include "vtkNativeInteractorStyle.h"
#include "vtkNativeWidgetSet.h"

#include <vtkActor.h>
#include <vtkCallbackCommand.h>
#include <vtkCamera.h>
#include <vtkLight.h>
#include <vtkRenderWindow.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

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

        std::vector<uint8_t> ReadPixels(int magnification, int& width, int& height);
        void WritePng(const std::string& fileName, int magnification);

        // Picking (vtkNativeViewPick.cpp)
        bool PickPoint(int x, int y, int selectionDistance, bool limitDistance,
                       const std::set<std::string>* selectableParts, double point[3], double direction[3],
                       std::string& partName, int& cellId);
        std::vector<std::string> PickArea(int x1, int y1, int x2, int y2, double planes36[36]);

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

        bool OffScreen;
        bool Embedded = false;
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
    };
}
