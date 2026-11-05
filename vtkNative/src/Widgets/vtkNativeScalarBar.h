// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The object behind a vtkn_scalar_bar handle: a scalar bar widget owned by its view, with the id its events carry.
#pragma once

#include "vtkNative.h"
#include "vtkNativeScalarBarWidget.h"

struct vtkn_scalar_bar_s
{
};

namespace vtkn
{
    class View;

    class ScalarBar : public vtkn_scalar_bar_s
    {
    public:
        ScalarBar(View* owner, int id);
        ~ScalarBar();
        ScalarBar(const ScalarBar&) = delete;
        ScalarBar& operator=(const ScalarBar&) = delete;

        static ScalarBar* FromHandle(vtkn_scalar_bar handle);

        View* Owner;
        int Id;
        ScalarBarWidget Widget;
    };
}
