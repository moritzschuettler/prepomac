// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxWidgetHighlight: the blue frame and light fill drawn over the widget part under the mouse.
#pragma once

#include "vtkNativeRect.h"

#include <vtkActor2D.h>
#include <vtkPoints.h>
#include <vtkPolyData.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

namespace vtkn
{
    class WidgetHighlight
    {
    public:
        static constexpr double WidgetFillOpacity = 0.05;
        static constexpr double WidgetBorderOpacity = 0.5;
        static constexpr float WidgetBorderLineWidth = 2.0f;

        WidgetHighlight();

        void AddToRenderer(vtkRenderer* renderer);
        void RemoveFromRenderer(vtkRenderer* renderer);
        bool SetRectangle(const Rect& rectangle, double fillOpacity);
        // A highlight that is not a display rectangle, such as the band along an axis of the triad; corners is
        // null to clear it
        bool SetCorners(const double (*corners)[2], double fillOpacity);
        void BringToFront(vtkRenderer* renderer);
        bool Clear();
        bool GetRectangle(Rect& rectangle) const;
        bool GetCorners(double corners[4][2]) const;

    private:
        vtkSmartPointer<vtkPoints> CornerPoints;
        vtkSmartPointer<vtkPolyData> BorderPolyData;
        vtkSmartPointer<vtkActor2D> BorderActor;
        vtkSmartPointer<vtkPolyData> FillPolyData;
        vtkSmartPointer<vtkActor2D> FillActor;
        bool HasRectangle = false;
        Rect Current;
        double CurrentCorners[4][2] = {};
        double FillOpacity = WidgetFillOpacity;
    };
}
