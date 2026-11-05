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

#include <algorithm>
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

    // The renderer setup of vtkControl.OnHandleCreated and InitializeControl: layered renderers sharing one camera,
    // three camera lights, multisampling, a parallel projection and a gray gradient background, and the widget layer
    View::View(bool offScreen) : OffScreen(offScreen)
    {
        Renderer = vtkSmartPointer<vtkRenderer>::New();
        Renderer->SetLayer(0);
        OverlayRenderer = vtkSmartPointer<vtkRenderer>::New();
        OverlayRenderer->SetLayer(1);
        OverlayRenderer->InteractiveOff();
        OverlayRenderer->SetActiveCamera(Renderer->GetActiveCamera());
        WidgetRenderer = vtkSmartPointer<vtkRenderer>::New();
        WidgetRenderer->SetLayer(2);
        WidgetRenderer->InteractiveOff();
        WidgetRenderer->SetActiveCamera(Renderer->GetActiveCamera());
        RenderWindow = vtkSmartPointer<vtkNativeRenderWindow>::New();
        RenderWindow->SetOffScreenRendering(offScreen ? 1 : 0);
        RenderWindow->SetNumberOfLayers(3);
        RenderWindow->AddRenderer(Renderer);
        RenderWindow->AddRenderer(OverlayRenderer);
        RenderWindow->AddRenderer(WidgetRenderer);
        RenderWindow->PointSmoothingOn();
        RenderWindow->LineSmoothingOn();
        // VTK 9 has no line smoothing in its core profile, so the lines are smoothed only by multisampling, and
        // two samples leave lines thinner than a pixel dotted
        RenderWindow->SetMultiSamples(8);
        // Camera lights are in camera coordinates: the camera at (0, 0, 1) looks at the origin, up is (0, 1, 0)
        const double lightPositions[3][3] = { { -1, 1, 1 }, { 1, 1, 1 }, { 0, -1, 0 } };
        for (const auto& position : lightPositions)
        {
            vtkSmartPointer<vtkLight> light = vtkSmartPointer<vtkLight>::New();
            light->SetPosition(position[0], position[1], position[2]);
            light->SetFocalPoint(0, 0, 0);
            light->SetColor(1, 1, 1);
            light->SetIntensity(0.4);
            light->SetLightTypeToCameraLight();
            Renderer->AddLight(light);
            OverlayRenderer->AddLight(light);
            Lights.push_back(light);
        }
        //
        Renderer->SetBackground(0.4, 0.4, 0.4);
        Renderer->SetBackground2(0.8, 0.8, 0.8);
        Renderer->GradientBackgroundOn();
        Renderer->GetActiveCamera()->ParallelProjectionOn();
        vtkMapper::SetResolveCoincidentTopologyToPolygonOffset();
        // VTK 9 leaves the polygons in place and moves the lines forward by a constant only, which loses to the
        // depth slope of an oblique face, so the polygons are pushed back by their slope as in VTK 8 and the lines
        // pulled forward
        vtkMapper::SetResolveCoincidentTopologyPolygonOffsetParameters(2.0, 1.0);
        vtkMapper::SetResolveCoincidentTopologyLineOffsetParameters(-1.0, -8.0);
        //
        EventCommand = vtkSmartPointer<vtkCallbackCommand>::New();
        EventCommand->SetCallback(&View::OnVtkEvent);
        EventCommand->SetClientData(this);
        Renderer->AddObserver(vtkCommand::EndEvent, EventCommand);
        Renderer->GetActiveCamera()->AddObserver(vtkCommand::ModifiedEvent, EventCommand);
        // The widgets follow the window size the way they followed the ModifiedEvt of the ActiViz window
        WidgetCommand = vtkSmartPointer<vtkCallbackCommand>::New();
        WidgetCommand->SetCallback(&View::OnWidgetEvent);
        WidgetCommand->SetClientData(this);
        RenderWindow->AddObserver(vtkCommand::ModifiedEvent, WidgetCommand);
        Widgets.SetDoubleClickHandler([this](BorderWidget* widget, int x, int y) {
            for (const auto& bar : ScalarBars)
            {
                if (&bar->Widget != widget) continue;
                double values[3] = { static_cast<double>(bar->Id), static_cast<double>(x), static_cast<double>(y) };
                RaiseEvent(VTKN_EVENT_WIDGET_DOUBLE_CLICK, values, 3);
            }
            for (const auto& areaWidget : AreaWidgets)
            {
                if (areaWidget->Content.get() != widget) continue;
                double values[3] = { static_cast<double>(areaWidget->Id), static_cast<double>(x), static_cast<double>(y) };
                RaiseEvent(VTKN_EVENT_WIDGET_DOUBLE_CLICK, values, 3);
            }
        });
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
        RenderWindow->RemoveObserver(WidgetCommand);
        while (!ScalarBars.empty()) DestroyScalarBar(ScalarBars.back().get());
        while (!AreaWidgets.empty()) DestroyWidget(AreaWidgets.back().get());
        Renderer->RemoveObserver(EventCommand);
        Renderer->GetActiveCamera()->RemoveObserver(EventCommand);
        Renderer->RemoveAllViewProps();
        OverlayRenderer->RemoveAllViewProps();
        Actors.clear();
        Parts.clear();
        if (Interactor != nullptr)
        {
            Interactor->RemoveObserver(InteractorCommand);
            Interactor->RemoveObserver(WidgetCommand);
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
        // The widgets see the mouse before the style and keep a press on a widget from moving the camera
        Interactor->AddObserver(vtkCommand::LeftButtonPressEvent, WidgetCommand, 1.0f);
        Interactor->AddObserver(vtkCommand::LeftButtonReleaseEvent, WidgetCommand, 1.0f);
        Interactor->AddObserver(vtkCommand::MouseMoveEvent, WidgetCommand, 1.0f);
        // The host application pumps the messages, so the interactor is initialized and never started
        Interactor->Initialize();
        Embedded = true;
    }

    void View::SetSize(int width, int height)
    {
        Require(width > 0 && height > 0, "The view size must be positive.");
        RenderWindow->SetSize(width, height);
        if (Interactor != nullptr) Interactor->UpdateSize(width, height);
        Widgets.CheckWindowSize();
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

    void View::SetBackground(bool gradient, const double* top, const double* bottom)
    {
        Require(top != nullptr && bottom != nullptr, "The background colors must not be null.");
        Renderer->SetBackground(bottom[0], bottom[1], bottom[2]);
        Renderer->SetBackground2(top[0], top[1], top[2]);
        Renderer->SetGradientBackground(gradient);
    }

    void View::SetSmoothing(bool pointSmoothing, bool lineSmoothing)
    {
        RenderWindow->SetPointSmoothing(pointSmoothing);
        RenderWindow->SetLineSmoothing(lineSmoothing);
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

    // Port of AddCells for a part: the actor is built, formatted by the edges mode, and the camera distance and
    // clipping range follow the new bounds without a render
    void View::AddPartActor(const std::string& name, const vtkn_part_style& style, const vtkn_mesh& geometry,
                            const vtkn_mesh* modelEdges, const vtkn_mesh* modelEdgesToDraw)
    {
        Require(!name.empty(), "The part name must not be empty.");
        std::unique_ptr<PartActor> part(new PartActor(style, geometry, modelEdges, modelEdgesToDraw));
        RemoveActor(name);
        part->AddTo(Renderer);
        part->ApplyEdgesVisibility(EdgesVisibility);
        Parts[name] = std::move(part);
        AdjustCameraDistanceAndClipping();
    }

    void View::RemoveActor(const std::string& name)
    {
        auto entry = Actors.find(name);
        if (entry != Actors.end())
        {
            Renderer->RemoveActor(entry->second);
            Actors.erase(entry);
        }
        auto part = Parts.find(name);
        if (part != Parts.end())
        {
            part->second->RemoveFrom(Renderer);
            Parts.erase(part);
        }
    }

    void View::ClearActors()
    {
        for (const auto& entry : Actors) Renderer->RemoveActor(entry.second);
        for (const auto& entry : Parts) entry.second->RemoveFrom(Renderer);
        Actors.clear();
        Parts.clear();
    }

    std::string View::GetActorNames()
    {
        std::vector<std::string> sorted;
        for (const auto& entry : Actors) sorted.push_back(entry.first);
        for (const auto& entry : Parts) sorted.push_back(entry.first);
        std::sort(sorted.begin(), sorted.end());
        std::string names;
        for (const std::string& name : sorted)
        {
            names += name;
            names.push_back('\0');
        }
        return names;
    }

    void View::SetEdgesVisibility(vtkn_edges_visibility edgesVisibility)
    {
        Require(edgesVisibility >= VTKN_EDGES_WIREFRAME && edgesVisibility <= VTKN_EDGES_NONE,
                "The edges visibility is not one of vtkn_edges_visibility.");
        EdgesVisibility = edgesVisibility;
        for (const auto& entry : Parts) entry.second->ApplyEdgesVisibility(EdgesVisibility);
    }

    void View::SetLighting(double ambient, double diffuse)
    {
        for (const auto& entry : Parts) entry.second->SetGlobalLighting(ambient, diffuse);
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

    void View::OnWidgetEvent(vtkObject* caller, unsigned long eventId, void* clientData, void*)
    {
        View* view = static_cast<View*>(clientData);
        if (eventId == vtkCommand::ModifiedEvent)
        {
            view->Widgets.CheckWindowSize();
            return;
        }
        int* position = view->Interactor->GetEventPosition();
        int widgetEvent = eventId == vtkCommand::LeftButtonPressEvent ? VTKN_EVENT_LEFT_BUTTON_DOWN
                        : eventId == vtkCommand::LeftButtonReleaseEvent ? VTKN_EVENT_LEFT_BUTTON_UP
                        : VTKN_EVENT_MOUSE_MOVE;
        try
        {
            if (view->HandleMouseEvent(widgetEvent, position[0], position[1]))
                static_cast<vtkCommand*>(view->WidgetCommand)->AbortFlagOn();
        }
        catch (...)
        {
        }
        (void)caller;
    }

    // The widget part of the press, release and move handlers of vtkInteractorStyleControl
    bool View::HandleMouseEvent(int eventId, int x, int y)
    {
        bool handled = false;
        bool changed = false;
        if (eventId == VTKN_EVENT_LEFT_BUTTON_DOWN) changed = Widgets.LeftButtonPress(x, y, handled);
        else if (eventId == VTKN_EVENT_LEFT_BUTTON_UP) changed = Widgets.LeftButtonRelease(x, y, handled);
        else if (eventId == VTKN_EVENT_MOUSE_MOVE) changed = Widgets.MouseMove(x, y, handled);
        else throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The event is not a mouse button or move event.");
        if (changed) RenderWindow->Render();
        return handled;
    }

    ScalarBar* View::CreateScalarBar()
    {
        std::unique_ptr<ScalarBar> bar(new ScalarBar(this, NextWidgetId++));
        bar->Widget.SetRenderer(WidgetRenderer);
        Widgets.Add(&bar->Widget);
        ScalarBars.push_back(std::move(bar));
        return ScalarBars.back().get();
    }

    void View::DestroyScalarBar(ScalarBar* bar)
    {
        for (auto entry = ScalarBars.begin(); entry != ScalarBars.end(); ++entry)
        {
            if (entry->get() != bar) continue;
            Widgets.Remove(&bar->Widget);
            bar->Widget.RemoveRenderer();
            ScalarBars.erase(entry);
            return;
        }
        throw Error(VTKN_ERROR_INVALID_HANDLE, "The scalar bar does not belong to this view.");
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
