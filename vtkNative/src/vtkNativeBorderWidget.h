// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxBorderWidget: a box of 2D actors in the widget layer that keeps its corner when the window is
// resized, stays inside the window, is dragged with the left button, and highlights the part under the mouse.
// Positions are display pixels with the origin at the bottom left, as VTK reports the mouse.
#pragma once

#include "vtkNative.h"
#include "vtkNativeRect.h"
#include "vtkNativeWidgetHighlight.h"

#include <vtkActor2D.h>
#include <vtkCoordinate.h>
#include <vtkPoints.h>
#include <vtkPolyData.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

#include <functional>

namespace vtkn
{
    class BorderWidget
    {
    public:
        BorderWidget();
        virtual ~BorderWidget() = default;
        BorderWidget(const BorderWidget&) = delete;
        BorderWidget& operator=(const BorderWidget&) = delete;

        virtual void SetRenderer(vtkRenderer* renderer);
        virtual void RemoveRenderer();
        void SetDoubleClickHandler(std::function<void(int, int)> handler) { DoubleClickHandler = handler; }
        void SetShownHandler(std::function<void()> handler) { ShownHandler = handler; }

        virtual void VisibilityOn();
        virtual void VisibilityOff();
        void BorderVisibilityOn();
        void BorderVisibilityOff();
        virtual void BackgroundVisibilityOn();
        virtual void BackgroundVisibilityOff();
        void SetLocalHighlightEnabled(bool enabled);

        void OnRenderWindowModified();
        virtual void OnMovedOrSizeChanged();
        virtual void OnSizeChanged();
        void CheckWindowSize();

        virtual bool LeftButtonPress(int x, int y, int clicks);
        virtual bool LeftButtonRelease(int x, int y);
        virtual bool MouseOver(int x, int y);
        virtual bool MouseMove(int x, int y);
        virtual bool UpdateMouseHover(int x, int y, bool mouseOver, bool highlightEntireWidget);
        virtual bool ClearMouseHover();
        virtual void BringToFront();
        void BringHighlightToFront();
        virtual void UpdateBorderGeometry();

        void SetPosition(vtkn_widget_anchor anchor, double x, double y);
        void SetBorderColor(double r, double g, double b);
        void SetBackgroundColor(double r, double g, double b);

        bool GetVisibility() const { return Visibility; }
        Rect GetRectangle() const;
        bool GetShownHighlight(Rect& rectangle) const { return Highlight.GetRectangle(rectangle); }

    protected:
        virtual Rect GetDisplayRectangle();
        virtual Rect GetHighlightRectangle(int x, int y);
        virtual double GetHighlightFillOpacity(const Rect&) { return WidgetHighlight::WidgetFillOpacity; }
        double PositionY(const int* rendererSize) const;

        vtkSmartPointer<vtkCoordinate> PositionCoordinate;
        vtkSmartPointer<vtkPoints> BorderPoints;
        vtkSmartPointer<vtkPolyData> BorderPolyData;
        vtkSmartPointer<vtkActor2D> BorderActor;
        vtkSmartPointer<vtkActor2D> BackgroundActor;
        WidgetHighlight Highlight;
        //
        vtkRenderer* Renderer = nullptr;
        vtkRenderer* HighlightRenderer = nullptr;
        //
        bool Visibility = true;
        bool BorderVisibility = true;
        bool BackgroundVisibility = true;
        bool KeepInsideVisibleBounds = true;
        bool ScaleBySectors = true;
        bool LocalHighlightEnabled = true;
        int Quadrant = 0;
        int PrevWindowSize[2] = { 0, 0 };
        double Position[2] = { 100, 100 };
        double Size[2] = { 100, 100 };
        vtkn_widget_anchor Anchor = VTKN_ANCHOR_BOTTOM_LEFT;
        bool Clicked = false;
        int ClickPos[2] = { 0, 0 };
        std::function<void(int, int)> DoubleClickHandler;
        std::function<void()> ShownHandler;
    };
}
