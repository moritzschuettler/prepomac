// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The interaction style of a view: made on first use, attached to the window's interactor of an embedded view, and
// to an interactor of its own that a host feeds when the view is drawn off screen.
#include "vtkNativeView.h"

#include <vtkCommand.h>
#include <vtkGenericRenderWindowInteractor.h>

namespace vtkn
{
    InteractorStyle* View::GetInteractorStyle()
    {
        if (Style == nullptr)
        {
            Style = vtkSmartPointer<InteractorStyle>::New();
            // The rubber band of the box selection is drawn in the selection layer, as in vtkControl
            Style->SetView(this, OverlayRenderer, SelectionRenderer);
        }
        // An off screen view gets an interactor of its own, which the host feeds; the style needs one even for the
        // navigation calls
        if (Interactor == nullptr && ScriptInteractor == nullptr)
        {
            ScriptInteractor = vtkSmartPointer<vtkGenericRenderWindowInteractor>::New();
            ScriptInteractor->SetRenderWindow(RenderWindow);
            ScriptInteractor->SetInteractorStyle(Style);
            ScriptInteractor->Initialize();
        }
        return Style;
    }

    // The window of an embedded view feeds the widgets from an observer above the style that aborts what a widget
    // took; the input of a host takes the same way
    void View::HandleInput(int inputEvent, int x, int y, bool ctrl, bool shift, int keyCode, const char* keySym)
    {
        InteractorStyle* style = GetInteractorStyle();
        vtkRenderWindowInteractor* interactor = Interactor != nullptr ? Interactor.GetPointer() : ScriptInteractor.GetPointer();
        interactor->SetEventInformation(x, y, ctrl ? 1 : 0, shift ? 1 : 0, static_cast<char>(keyCode), 0, keySym);
        bool widgetsFirst = interactor == ScriptInteractor && style->GetState() == 0 && !style->GetAnimating();
        unsigned long eventId = vtkCommand::NoEvent;
        switch (inputEvent)
        {
            case VTKN_INPUT_LEFT_BUTTON_DOWN:
                if (widgetsFirst && HandleMouseEvent(VTKN_EVENT_LEFT_BUTTON_DOWN, x, y)) return;
                eventId = vtkCommand::LeftButtonPressEvent;
                break;
            case VTKN_INPUT_LEFT_BUTTON_UP:
                if (widgetsFirst && HandleMouseEvent(VTKN_EVENT_LEFT_BUTTON_UP, x, y)) return;
                eventId = vtkCommand::LeftButtonReleaseEvent;
                break;
            case VTKN_INPUT_MOUSE_MOVE:
                Widgets.LastMoveChanged = false;
                if (widgetsFirst && HandleMouseEvent(VTKN_EVENT_MOUSE_MOVE, x, y)) return;
                eventId = vtkCommand::MouseMoveEvent;
                break;
            case VTKN_INPUT_MIDDLE_BUTTON_DOWN: eventId = vtkCommand::MiddleButtonPressEvent; break;
            case VTKN_INPUT_MIDDLE_BUTTON_UP: eventId = vtkCommand::MiddleButtonReleaseEvent; break;
            case VTKN_INPUT_RIGHT_BUTTON_DOWN: eventId = vtkCommand::RightButtonPressEvent; break;
            case VTKN_INPUT_RIGHT_BUTTON_UP: eventId = vtkCommand::RightButtonReleaseEvent; break;
            case VTKN_INPUT_WHEEL_FORWARD: eventId = vtkCommand::MouseWheelForwardEvent; break;
            case VTKN_INPUT_WHEEL_BACKWARD: eventId = vtkCommand::MouseWheelBackwardEvent; break;
            case VTKN_INPUT_KEY_PRESS: eventId = vtkCommand::KeyPressEvent; break;
            case VTKN_INPUT_KEY_RELEASE: eventId = vtkCommand::KeyReleaseEvent; break;
            case VTKN_INPUT_LEAVE: eventId = vtkCommand::LeaveEvent; break;
            default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The input event is not one of vtkn_input_event.");
        }
        interactor->InvokeEvent(eventId, nullptr);
    }
}
