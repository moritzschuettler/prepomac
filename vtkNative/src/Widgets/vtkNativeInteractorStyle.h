// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkInteractorStyleControl without its widget part (the widget set of the view): the middle button turns
// the camera about the rotation center, with shift it pans and with ctrl it zooms; the wheel zooms at the mouse,
// smoothly over a few frames; a middle click picks the rotation center; the left button draws the box selection
// rubber band and reports the pick; the arrow keys turn the camera, escape cancels the box and z starts a box
// zoom. The view supplies the picking, the camera distance and clipping, and raises the events.
#pragma once

#include "vtkNative.h"

#include <vtkActor.h>
#include <vtkActor2D.h>
#include <vtkCamera.h>
#include <vtkInteractorStyleTrackballCamera.h>
#include <vtkPoints.h>
#include <vtkSmartPointer.h>

#include <chrono>
#include <functional>

namespace vtkn
{
    class View;

    class InteractorStyle : public vtkInteractorStyleTrackballCamera
    {
    public:
        static InteractorStyle* New();
        vtkTypeMacro(InteractorStyle, vtkInteractorStyleTrackballCamera);

        void SetView(View* view, vtkRenderer* overlayRenderer, vtkRenderer* selectionRenderer);
        void SetSettings(const vtkn_interaction_settings& settings);
        void GetSettings(vtkn_interaction_settings& settings) const;

        void OnLeftButtonDown() override;
        void OnLeftButtonUp() override;
        void OnMiddleButtonDown() override;
        void OnMiddleButtonUp() override;
        void OnRightButtonDown() override;
        void OnRightButtonUp() override;
        void OnMouseMove() override;
        void OnMouseWheelForward() override;
        void OnMouseWheelBackward() override;
        void OnKeyPress() override;
        void OnKeyRelease() override;
        void OnChar() override {}
        void OnTimer() override;
        void OnLeave() override;

        void Rotate() override;
        void Pan() override;
        void Zoom() override;

        // The navigation the touchpad and the trackpad also use
        void Rotate(double azimuthAngle, double elevationAngle, double rollAngle, bool render);
        void RotateByDisplayDelta(double dx, double dy, bool render);
        void PanByDisplayDelta(double dx, double dy, bool render);
        void ZoomAtDisplayPoint(double parallelScaleFactor, int x, int y, bool render);
        void ZoomAtDisplayPointSmooth(double parallelScaleFactor, int x, int y);
        void FinishSmoothZoom();
        // One frame of the smooth zoom; true while frames remain. The embedded view steps it from its timer, a
        // host that draws the view off screen calls it itself
        bool StepSmoothZoom();
        void StartMultiTouchGesture(int x, int y, bool showRotationCenter);
        void EndMultiTouchGesture();
        void SetRotationPoint(const double* point);
        void ResetRotationCenter();
        void UpdateRotationCenterDisplay();
        void SetAnimating(bool animating);
        bool GetAnimating() const { return Animating; }
        bool IsBoxSelectionActive() const { return BoxSelection; }
        bool GetBoxZoomEnabled() const { return BoxZoomEnabled; }
        void SetBoxZoomEnabled(bool enabled);
        bool HasSmoothZoomPending() const { return SmoothZoomRemainingFactor != 1; }
        // The selection timer of the C# style: the mouse move pick 200 ms after the mouse stopped
        void FireSelectionTimer();

    protected:
        InteractorStyle();
        ~InteractorStyle() override;

    private:
        void PanCamera(vtkCamera* camera, const double posStart[3], const double posEnd[3]);
        void DisplayToWorld(const double display[2], double world[3]);
        void WorldToDisplay(const double world[3], double display[2]);
        double GetMouseWheelZoomFactor();
        void ApplyRemainingSmoothZoom();
        void ZoomAtDisplayPointTimed(double parallelScaleFactor, int x, int y);
        void DrawBoxSelection(int x, int y);
        void CancelBoxSelection();
        void SetSelectionBoxDefaultColors();
        void InitializeBoxSelection();
        void AddAnnotationForCenter3D();
        void RemoveAnnotationForCenter();
        bool ComputeVisiblePropBounds(double bounds[6], bool withOverlay);
        void StartSelectionTimer();
        void StopSelectionTimer();
        void StartSmoothZoomTimer();
        void StopSmoothZoomTimer();
        void InteractionHappened();

        View* Owner = nullptr;
        vtkRenderer* OverlayRenderer = nullptr;
        vtkRenderer* SelectionRenderer = nullptr;
        // vtkInteractorStyleControl
        double MotionFactorValue = 10.0;
        double RotateSensitivity = 1;
        double ZoomSensitivity = 1;
        bool InvertZoom = false;
        bool SmoothZoom = true;
        int SelectionDistance = 5;
        double SmoothZoomRemainingFactor = 1;
        int SmoothZoomX = 0;
        int SmoothZoomY = 0;
        bool SmoothZoomStepping = false;
        double SmoothZoomFrameMilliseconds = 0;
        int SmoothZoomTimerId = -1;
        int SelectionTimerId = -1;
        bool SelectionTimerPending = false;
        bool HasRotationCenterWorld = false;
        double RotationCenterWorld[3] = { 0, 0, 0 };
        bool HasRotationCenterDisplay = false;
        double RotationCenterDisplay[2] = { 0, 0 };
        bool HasClickPos = false;
        int ClickPos[2] = { 0, 0 };
        bool HasClickPosWorld = false;
        double ClickPosWorld[3] = { 0, 0, 0 };
        vtkSmartPointer<vtkActor> CenterAnnotationActor;
        bool SelectionBoxEnabled = true;
        bool BoxZoomEnabled = false;
        bool Animating = false;
        int X = 0;
        int Y = 0;
        int NumOfClicks = 1;
        int LastX = -2147483647;
        int LastY = -2147483647;
        std::chrono::steady_clock::time_point LastClickTime;
        std::chrono::milliseconds DoubleClickTime;
        int DoubleClickDisplacement = 5;
        bool LeftMouseButtonPressed = false;
        bool BoxSelection = false;
        bool SelectionCanceled = false;
        vtkSmartPointer<vtkPoints> SelectionBackgroundPoints;
        vtkSmartPointer<vtkActor2D> SelectionBackgroundActor;
        vtkSmartPointer<vtkActor2D> SelectionBorderActor;
    };
}
