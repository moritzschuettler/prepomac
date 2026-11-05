// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeView.h"
#include "vtkNativeError.h"
#include "vtkNativeRenderWindow.h"

#include <vtkCamera.h>
#include <vtkCellArray.h>
#include <vtkCommand.h>
#include <vtkDataSetMapper.h>
#include <vtkDoubleArray.h>
#include <vtkIdTypeArray.h>
#include <vtkImageData.h>
#include <vtkInteractorStyleTrackballCamera.h>
#include <vtkNew.h>
#include <vtkOpenGLRenderWindow.h>
#include <vtkPNGWriter.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkProperty.h>
#include <vtkUnsignedCharArray.h>
#include <vtkUnstructuredGrid.h>
#include <vtkWindowToImageFilter.h>

#include <cstring>
#include <mutex>
#include <set>

namespace vtkn
{
    namespace
    {
        std::mutex registryMutex;
        std::set<View*> liveViews;
    }

    View::View(bool offScreen) : OffScreen(offScreen)
    {
        Renderer = vtkSmartPointer<vtkRenderer>::New();
        RenderWindow = vtkSmartPointer<vtkNativeRenderWindow>::New();
        RenderWindow->SetOffScreenRendering(offScreen ? 1 : 0);
        RenderWindow->AddRenderer(Renderer);
        RenderWindow->SetMultiSamples(0);
        //
        EventCommand = vtkSmartPointer<vtkCallbackCommand>::New();
        EventCommand->SetCallback(&View::OnVtkEvent);
        EventCommand->SetClientData(this);
        Renderer->AddObserver(vtkCommand::EndEvent, EventCommand);
        Renderer->GetActiveCamera()->AddObserver(vtkCommand::ModifiedEvent, EventCommand);
        //
        std::lock_guard<std::mutex> lock(registryMutex);
        liveViews.insert(this);
    }

    View::~View()
    {
        {
            std::lock_guard<std::mutex> lock(registryMutex);
            liveViews.erase(this);
        }
        Callback = nullptr;
        Renderer->RemoveObserver(EventCommand);
        Renderer->GetActiveCamera()->RemoveObserver(EventCommand);
        Renderer->RemoveAllViewProps();
        Actors.clear();
        if (Interactor != nullptr)
        {
            Interactor->RemoveObserver(InteractorCommand);
            Interactor->Disable();
            Interactor->SetRenderWindow(nullptr);
            Interactor = nullptr;
        }
        RenderWindow->Finalize();
    }

    View* View::FromHandle(vtkn_view handle)
    {
        View* view = static_cast<View*>(handle);
        std::lock_guard<std::mutex> lock(registryMutex);
        if (handle == nullptr || liveViews.find(view) == liveViews.end())
            throw Error(VTKN_ERROR_INVALID_HANDLE, "The view handle is not a live view.");
        return view;
    }

    // The parent must be set before the window exists, which is at the first render, and only once: VTK cannot
    // move an OpenGL window to another parent
    void View::SetParentWindow(void* parentWindow)
    {
        Require(parentWindow != nullptr, "The argument 'parent_window' must not be null.");
        if (OffScreen) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "An off screen view cannot have a parent window.");
        if (Embedded) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The view already has a parent window.");
        //
        RenderWindow->SetParentId(parentWindow);
        Interactor = vtkSmartPointer<vtkRenderWindowInteractor>::New();
        Interactor->SetInteractorStyle(vtkSmartPointer<vtkInteractorStyleTrackballCamera>::New());
        Interactor->SetRenderWindow(RenderWindow);
        // A style grabs the event focus between a button press and its release; only a passive observer still
        // sees the events of that drag, the release included
        InteractorCommand = vtkSmartPointer<vtkCallbackCommand>::New();
        InteractorCommand->SetCallback(&View::OnVtkEvent);
        InteractorCommand->SetClientData(this);
        InteractorCommand->PassiveObserverOn();
        Interactor->AddObserver(vtkCommand::LeftButtonPressEvent, InteractorCommand);
        Interactor->AddObserver(vtkCommand::LeftButtonReleaseEvent, InteractorCommand);
        Interactor->AddObserver(vtkCommand::MouseMoveEvent, InteractorCommand);
        // The host application pumps the messages, so the interactor is initialized and never started
        Interactor->Initialize();
        Embedded = true;
    }

    void View::SetSize(int width, int height)
    {
        Require(width > 0 && height > 0, "The view size must be positive.");
        RenderWindow->SetSize(width, height);
        if (Interactor != nullptr) Interactor->UpdateSize(width, height);
    }

    void View::GetSize(int& width, int& height)
    {
        int* size = RenderWindow->GetSize();
        width = size[0];
        height = size[1];
    }

    void View::SetEventCallback(vtkn_event_callback callback, void* userData)
    {
        Callback = callback;
        UserData = userData;
    }

    void View::SetBackground(const double* top, const double* bottom)
    {
        Require(top != nullptr && bottom != nullptr, "The background colors must not be null.");
        Renderer->SetBackground(bottom[0], bottom[1], bottom[2]);
        Renderer->SetBackground2(top[0], top[1], top[2]);
        Renderer->GradientBackgroundOn();
    }

    void View::Render()
    {
        RenderWindow->Render();
    }

    std::string View::GetRenderInfo()
    {
        vtkOpenGLRenderWindow* openGLWindow = vtkOpenGLRenderWindow::SafeDownCast(RenderWindow);
        if (openGLWindow == nullptr) return RenderWindow->GetClassName();
        if (!RenderWindow->GetNeverRendered()) openGLWindow->MakeCurrent();
        else RenderWindow->Render();
        const char* capabilities = openGLWindow->ReportCapabilities();
        return std::string(RenderWindow->GetClassName()) + "\n" + (capabilities != nullptr ? capabilities : "");
    }

    void View::GetCamera(double* values10)
    {
        Require(values10 != nullptr, "The argument 'values' must not be null.");
        vtkCamera* camera = Renderer->GetActiveCamera();
        camera->GetPosition(values10);
        camera->GetFocalPoint(values10 + 3);
        camera->GetViewUp(values10 + 6);
        values10[9] = camera->GetParallelScale();
    }

    void View::SetCamera(const double* values10)
    {
        Require(values10 != nullptr, "The argument 'values' must not be null.");
        vtkCamera* camera = Renderer->GetActiveCamera();
        camera->SetPosition(values10[0], values10[1], values10[2]);
        camera->SetFocalPoint(values10[3], values10[4], values10[5]);
        camera->SetViewUp(values10[6], values10[7], values10[8]);
        camera->SetParallelScale(values10[9]);
        camera->ParallelProjectionOn();
        Renderer->ResetCameraClippingRange();
    }

    void View::ResetCamera()
    {
        Renderer->GetActiveCamera()->ParallelProjectionOn();
        Renderer->ResetCamera();
    }

    void View::AddMeshActor(const std::string& name, const double* points, int pointCount, const int32_t* cells,
                            int cellsLength, const uint8_t* cellTypes, int cellCount, const double* color,
                            bool edgesVisible)
    {
        Require(!name.empty(), "The actor name must not be empty.");
        Require(points != nullptr && pointCount > 0, "The points must not be empty.");
        Require(cells != nullptr && cellsLength > 0, "The cells must not be empty.");
        Require(cellTypes != nullptr && cellCount > 0, "The cell types must not be empty.");
        Require(color != nullptr, "The color must not be null.");
        // The legacy layout is checked whole before anything is built
        std::vector<vtkIdType> legacy(static_cast<size_t>(cellsLength));
        int cellsRead = 0;
        for (int i = 0; i < cellsLength; cellsRead++)
        {
            int count = cells[i];
            Require(count > 0 && i + count < cellsLength, "The cells end inside a cell.");
            legacy[i] = count;
            for (int j = 1; j <= count; j++)
            {
                Require(cells[i + j] >= 0 && cells[i + j] < pointCount, "A cell refers to a point that does not exist.");
                legacy[i + j] = cells[i + j];
            }
            i += count + 1;
        }
        Require(cellsRead == cellCount, "The number of cells does not match the number of cell types.");
        //
        vtkNew<vtkDoubleArray> coordinates;
        coordinates->SetNumberOfComponents(3);
        coordinates->SetNumberOfTuples(pointCount);
        std::memcpy(coordinates->GetPointer(0), points, sizeof(double) * 3 * static_cast<size_t>(pointCount));
        vtkNew<vtkPoints> vtkPointsData;
        vtkPointsData->SetData(coordinates);
        //
        vtkNew<vtkCellArray> cellArray;
        cellArray->ImportLegacyFormat(legacy.data(), static_cast<vtkIdType>(legacy.size()));
        vtkNew<vtkUnsignedCharArray> types;
        types->SetNumberOfTuples(cellCount);
        std::memcpy(types->GetPointer(0), cellTypes, static_cast<size_t>(cellCount));
        //
        vtkNew<vtkUnstructuredGrid> grid;
        grid->SetPoints(vtkPointsData);
        grid->SetCells(types, cellArray);
        //
        vtkNew<vtkDataSetMapper> mapper;
        mapper->SetInputData(grid);
        mapper->ScalarVisibilityOff();
        vtkSmartPointer<vtkActor> actor = vtkSmartPointer<vtkActor>::New();
        actor->SetMapper(mapper);
        actor->GetProperty()->SetColor(color[0], color[1], color[2]);
        actor->GetProperty()->SetEdgeVisibility(edgesVisible ? 1 : 0);
        actor->GetProperty()->SetEdgeColor(0, 0, 0);
        //
        RemoveActor(name);
        Renderer->AddActor(actor);
        Actors[name] = actor;
    }

    void View::RemoveActor(const std::string& name)
    {
        auto entry = Actors.find(name);
        if (entry == Actors.end()) return;
        Renderer->RemoveActor(entry->second);
        Actors.erase(entry);
    }

    std::string View::GetActorNames()
    {
        std::string names;
        for (const auto& entry : Actors)
        {
            names += entry.first;
            names.push_back('\0');
        }
        return names;
    }

    std::vector<uint8_t> View::ReadPixels(int magnification, int& width, int& height)
    {
        Require(magnification >= 1 && magnification <= 8, "The magnification must be from 1 to 8.");
        vtkNew<vtkWindowToImageFilter> windowToImage;
        windowToImage->SetInput(RenderWindow);
        windowToImage->SetInputBufferTypeToRGB();
        windowToImage->SetScale(magnification);
        windowToImage->ReadFrontBufferOff();
        windowToImage->Update();
        vtkImageData* image = windowToImage->GetOutput();
        int* dimensions = image->GetDimensions();
        width = dimensions[0];
        height = dimensions[1];
        vtkDataArray* scalars = image->GetPointData()->GetScalars();
        if (scalars == nullptr || scalars->GetNumberOfComponents() != 3)
            throw Error(VTKN_ERROR_VTK, "The render window returned no RGB picture.");
        const uint8_t* data = static_cast<const uint8_t*>(scalars->GetVoidPointer(0));
        return std::vector<uint8_t>(data, data + static_cast<size_t>(width) * height * 3);
    }

    void View::WritePng(const std::string& fileName, int magnification)
    {
        Require(!fileName.empty(), "The file name must not be empty.");
        Require(magnification >= 1 && magnification <= 8, "The magnification must be from 1 to 8.");
        vtkNew<vtkWindowToImageFilter> windowToImage;
        windowToImage->SetInput(RenderWindow);
        windowToImage->SetInputBufferTypeToRGB();
        windowToImage->SetScale(magnification);
        windowToImage->ReadFrontBufferOff();
        vtkNew<vtkPNGWriter> writer;
        writer->SetInputConnection(windowToImage->GetOutputPort());
        writer->SetFileName(fileName.c_str());
        writer->Write();
        if (writer->GetErrorCode() != 0)
            throw Error(VTKN_ERROR_VTK, "The PNG file could not be written: " + fileName);
    }

    void View::OnVtkEvent(vtkObject* caller, unsigned long eventId, void* clientData, void*)
    {
        View* view = static_cast<View*>(clientData);
        if (view->Callback == nullptr) return;
        //
        if (eventId == vtkCommand::EndEvent)
        {
            view->RaiseEvent(VTKN_EVENT_RENDER_END, nullptr, 0);
        }
        else if (eventId == vtkCommand::ModifiedEvent)
        {
            double values[10];
            view->GetCamera(values);
            view->RaiseEvent(VTKN_EVENT_CAMERA_MODIFIED, values, 10);
        }
        else if (view->Interactor != nullptr && caller == view->Interactor.GetPointer())
        {
            int* position = view->Interactor->GetEventPosition();
            double values[4] = { static_cast<double>(position[0]), static_cast<double>(position[1]),
                                 static_cast<double>(view->Interactor->GetControlKey()),
                                 static_cast<double>(view->Interactor->GetShiftKey()) };
            if (eventId == vtkCommand::LeftButtonPressEvent) view->RaiseEvent(VTKN_EVENT_LEFT_BUTTON_DOWN, values, 4);
            else if (eventId == vtkCommand::LeftButtonReleaseEvent)
                view->RaiseEvent(VTKN_EVENT_LEFT_BUTTON_UP, values, 4);
            else if (eventId == vtkCommand::MouseMoveEvent) view->RaiseEvent(VTKN_EVENT_MOUSE_MOVE, values, 4);
        }
    }

    // The callback belongs to the host; nothing it does may unwind through VTK
    void View::RaiseEvent(int32_t eventId, const double* values, int32_t count)
    {
        try
        {
            Callback(UserData, eventId, values, count);
        }
        catch (...)
        {
        }
    }
}
