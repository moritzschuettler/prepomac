// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Replaces the VTK output window, which on Windows opens a console or a message box for every warning, by a
// callback. The last VTK error is also kept, so a call that fails inside VTK can report what VTK said.
#pragma once

#include "vtkNative.h"

#include <vtkOutputWindow.h>

#include <mutex>
#include <string>

class vtkNativeOutputWindow : public vtkOutputWindow
{
public:
    static vtkNativeOutputWindow* New();
    vtkTypeMacro(vtkNativeOutputWindow, vtkOutputWindow);

    void DisplayText(const char* text) override;
    void DisplayErrorText(const char* text) override;
    void DisplayWarningText(const char* text) override;
    void DisplayGenericWarningText(const char* text) override;
    void DisplayDebugText(const char* text) override;

    void SetCallback(vtkn_log_callback callback, void* userData);
    // The errors VTK reported since the last call, cleared by reading them
    std::string TakeErrors();

    static vtkNativeOutputWindow* Install();

protected:
    vtkNativeOutputWindow() = default;
    ~vtkNativeOutputWindow() override = default;

private:
    void Forward(int32_t level, const char* text);

    std::mutex Mutex;
    vtkn_log_callback Callback = nullptr;
    void* UserData = nullptr;
    std::string Errors;
};
