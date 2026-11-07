// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeWidgetHighlight.h"

#include <vtkCellArray.h>
#include <vtkNew.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty2D.h>

#include <cmath>

namespace vtkn
{
    namespace
    {
        const int NumOfCorners = 4;
        // The border: VTK 5.8 blended RoyalBlue at half opacity over the half gray border line of the widget; the
        // border line is one black row now, which showed through, so the border is drawn opaque in the color the
        // blend had. The fill: LightSkyBlue
        const double BorderColor[3] = { 93 / 255.0, 114 / 255.0, 174 / 255.0 };
        const double FillColor[3] = { 135 / 255.0, 206 / 255.0, 250 / 255.0 };
    }

    WidgetHighlight::WidgetHighlight()
    {
        CornerPoints = vtkSmartPointer<vtkPoints>::New();
        for (int i = 0; i < NumOfCorners; i++) CornerPoints->InsertNextPoint(0, 0, 0);
        //
        vtkNew<vtkCellArray> lines;
        for (int i = 0; i < NumOfCorners; i++)
        {
            lines->InsertNextCell(2);
            lines->InsertCellPoint(i);
            lines->InsertCellPoint((i + 1) % NumOfCorners);
        }
        BorderPolyData = vtkSmartPointer<vtkPolyData>::New();
        BorderPolyData->SetPoints(CornerPoints);
        BorderPolyData->SetLines(lines);
        vtkNew<vtkPolyDataMapper2D> borderMapper;
        borderMapper->SetInputData(BorderPolyData);
        BorderActor = vtkSmartPointer<vtkActor2D>::New();
        BorderActor->SetMapper(borderMapper);
        BorderActor->GetProperty()->SetColor(BorderColor[0], BorderColor[1], BorderColor[2]);
        BorderActor->GetProperty()->SetLineWidth(WidgetBorderLineWidth);
        BorderActor->VisibilityOff();
        //
        vtkNew<vtkCellArray> polygon;
        polygon->InsertNextCell(NumOfCorners);
        for (int i = 0; i < NumOfCorners; i++) polygon->InsertCellPoint(i);
        FillPolyData = vtkSmartPointer<vtkPolyData>::New();
        FillPolyData->SetPoints(CornerPoints);
        FillPolyData->SetPolys(polygon);
        vtkNew<vtkPolyDataMapper2D> fillMapper;
        fillMapper->SetInputData(FillPolyData);
        FillActor = vtkSmartPointer<vtkActor2D>::New();
        FillActor->SetMapper(fillMapper);
        FillActor->GetProperty()->SetColor(FillColor[0], FillColor[1], FillColor[2]);
        FillActor->GetProperty()->SetOpacity(WidgetFillOpacity);
        FillActor->VisibilityOff();
    }

    void WidgetHighlight::AddToRenderer(vtkRenderer* renderer)
    {
        renderer->AddActor(FillActor);
        renderer->AddActor(BorderActor);
        Clear();
    }

    void WidgetHighlight::RemoveFromRenderer(vtkRenderer* renderer)
    {
        renderer->RemoveActor(BorderActor);
        renderer->RemoveActor(FillActor);
        Clear();
    }

    bool WidgetHighlight::SetRectangle(const Rect& rectangle, double fillOpacity)
    {
        if (rectangle.IsEmpty()) return Clear();
        //
        const double corners[NumOfCorners][2] = { { double(rectangle.Left()), double(rectangle.Top()) },
                                                  { double(rectangle.Right()), double(rectangle.Top()) },
                                                  { double(rectangle.Right()), double(rectangle.Bottom()) },
                                                  { double(rectangle.Left()), double(rectangle.Bottom()) } };
        bool changed = SetCorners(corners, fillOpacity);
        Current = rectangle;
        return changed;
    }

    bool WidgetHighlight::SetCorners(const double (*corners)[2], double fillOpacity)
    {
        if (corners == nullptr) return Clear();
        bool sameCorners = HasRectangle;
        for (int i = 0; i < NumOfCorners && sameCorners; i++)
            sameCorners = CurrentCorners[i][0] == corners[i][0] && CurrentCorners[i][1] == corners[i][1];
        if (sameCorners && std::abs(fillOpacity - FillOpacity) < 1E-12) return false;
        //
        HasRectangle = true;
        Current = Rect::FromLTRB(static_cast<int>(corners[0][0]), static_cast<int>(corners[0][1]),
                                 static_cast<int>(corners[2][0]), static_cast<int>(corners[2][1]));
        FillOpacity = fillOpacity;
        FillActor->GetProperty()->SetOpacity(FillOpacity);
        for (int i = 0; i < NumOfCorners; i++)
        {
            CurrentCorners[i][0] = corners[i][0];
            CurrentCorners[i][1] = corners[i][1];
            CornerPoints->SetPoint(i, corners[i][0], corners[i][1], 0);
        }
        CornerPoints->Modified();
        BorderPolyData->Modified();
        FillPolyData->Modified();
        FillActor->VisibilityOn();
        BorderActor->VisibilityOn();
        return true;
    }

    void WidgetHighlight::BringToFront(vtkRenderer* renderer)
    {
        renderer->RemoveActor(FillActor);
        renderer->RemoveActor(BorderActor);
        renderer->AddActor(FillActor);
        renderer->AddActor(BorderActor);
    }

    bool WidgetHighlight::Clear()
    {
        if (!HasRectangle) return false;
        //
        HasRectangle = false;
        BorderActor->VisibilityOff();
        FillActor->VisibilityOff();
        return true;
    }

    bool WidgetHighlight::GetRectangle(Rect& rectangle) const
    {
        rectangle = HasRectangle ? Current : Rect();
        return HasRectangle;
    }

    bool WidgetHighlight::GetCorners(double corners[4][2]) const
    {
        for (int i = 0; i < NumOfCorners; i++)
        {
            corners[i][0] = HasRectangle ? CurrentCorners[i][0] : 0;
            corners[i][1] = HasRectangle ? CurrentCorners[i][1] : 0;
        }
        return HasRectangle;
    }
}
