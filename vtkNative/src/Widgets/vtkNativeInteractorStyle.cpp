// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeInteractorStyle.h"
#include "vtkNativeView.h"

#include <vtkCallbackCommand.h>
#include <vtkCamera.h>
#include <vtkCellArray.h>
#include <vtkNew.h>
#include <vtkObjectFactory.h>
#include <vtkPolyData.h>
#include <vtkPolyDataMapper.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty.h>
#include <vtkProperty2D.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkRenderer.h>

#include <algorithm>
#include <cmath>
#include <cstdlib>
#include <cstring>

#if defined(_WIN32)
#  define NOMINMAX
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>
#endif

namespace vtkn
{
    namespace
    {
        const int SmoothZoomIntervalMilliseconds = 15;
        const double SmoothZoomApproachRatio = 0.4;
        const double SmoothZoomMinLogFactor = 0.005;
        const double SmoothZoomMaxFrameMilliseconds = 30;
        const int SelectionTimerMilliseconds = 200;
        // vtkMaxWidgetHighlight: RoyalBlue, LightSkyBlue and the opacities of the box selection
        const double BorderColor[3] = { 65 / 255.0, 105 / 255.0, 225 / 255.0 };
        const double FillColor[3] = { 135 / 255.0, 206 / 255.0, 250 / 255.0 };
        const double SurfaceFillOpacity = 0.15;
        const double WidgetBorderOpacity = 0.5;
        const float WidgetBorderLineWidth = 2.0f;

        // The key codes vtkInteractorStyleControl compared: VTK 5.8 reported the virtual key of an arrow key as
        // its key code, VTK 9 reports a zero code and the key symbol
        int GetLegacyKeyCode(vtkRenderWindowInteractor* interactor)
        {
            const char* keySym = interactor->GetKeySym();
            if (keySym != nullptr)
            {
                if (std::strcmp(keySym, "Left") == 0) return 37;
                if (std::strcmp(keySym, "Up") == 0) return 38;
                if (std::strcmp(keySym, "Right") == 0) return 39;
                if (std::strcmp(keySym, "Down") == 0) return 40;
                if (std::strcmp(keySym, "Escape") == 0) return 27;
            }
            return static_cast<unsigned char>(interactor->GetKeyCode());
        }

        bool AreBoundsInitialized(const double bounds[6])
        {
            return bounds[0] <= bounds[1] && bounds[2] <= bounds[3] && bounds[4] <= bounds[5];
        }
    }

    vtkStandardNewMacro(InteractorStyle);

    InteractorStyle::InteractorStyle()
    {
#if defined(_WIN32)
        DoubleClickTime = std::chrono::milliseconds(GetDoubleClickTime());
#else
        DoubleClickTime = std::chrono::milliseconds(500);
#endif
        LastClickTime = std::chrono::steady_clock::now();
        AutoAdjustCameraClippingRangeOn();
    }

    InteractorStyle::~InteractorStyle() = default;

    void InteractorStyle::SetView(View* view, vtkRenderer* overlayRenderer, vtkRenderer* selectionRenderer)
    {
        Owner = view;
        OverlayRenderer = overlayRenderer;
        SelectionRenderer = selectionRenderer;
        SetDefaultRenderer(view->Renderer);
        InitializeBoxSelection();
    }

    void InteractorStyle::SetSettings(const vtkn_interaction_settings& settings)
    {
        RotateSensitivity = settings.rotate_sensitivity;
        ZoomSensitivity = settings.zoom_sensitivity;
        InvertZoom = settings.invert_zoom != 0;
        SmoothZoom = settings.smooth_zoom != 0;
        if (!SmoothZoom) FinishSmoothZoom();
        SelectionBoxEnabled = settings.selection_box_enabled != 0;
        SelectionDistance = settings.selection_distance;
    }

    void InteractorStyle::GetSettings(vtkn_interaction_settings& settings) const
    {
        settings.rotate_sensitivity = RotateSensitivity;
        settings.zoom_sensitivity = ZoomSensitivity;
        settings.invert_zoom = InvertZoom ? 1 : 0;
        settings.smooth_zoom = SmoothZoom ? 1 : 0;
        settings.selection_box_enabled = SelectionBoxEnabled ? 1 : 0;
        settings.selection_distance = SelectionDistance;
    }

    void InteractorStyle::InteractionHappened()
    {
        Owner->RaiseEvent(VTKN_EVENT_MOUSE_INTERACTION, nullptr, 0);
    }

    void InteractorStyle::SetAnimating(bool animating)
    {
        Animating = animating;
        if (Animating) FinishSmoothZoom();
    }

    void InteractorStyle::SetBoxZoomEnabled(bool enabled)
    {
        BoxZoomEnabled = enabled;
        if (!BoxZoomEnabled) Owner->RaiseEvent(VTKN_EVENT_BOX_ZOOM_ENDED, nullptr, 0);
    }

    // The left button: a press beside the widgets starts the box selection; the widgets took their presses before
    void InteractorStyle::OnLeftButtonDown()
    {
        InteractionHappened();
        if (Animating) return;
        FinishSmoothZoom();
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int x = rwi->GetEventPosition()[0];
        int y = rwi->GetEventPosition()[1];
        //
        NumOfClicks = 1;
        auto now = std::chrono::steady_clock::now();
        if (now - LastClickTime < DoubleClickTime && std::abs(x - LastX) < DoubleClickDisplacement &&
            std::abs(y - LastY) < DoubleClickDisplacement) NumOfClicks = 2;
        HasClickPos = true;
        ClickPos[0] = x;
        ClickPos[1] = y;
        SetCurrentRenderer(Owner->Renderer);
        const double display[2] = { static_cast<double>(x), static_cast<double>(y) };
        DisplayToWorld(display, ClickPosWorld);
        HasClickPosWorld = true;
        LeftMouseButtonPressed = true;
        BoxSelection = false;
        SelectionCanceled = false;
        for (int i = 0; i < 4; i++) SelectionBackgroundPoints->SetPoint(i, ClickPos[0], ClickPos[1], 0.0);
        SelectionBackgroundPoints->Modified();
        //
        LastX = x;
        LastY = y;
        LastClickTime = now;
    }

    void InteractorStyle::OnLeftButtonUp()
    {
        if (Animating) return;
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int* mousePos = rwi->GetEventPosition();
        SelectionBackgroundActor->VisibilityOff();
        SelectionBorderActor->VisibilityOff();
        if (!SelectionCanceled && HasClickPos)
        {
            if (BoxZoomEnabled) Owner->BoxZoom(ClickPos[0], ClickPos[1], mousePos[0], mousePos[1]);
            else
            {
                double values[8] = { static_cast<double>(mousePos[0]), static_cast<double>(mousePos[1]),
                                     static_cast<double>(NumOfClicks), BoxSelection ? 1.0 : 0.0,
                                     static_cast<double>(ClickPos[0]), static_cast<double>(ClickPos[1]),
                                     static_cast<double>(rwi->GetControlKey()),
                                     static_cast<double>(rwi->GetShiftKey()) };
                Owner->RaiseEvent(VTKN_EVENT_POINT_PICKED_ON_LEFT_UP, values, 8);
            }
        }
        HasClickPos = false;
        HasClickPosWorld = false;
        LeftMouseButtonPressed = false;
        BoxSelection = false;
        SetBoxZoomEnabled(false);
        rwi->Render();
    }

    // The middle button turns the camera, with shift it pans and with ctrl it zooms (the trackball states)
    void InteractorStyle::OnMiddleButtonDown()
    {
        InteractionHappened();
        if (Animating) return;
        FinishSmoothZoom();
        Owner->Widgets.ClearHighlights();
        CancelBoxSelection();
        //
        vtkInteractorStyleTrackballCamera::OnLeftButtonDown();
        SetCurrentRenderer(Owner->Renderer);
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int x = rwi->GetEventPosition()[0];
        int y = rwi->GetEventPosition()[1];
        HasClickPos = true;
        ClickPos[0] = x;
        ClickPos[1] = y;
        UpdateRotationCenterDisplay();
        AddAnnotationForCenter3D();
        Owner->Widgets.MiddleButtonPress(x, y);
        rwi->Render();
    }

    // A middle click without a move picks the rotation center: a point of the model, or the center of the
    // visible bounds when the click is beside it
    void InteractorStyle::OnMiddleButtonUp()
    {
        if (Animating) return;
        SetCurrentRenderer(Owner->Renderer);
        vtkRenderer* renderer = GetCurrentRenderer();
        RemoveAnnotationForCenter();
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int currClickPos[2] = { rwi->GetEventPosition()[0], rwi->GetEventPosition()[1] };
        // The C# style compares the x coordinates twice, so only a horizontal move counts
        if (HasClickPos && std::abs(ClickPos[0] - currClickPos[0]) + std::abs(ClickPos[0] - currClickPos[0]) < 2)
        {
            double point[3];
            double direction[3];
            std::string partName;
            int cellId = -1;
            bool hit = !Owner->Widgets.IsPositionOverWidget(currClickPos[0], currClickPos[1]) &&
                       Owner->PickPoint(currClickPos[0], currClickPos[1], SelectionDistance, true, nullptr, point,
                                        direction, partName, cellId);
            if (hit && !partName.empty())
            {
                std::copy(point, point + 3, RotationCenterWorld);
                HasRotationCenterWorld = true;
                vtkCamera* camera = renderer->GetActiveCamera();
                double center[3];
                camera->GetFocalPoint(center);
                PanCamera(camera, center, RotationCenterWorld);
                Owner->AdjustCameraDistance();
            }
            else
            {
                double bounds[6];
                if (ComputeVisiblePropBounds(bounds, true))
                {
                    RotationCenterWorld[0] = (bounds[1] + bounds[0]) / 2;
                    RotationCenterWorld[1] = (bounds[3] + bounds[2]) / 2;
                    RotationCenterWorld[2] = (bounds[5] + bounds[4]) / 2;
                    HasRotationCenterWorld = true;
                }
            }
            if (HasRotationCenterWorld) Owner->RaiseEvent(VTKN_EVENT_ROTATION_POINT_CHANGED, RotationCenterWorld, 3);
        }
        if (GetAutoAdjustCameraClippingRange()) Owner->ResetClippingRange();
        if (rwi->GetLightFollowCamera()) renderer->UpdateLightsGeometryToFollowCamera();
        Owner->Widgets.MiddleButtonRelease(currClickPos[0], currClickPos[1]);
        Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
        HasClickPos = false;
        HasClickPosWorld = false;
        vtkInteractorStyleTrackballCamera::OnLeftButtonUp();
    }

    // The right button opens the context menu of the widget under the mouse or of the view
    void InteractorStyle::OnRightButtonDown()
    {
        InteractionHappened();
        if (Animating) return;
        FinishSmoothZoom();
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int x = rwi->GetEventPosition()[0];
        int y = rwi->GetEventPosition()[1];
        BorderWidget* clickedWidget = Owner->Widgets.RightButtonPress(x, y);
        double widgetId = 0;
        if (clickedWidget != nullptr)
        {
            Owner->Widgets.PromoteAndRefresh(clickedWidget, x, y);
            widgetId = Owner->GetWidgetId(clickedWidget);
            rwi->Render();
        }
        double values[3] = { static_cast<double>(x), static_cast<double>(y), widgetId };
        Owner->RaiseEvent(VTKN_EVENT_RIGHT_BUTTON_PRESS, values, 3);
    }

    void InteractorStyle::OnRightButtonUp()
    {
    }

    void InteractorStyle::OnMouseMove()
    {
        if (Animating) return;
        vtkRenderWindowInteractor* rwi = GetInteractor();
        X = rwi->GetEventPosition()[0];
        Y = rwi->GetEventPosition()[1];
        // The widget set took the move first and rendered what it changed
        if (State == VTKIS_NONE && Owner->Widgets.LastMoveChanged) return;
        SetCurrentRenderer(Owner->Renderer);
        switch (State)
        {
            case VTKIS_NONE:
                StartSelectionTimer();
                if (SelectionBoxEnabled || BoxZoomEnabled)
                {
                    if (!BoxSelection && LeftMouseButtonPressed && HasClickPos && NumOfClicks != 2 &&
                        std::abs(X - ClickPos[0]) > 1 && std::abs(Y - ClickPos[1]) > 1) BoxSelection = true;
                    if (BoxSelection) DrawBoxSelection(X, Y);
                }
                break;
            case VTKIS_ROTATE:
                Rotate();
                InvokeEvent(vtkCommand::InteractionEvent, nullptr);
                break;
            case VTKIS_PAN:
                Pan();
                InvokeEvent(vtkCommand::InteractionEvent, nullptr);
                break;
            case VTKIS_SPIN:
                Zoom();
                InvokeEvent(vtkCommand::InteractionEvent, nullptr);
                break;
            default:
                break;
        }
    }

    void InteractorStyle::OnMouseWheelForward()
    {
        InteractionHappened();
        if (Animating) return;
        if (State != VTKIS_NONE) return;
        int* clickPos = GetInteractor()->GetEventPosition();
        ZoomAtDisplayPointSmooth(GetMouseWheelZoomFactor(), clickPos[0], clickPos[1]);
    }

    void InteractorStyle::OnMouseWheelBackward()
    {
        InteractionHappened();
        if (Animating) return;
        if (State != VTKIS_NONE) return;
        int* clickPos = GetInteractor()->GetEventPosition();
        ZoomAtDisplayPointSmooth(1 / GetMouseWheelZoomFactor(), clickPos[0], clickPos[1]);
    }

    double InteractorStyle::GetMouseWheelZoomFactor()
    {
        double ctrlKeyFactor = GetInteractor()->GetControlKey() == 0 ? 1 : 0.2;
        return std::pow(1.1, MotionFactorValue * ctrlKeyFactor * 0.2 * GetMouseWheelMotionFactor());
    }

    // The arrow keys turn the camera by 5 degrees, escape cancels the box selection, z holds a box zoom
    void InteractorStyle::OnKeyPress()
    {
        if (Animating) return;
        FinishSmoothZoom();
        SetCurrentRenderer(Owner->Renderer);
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int key = GetLegacyKeyCode(rwi);
        if (key >= 37 && key <= 40)
        {
            CancelBoxSelection();
            UpdateRotationCenterDisplay();
            Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
            const double delta = 5;
            if (key == 37) Rotate(+delta, 0, 0, false);
            else if (key == 39) Rotate(-delta, 0, 0, false);
            else if (key == 38) Rotate(0, -delta, 0, false);
            else if (key == 40) Rotate(0, +delta, 0, false);
            rwi->Render();
        }
        else if (key == 27) CancelBoxSelection();
        else if (key == 90 || key == 122) SetBoxZoomEnabled(true);
    }

    void InteractorStyle::OnKeyRelease()
    {
        if (Animating) return;
        int key = GetLegacyKeyCode(GetInteractor());
        if ((key == 90 || key == 122) && !LeftMouseButtonPressed) SetBoxZoomEnabled(false);
    }

    void InteractorStyle::OnLeave()
    {
        Owner->RaiseEvent(VTKN_EVENT_MOUSE_LEFT, nullptr, 0);
    }

    // The Win32 interactor of VTK 9 does not report which timer fired; the selection timer is stopped while the
    // smooth zoom runs, as in the C# style, so the one that runs is the one that fired
    void InteractorStyle::OnTimer()
    {
        if (SmoothZoomTimerId >= 0) StepSmoothZoom();
        else if (SelectionTimerId >= 0) FireSelectionTimer();
    }

    void InteractorStyle::StartSelectionTimer()
    {
        StopSelectionTimer();
        SelectionTimerPending = true;
        vtkRenderWindowInteractor* rwi = GetInteractor();
        if (rwi != nullptr && rwi->GetInitialized()) SelectionTimerId = rwi->CreateOneShotTimer(SelectionTimerMilliseconds);
    }

    void InteractorStyle::StopSelectionTimer()
    {
        SelectionTimerPending = false;
        if (SelectionTimerId >= 0 && GetInteractor() != nullptr) GetInteractor()->DestroyTimer(SelectionTimerId);
        SelectionTimerId = -1;
    }

    // The mouse move pick of the C# selection timer, for the hover of the selection
    void InteractorStyle::FireSelectionTimer()
    {
        bool pending = SelectionTimerPending;
        StopSelectionTimer();
        if (!pending || BoxZoomEnabled) return;
        SetCurrentRenderer(Owner->Renderer);
        int clickPos[2] = { 0, 0 };
        if (HasClickPos && HasClickPosWorld)
        {
            if (BoxSelection)
            {
                double display[2];
                WorldToDisplay(ClickPosWorld, display);
                clickPos[0] = static_cast<int>(display[0]);
                clickPos[1] = static_cast<int>(display[1]);
            }
            else
            {
                clickPos[0] = ClickPos[0];
                clickPos[1] = ClickPos[1];
            }
        }
        double values[5] = { static_cast<double>(X), static_cast<double>(Y), BoxSelection ? 1.0 : 0.0,
                             static_cast<double>(clickPos[0]), static_cast<double>(clickPos[1]) };
        Owner->RaiseEvent(VTKN_EVENT_POINT_PICKED_ON_MOUSE_MOVE, values, 5);
    }

    void InteractorStyle::StartSmoothZoomTimer()
    {
        vtkRenderWindowInteractor* rwi = GetInteractor();
        if (SmoothZoomTimerId < 0 && rwi != nullptr && rwi->GetInitialized())
            SmoothZoomTimerId = rwi->CreateRepeatingTimer(SmoothZoomIntervalMilliseconds);
    }

    void InteractorStyle::StopSmoothZoomTimer()
    {
        if (SmoothZoomTimerId >= 0 && GetInteractor() != nullptr) GetInteractor()->DestroyTimer(SmoothZoomTimerId);
        SmoothZoomTimerId = -1;
    }

    void InteractorStyle::DisplayToWorld(const double display[2], double world[3])
    {
        double worldPoint[4];
        ComputeDisplayToWorld(Owner->Renderer, display[0], display[1], 0, worldPoint);
        std::copy(worldPoint, worldPoint + 3, world);
    }

    void InteractorStyle::WorldToDisplay(const double world[3], double display[2])
    {
        double displayPoint[3];
        ComputeWorldToDisplay(Owner->Renderer, world[0], world[1], world[2], displayPoint);
        display[0] = displayPoint[0];
        display[1] = displayPoint[1];
    }

    void InteractorStyle::PanCamera(vtkCamera* camera, const double posStart[3], const double posEnd[3])
    {
        double motion[3] = { posEnd[0] - posStart[0], posEnd[1] - posStart[1], posEnd[2] - posStart[2] };
        double position[3];
        double focalPoint[3];
        camera->GetPosition(position);
        camera->GetFocalPoint(focalPoint);
        camera->SetPosition(position[0] + motion[0], position[1] + motion[1], position[2] + motion[2]);
        camera->SetFocalPoint(focalPoint[0] + motion[0], focalPoint[1] + motion[1], focalPoint[2] + motion[2]);
        Owner->AdjustCameraDistanceAndClipping();
    }

    void InteractorStyle::Pan()
    {
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int* clickPos = rwi->GetEventPosition();
        int* lastPos = rwi->GetLastEventPosition();
        const double newDisplay[2] = { static_cast<double>(clickPos[0]), static_cast<double>(clickPos[1]) };
        const double oldDisplay[2] = { static_cast<double>(lastPos[0]), static_cast<double>(lastPos[1]) };
        double newPickPoint[3];
        double oldPickPoint[3];
        DisplayToWorld(newDisplay, newPickPoint);
        DisplayToWorld(oldDisplay, oldPickPoint);
        PanCamera(Owner->Renderer->GetActiveCamera(), newPickPoint, oldPickPoint);
        if (rwi->GetLightFollowCamera()) Owner->Renderer->UpdateLightsGeometryToFollowCamera();
        Owner->Widgets.MousePan(clickPos[0] - lastPos[0], clickPos[1] - lastPos[1]);
        rwi->Render();
    }

    void InteractorStyle::PanByDisplayDelta(double dx, double dy, bool render)
    {
        int* size = Owner->Renderer->GetSize();
        const double basePos[2] = { size[0] / 2.0, size[1] / 2.0 };
        const double newDisplay[2] = { basePos[0] + dx, basePos[1] + dy };
        double newPickPoint[3];
        double oldPickPoint[3];
        DisplayToWorld(newDisplay, newPickPoint);
        DisplayToWorld(basePos, oldPickPoint);
        PanCamera(Owner->Renderer->GetActiveCamera(), newPickPoint, oldPickPoint);
        vtkRenderWindowInteractor* rwi = GetInteractor();
        if (rwi->GetLightFollowCamera()) Owner->Renderer->UpdateLightsGeometryToFollowCamera();
        Owner->Widgets.MousePan(static_cast<int>(dx), static_cast<int>(dy));
        Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
        if (render) rwi->Render();
    }

    // Zoom on mouse move with ctrl and the middle button: the slow zoom
    void InteractorStyle::Zoom()
    {
        Owner->Widgets.ClearHighlights();
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int* clickPos = rwi->GetEventPosition();
        vtkCamera* camera = Owner->Renderer->GetActiveCamera();
        double factor = rwi->GetLastEventPosition()[1] - clickPos[1];
        if (factor < 0) factor = std::pow(1.001, factor);
        else factor = std::pow(1 / 1.001, -factor);
        factor = std::pow(factor, ZoomSensitivity);
        camera->SetParallelScale(camera->GetParallelScale() * factor);
        Owner->Widgets.MouseWheelScrolled();
        Owner->RaiseEvent(VTKN_EVENT_ZOOM_CHANGED, nullptr, 0);
        Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
        rwi->Render();
    }

    // The point under the mouse stays where it is
    void InteractorStyle::ZoomAtDisplayPoint(double parallelScaleFactor, int x, int y, bool render)
    {
        SetCurrentRenderer(Owner->Renderer);
        const double displayPos[2] = { static_cast<double>(x), static_cast<double>(y) };
        double worldPos[3];
        DisplayToWorld(displayPos, worldPos);
        double factor = std::pow(parallelScaleFactor, ZoomSensitivity);
        if (InvertZoom) factor = 1 / factor;
        vtkCamera* camera = Owner->Renderer->GetActiveCamera();
        camera->SetParallelScale(camera->GetParallelScale() * factor);
        double worldPosAfter[3];
        DisplayToWorld(displayPos, worldPosAfter);
        PanCamera(camera, worldPosAfter, worldPos);
        if (BoxSelection) DrawBoxSelection(X, Y);
        Owner->Widgets.MouseWheelScrolled();
        Owner->RaiseEvent(VTKN_EVENT_ZOOM_CHANGED, nullptr, 0);
        Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
        // A zoom may move the projected widgets; the hover is restored at the unchanged mouse position
        Owner->Widgets.RefreshHighlights(x, y);
        if (render && GetInteractor() != nullptr) GetInteractor()->Render();
    }

    // The wheel notches accumulate into a target the camera approaches over a few frames
    void InteractorStyle::ZoomAtDisplayPointSmooth(double parallelScaleFactor, int x, int y)
    {
        SmoothZoomX = x;
        SmoothZoomY = y;
        SmoothZoomRemainingFactor *= parallelScaleFactor;
        if (!SmoothZoom || SmoothZoomFrameMilliseconds > SmoothZoomMaxFrameMilliseconds)
        {
            FinishSmoothZoom();
            return;
        }
        if (SmoothZoomTimerId < 0) StopSelectionTimer();
        StartSmoothZoomTimer();
    }

    void InteractorStyle::FinishSmoothZoom()
    {
        StopSmoothZoomTimer();
        ApplyRemainingSmoothZoom();
    }

    bool InteractorStyle::StepSmoothZoom()
    {
        if (SmoothZoomStepping) return true;
        SmoothZoomStepping = true;
        double logRemainingFactor = std::log(SmoothZoomRemainingFactor);
        bool remaining = true;
        if (std::abs(logRemainingFactor) < SmoothZoomMinLogFactor ||
            SmoothZoomFrameMilliseconds > SmoothZoomMaxFrameMilliseconds)
        {
            StopSmoothZoomTimer();
            ApplyRemainingSmoothZoom();
            StartSelectionTimer();
            remaining = false;
        }
        else
        {
            // A constant ratio per frame reads as a uniform speed under a parallel projection
            double stepFactor = std::exp(logRemainingFactor * SmoothZoomApproachRatio);
            SmoothZoomRemainingFactor /= stepFactor;
            ZoomAtDisplayPointTimed(stepFactor, SmoothZoomX, SmoothZoomY);
        }
        SmoothZoomStepping = false;
        return remaining;
    }

    void InteractorStyle::ApplyRemainingSmoothZoom()
    {
        if (SmoothZoomRemainingFactor == 1) return;
        double parallelScaleFactor = SmoothZoomRemainingFactor;
        SmoothZoomRemainingFactor = 1;
        ZoomAtDisplayPointTimed(parallelScaleFactor, SmoothZoomX, SmoothZoomY);
    }

    void InteractorStyle::ZoomAtDisplayPointTimed(double parallelScaleFactor, int x, int y)
    {
        auto start = std::chrono::steady_clock::now();
        ZoomAtDisplayPoint(parallelScaleFactor, x, y, true);
        SmoothZoomFrameMilliseconds =
            std::chrono::duration<double, std::milli>(std::chrono::steady_clock::now() - start).count();
    }

    void InteractorStyle::Rotate()
    {
        vtkRenderWindowInteractor* rwi = GetInteractor();
        int* clickPos = rwi->GetEventPosition();
        int dx = clickPos[0] - rwi->GetLastEventPosition()[0];
        int dy = clickPos[1] - rwi->GetLastEventPosition()[1];
        RotateByDisplayDelta(dx, dy, true);
    }

    void InteractorStyle::RotateByDisplayDelta(double dx, double dy, bool render)
    {
        const double deltaElevation = -20.0 / 500;
        const double deltaAzimuth = -20.0 / 500;
        double rxf = dx * deltaAzimuth * MotionFactorValue * RotateSensitivity;
        double ryf = dy * deltaElevation * MotionFactorValue * RotateSensitivity;
        Rotate(rxf, ryf, 0, render);
    }

    // The camera turns about the focal point and is then moved so that the rotation center stays where it was
    void InteractorStyle::Rotate(double azimuthAngle, double elevationAngle, double rollAngle, bool render)
    {
        vtkRenderer* renderer = Owner->Renderer;
        vtkCamera* camera = renderer->GetActiveCamera();
        vtkRenderWindowInteractor* rwi = GetInteractor();
        if (azimuthAngle != 0) camera->Azimuth(azimuthAngle);
        if (elevationAngle != 0) camera->Elevation(elevationAngle);
        if (rollAngle != 0) camera->Roll(rollAngle);
        camera->OrthogonalizeViewUp();
        if (HasRotationCenterWorld && HasRotationCenterDisplay)
        {
            double wpa[3];
            DisplayToWorld(RotationCenterDisplay, wpa);
            double n[3];
            camera->GetViewPlaneNormal(n);
            double v[3] = { wpa[0] - RotationCenterWorld[0], wpa[1] - RotationCenterWorld[1],
                            wpa[2] - RotationCenterWorld[2] };
            double d = n[0] * v[0] + n[1] * v[1] + n[2] * v[2];
            for (int i = 0; i < 3; i++) wpa[i] -= n[i] * d;
            PanCamera(camera, wpa, RotationCenterWorld);
        }
        else if (GetAutoAdjustCameraClippingRange()) Owner->ResetClippingRange();
        if (rwi != nullptr && rwi->GetLightFollowCamera()) renderer->UpdateLightsGeometryToFollowCamera();
        Owner->Widgets.MouseRotate(azimuthAngle, elevationAngle);
        Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
        if (render && rwi != nullptr) rwi->Render();
    }

    bool InteractorStyle::ComputeVisiblePropBounds(double bounds[6], bool withOverlay)
    {
        bool found = false;
        vtkRenderer* renderers[2] = { Owner->Renderer, withOverlay ? OverlayRenderer : nullptr };
        for (vtkRenderer* renderer : renderers)
        {
            if (renderer == nullptr) continue;
            double rendererBounds[6];
            renderer->ComputeVisiblePropBounds(rendererBounds);
            if (!AreBoundsInitialized(rendererBounds)) continue;
            if (!found) std::copy(rendererBounds, rendererBounds + 6, bounds);
            else
            {
                for (int i = 0; i < 6; i += 2)
                {
                    bounds[i] = std::min(bounds[i], rendererBounds[i]);
                    bounds[i + 1] = std::max(bounds[i + 1], rendererBounds[i + 1]);
                }
            }
            found = true;
        }
        return found;
    }

    // The center of the visible bounds the first time; its display position is updated each time, since a zoom
    // moves it too
    void InteractorStyle::UpdateRotationCenterDisplay()
    {
        if (!HasRotationCenterWorld)
        {
            double bounds[6];
            if (!ComputeVisiblePropBounds(bounds, false))
            {
                HasRotationCenterDisplay = false;
                return;
            }
            RotationCenterWorld[0] = (bounds[1] + bounds[0]) / 2;
            RotationCenterWorld[1] = (bounds[3] + bounds[2]) / 2;
            RotationCenterWorld[2] = (bounds[5] + bounds[4]) / 2;
            HasRotationCenterWorld = true;
        }
        WorldToDisplay(RotationCenterWorld, RotationCenterDisplay);
        HasRotationCenterDisplay = true;
    }

    void InteractorStyle::SetRotationPoint(const double* point)
    {
        if (point == nullptr)
        {
            ResetRotationCenter();
            return;
        }
        std::copy(point, point + 3, RotationCenterWorld);
        HasRotationCenterWorld = true;
    }

    void InteractorStyle::ResetRotationCenter()
    {
        HasRotationCenterWorld = false;
        HasRotationCenterDisplay = false;
    }

    void InteractorStyle::StartMultiTouchGesture(int, int, bool showRotationCenter)
    {
        if (Animating) return;
        FinishSmoothZoom();
        Owner->Widgets.ClearHighlights();
        CancelBoxSelection();
        SetCurrentRenderer(Owner->Renderer);
        UpdateRotationCenterDisplay();
        if (showRotationCenter)
        {
            if (CenterAnnotationActor == nullptr) AddAnnotationForCenter3D();
        }
        else RemoveAnnotationForCenter();
        Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
    }

    void InteractorStyle::EndMultiTouchGesture()
    {
        RemoveAnnotationForCenter();
        Owner->RaiseEvent(VTKN_EVENT_INTERACTOR_MODIFIED, nullptr, 0);
        if (GetInteractor() != nullptr) GetInteractor()->Render();
    }

    void InteractorStyle::InitializeBoxSelection()
    {
        SelectionBackgroundPoints = vtkSmartPointer<vtkPoints>::New();
        SelectionBackgroundPoints->SetNumberOfPoints(4);
        SelectionBackgroundPoints->SetPoint(0, 0.0, 0.0, 0.0);
        SelectionBackgroundPoints->SetPoint(1, 10.5, 0.0, 0.0);
        SelectionBackgroundPoints->SetPoint(2, 10.5, 10.5, 0.0);
        SelectionBackgroundPoints->SetPoint(3, 0.0, 10.5, 0.0);
        vtkNew<vtkCellArray> backgroundPolygon;
        backgroundPolygon->InsertNextCell(4);
        for (int i = 0; i < 4; i++) backgroundPolygon->InsertCellPoint(i);
        vtkNew<vtkPolyData> backgroundPolyData;
        backgroundPolyData->SetPoints(SelectionBackgroundPoints);
        backgroundPolyData->SetPolys(backgroundPolygon);
        vtkNew<vtkPolyDataMapper2D> backgroundMapper;
        backgroundMapper->SetInputData(backgroundPolyData);
        SelectionBackgroundActor = vtkSmartPointer<vtkActor2D>::New();
        SelectionBackgroundActor->SetMapper(backgroundMapper);
        SelectionBackgroundActor->GetProperty()->SetOpacity(SurfaceFillOpacity);
        SelectionBackgroundActor->VisibilityOff();
        SelectionRenderer->AddActor(SelectionBackgroundActor);
        vtkNew<vtkCellArray> borderPolygon;
        borderPolygon->InsertNextCell(5);
        for (int i : { 0, 1, 2, 3, 0 }) borderPolygon->InsertCellPoint(i);
        vtkNew<vtkPolyData> borderPolyData;
        borderPolyData->SetPoints(SelectionBackgroundPoints);
        borderPolyData->SetLines(borderPolygon);
        vtkNew<vtkPolyDataMapper2D> borderMapper;
        borderMapper->SetInputData(borderPolyData);
        SelectionBorderActor = vtkSmartPointer<vtkActor2D>::New();
        SelectionBorderActor->SetMapper(borderMapper);
        SelectionBorderActor->GetProperty()->SetOpacity(WidgetBorderOpacity);
        SelectionBorderActor->GetProperty()->SetLineWidth(WidgetBorderLineWidth);
        SelectionBorderActor->VisibilityOff();
        SelectionRenderer->AddActor(SelectionBorderActor);
        SetSelectionBoxDefaultColors();
    }

    void InteractorStyle::SetSelectionBoxDefaultColors()
    {
        SelectionBorderActor->GetProperty()->SetColor(BorderColor[0], BorderColor[1], BorderColor[2]);
        SelectionBackgroundActor->GetProperty()->SetColor(FillColor[0], FillColor[1], FillColor[2]);
    }

    // A box drawn from right to left selects what it touches and is green
    void InteractorStyle::DrawBoxSelection(int x, int y)
    {
        if (!HasClickPosWorld) return;
        double clickPos[2];
        WorldToDisplay(ClickPosWorld, clickPos);
        SelectionBackgroundActor->VisibilityOn();
        SelectionBorderActor->VisibilityOn();
        if (clickPos[0] > x)
        {
            SelectionBorderActor->GetProperty()->SetColor(0.3, 1.0, 0.3);
            SelectionBackgroundActor->GetProperty()->SetColor(0.2, 1, 0.5);
        }
        else SetSelectionBoxDefaultColors();
        int id1 = 1;
        int id2 = 2;
        int id3 = 3;
        if ((x < clickPos[0] && y > clickPos[1]) || (x > clickPos[0] && y < clickPos[1]))
        {
            id1 = 3;
            id3 = 1;
        }
        SelectionBackgroundPoints->SetPoint(0, clickPos[0], clickPos[1], 0.0);
        SelectionBackgroundPoints->SetPoint(id1, x, clickPos[1], 0.0);
        SelectionBackgroundPoints->SetPoint(id2, x, y, 0.0);
        SelectionBackgroundPoints->SetPoint(id3, clickPos[0], y, 0.0);
        SelectionBackgroundPoints->Modified();
        GetInteractor()->Render();
    }

    void InteractorStyle::CancelBoxSelection()
    {
        if (!BoxSelection) return;
        SelectionCanceled = true;
        HasClickPos = false;
        HasClickPosWorld = false;
        BoxSelection = false;
        SetBoxZoomEnabled(false);
        SelectionBackgroundActor->VisibilityOff();
        SelectionBorderActor->VisibilityOff();
        Owner->RaiseEvent(VTKN_EVENT_CLEAR_MOUSE_SELECTION, nullptr, 0);
        GetInteractor()->Render();
    }

    // The orange cross through the rotation center while the camera turns
    void InteractorStyle::AddAnnotationForCenter3D()
    {
        if (CenterAnnotationActor != nullptr) RemoveAnnotationForCenter();
        if (!HasRotationCenterWorld) return;
        vtkRenderer* renderer = Owner->Renderer;
        int* size = renderer->GetSize();
        double minSize = std::min(size[0], size[1]);
        double scale = renderer->GetActiveCamera()->GetParallelScale();
        double dMin = 30 / minSize * scale;
        double dMax = 100 / minSize * scale;
        const double* c = RotationCenterWorld;
        vtkNew<vtkPoints> points;
        points->InsertNextPoint(c[0] + dMin, c[1], c[2]);
        points->InsertNextPoint(c[0] + dMax, c[1], c[2]);
        points->InsertNextPoint(c[0] - dMin, c[1], c[2]);
        points->InsertNextPoint(c[0] - dMax, c[1], c[2]);
        points->InsertNextPoint(c[0], c[1] + dMin, c[2]);
        points->InsertNextPoint(c[0], c[1] + dMax, c[2]);
        points->InsertNextPoint(c[0], c[1] - dMin, c[2]);
        points->InsertNextPoint(c[0], c[1] - dMax, c[2]);
        points->InsertNextPoint(c[0], c[1], c[2] + dMax * 1.3);
        points->InsertNextPoint(c[0], c[1], c[2] - dMax * 0.7);
        vtkNew<vtkCellArray> lines;
        for (int i = 0; i < 5; i++)
        {
            lines->InsertNextCell(2);
            lines->InsertCellPoint(2 * i);
            lines->InsertCellPoint(2 * i + 1);
        }
        vtkNew<vtkPolyData> polyData;
        polyData->SetPoints(points);
        polyData->SetLines(lines);
        vtkNew<vtkPolyDataMapper> mapper;
        mapper->SetInputData(polyData);
        CenterAnnotationActor = vtkSmartPointer<vtkActor>::New();
        CenterAnnotationActor->SetMapper(mapper);
        CenterAnnotationActor->GetProperty()->SetColor(1, 0.7, 0);
        renderer->AddActor(CenterAnnotationActor);
    }

    // Removed first, so that it does not count in the bounds of the model
    void InteractorStyle::RemoveAnnotationForCenter()
    {
        if (CenterAnnotationActor == nullptr) return;
        Owner->Renderer->RemoveActor(CenterAnnotationActor);
        CenterAnnotationActor = nullptr;
    }
}
