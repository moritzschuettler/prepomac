// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeWidgetSet.h"

#include <algorithm>
#include <cstdlib>

#if defined(_WIN32)
#  define NOMINMAX
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>
#endif

namespace vtkn
{
    WidgetSet::WidgetSet()
    {
#if defined(_WIN32)
        DoubleClickTime = std::chrono::milliseconds(GetDoubleClickTime());
#else
        DoubleClickTime = std::chrono::milliseconds(500);
#endif
        LastClickTime = std::chrono::steady_clock::now() - std::chrono::hours(1);
    }

    void WidgetSet::Add(BorderWidget* widget)
    {
        Widgets.push_back(widget);
        widget->SetShownHandler([this, widget]() { MoveWidgetToTop(widget); });
        widget->SetDoubleClickHandler([this, widget](int x, int y) {
            if (DoubleClickHandler) DoubleClickHandler(widget, x, y);
        });
    }

    void WidgetSet::Remove(BorderWidget* widget)
    {
        if (CapturedWidget == widget)
        {
            CapturedWidget = nullptr;
            CapturedWidgetPromoted = false;
        }
        widget->ClearMouseHover();
        widget->SetShownHandler(nullptr);
        widget->SetDoubleClickHandler(nullptr);
        Widgets.erase(std::remove(Widgets.begin(), Widgets.end(), widget), Widgets.end());
    }

    void WidgetSet::SetDoubleClickHandler(std::function<void(BorderWidget*, int, int)> handler)
    {
        DoubleClickHandler = handler;
    }

    // The front widget gets the press first; a press inside a widget captures the mouse until the release
    bool WidgetSet::LeftButtonPress(int x, int y, bool& handled)
    {
        handled = false;
        if (CapturedWidget != nullptr)
        {
            CapturedWidget->LeftButtonRelease(x, y);
            CapturedWidget = nullptr;
            CapturedWidgetPromoted = false;
        }
        //
        auto now = std::chrono::steady_clock::now();
        int clicks = 1;
        if (now - LastClickTime < DoubleClickTime && std::abs(x - LastX) < DoubleClickDisplacement &&
            std::abs(y - LastY) < DoubleClickDisplacement) clicks = 2;
        //
        BorderWidget* pressedWidget = nullptr;
        std::vector<BorderWidget*> reversed(Widgets.rbegin(), Widgets.rend());
        for (BorderWidget* widget : reversed)
        {
            if (widget->LeftButtonPress(x, y, clicks))
            {
                pressedWidget = widget;
                break;
            }
        }
        if (pressedWidget != nullptr)
        {
            handled = true;
            CapturedWidget = pressedWidget;
            CapturedWidgetPromoted = false;
            if (clicks == 2)
            {
                PromoteWidget(pressedWidget);
                CapturedWidgetPromoted = true;
            }
            UpdateWidgetHighlights(x, y);
        }
        LastX = x;
        LastY = y;
        LastClickTime = now;
        return handled;
    }

    bool WidgetSet::LeftButtonRelease(int x, int y, bool& handled)
    {
        handled = CapturedWidget != nullptr;
        if (!handled) return false;
        //
        CapturedWidget->LeftButtonRelease(x, y);
        CapturedWidget = nullptr;
        CapturedWidgetPromoted = false;
        UpdateWidgetHighlights(x, y);
        return true;
    }

    bool WidgetSet::MouseMove(int x, int y, bool& handled)
    {
        bool widgetMoved = CapturedWidget != nullptr && CapturedWidget->MouseMove(x, y);
        if (widgetMoved && !CapturedWidgetPromoted)
        {
            PromoteWidget(CapturedWidget);
            CapturedWidgetPromoted = true;
        }
        bool highlightChanged = UpdateWidgetHighlights(x, y);
        handled = CapturedWidget != nullptr;
        return widgetMoved || highlightChanged;
    }

    void WidgetSet::CheckWindowSize()
    {
        for (BorderWidget* widget : Widgets) widget->CheckWindowSize();
    }

    void WidgetSet::PromoteWidget(BorderWidget* widget)
    {
        MoveWidgetToTop(widget);
        UpdateWidgetZOrder();
    }

    void WidgetSet::MoveWidgetToTop(BorderWidget* widget)
    {
        Widgets.erase(std::remove(Widgets.begin(), Widgets.end(), widget), Widgets.end());
        Widgets.push_back(widget);
    }

    bool WidgetSet::UpdateWidgetHighlights(int x, int y)
    {
        if (!WidgetHighlightEnabled)
        {
            bool cleared = false;
            for (BorderWidget* widget : Widgets) cleared |= widget->ClearMouseHover();
            return cleared;
        }
        //
        BorderWidget* highlightedWidget = CapturedWidget;
        if (highlightedWidget == nullptr)
        {
            for (auto widget = Widgets.rbegin(); widget != Widgets.rend(); ++widget)
            {
                if ((*widget)->MouseOver(x, y))
                {
                    highlightedWidget = *widget;
                    break;
                }
            }
        }
        //
        bool highlightChanged = false;
        for (BorderWidget* widget : Widgets)
        {
            bool isHighlighted = widget == highlightedWidget;
            bool highlightEntireWidget = isHighlighted && CapturedWidget != nullptr;
            if (widget->UpdateMouseHover(x, y, isHighlighted, highlightEntireWidget)) highlightChanged = true;
        }
        if (highlightChanged) UpdateWidgetZOrder();
        return highlightChanged;
    }

    // Each widget highlight belongs directly above its own content and below the widgets in front of it
    void WidgetSet::UpdateWidgetZOrder()
    {
        for (BorderWidget* widget : Widgets)
        {
            widget->BringToFront();
            widget->BringHighlightToFront();
        }
    }
}
