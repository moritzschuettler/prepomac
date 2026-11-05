// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The Win32 render window of vtkNative. VTK registers its window class "vtkOpenGL" once per module instance and
// reuses a class of that name it finds. ActiViz 5.8 registers the same name for the executable's instance, so a
// VTK 9.7 window created for that instance gets the window procedure of VTK 5.8, which reads the VTK 9.7 object
// as a VTK 5.8 one and crashes. This window registers and creates its windows for the instance of vtkNative.dll,
// so the two versions keep separate classes in one process.
#pragma once

#include <vtkWin32OpenGLRenderWindow.h>

class vtkNativeRenderWindow : public vtkWin32OpenGLRenderWindow
{
public:
    static vtkNativeRenderWindow* New();
    vtkTypeMacro(vtkNativeRenderWindow, vtkWin32OpenGLRenderWindow);

protected:
    vtkNativeRenderWindow();
    ~vtkNativeRenderWindow() override = default;
};
