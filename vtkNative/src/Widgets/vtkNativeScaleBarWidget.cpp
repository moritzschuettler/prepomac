// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeScaleBarWidget.h"
#include "vtkNativePixelLines.h"
#include "vtkNativeTextWidget.h"

#include <vtkNew.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty2D.h>

#include <cmath>
#include <cstdio>

namespace vtkn
{
    ScaleBarWidget::ScaleBarWidget()
    {
        vtkNew<vtkTextProperty> textProperty;
        for (int i = 0; i < NumOfFields + 1; i++)
        {
            vtkSmartPointer<vtkTextMapper> mapper = vtkSmartPointer<vtkTextMapper>::New();
            mapper->SetTextProperty(textProperty);
            vtkSmartPointer<vtkActor2D> actor = vtkSmartPointer<vtkActor2D>::New();
            actor->SetMapper(mapper);
            actor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
            actor->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
            actor->GetPositionCoordinate()->SetValue(0, 0);
            ScaleTextMappers.push_back(mapper);
            ScaleTextActors.push_back(actor);
        }
        LabelOffsets.assign(ScaleTextMappers.size(), 0);
        // Background
        ScaleBackgroundPoints = vtkSmartPointer<vtkPoints>::New();
        ScaleBackgroundPolygons = vtkSmartPointer<vtkCellArray>::New();
        ScaleBackgroundPolyData = vtkSmartPointer<vtkPolyData>::New();
        ScaleBackgroundPolyData->SetPoints(ScaleBackgroundPoints);
        ScaleBackgroundPolyData->SetPolys(ScaleBackgroundPolygons);
        vtkNew<vtkPolyDataMapper2D> backgroundMapper;
        backgroundMapper->SetInputData(ScaleBackgroundPolyData);
        ScaleBackgroundActor = vtkSmartPointer<vtkActor2D>::New();
        ScaleBackgroundActor->SetMapper(backgroundMapper);
        ScaleBackgroundActor->GetProperty()->SetColor(0, 0, 0);
        ScaleBackgroundActor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
        ScaleBackgroundActor->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
        // Border
        ScaleBorderLines = vtkSmartPointer<vtkCellArray>::New();
        ScaleBorderPolyData = vtkSmartPointer<vtkPolyData>::New();
        ScaleBorderPolyData->SetPoints(ScaleBackgroundPoints);
        ScaleBorderPolyData->SetLines(ScaleBorderLines);
        vtkNew<vtkPolyDataMapper2D> borderMapper;
        SetPixelCenteredInput(borderMapper, ScaleBorderPolyData);
        ScaleBorderActor = vtkSmartPointer<vtkActor2D>::New();
        ScaleBorderActor->SetMapper(borderMapper);
        ScaleBorderActor->GetProperty()->SetColor(0, 0, 0);
        ScaleBorderActor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
        ScaleBorderActor->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
    }

    std::vector<vtkActor2D*> ScaleBarWidget::GetActors() const
    {
        std::vector<vtkActor2D*> actors = { ScaleBackgroundActor, ScaleBorderActor };
        for (const auto& actor : ScaleTextActors) actors.push_back(actor);
        return actors;
    }

    void ScaleBarWidget::GenerateGeometry()
    {
        UpdateSize();
        GenerateLabelValues();
        GenerateBackground();
        GenerateBorders();
        PositionLabels();
        UpdateOuterBorder();
    }

    // The length of the bar in model units is the display width in world units rounded to a fifth of its decade
    void ScaleBarWidget::UpdateSize()
    {
        Renderer->SetDisplayPoint(0, 0, 0);
        Renderer->DisplayToWorld();
        double first[4];
        Renderer->GetWorldPoint(first);
        Renderer->SetDisplayPoint(0, Width, 0);
        Renderer->DisplayToWorld();
        double second[4];
        Renderer->GetWorldPoint(second);
        double length = std::sqrt(std::pow(first[0] - second[0], 2) + std::pow(first[1] - second[1], 2) +
                                  std::pow(first[2] - second[2], 2));
        if (std::isnan(length) || length == 0) return;
        //
        double previousWidth = BarWidth;
        double power = std::floor(std::log10(length));
        length /= std::pow(10, power);
        const int numOfIntervals = 5;
        double ratio = std::round(numOfIntervals * length) / numOfIntervals / length;
        BarWidth = Width * ratio;
        BarWidthInModelUnits = std::round(numOfIntervals * length) / numOfIntervals * std::pow(10, power);
        Position[0] -= (BarWidth - previousWidth) / 2;
    }

    void ScaleBarWidget::GenerateLabelValues()
    {
        double dx = BarWidthInModelUnits / NumOfFields;
        for (size_t i = 0; i < ScaleTextMappers.size(); i++)
        {
            double value = i * dx;
            std::string label;
            if (NumberFormatter) label = NumberFormatter(value);
            else
            {
                char buffer[64];
                std::snprintf(buffer, sizeof(buffer), "%.4g", value);
                label = buffer;
            }
            ScaleTextMappers[i]->SetInput(label.c_str());
        }
    }

    void ScaleBarWidget::GenerateBackground()
    {
        int sizeOfFirst[2];
        TextWidget::GetTextSize(ScaleTextMappers[0], Renderer, sizeOfFirst);
        int n = NumOfFields + 1;
        ScaleBackgroundPolygons->Reset();
        ScaleBackgroundPoints->SetNumberOfPoints(2 * n);
        //
        double dx = BarWidth / NumOfFields;
        double dy = BarHeight;
        double x0 = sizeOfFirst[0] / 2 + Padding;
        double y0 = sizeOfFirst[1] + 2 * Padding;
        for (int i = 0; i <= NumOfFields; i++)
        {
            ScaleBackgroundPoints->SetPoint(2 * i, x0 + i * dx, y0, 0);
            ScaleBackgroundPoints->SetPoint(2 * i + 1, x0 + i * dx, y0 + dy, 0);
        }
        for (int i = 0; i < NumOfFields; i += 2)
        {
            ScaleBackgroundPolygons->InsertNextCell(4);
            ScaleBackgroundPolygons->InsertCellPoint(2 * i);
            ScaleBackgroundPolygons->InsertCellPoint(2 * (i + 1));
            ScaleBackgroundPolygons->InsertCellPoint(2 * (i + 1) + 1);
            ScaleBackgroundPolygons->InsertCellPoint(2 * i + 1);
        }
        ScaleBackgroundPoints->Modified();
        ScaleBackgroundPolyData->Modified();
    }

    void ScaleBarWidget::GenerateBorders()
    {
        ScaleBorderLines->Reset();
        for (int i = 0; i <= NumOfFields; i++)
        {
            ScaleBorderLines->InsertNextCell(2);
            ScaleBorderLines->InsertCellPoint(2 * i);
            ScaleBorderLines->InsertCellPoint(2 * i + 1);
        }
        ScaleBorderLines->InsertNextCell(2);
        ScaleBorderLines->InsertCellPoint(0);
        ScaleBorderLines->InsertCellPoint(2 * NumOfFields);
        ScaleBorderLines->InsertNextCell(2);
        ScaleBorderLines->InsertCellPoint(1);
        ScaleBorderLines->InsertCellPoint(2 * NumOfFields + 1);
        ScaleBorderPolyData->Modified();
    }

    void ScaleBarWidget::PositionLabels()
    {
        size_t lastIndex = ScaleTextMappers.size() - 1;
        if (!Unit.empty())
        {
            std::string lastLabel = ScaleTextMappers[lastIndex]->GetInput();
            ScaleTextMappers[lastIndex]->SetInput((lastLabel + " " + Unit).c_str());
        }
        for (size_t i = 0; i < ScaleTextMappers.size(); i++)
        {
            double point[3];
            ScaleBackgroundPoints->GetPoint(2 * static_cast<vtkIdType>(i), point);
            int size[2];
            TextWidget::GetTextSize(ScaleTextMappers[i], Renderer, size);
            LabelOffsets[i] = point[0] - size[0] / 2;
        }
    }

    void ScaleBarWidget::UpdateOuterBorder()
    {
        int sizeOfFirst[2];
        int sizeOfLast[2];
        TextWidget::GetTextSize(ScaleTextMappers.front(), Renderer, sizeOfFirst);
        TextWidget::GetTextSize(ScaleTextMappers.back(), Renderer, sizeOfLast);
        Size[0] = Padding + sizeOfFirst[0] / 2 + BarWidth + sizeOfLast[0] / 2 + Padding;
        Size[1] = Padding + sizeOfLast[1] + Padding + BarHeight + Padding + sizeOfFirst[1] + Padding;
    }

    void ScaleBarWidget::PlaceActors()
    {
        const double origin[2] = { 0, 0 };
        PlaceActor(ScaleBackgroundActor->GetPositionCoordinate(), origin, false);
        PlaceActor(ScaleBorderActor->GetPositionCoordinate(), origin, false);
        for (size_t i = 0; i < ScaleTextActors.size(); i++)
        {
            const double offset[2] = { LabelOffsets[i], static_cast<double>(Padding) };
            PlaceActor(ScaleTextActors[i]->GetPositionCoordinate(), offset, true);
        }
    }

    void ScaleBarWidget::UpdateBorderGeometry()
    {
        BorderWidget::UpdateBorderGeometry();
        PlaceActors();
    }

    void ScaleBarWidget::OnRenderWindowModified()
    {
        if (!Visibility) return;
        BorderWidget::OnRenderWindowModified();
        OnSizeChanged();
    }

    void ScaleBarWidget::CameraModified()
    {
        if (!Visibility) return;
        OnSizeChanged();
    }

    void ScaleBarWidget::OnSizeChanged()
    {
        if (Renderer == nullptr) return;
        GenerateGeometry();
        OnMovedOrSizeChanged();
    }

    void ScaleBarWidget::SetRenderer(vtkRenderer* renderer)
    {
        BorderWidget::SetRenderer(renderer);
        if (Visibility)
        {
            for (vtkActor2D* actor : GetActors()) Renderer->AddActor(actor);
        }
        OnSizeChanged();
    }

    void ScaleBarWidget::RemoveRenderer()
    {
        if (Renderer != nullptr)
        {
            for (vtkActor2D* actor : GetActors()) Renderer->RemoveActor(actor);
        }
        BorderWidget::RemoveRenderer();
    }

    void ScaleBarWidget::VisibilityOn()
    {
        if (Visibility) return;
        BorderWidget::VisibilityOn();
        for (vtkActor2D* actor : GetActors()) Renderer->AddActor(actor);
    }

    void ScaleBarWidget::VisibilityOff()
    {
        if (!Visibility) return;
        BorderWidget::VisibilityOff();
        for (vtkActor2D* actor : GetActors()) Renderer->RemoveActor(actor);
    }

    void ScaleBarWidget::BringToFront()
    {
        if (!Visibility || Renderer == nullptr) return;
        BorderWidget::BringToFront();
        for (vtkActor2D* actor : GetActors())
        {
            Renderer->RemoveActor(actor);
            Renderer->AddActor(actor);
        }
    }

    void ScaleBarWidget::SetTextStyle(const vtkn_text_style& style)
    {
        TextWidget::ApplyTextStyle(ScaleTextMappers[0]->GetTextProperty(), style);
    }

    void ScaleBarWidget::SetUnit(const std::string& unit)
    {
        Unit = unit;
        if (Renderer == nullptr) return;
        GenerateGeometry();
        PlaceActors();
    }

    void ScaleBarWidget::SetWidth(int width)
    {
        Width = width;
        OnSizeChanged();
    }

    void ScaleBarWidget::SetPadding(int padding)
    {
        if (padding == Padding) return;
        Padding = padding;
        OnSizeChanged();
    }

    void ScaleBarWidget::SetNumberFormatter(std::function<std::string(double)> formatter)
    {
        NumberFormatter = formatter;
        OnSizeChanged();
    }

    std::vector<std::string> ScaleBarWidget::GetLabels() const
    {
        std::vector<std::string> labels;
        for (const auto& mapper : ScaleTextMappers) labels.push_back(mapper->GetInput() == nullptr ? "" : mapper->GetInput());
        return labels;
    }
}
