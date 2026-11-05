// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxCoorSysGeometry: the projection of three axes onto the display with the shafts, the cones of the
// positive ends, the disks of the negative ends and the origin, and the label positions. The triad widget draws
// the global axes with it; the coordinate system symbols of the model can draw their own axes the same way.
#pragma once

#include <vtkCellArray.h>
#include <vtkPoints.h>
#include <vtkUnsignedCharArray.h>

#include <array>

class vtkCamera;

namespace vtkn
{
    class CoorSysGeometry
    {
    public:
        static constexpr int NumOfAxes = 3;
        static constexpr int NumOfEnds = 2 * NumOfAxes;
        static constexpr double DefaultShaftLength = 45;
        // All the proportions are normalized on the widget, whose shaft length is the default one and whose text
        // is the default size; the default symbol size of a model coordinate system maps onto the same length
        static constexpr double DefaultSymbolSize = 50;
        static constexpr double DefaultLabelFontSize = 16;

        CoorSysGeometry();

        void Update(const double center[2], double centerDepth, const double axisDirections[3][3],
                    const double right[3], const double up[3], const double viewPlaneNormal[3]);
        void AppendOrigin(vtkPoints* points, vtkCellArray* polygons, vtkUnsignedCharArray* colors);
        void AppendShaft(vtkPoints* points, vtkCellArray* lines, vtkUnsignedCharArray* colors, int endId);
        void AppendEnd(vtkPoints* points, vtkCellArray* polygons, vtkUnsignedCharArray* colors, int endId);
        bool IsEndVisible(int endId) const { return IsPositiveEnd(endId) || NegativeAxesVisible; }

        double GetShaftLength() const { return ShaftLength; }
        double GetConeRadius() const { return ConeRadius; }
        double GetLabelGap() const { return LabelGap; }
        double GetEndDepth(int endId) const { return EndDepths[endId]; }
        double GetEndProjectedLength(int endId) const;
        double GetEndOpacity(int endId) const;
        std::array<double, 3> GetEndColor(int endId) const;
        std::array<double, 3> GetOriginColor() const;
        std::array<int, NumOfEnds> GetEndIdsSortedByDepth() const;
        std::array<double, 2> GetNormalizedEndDirection(int endId) const;
        std::array<double, 2> GetConeBaseCenter(int endId) const;
        std::array<double, 2> GetEndTargetPosition(int endId) const;
        std::array<double, 2> GetLabelPosition(int axisId, const int labelSize[2]) const;

        void SetShaftLength(double shaftLength);
        // A null color restores the per axis colors
        void SetOverrideColor(const double* color);
        bool SetNegativeAxesVisible(bool visible);

        static bool IsPositiveEnd(int endId) { return endId % 2 == 0; }
        static int GetEndId(int axisId, int signId) { return 2 * axisId + signId; }
        static double GetShaftLengthOfSymbol(double symbolSize) { return DefaultShaftLength * symbolSize / DefaultSymbolSize; }
        static int GetLabelFontSize(double shaftLength);
        static void GetCameraBasis(vtkCamera* camera, double right[3], double up[3], double viewPlaneNormal[3]);
        static const double GlobalAxes[3][3];

    private:
        void InsertCone(vtkPoints* points, vtkCellArray* polygons, int endId);
        void InsertDisk(vtkPoints* points, vtkCellArray* polygons, const double diskCenter[2], double radius);
        static void InsertColor(vtkUnsignedCharArray* colors, const std::array<double, 3>& color, double opacity);

        double ShaftLength = 0;
        double NegativeShaftLength = 0;
        double ConeHeight = 0;
        double ConeRadius = 0;
        double OriginRadius = 0;
        double NegativeEndRadius = 0;
        double LabelGap = 0;
        bool NegativeAxesVisible = false;
        bool HasOverrideColor = false;
        double OverrideColor[3] = { 0, 0, 0 };
        double Center[2] = { 0, 0 };
        double CenterDepth = 0;
        double EndDirections[NumOfEnds][2] = {};
        double EndPositions[NumOfEnds][2] = {};
        double EndDepths[NumOfEnds] = {};
    };
}
