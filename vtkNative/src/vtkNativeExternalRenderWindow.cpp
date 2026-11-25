// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeExternalRenderWindow.h"

#if defined(__APPLE__)
#include <vtkObjectFactory.h>
#include <vtkOpenGLState.h>
#include <vtk_glad.h>

#include <OpenGL/CGLCurrent.h>
#include <OpenGL/CGLTypes.h>
#include <OpenGL/OpenGL.h>

vtkStandardNewMacro(vtkNativeExternalRenderWindow);

namespace
{
    CGLContextObj ToContext(void* context)
    {
        return static_cast<CGLContextObj>(context);
    }
}

// Nothing is drawn until there is a context; the frame stays in the framebuffers of the window until Present
vtkNativeExternalRenderWindow::vtkNativeExternalRenderWindow()
{
    this->SetReadyForRendering(false);
    this->SetFrameBlitModeToNoBlit();
    this->SetIsDirect(true);
    this->SetSupportsOpenGL(true);
    this->SetOwnContext(false);
}

// The base class destructor releases the graphics resources again, without the override; a context is still
// released here while it can be made current
vtkNativeExternalRenderWindow::~vtkNativeExternalRenderWindow()
{
    if (this->Context != nullptr) this->DetachContext();
}

bool vtkNativeExternalRenderWindow::AttachCurrentContext()
{
    CGLContextObj current = CGLGetCurrentContext();
    if (current == nullptr) return false;
    if (this->Context == current) return true;
    if (this->Context != nullptr) this->DetachContext();
    CGLRetainContext(current);
    this->Context = current;
    this->OwnsContext = false;
    this->SetOwnContext(false);
    this->SetReadyForRendering(true);
    return true;
}

bool vtkNativeExternalRenderWindow::CreateOwnContext()
{
    if (this->Context != nullptr) this->DetachContext();
    // Accelerated first; a machine without a GPU (a virtual machine) gets the software renderer
    for (int accelerated = 1; accelerated >= 0; accelerated--)
    {
        CGLPixelFormatAttribute attributes[] = {
            kCGLPFAOpenGLProfile, static_cast<CGLPixelFormatAttribute>(kCGLOGLPVersion_GL4_Core),
            kCGLPFAColorSize, static_cast<CGLPixelFormatAttribute>(24),
            kCGLPFAAlphaSize, static_cast<CGLPixelFormatAttribute>(8),
            kCGLPFADepthSize, static_cast<CGLPixelFormatAttribute>(24),
            kCGLPFAAllowOfflineRenderers,
            accelerated ? kCGLPFAAccelerated : static_cast<CGLPixelFormatAttribute>(0),
            static_cast<CGLPixelFormatAttribute>(0) };
        CGLPixelFormatObj pixelFormat = nullptr;
        GLint count = 0;
        if (CGLChoosePixelFormat(attributes, &pixelFormat, &count) != kCGLNoError || pixelFormat == nullptr)
            continue;
        CGLContextObj context = nullptr;
        CGLError error = CGLCreateContext(pixelFormat, nullptr, &context);
        CGLReleasePixelFormat(pixelFormat);
        if (error != kCGLNoError || context == nullptr) continue;
        this->Context = context;
        this->OwnsContext = true;
        this->SetOwnContext(true);
        this->SetReadyForRendering(true);
        return true;
    }
    return false;
}

void vtkNativeExternalRenderWindow::DetachContext()
{
    if (this->Context == nullptr) return;
    CGLContextObj previous = CGLGetCurrentContext();
    this->ReleaseGraphicsResources(this);
    if (previous != this->Context) CGLSetCurrentContext(previous);
    // A new context loads the OpenGL functions and the state again at its first render
    this->Initialized = false;
    this->FrameSize[0] = this->FrameSize[1] = 0;
    CGLContextObj context = ToContext(this->Context);
    this->Context = nullptr;
    this->SetReadyForRendering(false);
    if (this->OwnsContext) CGLDestroyContext(context);
    else CGLReleaseContext(context);
    this->OwnsContext = false;
}

bool vtkNativeExternalRenderWindow::IsContextCurrent() const
{
    return this->Context != nullptr && CGLGetCurrentContext() == ToContext(this->Context);
}

void vtkNativeExternalRenderWindow::MakeCurrent()
{
    if (this->Context != nullptr && CGLGetCurrentContext() != ToContext(this->Context))
        CGLSetCurrentContext(ToContext(this->Context));
}

bool vtkNativeExternalRenderWindow::IsCurrent()
{
    return this->IsContextCurrent();
}

// The context of a host stays current: the host may be drawing with it
void vtkNativeExternalRenderWindow::ReleaseCurrent()
{
    if (this->OwnsContext && this->IsContextCurrent()) CGLSetCurrentContext(nullptr);
}

// The generic window loads the OpenGL functions before it makes its context current, which a host does for it
void vtkNativeExternalRenderWindow::OpenGLInit()
{
    this->MakeCurrent();
    this->Superclass::OpenGLInit();
}

void vtkNativeExternalRenderWindow::Frame()
{
    this->Superclass::Frame();
    // Frame copies the rendered picture into the display framebuffer only while the buffers are swapped
    if (this->SwapBuffers)
    {
        this->FrameSize[0] = this->Size[0];
        this->FrameSize[1] = this->Size[1];
    }
}

// Without a context there is nothing to release: the resources were released when the context was detached
void vtkNativeExternalRenderWindow::ReleaseGraphicsResources(vtkWindow* window)
{
    if (this->Context == nullptr) return;
    this->MakeCurrent();
    this->Superclass::ReleaseGraphicsResources(window);
}

bool vtkNativeExternalRenderWindow::IsFrameCurrent()
{
    return !this->GetNeverRendered() && this->FrameSize[0] == this->Size[0] && this->FrameSize[1] == this->Size[1];
}

void vtkNativeExternalRenderWindow::Present(unsigned int framebuffer, int width, int height)
{
    this->MakeCurrent();
    vtkOpenGLState* state = this->GetState();
    // The host has drawn with the context since the last render, so the state VTK keeps is read again
    state->Reset();
    state->PushDrawFramebufferBinding();
    state->vtkglBindFramebuffer(GL_DRAW_FRAMEBUFFER, framebuffer);
    bool sameSize = width == this->FrameSize[0] && height == this->FrameSize[1];
    this->BlitDisplayFramebuffer(0, 0, 0, this->FrameSize[0], this->FrameSize[1], 0, 0, width, height,
                                 GL_COLOR_BUFFER_BIT, sameSize ? GL_NEAREST : GL_LINEAR);
    state->PopDrawFramebufferBinding();
}
#endif
