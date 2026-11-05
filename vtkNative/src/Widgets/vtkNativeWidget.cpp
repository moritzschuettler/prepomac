// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeWidget.h"
#include "vtkNativeArrowWidget.h"
#include "vtkNativeColorBarWidget.h"
#include "vtkNativeCoorSysWidget.h"
#include "vtkNativeScaleBarWidget.h"
#include "vtkNativeStatusBlockWidget.h"

#include <mutex>
#include <set>

namespace vtkn
{
    namespace
    {
        std::mutex registryMutex;
        std::set<Widget*> liveWidgets;

        std::unique_ptr<BorderWidget> CreateContent(vtkn_widget_kind kind)
        {
            switch (kind)
            {
                case VTKN_WIDGET_TEXT: return std::unique_ptr<BorderWidget>(new TextWidget());
                case VTKN_WIDGET_STATUS_BLOCK: return std::unique_ptr<BorderWidget>(new StatusBlockWidget());
                case VTKN_WIDGET_SCALE_BAR: return std::unique_ptr<BorderWidget>(new ScaleBarWidget());
                case VTKN_WIDGET_COLOR_BAR: return std::unique_ptr<BorderWidget>(new ColorBarWidget());
                case VTKN_WIDGET_TEXT_WITH_ARROW: return std::unique_ptr<BorderWidget>(new ArrowWidget());
                case VTKN_WIDGET_TRIAD: return std::unique_ptr<BorderWidget>(new CoorSysWidget());
                default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The kind is not one of vtkn_widget_kind.");
            }
        }
    }

    Widget::Widget(View* owner, int id, vtkn_widget_kind kind)
        : Owner(owner), Id(id), Kind(kind), Content(CreateContent(kind))
    {
        std::lock_guard<std::mutex> lock(registryMutex);
        liveWidgets.insert(this);
    }

    Widget::~Widget()
    {
        std::lock_guard<std::mutex> lock(registryMutex);
        liveWidgets.erase(this);
    }

    Widget* Widget::FromHandle(vtkn_widget handle)
    {
        Widget* widget = static_cast<Widget*>(handle);
        std::lock_guard<std::mutex> lock(registryMutex);
        if (handle == nullptr || liveWidgets.find(widget) == liveWidgets.end())
            throw Error(VTKN_ERROR_INVALID_HANDLE, "The widget handle is not a live widget.");
        return widget;
    }
}
