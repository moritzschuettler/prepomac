// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of the widget part of vtkInteractorStyleControl: the widgets of a view in their stacking order, the widget
// that owns the mouse between a press and its release, double click detection, the hover highlights and the
// stacking order the highlights need.
#pragma once

#include "vtkNativeBorderWidget.h"

#include <chrono>
#include <functional>
#include <vector>

namespace vtkn
{
    class WidgetSet
    {
    public:
        WidgetSet();

        void Add(BorderWidget* widget);
        void Remove(BorderWidget* widget);
        void SetDoubleClickHandler(std::function<void(BorderWidget*, int, int)> handler);

        // Each returns whether the event changed a widget, so the view must be rendered
        bool LeftButtonPress(int x, int y, bool& handled);
        bool LeftButtonRelease(int x, int y, bool& handled);
        bool MouseMove(int x, int y, bool& handled);
        void CheckWindowSize();

    private:
        void PromoteWidget(BorderWidget* widget);
        void MoveWidgetToTop(BorderWidget* widget);
        bool UpdateWidgetHighlights(int x, int y);
        void UpdateWidgetZOrder();

        std::vector<BorderWidget*> Widgets;
        BorderWidget* CapturedWidget = nullptr;
        bool CapturedWidgetPromoted = false;
        bool WidgetHighlightEnabled = true;
        std::chrono::steady_clock::time_point LastClickTime;
        std::chrono::milliseconds DoubleClickTime;
        int DoubleClickDisplacement = 5;
        int LastX = 0;
        int LastY = 0;
        std::function<void(BorderWidget*, int, int)> DoubleClickHandler;
    };
}
