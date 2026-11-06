// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeCoorSysWidget.h"
#include "vtkNativeTextWidget.h"

#include <vtkCamera.h>
#include <vtkCellData.h>
#include <vtkNew.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty2D.h>
#include <vtkTextProperty.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    namespace
    {
        const int NumOfAxes = CoorSysGeometry::NumOfAxes;
        const int NumOfEnds = CoorSysGeometry::NumOfEnds;

        vtkn_triad_target GetTarget(int endId)
        {
            return static_cast<vtkn_triad_target>(VTKN_TRIAD_X_POSITIVE + endId);
        }

        vtkn_triad_target GetVerticalTarget(int endId)
        {
            return static_cast<vtkn_triad_target>(VTKN_TRIAD_X_POSITIVE_VERTICAL + endId);
        }

        vtkSmartPointer<vtkActor2D> CreateColoredActor(vtkPolyData* polyData)
        {
            vtkNew<vtkPolyDataMapper2D> mapper;
            mapper->SetInputData(polyData);
            mapper->SetScalarModeToUseCellData();
            mapper->SetColorModeToDefault();
            mapper->ScalarVisibilityOn();
            vtkSmartPointer<vtkActor2D> actor = vtkSmartPointer<vtkActor2D>::New();
            actor->SetMapper(mapper);
            return actor;
        }
    }

    CoorSysWidget::CoorSysWidget()
    {
        double side = 2 * (Geometry.GetShaftLength() + Geometry.GetConeRadius() + 2);
        Size[0] = side;
        Size[1] = side;
        // Labels
        const char* labels[NumOfAxes] = { "X", "Y", "Z" };
        vtkNew<vtkTextProperty> textProperty;
        for (int i = 0; i < NumOfAxes; i++)
        {
            LabelMappers[i] = vtkSmartPointer<vtkTextMapper>::New();
            LabelMappers[i]->SetTextProperty(textProperty);
            LabelMappers[i]->SetInput(labels[i]);
            LabelActors[i] = vtkSmartPointer<vtkActor2D>::New();
            LabelActors[i]->SetMapper(LabelMappers[i]);
            LabelActors[i]->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
            LabelActors[i]->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
            LabelActors[i]->GetPositionCoordinate()->SetValue(0, 0);
        }
        // Shafts
        ShaftPoints = vtkSmartPointer<vtkPoints>::New();
        ShaftLines = vtkSmartPointer<vtkCellArray>::New();
        ShaftColors = vtkSmartPointer<vtkUnsignedCharArray>::New();
        ShaftColors->SetNumberOfComponents(4);
        ShaftPolyData = vtkSmartPointer<vtkPolyData>::New();
        ShaftPolyData->SetPoints(ShaftPoints);
        ShaftPolyData->SetLines(ShaftLines);
        ShaftPolyData->GetCellData()->SetScalars(ShaftColors);
        ShaftActor = CreateColoredActor(ShaftPolyData);
        ShaftActor->GetProperty()->SetLineWidth(2.0f);
        // Ends
        EndPoints = vtkSmartPointer<vtkPoints>::New();
        EndPolygons = vtkSmartPointer<vtkCellArray>::New();
        EndColors = vtkSmartPointer<vtkUnsignedCharArray>::New();
        EndColors->SetNumberOfComponents(4);
        EndPolyData = vtkSmartPointer<vtkPolyData>::New();
        EndPolyData->SetPoints(EndPoints);
        EndPolyData->SetPolys(EndPolygons);
        EndPolyData->GetCellData()->SetScalars(EndColors);
        EndActor = CreateColoredActor(EndPolyData);
        for (vtkActor2D* actor : { ShaftActor.GetPointer(), EndActor.GetPointer() })
        {
            actor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
            actor->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
        }
    }

    std::vector<vtkActor2D*> CoorSysWidget::GetActors() const
    {
        std::vector<vtkActor2D*> actors = { ShaftActor, EndActor };
        for (const auto& actor : LabelActors) actors.push_back(actor);
        return actors;
    }

    void CoorSysWidget::GenerateGeometry(bool force)
    {
        if (Renderer == nullptr) return;
        int* size = Renderer->GetSize();
        if (size[0] == 0 && size[1] == 0) return;
        //
        vtkCamera* camera = Renderer->GetActiveCamera();
        vtkMTimeType cameraModifiedTime = camera == nullptr ? 0 : camera->GetMTime();
        if (!force && GeometryGenerated && cameraModifiedTime == CameraModifiedTime) return;
        CameraModifiedTime = cameraModifiedTime;
        // The widget always shows the global axes at the center of its own display rectangle
        double right[3];
        double up[3];
        double viewPlaneNormal[3];
        CoorSysGeometry::GetCameraBasis(camera, right, up, viewPlaneNormal);
        double center[2];
        GetLocalCenter(center);
        Geometry.Update(center, 0, CoorSysGeometry::GlobalAxes, right, up, viewPlaneNormal);
        GenerateShafts();
        GenerateEnds();
        PositionLabels();
        PlaceActors();
        GeometryGenerated = true;
    }

    // The ends closer to the viewer are drawn over the ends behind them
    void CoorSysWidget::GenerateShafts()
    {
        ShaftPoints->Reset();
        ShaftLines->Reset();
        ShaftColors->Reset();
        for (int endId : Geometry.GetEndIdsSortedByDepth())
        {
            if (Geometry.IsEndVisible(endId)) Geometry.AppendShaft(ShaftPoints, ShaftLines, ShaftColors, endId);
        }
        ShaftPoints->Modified();
        ShaftLines->Modified();
        ShaftColors->Modified();
        ShaftPolyData->Modified();
    }

    void CoorSysWidget::GenerateEnds()
    {
        EndPoints->Reset();
        EndPolygons->Reset();
        EndColors->Reset();
        Geometry.AppendOrigin(EndPoints, EndPolygons, EndColors);
        for (int endId : Geometry.GetEndIdsSortedByDepth())
        {
            if (Geometry.IsEndVisible(endId)) Geometry.AppendEnd(EndPoints, EndPolygons, EndColors, endId);
        }
        EndPoints->Modified();
        EndPolygons->Modified();
        EndColors->Modified();
        EndPolyData->Modified();
    }

    void CoorSysWidget::PositionLabels()
    {
        for (int axisId = 0; axisId < NumOfAxes; axisId++)
        {
            std::array<double, 2> labelPosition = Geometry.GetLabelPosition(axisId, GetLabelSize(axisId));
            LabelOffsets[axisId][0] = labelPosition[0];
            LabelOffsets[axisId][1] = labelPosition[1];
        }
    }

    void CoorSysWidget::PlaceActors()
    {
        const double origin[2] = { 0, 0 };
        PlaceActor(ShaftActor->GetPositionCoordinate(), origin, false);
        PlaceActor(EndActor->GetPositionCoordinate(), origin, false);
        for (int axisId = 0; axisId < NumOfAxes; axisId++)
            PlaceActor(LabelActors[axisId]->GetPositionCoordinate(), LabelOffsets[axisId], true);
    }

    void CoorSysWidget::UpdateBorderGeometry()
    {
        BorderWidget::UpdateBorderGeometry();
        PlaceActors();
    }

    const int* CoorSysWidget::GetLabelSize(int axisId)
    {
        if (!LabelSizeKnown[axisId])
        {
            TextWidget::GetTextSize(LabelMappers[axisId], Renderer, LabelSizes[axisId]);
            LabelSizeKnown[axisId] = true;
        }
        return LabelSizes[axisId];
    }

    int CoorSysWidget::GetMaxLabelSize()
    {
        int maxLabelSize = 0;
        for (int axisId = 0; axisId < NumOfAxes; axisId++)
        {
            const int* labelSize = GetLabelSize(axisId);
            maxLabelSize = std::max(maxLabelSize, std::max(labelSize[0], labelSize[1]));
        }
        return maxLabelSize;
    }

    void CoorSysWidget::GetLocalCenter(double center[2]) const
    {
        center[0] = Size[0] / 2;
        center[1] = Size[1] / 2;
    }

    bool CoorSysWidget::GetDisplayPosition(double position[2]) const
    {
        if (!Visibility || Renderer == nullptr) return false;
        position[0] = std::floor(Position[0]);
        position[1] = std::floor(PositionY(Renderer->GetSize()));
        return true;
    }

    double CoorSysWidget::GetEndHitRadius(int endId) const
    {
        return CoorSysGeometry::IsPositiveEnd(endId) ? PositiveHitRadius : NegativeHitRadius;
    }

    Rect CoorSysWidget::GetTargetRectangle(vtkn_triad_target target)
    {
        double position[2];
        if (target == VTKN_TRIAD_NONE || IsVerticalTarget(target) || !GetDisplayPosition(position)) return Rect();
        //
        double localPosition[2];
        double radius;
        if (target == VTKN_TRIAD_ORIGIN)
        {
            GetLocalCenter(localPosition);
            radius = OriginHitRadius;
        }
        else
        {
            std::array<double, 2> endPosition = Geometry.GetEndTargetPosition(GetEndId(target));
            localPosition[0] = endPosition[0];
            localPosition[1] = endPosition[1];
            radius = GetEndHitRadius(GetEndId(target));
        }
        double centerX = position[0] + localPosition[0];
        double centerY = position[1] + localPosition[1];
        return Rect::FromLTRB(static_cast<int>(std::floor(centerX - radius)),
                              static_cast<int>(std::floor(centerY - radius)),
                              static_cast<int>(std::ceil(centerX + radius)),
                              static_cast<int>(std::ceil(centerY + radius)));
    }

    bool CoorSysWidget::GetAxisBand(int endId, double bandOrigin[2], double direction[2], double& length)
    {
        double position[2];
        if (!GetDisplayPosition(position) || !GeometryGenerated || !Geometry.IsEndVisible(endId)) return false;
        double center[2];
        GetLocalCenter(center);
        bandOrigin[0] = position[0] + center[0];
        bandOrigin[1] = position[1] + center[1];
        std::array<double, 2> endDirection = Geometry.GetNormalizedEndDirection(endId);
        direction[0] = endDirection[0];
        direction[1] = endDirection[1];
        length = Geometry.GetEndProjectedLength(endId) + AxisHitMargin;
        return true;
    }

    bool CoorSysWidget::IsOnAxisBand(int endId, int x, int y, double& distanceFromAxis)
    {
        distanceFromAxis = 0;
        double bandOrigin[2];
        double direction[2];
        double length;
        if (!GetAxisBand(endId, bandOrigin, direction, length)) return false;
        double dx = x - bandOrigin[0];
        double dy = y - bandOrigin[1];
        double alongAxis = dx * direction[0] + dy * direction[1];
        distanceFromAxis = std::abs(dy * direction[0] - dx * direction[1]);
        return alongAxis >= 0 && alongAxis <= length && distanceFromAxis <= AxisHitHalfWidth;
    }

    // The axis that runs the closest to the mouse wins where the bands overlap near the origin
    int CoorSysWidget::GetAxisBandEndId(int x, int y)
    {
        int closestEndId = -1;
        double closestDistance = 0;
        for (int endId = 0; endId < NumOfEnds; endId++)
        {
            double distanceFromAxis;
            if (!IsOnAxisBand(endId, x, y, distanceFromAxis)) continue;
            if (closestEndId < 0 || distanceFromAxis < closestDistance)
            {
                closestEndId = endId;
                closestDistance = distanceFromAxis;
            }
        }
        return closestEndId;
    }

    // A band that follows the axis direction stays as narrow as the axis it covers
    bool CoorSysWidget::GetAxisBandCorners(int endId, double corners[4][2])
    {
        double bandOrigin[2];
        double direction[2];
        double length;
        if (!GetAxisBand(endId, bandOrigin, direction, length)) return false;
        double normal[2] = { -direction[1], direction[0] };
        double tipX = bandOrigin[0] + length * direction[0];
        double tipY = bandOrigin[1] + length * direction[1];
        double offsetX = AxisHitHalfWidth * normal[0];
        double offsetY = AxisHitHalfWidth * normal[1];
        const double values[4][2] = { { bandOrigin[0] + offsetX, bandOrigin[1] + offsetY },
                                      { tipX + offsetX, tipY + offsetY },
                                      { tipX - offsetX, tipY - offsetY },
                                      { bandOrigin[0] - offsetX, bandOrigin[1] - offsetY } };
        for (int i = 0; i < 4; i++)
        {
            corners[i][0] = values[i][0];
            corners[i][1] = values[i][1];
        }
        return true;
    }

    bool CoorSysWidget::SetNegativeAxesVisible(bool visible)
    {
        if (!Geometry.SetNegativeAxesVisible(visible)) return false;
        GenerateGeometry(true);
        return true;
    }

    int CoorSysWidget::GetEndId(vtkn_triad_target target)
    {
        if (target >= VTKN_TRIAD_X_POSITIVE_VERTICAL) return target - VTKN_TRIAD_X_POSITIVE_VERTICAL;
        return target - VTKN_TRIAD_X_POSITIVE;
    }

    bool CoorSysWidget::IsVerticalTarget(vtkn_triad_target target)
    {
        return target >= VTKN_TRIAD_X_POSITIVE_VERTICAL;
    }

    void CoorSysWidget::SetHovered(bool hovered)
    {
        if (Hovered == hovered) return;
        Hovered = hovered;
        if (HoverChanged) HoverChanged(Hovered);
    }

    // The end closest to the viewer wins when the projections overlap
    vtkn_triad_target CoorSysWidget::GetTargetAtPosition(int x, int y)
    {
        if (!Visibility || !GeometryGenerated) return VTKN_TRIAD_NONE;
        double position[2];
        if (!GetDisplayPosition(position)) return VTKN_TRIAD_NONE;
        int closestEndId = -1;
        for (int endId = 0; endId < NumOfEnds; endId++)
        {
            if (!Geometry.IsEndVisible(endId)) continue;
            std::array<double, 2> endTargetPosition = Geometry.GetEndTargetPosition(endId);
            double hitRadius = GetEndHitRadius(endId);
            double dx = position[0] + endTargetPosition[0] - x;
            double dy = position[1] + endTargetPosition[1] - y;
            if (dx * dx + dy * dy > hitRadius * hitRadius) continue;
            if (closestEndId < 0 || Geometry.GetEndDepth(endId) > Geometry.GetEndDepth(closestEndId))
                closestEndId = endId;
        }
        if (closestEndId >= 0) return GetTarget(closestEndId);
        //
        double center[2];
        GetLocalCenter(center);
        double dxOrigin = position[0] + center[0] - x;
        double dyOrigin = position[1] + center[1] - y;
        if (dxOrigin * dxOrigin + dyOrigin * dyOrigin <= OriginHitRadius * OriginHitRadius) return VTKN_TRIAD_ORIGIN;
        // The shaft of an axis turns that axis towards the vertical direction
        int bandEndId = GetAxisBandEndId(x, y);
        if (bandEndId >= 0) return GetVerticalTarget(bandEndId);
        return VTKN_TRIAD_NONE;
    }

    void CoorSysWidget::OnRenderWindowModified()
    {
        if (!Visibility) return;
        BorderWidget::OnRenderWindowModified();
        OnSizeChanged();
    }

    void CoorSysWidget::CameraModified()
    {
        if (!Visibility) return;
        GenerateGeometry(false);
    }

    void CoorSysWidget::MouseRotate(double, double)
    {
        if (!Visibility) return;
        GenerateGeometry(false);
    }

    void CoorSysWidget::UpdateGeometry()
    {
        if (Visibility) GenerateGeometry(false);
    }

    void CoorSysWidget::OnSizeChanged()
    {
        GenerateGeometry(true);
        OnMovedOrSizeChanged();
    }

    bool CoorSysWidget::LeftButtonPress(int x, int y, int)
    {
        PressedTarget = GetTargetAtPosition(x, y);
        return PressedTarget != VTKN_TRIAD_NONE;
    }

    bool CoorSysWidget::LeftButtonRelease(int x, int y)
    {
        if (PressedTarget == VTKN_TRIAD_NONE) return false;
        vtkn_triad_target pressedTarget = PressedTarget;
        PressedTarget = VTKN_TRIAD_NONE;
        if (GetTargetAtPosition(x, y) == pressedTarget && TargetClicked) TargetClicked(pressedTarget);
        return true;
    }

    bool CoorSysWidget::RightButtonPress(int, int)
    {
        return false;
    }

    bool CoorSysWidget::MouseMove(int, int)
    {
        return false;
    }

    bool CoorSysWidget::MouseOver(int x, int y)
    {
        return GetTargetAtPosition(x, y) != VTKN_TRIAD_NONE;
    }

    bool CoorSysWidget::UpdateMouseProximity(int x, int y)
    {
        return SetNegativeAxesVisible(GetDisplayRectangle().Contains(x, y));
    }

    // The entire axis is highlighted for a vertical target, which is not a rectangle on the display
    bool CoorSysWidget::UpdateMouseHover(int x, int y, bool mouseOver, bool)
    {
        vtkn_triad_target target = mouseOver ? GetTargetAtPosition(x, y) : VTKN_TRIAD_NONE;
        bool highlightChanged;
        if (IsVerticalTarget(target))
        {
            double corners[4][2];
            bool found = GetAxisBandCorners(GetEndId(target), corners);
            highlightChanged = Highlight.SetCorners(found ? corners : nullptr, WidgetHighlight::WidgetFillOpacity);
        }
        else highlightChanged = Highlight.SetRectangle(GetTargetRectangle(target), WidgetHighlight::WidgetFillOpacity);
        SetHovered(target != VTKN_TRIAD_NONE);
        return highlightChanged;
    }

    bool CoorSysWidget::ClearMouseProximity()
    {
        return SetNegativeAxesVisible(false);
    }

    bool CoorSysWidget::ClearMouseHover()
    {
        SetHovered(false);
        return BorderWidget::ClearMouseHover();
    }

    void CoorSysWidget::SetRenderer(vtkRenderer* renderer)
    {
        BorderWidget::SetRenderer(renderer);
        if (Visibility)
        {
            for (vtkActor2D* actor : GetActors()) Renderer->AddActor(actor);
        }
        OnSizeChanged();
    }

    void CoorSysWidget::RemoveRenderer()
    {
        if (Renderer != nullptr)
        {
            for (vtkActor2D* actor : GetActors()) Renderer->RemoveActor(actor);
        }
        BorderWidget::RemoveRenderer();
    }

    void CoorSysWidget::VisibilityOn()
    {
        if (Visibility) return;
        BorderWidget::VisibilityOn();
        for (vtkActor2D* actor : GetActors()) Renderer->AddActor(actor);
    }

    void CoorSysWidget::VisibilityOff()
    {
        if (!Visibility) return;
        BorderWidget::VisibilityOff();
        for (vtkActor2D* actor : GetActors()) Renderer->RemoveActor(actor);
        SetNegativeAxesVisible(false);
        SetHovered(false);
    }

    void CoorSysWidget::BringToFront()
    {
        if (!Visibility || Renderer == nullptr) return;
        BorderWidget::BringToFront();
        for (vtkActor2D* actor : GetActors())
        {
            Renderer->RemoveActor(actor);
            Renderer->AddActor(actor);
        }
    }

    // A square that contains the axis labels in any view orientation
    Rect CoorSysWidget::GetDisplayRectangle()
    {
        Rect rectangle = BorderWidget::GetDisplayRectangle();
        if (rectangle.IsEmpty()) return rectangle;
        double position[2];
        GetDisplayPosition(position);
        double center[2];
        GetLocalCenter(center);
        double halfSide = Geometry.GetShaftLength() + Geometry.GetLabelGap() + GetMaxLabelSize() + BoundsPadding;
        double centerX = position[0] + center[0];
        double centerY = position[1] + center[1];
        return Rect::FromLTRB(static_cast<int>(std::floor(centerX - halfSide)),
                              static_cast<int>(std::floor(centerY - halfSide)),
                              static_cast<int>(std::ceil(centerX + halfSide)),
                              static_cast<int>(std::ceil(centerY + halfSide)));
    }

    Rect CoorSysWidget::GetHighlightRectangle(int x, int y)
    {
        return GetTargetRectangle(GetTargetAtPosition(x, y));
    }

    void CoorSysWidget::SetTextStyle(const vtkn_text_style& style)
    {
        for (int i = 0; i < NumOfAxes; i++)
        {
            TextWidget::ApplyTextStyle(LabelMappers[i]->GetTextProperty(), style);
            LabelSizeKnown[i] = false;
        }
        GenerateGeometry(true);
    }
}
