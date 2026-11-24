// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeColorBarWidget.h"
#include "vtkNativePixelLines.h"

#include <vtkCellArray.h>
#include <vtkCellData.h>
#include <vtkIntArray.h>
#include <vtkNew.h>
#include <vtkProperty2D.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    ColorBarWidget::ColorBarWidget()
    {
        BackgroundVisibility = false;
        BorderVisibility = false;
        SetBorderColor(0, 0, 0);
        vtkTextProperty* textProperty = TextMapper->GetTextProperty();
        auto createText = [&](vtkSmartPointer<vtkTextMapper>& mapper, vtkSmartPointer<vtkActor2D>& actor) {
            mapper = vtkSmartPointer<vtkTextMapper>::New();
            mapper->SetTextProperty(textProperty);
            actor = vtkSmartPointer<vtkActor2D>::New();
            actor->SetMapper(mapper);
            actor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
            actor->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
        };
        createText(TextMapperFooter, TextActorFooter);
        createText(TextMapperLabel, TextActorLabel);
        // Bar
        vtkNew<vtkProperty2D> barProperty;
        barProperty->SetColor(0, 0, 0);
        BarBorderPolyData = vtkSmartPointer<vtkPolyData>::New();
        vtkNew<vtkPolyDataMapper2D> barBorderMapper;
        SetPixelCenteredInput(barBorderMapper, BarBorderPolyData);
        BarBorderActor = vtkSmartPointer<vtkActor2D>::New();
        BarBorderActor->SetMapper(barBorderMapper);
        BarBorderActor->SetProperty(barProperty);
        BarBorderActor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
        BarBorderActor->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
        BarColorsPolyData = vtkSmartPointer<vtkPolyData>::New();
        BarColorsMapper = vtkSmartPointer<vtkPolyDataMapper2D>::New();
        BarColorsMapper->SetInputData(BarColorsPolyData);
        BarColorsActor = vtkSmartPointer<vtkActor2D>::New();
        BarColorsActor->SetMapper(BarColorsMapper);
        BarColorsActor->SetProperty(barProperty);
        BarColorsActor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
        BarColorsActor->GetPositionCoordinate()->SetReferenceCoordinate(PositionCoordinate);
        LookupTable = vtkSmartPointer<vtkLookupTable>::New();
    }

    std::vector<vtkActor2D*> ColorBarWidget::GetActors() const
    {
        return { TextActorFooter, TextActorLabel, BarColorsActor, BarBorderActor };
    }

    void ColorBarWidget::GenerateGeometry()
    {
        if (Renderer == nullptr) return;
        const double boxAspectRatio = 3.0 / 4.0;
        TextMapperLabel->SetInput("1");
        int sizeOfOne[2];
        GetTextSize(TextMapperLabel, Renderer, sizeOfOne);
        TextMapperLabel->SetInput(ToVtkText("1\r\n1").c_str());
        int sizeOfTwo[2];
        GetTextSize(TextMapperLabel, Renderer, sizeOfTwo);
        //
        double lineSpacing = static_cast<double>(sizeOfTwo[1]) - 2 * sizeOfOne[1];
        double boxHeight = sizeOfOne[1] + lineSpacing;
        double boxWidth = boxHeight / boxAspectRatio;
        double spacing = boxHeight * 0.5;
        double lineOffset = TextMapperLabel->GetTextProperty()->GetLineOffset();
        // Footer
        FooterOffset[0] = Padding;
        FooterOffset[1] = Padding;
        int sizeFooter[2];
        GetTextSize(TextMapperFooter, Renderer, sizeFooter);
        double size1[2] = { FooterOffset[0] + sizeFooter[0], FooterOffset[1] + sizeFooter[1] + spacing };
        // Labels
        std::string label;
        for (size_t i = 0; i < Labels.size(); i++)
        {
            if (i != 0) label += "\r\n";
            label += Labels[i];
        }
        TextMapperLabel->SetInput(ToVtkText(label).c_str());
        int sizeLabel[2];
        GetTextSize(TextMapperLabel, Renderer, sizeLabel);
        LabelOffset[0] = Padding + boxWidth + spacing;
        LabelOffset[1] = size1[1];
        double size2[2] = { LabelOffset[0] + sizeLabel[0], LabelOffset[1] + sizeLabel[1] + spacing };
        // Bar
        double offsetY = lineOffset - 0.3 / 7 * sizeOfOne[1];
        BarOffset[0] = Padding;
        BarOffset[1] = offsetY + size1[1];
        GenerateBarBorders(boxWidth, boxHeight);
        GenerateBarColors(boxWidth, boxHeight);
        // Text
        TextOffset[0] = Padding;
        TextOffset[1] = size2[1];
        int sizeText[2];
        GetTextSize(TextMapper, Renderer, sizeText);
        double size3[2] = { TextOffset[0] + sizeText[0], TextOffset[1] + sizeText[1] };
        //
        Size[0] = std::max({ size1[0], size2[0], size3[0] }) + Padding;
        Size[1] = size3[1] + Padding;
        TitleSize[0] = sizeText[0];
        TitleSize[1] = sizeText[1];
    }

    bool ColorBarWidget::GetTitleRectangle(Rect& rectangle) const
    {
        rectangle = Rect();
        if (!Visibility || Renderer == nullptr || TitleSize[1] <= 0) return false;
        //
        // The box reaches a few pixels past the text as the boxes of the legend values do
        const int margin = 4;
        double* position = PositionCoordinate->GetComputedDoubleDisplayValue(Renderer);
        rectangle = Rect(static_cast<int>(std::floor(position[0] + TextOffset[0])) - margin,
                         static_cast<int>(std::floor(position[1] + TextOffset[1])) - margin,
                         static_cast<int>(std::ceil(TitleSize[0])) + 2 * margin,
                         static_cast<int>(std::ceil(TitleSize[1])) + 2 * margin);
        return true;
    }

    Rect ColorBarWidget::GetHighlightRectangle(int x, int y)
    {
        Rect titleRectangle;
        if (GetTitleRectangle(titleRectangle) && titleRectangle.Contains(x, y)) return titleRectangle;
        return TextWidget::GetHighlightRectangle(x, y);
    }

    void ColorBarWidget::GenerateBarBorders(double boxWidth, double boxHeight)
    {
        vtkNew<vtkPoints> points;
        vtkNew<vtkCellArray> lines;
        int count = static_cast<int>(Labels.size());
        points->SetNumberOfPoints(4 * count);
        for (int i = 0; i < count; i++)
        {
            double h = i * boxHeight;
            points->SetPoint(4 * i, 0, h + 2, 0);
            points->SetPoint(4 * i + 1, boxWidth, h + 2, 0);
            h += boxHeight;
            points->SetPoint(4 * i + 2, 0, h - 2, 0);
            points->SetPoint(4 * i + 3, boxWidth, h - 2, 0);
            const int pairs[4][2] = { { 0, 1 }, { 2, 3 }, { 0, 2 }, { 1, 3 } };
            for (const auto& pair : pairs)
            {
                lines->InsertNextCell(2);
                lines->InsertCellPoint(4 * i + pair[0]);
                lines->InsertCellPoint(4 * i + pair[1]);
            }
        }
        BarBorderPolyData->SetPoints(points);
        BarBorderPolyData->SetLines(lines);
    }

    void ColorBarWidget::GenerateBarColors(double boxWidth, double boxHeight)
    {
        vtkNew<vtkPoints> points;
        vtkNew<vtkCellArray> polygons;
        vtkNew<vtkIntArray> colors;
        int count = static_cast<int>(Labels.size());
        points->SetNumberOfPoints(4 * count);
        colors->SetNumberOfValues(count);
        for (int i = 0; i < count; i++)
        {
            double h = i * boxHeight;
            points->SetPoint(4 * i + 0, 0, h + 2, 0);
            points->SetPoint(4 * i + 1, boxWidth, h + 2, 0);
            h += boxHeight;
            points->SetPoint(4 * i + 2, boxWidth, h - 2, 0);
            points->SetPoint(4 * i + 3, 0, h - 2, 0);
            polygons->InsertNextCell(4);
            for (int j = 0; j < 4; j++) polygons->InsertCellPoint(4 * i + j);
            // The geometry is built from the bottom up
            colors->SetValue(i, count - 1 - i);
        }
        BarColorsPolyData->SetPoints(points);
        BarColorsPolyData->SetPolys(polygons);
        BarColorsPolyData->GetCellData()->SetScalars(colors);
        double* range = LookupTable->GetRange();
        BarColorsMapper->SetScalarRange(range[0], range[1]);
        BarColorsMapper->SetLookupTable(LookupTable);
    }

    void ColorBarWidget::PlaceActors()
    {
        PlaceActor(TextActorFooter->GetPositionCoordinate(), FooterOffset, true);
        PlaceActor(TextActorLabel->GetPositionCoordinate(), LabelOffset, true);
        PlaceActor(TextActorCoordinate, TextOffset, true);
        PlaceActor(BarBorderActor->GetPositionCoordinate(), BarOffset, false);
        PlaceActor(BarColorsActor->GetPositionCoordinate(), BarOffset, false);
    }

    void ColorBarWidget::UpdateBorderGeometry()
    {
        TextWidget::UpdateBorderGeometry();
        PlaceActors();
    }

    void ColorBarWidget::OnSizeChanged()
    {
        GenerateGeometry();
        OnMovedOrSizeChanged();
    }

    void ColorBarWidget::SetColorsAndLabels(const double* colors, const std::vector<std::string>& labels)
    {
        Labels = labels;
        Colors.assign(colors, colors + 3 * labels.size());
        int count = static_cast<int>(labels.size());
        LookupTable->SetNumberOfTableValues(std::max(1, count));
        LookupTable->SetRange(0, count);
        for (int i = 0; i < count; i++)
            LookupTable->SetTableValue(i, Colors[3 * i], Colors[3 * i + 1], Colors[3 * i + 2], 1.0);
        OnSizeChanged();
    }

    void ColorBarWidget::SetRenderer(vtkRenderer* renderer)
    {
        TextWidget::SetRenderer(renderer);
        if (Visibility)
        {
            for (vtkActor2D* actor : GetActors()) Renderer->AddActor(actor);
        }
    }

    void ColorBarWidget::RemoveRenderer()
    {
        if (Renderer != nullptr)
        {
            for (vtkActor2D* actor : GetActors()) Renderer->RemoveActor(actor);
        }
        TextWidget::RemoveRenderer();
    }

    void ColorBarWidget::VisibilityOn()
    {
        if (Visibility) return;
        TextWidget::VisibilityOn();
        for (vtkActor2D* actor : GetActors()) Renderer->AddActor(actor);
    }

    void ColorBarWidget::VisibilityOff()
    {
        if (!Visibility) return;
        TextWidget::VisibilityOff();
        for (vtkActor2D* actor : GetActors()) Renderer->RemoveActor(actor);
    }

    void ColorBarWidget::BringToFront()
    {
        if (!Visibility || Renderer == nullptr) return;
        TextWidget::BringToFront();
        for (vtkActor2D* actor : GetActors())
        {
            Renderer->RemoveActor(actor);
            Renderer->AddActor(actor);
        }
    }
}
