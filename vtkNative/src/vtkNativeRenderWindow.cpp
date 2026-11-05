// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeRenderWindow.h"

#include <vtkObjectFactory.h>

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>

vtkStandardNewMacro(vtkNativeRenderWindow);

namespace
{
    HINSTANCE GetLibraryInstance()
    {
        HMODULE module = nullptr;
        GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                           reinterpret_cast<LPCWSTR>(&GetLibraryInstance), &module);
        return reinterpret_cast<HINSTANCE>(module);
    }
}

// InitializeApplication keeps an instance that is already set, so the parent window's instance is never taken
vtkNativeRenderWindow::vtkNativeRenderWindow()
{
    this->ApplicationInstance = GetLibraryInstance();
}
