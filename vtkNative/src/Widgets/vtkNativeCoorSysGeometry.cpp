// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeCoorSysGeometry.h"

#include <vtkCamera.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    namespace
    {
        const double Pi = 3.14159265358979323846;
        const int NumOfCircleSegments = 24;
        const double NegativeShaftLengthRatio = 2.0 / 3.0;
        const double ConeHeightRatio = 14.0 / CoorSysGeometry::DefaultShaftLength;
        const double ConeRadiusRatio = 3.5 / CoorSysGeometry::DefaultShaftLength;
        const double ConeTargetRatio = 0.75;
        const double OriginRadiusRatio = 3.5 / CoorSysGeometry::DefaultShaftLength;
        const double LabelGapRatio = 6.0 / CoorSysGeometry::DefaultShaftLength;
        const double LabelFontSizeRatio = CoorSysGeometry::DefaultLabelFontSize / CoorSysGeometry::DefaultShaftLength;
        const double NegativeOpacity = 0.5;
        const double DimFactor = 0.35;
        const double DimColor[3] = { 0.35, 0.35, 0.35 };
        const double AxisColors[3][3] = { { 1, 0, 0 }, { 0, 0.9, 0 }, { 0, 0, 1 } };

        double Dot(const double a[3], const double b[3])
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }

        void NormalizedCross(const double a[3], const double b[3], const double onFailure[3], double result[3])
        {
            double product[3] = { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
            double length = std::sqrt(Dot(product, product));
            for (int i = 0; i < 3; i++) result[i] = length < 1E-12 ? onFailure[i] : product[i] / length;
        }
    }

    const double CoorSysGeometry::GlobalAxes[3][3] = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

    CoorSysGeometry::CoorSysGeometry()
    {
        SetShaftLength(DefaultShaftLength);
    }

    void CoorSysGeometry::InsertCone(vtkPoints* points, vtkCellArray* polygons, int endId)
    {
        std::array<double, 2> direction = GetNormalizedEndDirection(endId);
        double normal[2] = { -direction[1], direction[0] };
        const double* apex = EndPositions[endId];
        std::array<double, 2> baseCenter = GetConeBaseCenter(endId);
        // The projection of the base circle is an ellipse that is foreshortened along the axis
        double semiAxis = ConeRadius * std::abs(EndDepths[endId]);
        vtkIdType firstPointId = points->GetNumberOfPoints();
        polygons->InsertNextCell(NumOfCircleSegments);
        for (int i = 0; i < NumOfCircleSegments; i++)
        {
            double angle = 2 * Pi * i / NumOfCircleSegments;
            double alongAxis = semiAxis * std::cos(angle);
            double alongNormal = ConeRadius * std::sin(angle);
            points->InsertNextPoint(baseCenter[0] + alongAxis * direction[0] + alongNormal * normal[0],
                                    baseCenter[1] + alongAxis * direction[1] + alongNormal * normal[1], 0);
            polygons->InsertCellPoint(firstPointId + i);
        }
        firstPointId = points->GetNumberOfPoints();
        points->InsertNextPoint(apex[0], apex[1], 0);
        points->InsertNextPoint(baseCenter[0] + ConeRadius * normal[0], baseCenter[1] + ConeRadius * normal[1], 0);
        points->InsertNextPoint(baseCenter[0] - ConeRadius * normal[0], baseCenter[1] - ConeRadius * normal[1], 0);
        polygons->InsertNextCell(3);
        for (int i = 0; i < 3; i++) polygons->InsertCellPoint(firstPointId + i);
    }

    void CoorSysGeometry::InsertDisk(vtkPoints* points, vtkCellArray* polygons, const double diskCenter[2],
                                     double radius)
    {
        vtkIdType firstPointId = points->GetNumberOfPoints();
        polygons->InsertNextCell(NumOfCircleSegments);
        for (int i = 0; i < NumOfCircleSegments; i++)
        {
            double angle = 2 * Pi * i / NumOfCircleSegments;
            points->InsertNextPoint(diskCenter[0] + radius * std::cos(angle), diskCenter[1] + radius * std::sin(angle),
                                    0);
            polygons->InsertCellPoint(firstPointId + i);
        }
    }

    // Math.Round rounds half to even
    void CoorSysGeometry::InsertColor(vtkUnsignedCharArray* colors, const std::array<double, 3>& color, double opacity)
    {
        colors->InsertNextTuple4(std::nearbyint(255 * color[0]), std::nearbyint(255 * color[1]),
                                 std::nearbyint(255 * color[2]), std::nearbyint(255 * opacity));
    }

    void CoorSysGeometry::Update(const double center[2], double centerDepth, const double axisDirections[3][3],
                                 const double right[3], const double up[3], const double viewPlaneNormal[3])
    {
        Center[0] = center[0];
        Center[1] = center[1];
        CenterDepth = centerDepth;
        for (int axisId = 0; axisId < NumOfAxes; axisId++)
        {
            double alongRight = Dot(axisDirections[axisId], right);
            double alongUp = Dot(axisDirections[axisId], up);
            double alongNormal = Dot(axisDirections[axisId], viewPlaneNormal);
            for (int signId = 0; signId < 2; signId++)
            {
                int endId = GetEndId(axisId, signId);
                double sign = signId == 0 ? 1 : -1;
                double shaftLength = IsPositiveEnd(endId) ? ShaftLength : NegativeShaftLength;
                EndDirections[endId][0] = sign * alongRight;
                EndDirections[endId][1] = sign * alongUp;
                EndDepths[endId] = sign * alongNormal;
                EndPositions[endId][0] = Center[0] + shaftLength * EndDirections[endId][0];
                EndPositions[endId][1] = Center[1] + shaftLength * EndDirections[endId][1];
            }
        }
    }

    void CoorSysGeometry::AppendOrigin(vtkPoints* points, vtkCellArray* polygons, vtkUnsignedCharArray* colors)
    {
        InsertDisk(points, polygons, Center, OriginRadius);
        InsertColor(colors, GetOriginColor(), 1);
    }

    void CoorSysGeometry::AppendShaft(vtkPoints* points, vtkCellArray* lines, vtkUnsignedCharArray* colors, int endId)
    {
        vtkIdType firstPointId = points->GetNumberOfPoints();
        points->InsertNextPoint(Center[0], Center[1], 0);
        points->InsertNextPoint(EndPositions[endId][0], EndPositions[endId][1], 0);
        lines->InsertNextCell(2);
        lines->InsertCellPoint(firstPointId);
        lines->InsertCellPoint(firstPointId + 1);
        InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
    }

    void CoorSysGeometry::AppendEnd(vtkPoints* points, vtkCellArray* polygons, vtkUnsignedCharArray* colors, int endId)
    {
        if (IsPositiveEnd(endId))
        {
            InsertCone(points, polygons, endId);
            InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
            InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
        }
        else
        {
            InsertDisk(points, polygons, EndPositions[endId], NegativeEndRadius);
            InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
        }
    }

    double CoorSysGeometry::GetEndProjectedLength(int endId) const
    {
        double dx = EndPositions[endId][0] - Center[0];
        double dy = EndPositions[endId][1] - Center[1];
        return std::sqrt(dx * dx + dy * dy);
    }

    double CoorSysGeometry::GetEndOpacity(int endId) const
    {
        return IsPositiveEnd(endId) ? 1 : NegativeOpacity;
    }

    // Only the ends that point away from the viewer are dimmed, so that the axis colors stay vivid
    std::array<double, 3> CoorSysGeometry::GetEndColor(int endId) const
    {
        const double* color = HasOverrideColor ? OverrideColor : AxisColors[endId / 2];
        double depth = EndDepths[endId];
        double factor = depth < 0 ? -DimFactor * depth : 0;
        return { color[0] + (DimColor[0] - color[0]) * factor, color[1] + (DimColor[1] - color[1]) * factor,
                 color[2] + (DimColor[2] - color[2]) * factor };
    }

    std::array<double, 3> CoorSysGeometry::GetOriginColor() const
    {
        const double* color = HasOverrideColor ? OverrideColor : DimColor;
        return { color[0], color[1], color[2] };
    }

    std::array<int, CoorSysGeometry::NumOfEnds> CoorSysGeometry::GetEndIdsSortedByDepth() const
    {
        std::array<int, NumOfEnds> endIds;
        for (int i = 0; i < NumOfEnds; i++) endIds[i] = i;
        std::stable_sort(endIds.begin(), endIds.end(), [this](int a, int b) { return EndDepths[a] < EndDepths[b]; });
        return endIds;
    }

    // An axis that points towards or away from the viewer has no projected direction
    std::array<double, 2> CoorSysGeometry::GetNormalizedEndDirection(int endId) const
    {
        const double* direction = EndDirections[endId];
        double length = std::sqrt(direction[0] * direction[0] + direction[1] * direction[1]);
        if (length < 0.05) return { 0.7071, -0.7071 };
        return { direction[0] / length, direction[1] / length };
    }

    std::array<double, 2> CoorSysGeometry::GetConeBaseCenter(int endId) const
    {
        return { EndPositions[endId][0] - ConeHeight * EndDirections[endId][0],
                 EndPositions[endId][1] - ConeHeight * EndDirections[endId][1] };
    }

    // The cone is the only clickable part of a positive axis and it sits close to its tip, so that the shaft
    // behind it stays free for the vertical axis target
    std::array<double, 2> CoorSysGeometry::GetEndTargetPosition(int endId) const
    {
        if (!IsPositiveEnd(endId)) return { EndPositions[endId][0], EndPositions[endId][1] };
        std::array<double, 2> baseCenter = GetConeBaseCenter(endId);
        return { baseCenter[0] + ConeTargetRatio * (EndPositions[endId][0] - baseCenter[0]),
                 baseCenter[1] + ConeTargetRatio * (EndPositions[endId][1] - baseCenter[1]) };
    }

    std::array<double, 2> CoorSysGeometry::GetLabelPosition(int axisId, const int labelSize[2]) const
    {
        int endId = GetEndId(axisId, 0);
        std::array<double, 2> direction = GetNormalizedEndDirection(endId);
        double distance = LabelGap + std::max(labelSize[0], labelSize[1]) / 2.0;
        return { EndPositions[endId][0] + distance * direction[0] - labelSize[0] / 2.0,
                 EndPositions[endId][1] + distance * direction[1] - labelSize[1] / 2.0 };
    }

    void CoorSysGeometry::SetShaftLength(double shaftLength)
    {
        ShaftLength = shaftLength;
        NegativeShaftLength = NegativeShaftLengthRatio * shaftLength;
        ConeHeight = ConeHeightRatio * shaftLength;
        ConeRadius = ConeRadiusRatio * shaftLength;
        OriginRadius = OriginRadiusRatio * shaftLength;
        NegativeEndRadius = OriginRadius;
        LabelGap = LabelGapRatio * shaftLength;
    }

    void CoorSysGeometry::SetOverrideColor(const double* color)
    {
        HasOverrideColor = color != nullptr;
        for (int i = 0; i < 3 && HasOverrideColor; i++) OverrideColor[i] = color[i];
    }

    bool CoorSysGeometry::SetNegativeAxesVisible(bool visible)
    {
        if (NegativeAxesVisible == visible) return false;
        NegativeAxesVisible = visible;
        return true;
    }

    int CoorSysGeometry::GetLabelFontSize(double shaftLength)
    {
        int fontSize = static_cast<int>(std::round(LabelFontSizeRatio * shaftLength));
        return fontSize < 1 ? 1 : fontSize;
    }

    void CoorSysGeometry::GetCameraBasis(vtkCamera* camera, double right[3], double up[3], double viewPlaneNormal[3])
    {
        double viewUp[3] = { 0, 1, 0 };
        viewPlaneNormal[0] = 0;
        viewPlaneNormal[1] = 0;
        viewPlaneNormal[2] = 1;
        if (camera != nullptr)
        {
            camera->GetViewPlaneNormal(viewPlaneNormal);
            camera->GetViewUp(viewUp);
        }
        const double xAxis[3] = { 1, 0, 0 };
        const double yAxis[3] = { 0, 1, 0 };
        NormalizedCross(viewUp, viewPlaneNormal, xAxis, right);
        NormalizedCross(viewPlaneNormal, right, yAxis, up);
    }
}
