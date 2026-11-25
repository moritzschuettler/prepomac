// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeOutputWindow.h"

#include <vtkLogger.h>
#include <vtkObjectFactory.h>
#include <vtkSmartPointer.h>

vtkStandardNewMacro(vtkNativeOutputWindow);

void vtkNativeOutputWindow::DisplayText(const char* text)
{
    Forward(VTKN_LOG_TEXT, text);
}

void vtkNativeOutputWindow::DisplayErrorText(const char* text)
{
    {
        std::lock_guard<std::mutex> lock(Mutex);
        if (!Errors.empty()) Errors += "\n";
        Errors += text != nullptr ? text : "";
    }
    Forward(VTKN_LOG_ERROR, text);
}

void vtkNativeOutputWindow::DisplayWarningText(const char* text)
{
    Forward(VTKN_LOG_WARNING, text);
}

void vtkNativeOutputWindow::DisplayGenericWarningText(const char* text)
{
    Forward(VTKN_LOG_WARNING, text);
}

void vtkNativeOutputWindow::DisplayDebugText(const char* text)
{
    Forward(VTKN_LOG_TEXT, text);
}

void vtkNativeOutputWindow::SetCallback(vtkn_log_callback callback, void* userData)
{
    std::lock_guard<std::mutex> lock(Mutex);
    Callback = callback;
    UserData = userData;
}

std::string vtkNativeOutputWindow::TakeErrors()
{
    std::lock_guard<std::mutex> lock(Mutex);
    std::string errors;
    errors.swap(Errors);
    return errors;
}

vtkNativeOutputWindow* vtkNativeOutputWindow::Install()
{
    static vtkSmartPointer<vtkNativeOutputWindow> instance;
    if (instance == nullptr)
    {
        instance = vtkSmartPointer<vtkNativeOutputWindow>::New();
        vtkOutputWindow::SetInstance(instance);
        // The logger writes every error to the host's console as well; the callback is the one channel
        vtkLogger::SetStderrVerbosity(vtkLogger::VERBOSITY_OFF);
    }
    return instance;
}

void vtkNativeOutputWindow::Forward(int32_t level, const char* text)
{
    vtkn_log_callback callback;
    void* userData;
    {
        std::lock_guard<std::mutex> lock(Mutex);
        callback = Callback;
        userData = UserData;
    }
    if (callback != nullptr) callback(userData, level, text != nullptr ? text : "");
}
