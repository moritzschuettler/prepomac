// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The guard of every exported function: it turns every exception into a status and the last error, and fails the
// call on an error VTK reported meanwhile. Shared by the entry points of all areas.
#pragma once

#include "vtkNative.h"
#include "vtkNativeError.h"
#include "vtkNativeOutputWindow.h"

#include <string>

namespace vtkn
{
    template <typename Function>
    inline vtkn_status GuardVtk(Function function)
    {
        return Guard([&]() {
            vtkNativeOutputWindow* outputWindow = vtkNativeOutputWindow::Install();
            outputWindow->TakeErrors();
            vtkn_status status = function();
            std::string errors = outputWindow->TakeErrors();
            if (status == VTKN_OK && !errors.empty()) throw Error(VTKN_ERROR_VTK, errors);
            return status;
        });
    }

    inline std::string ToName(const char* text, const char* argumentName)
    {
        if (text == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, std::string("The argument '") + argumentName +
                                                                       "' must not be null.");
        return std::string(text);
    }
}
