// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxCoorSysWidget: the triad of the global axes in a corner of the view. The cones, the negative ends
// and the origin are click targets for the standard views and a shaft turns its axis to the vertical; the negative
// axes show while the mouse is near the triad.
#pragma once

#include "vtkNativeBorderWidget.h"
#include "vtkNativeCoorSysGeometry.h"

#include <vtkTextMapper.h>
#include <vtkUnsignedCharArray.h>

#include <functional>

namespace vtkn
{
    class CoorSysWidget : public BorderWidget
    {
    public:
        CoorSysWidget();

        void SetRenderer(vtkRenderer* renderer) override;
        void RemoveRenderer() override;
        void VisibilityOn() override;
        void VisibilityOff() override;
        void BringToFront() override;
        void OnRenderWindowModified() override;
        void CameraModified() override;
        void MouseRotate(double azimuthAngle, double elevationAngle) override;
        void OnSizeChanged() override;
        void UpdateBorderGeometry() override;
        bool LeftButtonPress(int x, int y, int clicks) override;
        bool LeftButtonRelease(int x, int y) override;
        bool RightButtonPress(int x, int y) override;
        bool MouseMove(int x, int y) override;
        bool MouseOver(int x, int y) override;
        bool UpdateMouseProximity(int x, int y) override;
        bool UpdateMouseHover(int x, int y, bool mouseOver, bool highlightEntireWidget) override;
        bool ClearMouseProximity() override;
        bool ClearMouseHover() override;

        void SetTextStyle(const vtkn_text_style& style);
        void UpdateGeometry();
        vtkn_triad_target GetTargetAtPosition(int x, int y);
        void SetTargetClickedHandler(std::function<void(vtkn_triad_target)> handler) { TargetClicked = handler; }
        void SetHoverChangedHandler(std::function<void(bool)> handler) { HoverChanged = handler; }

    protected:
        Rect GetDisplayRectangle() override;
        Rect GetHighlightRectangle(int x, int y) override;

    private:
        void GenerateGeometry(bool force);
        void GenerateShafts();
        void GenerateEnds();
        void PositionLabels();
        void PlaceActors();
        const int* GetLabelSize(int axisId);
        int GetMaxLabelSize();
        void GetLocalCenter(double center[2]) const;
        bool GetDisplayPosition(double position[2]) const;
        double GetEndHitRadius(int endId) const;
        Rect GetTargetRectangle(vtkn_triad_target target);
        bool GetAxisBand(int endId, double bandOrigin[2], double direction[2], double& length);
        bool IsOnAxisBand(int endId, int x, int y, double& distanceFromAxis);
        int GetAxisBandEndId(int x, int y);
        bool GetAxisBandCorners(int endId, double corners[4][2]);
        bool SetNegativeAxesVisible(bool visible);
        void SetHovered(bool hovered);
        std::vector<vtkActor2D*> GetActors() const;
        static int GetEndId(vtkn_triad_target target);
        static bool IsVerticalTarget(vtkn_triad_target target);

        CoorSysGeometry Geometry;
        double PositiveHitRadius = 11;
        double NegativeHitRadius = 8;
        double OriginHitRadius = 7;
        double AxisHitHalfWidth = 9;
        double AxisHitMargin = 4;
        double BoundsPadding = 5;
        int LabelSizes[CoorSysGeometry::NumOfAxes][2] = {};
        bool LabelSizeKnown[CoorSysGeometry::NumOfAxes] = {};
        vtkMTimeType CameraModifiedTime = 0;
        bool GeometryGenerated = false;
        bool Hovered = false;
        vtkn_triad_target PressedTarget = VTKN_TRIAD_NONE;
        double LabelOffsets[CoorSysGeometry::NumOfAxes][2] = {};
        std::function<void(vtkn_triad_target)> TargetClicked;
        std::function<void(bool)> HoverChanged;
        //
        vtkSmartPointer<vtkTextMapper> LabelMappers[CoorSysGeometry::NumOfAxes];
        vtkSmartPointer<vtkActor2D> LabelActors[CoorSysGeometry::NumOfAxes];
        vtkSmartPointer<vtkPoints> ShaftPoints;
        vtkSmartPointer<vtkCellArray> ShaftLines;
        vtkSmartPointer<vtkUnsignedCharArray> ShaftColors;
        vtkSmartPointer<vtkPolyData> ShaftPolyData;
        vtkSmartPointer<vtkActor2D> ShaftActor;
        vtkSmartPointer<vtkPoints> EndPoints;
        vtkSmartPointer<vtkCellArray> EndPolygons;
        vtkSmartPointer<vtkUnsignedCharArray> EndColors;
        vtkSmartPointer<vtkPolyData> EndPolyData;
        vtkSmartPointer<vtkActor2D> EndActor;
    };
}
