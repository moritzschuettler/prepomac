// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The render window of a view without a window of its own on macOS. It draws with an OpenGL context of the host -
// the context of an Avalonia OpenGlControlBase, taken while it is current - or, for an off screen view, with a CGL
// context of its own. It always draws into its own framebuffers, so the library can render, pick and read pictures
// at any time on the thread of the view; the host shows the last frame by blitting it into the framebuffer it draws
// (Present). The context is made current by the window itself and left current, as an embedded VTK window does.
#pragma once

#if defined(__APPLE__)
#include <vtkGenericOpenGLRenderWindow.h>

class vtkNativeExternalRenderWindow : public vtkGenericOpenGLRenderWindow
{
public:
    static vtkNativeExternalRenderWindow* New();
    vtkTypeMacro(vtkNativeExternalRenderWindow, vtkGenericOpenGLRenderWindow);

    // The context current on the calling thread becomes the context of the window; false without a current context
    bool AttachCurrentContext();
    // A context of its own (OpenGL 4.1 core, accelerated), for an off screen view; false when none can be created
    bool CreateOwnContext();
    // Releases the OpenGL resources of the window and its renderers with the context current, then forgets the
    // context. A host calls it before it destroys its context
    void DetachContext();
    bool HasContext() const { return this->Context != nullptr; }
    // Whether the context current on the calling thread is the context of the window
    bool IsContextCurrent() const;
    // Blits the last frame into the framebuffer, stretched to width x height; the context must be current
    void Present(unsigned int framebuffer, int width, int height);
    // Whether the last frame has the size of the window
    bool IsFrameCurrent();

    void MakeCurrent() override;
    bool IsCurrent() override;
    void ReleaseCurrent() override;
    void Frame() override;
    void OpenGLInit() override;
    void ReleaseGraphicsResources(vtkWindow* window) override;

protected:
    vtkNativeExternalRenderWindow();
    ~vtkNativeExternalRenderWindow() override;

private:
    vtkNativeExternalRenderWindow(const vtkNativeExternalRenderWindow&) = delete;
    void operator=(const vtkNativeExternalRenderWindow&) = delete;

    void* Context = nullptr;
    bool OwnsContext = false;
    int FrameSize[2] = { 0, 0 };
};
#endif
