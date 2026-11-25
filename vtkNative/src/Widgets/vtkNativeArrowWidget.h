// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxTextWithArrowWidget: a text box with a leader line and an arrow head that points at an anchor
// point of the model. The box keeps its top left corner in the plane through the anchor that faces the camera,
// so it follows the model when the camera moves; a drag moves that corner.
#pragma once

#include "vtkNativeTextWidget.h"

#include <vtkGlyph3D.h>

namespace vtkn
{
    class ArrowWidget : public TextWidget
    {
    public:
        ArrowWidget();

        void SetRenderer(vtkRenderer* renderer) override;
        void RemoveRenderer() override;
        void VisibilityOn() override;
        void VisibilityOff() override;
        void BringToFront() override;
        void OnRenderWindowModified() override;
        void OnSizeChanged() override;
        void CameraModified() override;
        void MiddleButtonPress(int x, int y) override;
        void MiddleButtonRelease(int x, int y) override;
        bool MouseMove(int x, int y) override;
        void MousePan(int dx, int dy) override;
        void MouseRotate(double azimuthAngle, double elevationAngle) override;
        void MouseWheelScrolled() override;
        void SetText(const std::string& text) override;

        void SetAnchorPoint(double x, double y, double z);
        void ResetInitialPosition() { AnchorAlreadySet = false; }
        void ResetPositionFromAnchor();
        void OffsetPosition(double x, double y);
        // The leader from the box to the arrow head, in display coordinates: x0, y0, x1, y1
        void GetLeaderLine(double line4[4]);

    private:
        void RecomputeLeader();
        void RecomputeLeaderStart(double start[2]);
        void RecomputeLeaderEnd(double end[2]);
        void SetPositionFromWorldTopLeft();
        void SetWorldTopLeftFromPosition();
        void SetWorldTopLeftFromDisplayPoint(const double displayTopLeft[2]);
        void WorldToDisplay(const double world[3], double display[2]);

        bool AnchorAlreadySet = false;
        double WorldAnchorPoint[3] = { 0, 0, 0 };
        double WorldTopLeftPoint[3] = { 0, 0, 0 };
        vtkSmartPointer<vtkPolyData> LeaderPolyData;
        vtkSmartPointer<vtkActor2D> LeaderActor;
        vtkSmartPointer<vtkPolyData> HeadPolyData;
        vtkSmartPointer<vtkGlyph3D> HeadGlyph;
        vtkSmartPointer<vtkActor2D> HeadActor;
    };
}
