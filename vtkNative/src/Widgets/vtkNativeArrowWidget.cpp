// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeArrowWidget.h"
#include "vtkNativePixelLines.h"

#include <vtkArrowSource.h>
#include <vtkCamera.h>
#include <vtkCellArray.h>
#include <vtkDoubleArray.h>
#include <vtkNew.h>
#include <vtkPointData.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty2D.h>
#include <vtkTransform.h>
#include <vtkTransformFilter.h>

namespace vtkn
{
    ArrowWidget::ArrowWidget()
    {
        ScaleBySectors = true;
        // Leader
        LeaderPolyData = vtkSmartPointer<vtkPolyData>::New();
        vtkNew<vtkPoints> leaderPoints;
        leaderPoints->SetNumberOfPoints(2);
        leaderPoints->SetPoint(0, 100, 100, 0);
        leaderPoints->SetPoint(1, 0, 0, 0);
        LeaderPolyData->SetPoints(leaderPoints);
        vtkNew<vtkCellArray> leader;
        leader->InsertNextCell(2);
        leader->InsertCellPoint(0);
        leader->InsertCellPoint(1);
        LeaderPolyData->SetLines(leader);
        vtkNew<vtkPolyDataMapper2D> leaderMapper;
        SetPixelCenteredInput(leaderMapper, LeaderPolyData);
        LeaderActor = vtkSmartPointer<vtkActor2D>::New();
        LeaderActor->SetMapper(leaderMapper);
        LeaderActor->GetProperty()->SetColor(0, 0, 0);
        // Arrow head: a glyph whose tip is at the point
        vtkNew<vtkArrowSource> arrowSource;
        arrowSource->SetTipLength(1);
        arrowSource->SetTipRadius(0.2);
        arrowSource->SetShaftRadius(0.3);
        vtkNew<vtkTransform> transform;
        transform->Translate(-1, 0, 0);
        vtkNew<vtkTransformFilter> translatedArrow;
        translatedArrow->SetTransform(transform);
        translatedArrow->SetInputConnection(arrowSource->GetOutputPort());
        HeadPolyData = vtkSmartPointer<vtkPolyData>::New();
        vtkNew<vtkPoints> headPoints;
        headPoints->SetNumberOfPoints(1);
        headPoints->SetPoint(0, 0, 0, 0);
        HeadPolyData->SetPoints(headPoints);
        vtkNew<vtkDoubleArray> vectors;
        vectors->SetNumberOfComponents(3);
        vectors->SetNumberOfTuples(1);
        vectors->SetTuple3(0, 1, 1, 1);
        HeadPolyData->GetPointData()->SetVectors(vectors);
        HeadGlyph = vtkSmartPointer<vtkGlyph3D>::New();
        HeadGlyph->SetSourceConnection(translatedArrow->GetOutputPort());
        HeadGlyph->SetInputData(HeadPolyData);
        HeadGlyph->SetScaleModeToDataScalingOff();
        HeadGlyph->SetScaleFactor(20);
        HeadGlyph->Update();
        vtkNew<vtkPolyDataMapper2D> arrowMapper;
        arrowMapper->SetInputConnection(HeadGlyph->GetOutputPort());
        HeadActor = vtkSmartPointer<vtkActor2D>::New();
        HeadActor->SetMapper(arrowMapper);
        HeadActor->GetProperty()->SetColor(0, 0, 0);
    }

    void ArrowWidget::WorldToDisplay(const double world[3], double display[2])
    {
        Renderer->SetWorldPoint(world[0], world[1], world[2], 1.0);
        Renderer->WorldToDisplay();
        double* point = Renderer->GetDisplayPoint();
        display[0] = point[0];
        display[1] = point[1];
    }

    void ArrowWidget::RecomputeLeader()
    {
        double end[2];
        double start[2];
        RecomputeLeaderEnd(end);
        RecomputeLeaderStart(start);
        HeadPolyData->GetPoints()->SetPoint(0, end[0], end[1], 0);
        HeadPolyData->GetPointData()->GetVectors()->SetTuple3(0, end[0] - start[0], end[1] - start[1], 0);
        HeadPolyData->GetPoints()->Modified();
        HeadPolyData->GetPointData()->GetVectors()->Modified();
        HeadPolyData->Modified();
    }

    // The leader leaves the box at the middle of the side that faces the head
    void ArrowWidget::RecomputeLeaderStart(double start[2])
    {
        vtkPoints* points = LeaderPolyData->GetPoints();
        double head[3];
        points->GetPoint(1, head);
        if (head[0] < Position[0])
        {
            start[0] = Position[0];
            start[1] = Position[1] + Size[1] / 2;
        }
        else if (head[0] > Position[0] + Size[0])
        {
            start[0] = Position[0] + Size[0];
            start[1] = Position[1] + Size[1] / 2;
        }
        else if (head[1] < Position[1])
        {
            start[0] = Position[0] + Size[0] / 2;
            start[1] = Position[1];
        }
        else
        {
            start[0] = Position[0] + Size[0] / 2;
            start[1] = Position[1] + Size[1];
        }
        points->SetPoint(0, start[0], start[1], 0);
        points->Modified();
    }

    void ArrowWidget::RecomputeLeaderEnd(double end[2])
    {
        WorldToDisplay(WorldAnchorPoint, end);
        LeaderPolyData->GetPoints()->SetPoint(1, end[0], end[1], 0);
        LeaderPolyData->GetPoints()->Modified();
    }

    void ArrowWidget::OnRenderWindowModified()
    {
        if (!Visibility) return;
        TextWidget::OnRenderWindowModified();
        if (AnchorAlreadySet) SetWorldTopLeftFromPosition();
        RecomputeLeader();
    }

    void ArrowWidget::CameraModified()
    {
        if (!Visibility) return;
        SetPositionFromWorldTopLeft();
        RecomputeLeader();
    }

    // The camera reference is the top left point, so the text can be resized independently
    void ArrowWidget::OnSizeChanged()
    {
        TextWidget::OnSizeChanged();
        if (AnchorAlreadySet && Renderer != nullptr) SetPositionFromWorldTopLeft();
        if (Renderer != nullptr) RecomputeLeader();
    }

    void ArrowWidget::MiddleButtonPress(int, int)
    {
        if (!Visibility) return;
        RecomputeLeader();
    }

    void ArrowWidget::MiddleButtonRelease(int, int)
    {
        if (!Visibility) return;
        SetPositionFromWorldTopLeft();
        RecomputeLeader();
    }

    bool ArrowWidget::MouseMove(int x, int y)
    {
        if (!Visibility) return false;
        if (!TextWidget::MouseMove(x, y)) return false;
        SetWorldTopLeftFromPosition();
        RecomputeLeader();
        return true;
    }

    void ArrowWidget::MousePan(int dx, int dy)
    {
        if (!Visibility) return;
        SetPosition(VTKN_ANCHOR_BOTTOM_LEFT, Position[0] + dx, Position[1] + dy);
        SetWorldTopLeftFromPosition();
        RecomputeLeader();
    }

    void ArrowWidget::MouseRotate(double, double)
    {
        if (!Visibility) return;
        SetPositionFromWorldTopLeft();
        RecomputeLeader();
    }

    void ArrowWidget::MouseWheelScrolled()
    {
        if (!Visibility) return;
        SetPositionFromWorldTopLeft();
        RecomputeLeader();
    }

    // The arrow is drawn first, the box over it
    void ArrowWidget::VisibilityOn()
    {
        if (Visibility) return;
        SetWorldTopLeftFromPosition();
        Renderer->AddActor(LeaderActor);
        Renderer->AddActor(HeadActor);
        TextWidget::VisibilityOn();
    }

    void ArrowWidget::VisibilityOff()
    {
        if (!Visibility) return;
        TextWidget::VisibilityOff();
        Renderer->RemoveActor(LeaderActor);
        Renderer->RemoveActor(HeadActor);
    }

    void ArrowWidget::BringToFront()
    {
        if (!Visibility || Renderer == nullptr) return;
        Renderer->RemoveActor(LeaderActor);
        Renderer->AddActor(LeaderActor);
        Renderer->RemoveActor(HeadActor);
        Renderer->AddActor(HeadActor);
        TextWidget::BringToFront();
    }

    void ArrowWidget::SetRenderer(vtkRenderer* renderer)
    {
        if (Visibility)
        {
            renderer->AddActor(LeaderActor);
            renderer->AddActor(HeadActor);
        }
        TextWidget::SetRenderer(renderer);
    }

    void ArrowWidget::RemoveRenderer()
    {
        if (Renderer != nullptr)
        {
            Renderer->RemoveActor(LeaderActor);
            Renderer->RemoveActor(HeadActor);
        }
        TextWidget::RemoveRenderer();
    }

    void ArrowWidget::ResetPositionFromAnchor()
    {
        AnchorAlreadySet = false;
        SetAnchorPoint(WorldAnchorPoint[0], WorldAnchorPoint[1], WorldAnchorPoint[2]);
    }

    // The first anchor places the box up and to the left of it, or where there is room
    void ArrowWidget::SetAnchorPoint(double x, double y, double z)
    {
        WorldAnchorPoint[0] = x;
        WorldAnchorPoint[1] = y;
        WorldAnchorPoint[2] = z;
        if (!AnchorAlreadySet)
        {
            double displayAnchor[2];
            WorldToDisplay(WorldAnchorPoint, displayAnchor);
            int* windowSize = Renderer->GetSize();
            double dx = -80;
            double dy = 50;
            if (displayAnchor[0] + dx + Size[0] > windowSize[0]) dx = -(Size[0] + dx);
            if (displayAnchor[1] + dy + Size[1] > windowSize[1]) dy = -(Size[1] + dy);
            // The placement is defined around the label center, the corner is the camera reference
            const double displayTopLeft[2] = { displayAnchor[0] + dx - Size[0] / 2,
                                               displayAnchor[1] + dy + Size[1] / 2 };
            SetWorldTopLeftFromDisplayPoint(displayTopLeft);
            SetPositionFromWorldTopLeft();
            AnchorAlreadySet = true;
        }
        RecomputeLeader();
    }

    void ArrowWidget::SetText(const std::string& text)
    {
        TextWidget::SetText(text);
        if (Renderer != nullptr) RecomputeLeader();
    }

    void ArrowWidget::SetPositionFromWorldTopLeft()
    {
        double display[2];
        WorldToDisplay(WorldTopLeftPoint, display);
        SetDisplayTopLeftPoint(display);
    }

    void ArrowWidget::SetWorldTopLeftFromPosition()
    {
        double displayTopLeft[2];
        if (!GetDisplayTopLeftPoint(displayTopLeft)) return;
        SetWorldTopLeftFromDisplayPoint(displayTopLeft);
    }

    // The display point is taken to the world and projected on the plane through the anchor that faces the camera
    void ArrowWidget::SetWorldTopLeftFromDisplayPoint(const double displayTopLeft[2])
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        Renderer->SetDisplayPoint(displayTopLeft[0], displayTopLeft[1], 0);
        Renderer->DisplayToWorld();
        double world[4];
        Renderer->GetWorldPoint(world);
        double normal[3];
        camera->GetViewPlaneNormal(normal);
        double dot = 0;
        for (int i = 0; i < 3; i++) dot += (world[i] - WorldAnchorPoint[i]) * normal[i];
        for (int i = 0; i < 3; i++) WorldTopLeftPoint[i] = world[i] - dot * normal[i];
    }

    void ArrowWidget::OffsetPosition(double x, double y)
    {
        Position[0] += x;
        Position[1] += y;
        SetWorldTopLeftFromPosition();
        SetPositionFromWorldTopLeft();
        RecomputeLeader();
    }

    void ArrowWidget::GetLeaderLine(double line4[4])
    {
        double start[3];
        double end[3];
        LeaderPolyData->GetPoints()->GetPoint(0, start);
        LeaderPolyData->GetPoints()->GetPoint(1, end);
        line4[0] = start[0];
        line4[1] = start[1];
        line4[2] = end[0];
        line4[3] = end[1];
    }
}
