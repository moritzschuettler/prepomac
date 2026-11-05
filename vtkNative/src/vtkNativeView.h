// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The object behind a vtkn_view handle: one render window with one renderer, its interactor when it is embedded
// in a parent window, and the named actors.
#pragma once

#include "vtkNative.h"

#include <vtkActor.h>
#include <vtkCallbackCommand.h>
#include <vtkRenderWindow.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

#include <map>
#include <string>
#include <vector>

struct vtkn_view_s
{
};

namespace vtkn
{
    class View : public vtkn_view_s
    {
    public:
        explicit View(bool offScreen);
        ~View();
        View(const View&) = delete;
        View& operator=(const View&) = delete;

        static View* FromHandle(vtkn_view handle);

        void SetParentWindow(void* parentWindow);
        void SetSize(int width, int height);
        void GetSize(int& width, int& height);
        void SetEventCallback(vtkn_event_callback callback, void* userData);
        void SetBackground(const double* top, const double* bottom);
        void Render();
        std::string GetRenderInfo();

        void GetCamera(double* values10);
        void SetCamera(const double* values10);
        void ResetCamera();

        void AddMeshActor(const std::string& name, const double* points, int pointCount, const int32_t* cells,
                          int cellsLength, const uint8_t* cellTypes, int cellCount, const double* color,
                          bool edgesVisible);
        void RemoveActor(const std::string& name);
        std::string GetActorNames();

        std::vector<uint8_t> ReadPixels(int magnification, int& width, int& height);
        void WritePng(const std::string& fileName, int magnification);

    private:
        static void OnVtkEvent(vtkObject* caller, unsigned long eventId, void* clientData, void* callData);
        void RaiseEvent(int32_t eventId, const double* values, int32_t count);

        bool OffScreen;
        bool Embedded = false;
        vtkSmartPointer<vtkRenderWindow> RenderWindow;
        vtkSmartPointer<vtkRenderer> Renderer;
        vtkSmartPointer<vtkRenderWindowInteractor> Interactor;
        vtkSmartPointer<vtkCallbackCommand> EventCommand;
        vtkSmartPointer<vtkCallbackCommand> InteractorCommand;
        std::map<std::string, vtkSmartPointer<vtkActor>> Actors;
        vtkn_event_callback Callback = nullptr;
        void* UserData = nullptr;
    };
}
