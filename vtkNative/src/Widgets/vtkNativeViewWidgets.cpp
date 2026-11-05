// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The widgets of a view besides the scalar bar: created by kind into the widget layer, fed by the widget set, and
// raising their own events through the view.
#include "vtkNativeCoorSysWidget.h"
#include "vtkNativeView.h"

namespace vtkn
{
    Widget* View::CreateWidget(vtkn_widget_kind kind)
    {
        std::unique_ptr<Widget> widget(new Widget(this, NextWidgetId++, kind));
        int id = widget->Id;
        if (kind == VTKN_WIDGET_TRIAD)
        {
            CoorSysWidget& triad = static_cast<CoorSysWidget&>(*widget->Content);
            triad.SetTargetClickedHandler([this, id](vtkn_triad_target target) {
                double values[2] = { static_cast<double>(id), static_cast<double>(target) };
                RaiseEvent(VTKN_EVENT_WIDGET_TARGET_CLICKED, values, 2);
            });
            triad.SetHoverChangedHandler([this, id](bool hovered) {
                double values[2] = { static_cast<double>(id), hovered ? 1.0 : 0.0 };
                RaiseEvent(VTKN_EVENT_WIDGET_HOVER_CHANGED, values, 2);
            });
        }
        widget->Content->SetRenderer(WidgetRenderer);
        Widgets.Add(widget->Content.get());
        AreaWidgets.push_back(std::move(widget));
        return AreaWidgets.back().get();
    }

    void View::DestroyWidget(Widget* widget)
    {
        for (auto entry = AreaWidgets.begin(); entry != AreaWidgets.end(); ++entry)
        {
            if (entry->get() != widget) continue;
            Widgets.Remove(widget->Content.get());
            widget->Content->RemoveRenderer();
            AreaWidgets.erase(entry);
            return;
        }
        throw Error(VTKN_ERROR_INVALID_HANDLE, "The widget does not belong to this view.");
    }

    void View::SetWidgetHighlight(bool enabled, bool localEnabled)
    {
        Widgets.SetHighlightEnabled(enabled, localEnabled);
        Render();
    }
}
