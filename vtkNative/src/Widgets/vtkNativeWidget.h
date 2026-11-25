// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The object behind a vtkn_widget handle: one of the widgets of the widget layer, owned by its view, with the id
// its events carry and the kind that tells which calls it takes.
#pragma once

#include "vtkNative.h"
#include "vtkNativeBorderWidget.h"
#include "vtkNativeError.h"

#include <memory>

struct vtkn_widget_s
{
};

namespace vtkn
{
    class View;

    class Widget : public vtkn_widget_s
    {
    public:
        Widget(View* owner, int id, vtkn_widget_kind kind);
        ~Widget();
        Widget(const Widget&) = delete;
        Widget& operator=(const Widget&) = delete;

        static Widget* FromHandle(vtkn_widget handle);

        template <typename T>
        T& As(vtkn_widget_kind kind)
        {
            if (Kind != kind) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The widget is not of the kind this call takes.");
            return static_cast<T&>(*Content);
        }

        View* Owner;
        int Id;
        vtkn_widget_kind Kind;
        std::unique_ptr<BorderWidget> Content;
    };
}
