// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeBorderWidget.h"

#include <vtkCellArray.h>
#include <vtkNew.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty2D.h>

#include <cmath>

namespace vtkn
{
    BorderWidget::BorderWidget()
    {
        PositionCoordinate = vtkSmartPointer<vtkCoordinate>::New();
        PositionCoordinate->SetCoordinateSystemToDisplay();
        // Border
        BorderPoints = vtkSmartPointer<vtkPoints>::New();
        BorderPoints->InsertNextPoint(Position[0], Position[1], 0);
        BorderPoints->InsertNextPoint(Position[0] + Size[0], Position[1], 0);
        BorderPoints->InsertNextPoint(Position[0] + Size[0], Position[1] + Size[1], 0);
        BorderPoints->InsertNextPoint(Position[0], Position[1] + Size[1], 0);
        vtkNew<vtkCellArray> lines;
        for (int i = 0; i < 4; i++)
        {
            lines->InsertNextCell(2);
            lines->InsertCellPoint(i);
            lines->InsertCellPoint((i + 1) % 4);
        }
        BorderPolyData = vtkSmartPointer<vtkPolyData>::New();
        BorderPolyData->SetPoints(BorderPoints);
        BorderPolyData->SetLines(lines);
        vtkNew<vtkPolyDataMapper2D> borderMapper;
        borderMapper->SetInputData(BorderPolyData);
        BorderActor = vtkSmartPointer<vtkActor2D>::New();
        BorderActor->SetMapper(borderMapper);
        BorderActor->GetProperty()->SetColor(0, 0, 0);
        // Background
        vtkNew<vtkCellArray> backgroundPolygon;
        backgroundPolygon->InsertNextCell(4);
        for (int i = 0; i < 4; i++) backgroundPolygon->InsertCellPoint(i);
        vtkNew<vtkPolyData> backgroundPolyData;
        backgroundPolyData->SetPoints(BorderPoints);
        backgroundPolyData->SetPolys(backgroundPolygon);
        vtkNew<vtkPolyDataMapper2D> backgroundMapper;
        backgroundMapper->SetInputData(backgroundPolyData);
        BackgroundActor = vtkSmartPointer<vtkActor2D>::New();
        BackgroundActor->SetMapper(backgroundMapper);
        BackgroundActor->GetProperty()->SetColor(1, 1, 1);
    }

    void BorderWidget::SetRenderer(vtkRenderer* renderer)
    {
        Renderer = renderer;
        HighlightRenderer = renderer;
        if (Visibility)
        {
            if (BackgroundVisibility) Renderer->AddActor(BackgroundActor);
            if (BorderVisibility) Renderer->AddActor(BorderActor);
            Highlight.AddToRenderer(HighlightRenderer);
        }
    }

    void BorderWidget::RemoveRenderer()
    {
        if (Renderer != nullptr)
        {
            Renderer->RemoveActor(BackgroundActor);
            Renderer->RemoveActor(BorderActor);
        }
        if (HighlightRenderer != nullptr) Highlight.RemoveFromRenderer(HighlightRenderer);
        Renderer = nullptr;
        HighlightRenderer = nullptr;
    }

    void BorderWidget::VisibilityOn()
    {
        if (Visibility) return;
        //
        OnSizeChanged();
        Visibility = true;
        if (BackgroundVisibility) Renderer->AddActor(BackgroundActor);
        if (BorderVisibility) Renderer->AddActor(BorderActor);
        if (HighlightRenderer != nullptr) Highlight.AddToRenderer(HighlightRenderer);
        if (ShownHandler) ShownHandler();
    }

    void BorderWidget::VisibilityOff()
    {
        if (!Visibility) return;
        //
        Visibility = false;
        Renderer->RemoveActor(BorderActor);
        Renderer->RemoveActor(BackgroundActor);
        if (HighlightRenderer != nullptr) Highlight.RemoveFromRenderer(HighlightRenderer);
    }

    // The C# widget added the border and the background even to a hidden widget, which then showed them; a
    // hidden widget only remembers them for VisibilityOn
    void BorderWidget::BorderVisibilityOn()
    {
        if (BorderVisibility) return;
        BorderVisibility = true;
        if (Visibility) Renderer->AddActor(BorderActor);
    }

    void BorderWidget::BorderVisibilityOff()
    {
        if (!BorderVisibility) return;
        BorderVisibility = false;
        Renderer->RemoveActor(BorderActor);
    }

    void BorderWidget::BackgroundVisibilityOn()
    {
        if (BackgroundVisibility) return;
        BackgroundVisibility = true;
        if (!Visibility) return;
        if (BorderVisibility) Renderer->RemoveActor(BorderActor);
        Renderer->AddActor(BackgroundActor);
        if (BorderVisibility) Renderer->AddActor(BorderActor);
    }

    void BorderWidget::BackgroundVisibilityOff()
    {
        if (!BackgroundVisibility) return;
        BackgroundVisibility = false;
        Renderer->RemoveActor(BackgroundActor);
    }

    void BorderWidget::SetLocalHighlightEnabled(bool enabled)
    {
        if (LocalHighlightEnabled == enabled) return;
        LocalHighlightEnabled = enabled;
        Highlight.Clear();
    }

    // The renderWindow_ModifiedEvt handler: the window size is compared with the last one seen
    void BorderWidget::CheckWindowSize()
    {
        if (Renderer == nullptr) return;
        int* windowSize = Renderer->GetSize();
        if (windowSize[0] != PrevWindowSize[0] || windowSize[1] != PrevWindowSize[1]) OnRenderWindowModified();
        windowSize = Renderer->GetSize();
        PrevWindowSize[0] = windowSize[0];
        PrevWindowSize[1] = windowSize[1];
    }

    // A resized window moves the widget with the corner of its quadrant
    void BorderWidget::OnRenderWindowModified()
    {
        if (!Visibility) return;
        if (PrevWindowSize[0] == 0) return;
        //
        int* windowSize = Renderer->GetSize();
        if (windowSize[0] == PrevWindowSize[0] && windowSize[1] == PrevWindowSize[1]) return;
        if (Anchor == VTKN_ANCHOR_HORIZONTALLY_RELATIVE)
        {
            double center = (Position[0] + Size[0] / 2) / PrevWindowSize[0];
            Position[0] = windowSize[0] * center - Size[0] / 2;
        }
        else
        {
            double dx = 0;
            double dy = 0;
            if (Quadrant == 2 || Quadrant == 3) dx = windowSize[0] - PrevWindowSize[0];
            if (Quadrant == 3 || Quadrant == 4) dy = windowSize[1] - PrevWindowSize[1];
            if (ScaleBySectors)
            {
                Position[0] += dx;
                Position[1] += dy;
                OnMovedOrSizeChanged();
            }
        }
    }

    void BorderWidget::OnMovedOrSizeChanged()
    {
        if (Renderer == nullptr) return;
        int* size = Renderer->GetSize();
        if (size[0] == 0 && size[1] == 0) return;
        int windowSize[2] = { size[0], size[1] };
        //
        Highlight.Clear();
        //
        double max[2] = { windowSize[0] - Size[0], windowSize[1] - Size[1] };
        if (Anchor == VTKN_ANCHOR_TOP_LEFT) Position[1] = windowSize[1] - Size[1] - Position[1];
        if (KeepInsideVisibleBounds && Size[0] < windowSize[0] && Size[1] < windowSize[1])
        {
            if (Position[0] < 0) Position[0] = 0;
            else if (Position[0] > max[0]) Position[0] = max[0];
            if (Position[1] < 0) Position[1] = 0;
            else if (Position[1] > max[1]) Position[1] = max[1];
        }
        if (Anchor == VTKN_ANCHOR_TOP_LEFT) Position[1] = windowSize[1] - Size[1] - Position[1];
        //
        double center[2] = { Position[0] + Size[0] / 2, Position[1] + Size[1] / 2 };
        double rendererCenter[2] = { windowSize[0] * 0.5, windowSize[1] * 0.5 };
        if (center[0] < rendererCenter[0] && center[1] < rendererCenter[1]) Quadrant = 1;
        else if (center[0] > rendererCenter[0] && center[1] < rendererCenter[1]) Quadrant = 2;
        else if (center[0] > rendererCenter[0] && center[1] > rendererCenter[1]) Quadrant = 3;
        else if (center[0] < rendererCenter[0] && center[1] > rendererCenter[1]) Quadrant = 4;
        //
        UpdateBorderGeometry();
    }

    void BorderWidget::OnSizeChanged()
    {
        Highlight.Clear();
        OnMovedOrSizeChanged();
    }

    double BorderWidget::PositionY(const int* rendererSize) const
    {
        return Anchor == VTKN_ANCHOR_TOP_LEFT ? rendererSize[1] - Size[1] - Position[1] : Position[1];
    }

    bool BorderWidget::LeftButtonPress(int x, int y, int clicks)
    {
        if (!Visibility) return false;
        //
        double positionY = PositionY(Renderer->GetSize());
        if (x >= Position[0] && x <= Position[0] + Size[0] && y >= positionY && y <= positionY + Size[1])
        {
            if (clicks == 2)
            {
                if (DoubleClickHandler) DoubleClickHandler(x, y);
                return true;
            }
            Clicked = true;
            ClickPos[0] = x;
            ClickPos[1] = y;
            return true;
        }
        return false;
    }

    bool BorderWidget::LeftButtonRelease(int, int)
    {
        if (!Visibility) return false;
        //
        if (!Clicked) return false;
        Clicked = false;
        return true;
    }

    bool BorderWidget::MouseOver(int x, int y)
    {
        return GetDisplayRectangle().Contains(x, y);
    }

    bool BorderWidget::MouseMove(int x, int y)
    {
        if (!Visibility || !Clicked) return false;
        //
        int delta[2] = { x - ClickPos[0], y - ClickPos[1] };
        Position[0] += delta[0];
        if (Anchor == VTKN_ANCHOR_TOP_LEFT) Position[1] -= delta[1];
        else Position[1] += delta[1];
        ClickPos[0] = x;
        ClickPos[1] = y;
        //
        UpdateBorderGeometry();
        OnMovedOrSizeChanged();
        return true;
    }

    bool BorderWidget::UpdateMouseHover(int x, int y, bool mouseOver, bool highlightEntireWidget)
    {
        Rect rectangle;
        Rect displayRectangle = GetDisplayRectangle();
        if (mouseOver)
        {
            Rect localRectangle = LocalHighlightEnabled ? GetHighlightRectangle(x, y) : displayRectangle;
            bool internalHighlight = !localRectangle.IsEmpty() && localRectangle != displayRectangle;
            rectangle = highlightEntireWidget && !internalHighlight ? displayRectangle : localRectangle;
        }
        return Highlight.SetRectangle(rectangle, GetHighlightFillOpacity(rectangle));
    }

    bool BorderWidget::ClearMouseHover()
    {
        return Highlight.Clear();
    }

    void BorderWidget::BringToFront()
    {
        if (!Visibility || Renderer == nullptr) return;
        //
        if (BackgroundVisibility)
        {
            Renderer->RemoveActor(BackgroundActor);
            Renderer->AddActor(BackgroundActor);
        }
        if (BorderVisibility)
        {
            Renderer->RemoveActor(BorderActor);
            Renderer->AddActor(BorderActor);
        }
    }

    void BorderWidget::BringHighlightToFront()
    {
        if (!Visibility || HighlightRenderer == nullptr) return;
        Highlight.BringToFront(HighlightRenderer);
    }

    Rect BorderWidget::GetDisplayRectangle()
    {
        if (!Visibility || Renderer == nullptr) return Rect();
        //
        double positionY = PositionY(Renderer->GetSize());
        int left = static_cast<int>(std::floor(Position[0]));
        int bottom = static_cast<int>(std::floor(positionY));
        int right = static_cast<int>(std::ceil(Position[0] + Size[0]));
        int top = static_cast<int>(std::ceil(positionY + Size[1]));
        return Rect::FromLTRB(left, bottom, right, top);
    }

    Rect BorderWidget::GetHighlightRectangle(int x, int y)
    {
        Rect rectangle = GetDisplayRectangle();
        return rectangle.Contains(x, y) ? rectangle : Rect();
    }

    void BorderWidget::UpdateBorderGeometry()
    {
        double x = Position[0];
        double y = PositionY(Renderer->GetSize());
        PositionCoordinate->SetValue(x, y, 0);
        BorderPoints->SetPoint(0, x, y, 0);
        BorderPoints->SetPoint(1, x + Size[0], y, 0);
        BorderPoints->SetPoint(2, x + Size[0], y + Size[1], 0);
        BorderPoints->SetPoint(3, x, y + Size[1], 0);
        BorderPoints->Modified();
    }

    void BorderWidget::SetPosition(vtkn_widget_anchor anchor, double x, double y)
    {
        Anchor = anchor;
        Position[0] = x;
        Position[1] = y;
        OnMovedOrSizeChanged();
    }

    void BorderWidget::SetBorderColor(double r, double g, double b)
    {
        BorderActor->GetProperty()->SetColor(r, g, b);
    }

    void BorderWidget::SetBackgroundColor(double r, double g, double b)
    {
        BackgroundActor->GetProperty()->SetColor(r, g, b);
    }

    Rect BorderWidget::GetRectangle() const
    {
        return Rect(static_cast<int>(std::round(Position[0])), static_cast<int>(std::round(Position[1])),
                    static_cast<int>(std::round(Size[0])), static_cast<int>(std::round(Size[1])));
    }
}
