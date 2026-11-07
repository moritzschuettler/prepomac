// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeScalarBarWidget.h"
#include "vtkNativePixelLines.h"
#include "vtkNativeError.h"

#include <vtkNew.h>
#include <vtkPointData.h>
#include <vtkProperty2D.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    ScalarBarWidget::ScalarBarWidget()
    {
        BackgroundVisibility = false;
        BorderVisibility = false;
        SetBorderColor(0, 0, 0);
        vtkTextProperty* textProperty = TextMapper->GetTextProperty();
        // Footer
        TextMapperFooter = vtkSmartPointer<vtkTextMapper>::New();
        TextMapperFooter->SetTextProperty(textProperty);
        TextActorFooter = vtkSmartPointer<vtkActor2D>::New();
        TextActorFooter->SetMapper(TextMapperFooter);
        TextActorFooterCoordinate = TextActorFooter->GetPositionCoordinate();
        TextActorFooterCoordinate->SetCoordinateSystemToDisplay();
        TextActorFooterCoordinate->SetValue(Padding, Padding);
        TextActorFooterCoordinate->SetReferenceCoordinate(PositionCoordinate);
        // Labels
        TextMapperLabel = vtkSmartPointer<vtkTextMapper>::New();
        TextMapperLabel->SetTextProperty(textProperty);
        TextActorLabel = vtkSmartPointer<vtkActor2D>::New();
        TextActorLabel->SetMapper(TextMapperLabel);
        TextActorLabelCoordinate = TextActorLabel->GetPositionCoordinate();
        TextActorLabelCoordinate->SetCoordinateSystemToDisplay();
        TextActorLabelCoordinate->SetReferenceCoordinate(PositionCoordinate);
        // Bar border
        vtkNew<vtkProperty2D> barProperty;
        barProperty->SetColor(0, 0, 0);
        BarBorderPoints = vtkSmartPointer<vtkPoints>::New();
        BarBorderLines = vtkSmartPointer<vtkCellArray>::New();
        BarBorderPolyData = vtkSmartPointer<vtkPolyData>::New();
        BarBorderPolyData->SetPoints(BarBorderPoints);
        BarBorderPolyData->SetLines(BarBorderLines);
        vtkNew<vtkPolyDataMapper2D> barBorderMapper;
        SetPixelCenteredInput(barBorderMapper, BarBorderPolyData);
        BarBorderActor = vtkSmartPointer<vtkActor2D>::New();
        BarBorderActor->SetMapper(barBorderMapper);
        BarBorderActor->SetProperty(barProperty);
        BarBorderActorCoordinate = BarBorderActor->GetPositionCoordinate();
        BarBorderActorCoordinate->SetCoordinateSystemToDisplay();
        BarBorderActorCoordinate->SetReferenceCoordinate(PositionCoordinate);
        // Bar colors
        BarColorsPoints = vtkSmartPointer<vtkPoints>::New();
        BarColorsPolygons = vtkSmartPointer<vtkCellArray>::New();
        BarColorsScalars = vtkSmartPointer<vtkFloatArray>::New();
        BarColorsPolyData = vtkSmartPointer<vtkPolyData>::New();
        BarColorsPolyData->SetPoints(BarColorsPoints);
        BarColorsPolyData->SetPolys(BarColorsPolygons);
        BarColorsPolyData->GetPointData()->SetScalars(BarColorsScalars);
        BarColorsMapper = vtkSmartPointer<vtkPolyDataMapper2D>::New();
        BarColorsMapper->SetInputData(BarColorsPolyData);
        BarColorsActor = vtkSmartPointer<vtkActor2D>::New();
        BarColorsActor->SetMapper(BarColorsMapper);
        BarColorsActor->SetProperty(barProperty);
        BarColorsActorCoordinate = BarColorsActor->GetPositionCoordinate();
        BarColorsActorCoordinate->SetCoordinateSystemToDisplay();
        BarColorsActorCoordinate->SetReferenceCoordinate(PositionCoordinate);
        //
        LookupTable = vtkSmartPointer<vtkLookupTable>::New();
    }

    void ScalarBarWidget::SetRenderer(vtkRenderer* renderer)
    {
        TextWidget::SetRenderer(renderer);
        if (Visibility)
        {
            Renderer->AddActor(TextActorFooter);
            Renderer->AddActor(TextActorLabel);
            Renderer->AddActor(BarColorsActor);
            Renderer->AddActor(BarBorderActor);
        }
    }

    void ScalarBarWidget::RemoveRenderer()
    {
        if (Renderer != nullptr)
        {
            Renderer->RemoveActor(TextActorFooter);
            Renderer->RemoveActor(TextActorLabel);
            Renderer->RemoveActor(BarColorsActor);
            Renderer->RemoveActor(BarBorderActor);
        }
        TextWidget::RemoveRenderer();
    }

    void ScalarBarWidget::VisibilityOn()
    {
        if (Visibility) return;
        TextWidget::VisibilityOn();
        Renderer->AddActor(TextActorFooter);
        Renderer->AddActor(TextActorLabel);
        Renderer->AddActor(BarColorsActor);
        Renderer->AddActor(BarBorderActor);
    }

    void ScalarBarWidget::VisibilityOff()
    {
        if (!Visibility) return;
        TextWidget::VisibilityOff();
        Renderer->RemoveActor(TextActorFooter);
        Renderer->RemoveActor(TextActorLabel);
        Renderer->RemoveActor(BarBorderActor);
        Renderer->RemoveActor(BarColorsActor);
    }

    void ScalarBarWidget::BringToFront()
    {
        if (!Visibility || Renderer == nullptr) return;
        TextWidget::BringToFront();
        for (vtkActor2D* actor : { TextActorFooter.GetPointer(), TextActorLabel.GetPointer(),
                                   BarColorsActor.GetPointer(), BarBorderActor.GetPointer() })
        {
            Renderer->RemoveActor(actor);
            Renderer->AddActor(actor);
        }
    }

    void ScalarBarWidget::OnSizeChanged()
    {
        Highlight.Clear();
        GenerateGeometry();
        OnMovedOrSizeChanged();
    }

    void ScalarBarWidget::SetTextStyle(const vtkn_text_style& style)
    {
        TextWidget::SetTextStyle(style);
    }

    void ScalarBarWidget::SetTitle(const std::string& title)
    {
        TextWidget::SetText(ToVtkText(title));
        OnSizeChanged();
    }

    int ScalarBarWidget::VisibleExtraColors() const
    {
        return (AddMinColor && MinColorVisible ? 1 : 0) + (AddMaxColor && MaxColorVisible ? 1 : 0);
    }

    // The data of CreateLookupTable and of the color spectrum setters, with the labels GenerateLabels made
    void ScalarBarWidget::SetColors(const vtkn_scalar_bar_colors& colors)
    {
        Require(colors.number_of_colors >= 2, "The number of colors must be at least two.");
        int gradientColors = colors.smooth_colors != 0 ? SmoothNumberOfColors : colors.number_of_colors;
        int tableColors = gradientColors + (colors.add_min_color != 0 ? 1 : 0) + (colors.add_max_color != 0 ? 1 : 0);
        Require(colors.table_colors != nullptr && colors.table_color_count == tableColors,
                "The lookup table must hold the gradient colors and the added minimum and maximum colors.");
        Require(colors.labels != nullptr, "The argument 'labels' must not be null.");
        //
        NumberOfColors = colors.number_of_colors;
        SmoothColors = colors.smooth_colors != 0;
        AddMinColor = colors.add_min_color != 0;
        AddMaxColor = colors.add_max_color != 0;
        MinColorVisible = colors.min_color_visible != 0;
        MaxColorVisible = colors.max_color_visible != 0;
        std::vector<std::string> labels;
        for (const char* label = colors.labels; *label != '\0'; label += labels.back().size() + 1)
            labels.emplace_back(label);
        Require(static_cast<int>(labels.size()) == NumberOfColors + 1 + VisibleExtraColors(),
                "There must be one label per color box border.");
        Labels = labels;
        //
        LookupTable->SetNumberOfTableValues(tableColors);
        for (int i = 0; i < tableColors; i++)
        {
            const double* rgb = colors.table_colors + 3 * i;
            LookupTable->SetTableValue(i, rgb[0], rgb[1], rgb[2], 1.0);
        }
        OnSizeChanged();
    }

    void ScalarBarWidget::GenerateGeometry()
    {
        const double boxAspectRatio = 3.0 / 4.0;
        // The box height is one line of the labels
        std::string labelText = TextMapperLabel->GetInput() != nullptr ? TextMapperLabel->GetInput() : "";
        int sizeOfOne[2];
        int sizeOfTwo[2];
        TextMapperLabel->SetInput("1");
        GetTextSize(TextMapperLabel, Renderer, sizeOfOne);
        TextMapperLabel->SetInput(ToVtkText("1\r\n1").c_str());
        GetTextSize(TextMapperLabel, Renderer, sizeOfTwo);
        TextMapperLabel->SetInput(labelText.c_str());
        //
        double lineSpacing = static_cast<double>(sizeOfTwo[1]) - 2 * sizeOfOne[1];
        LabelLineStep = sizeOfOne[1] + lineSpacing;
        double boxHeight = sizeOfOne[1] + lineSpacing;
        ColorBoxWidth = boxHeight / boxAspectRatio;
        ColorBoxHeight = boxHeight;
        double boxWidth = ColorBoxWidth;
        double spacing = boxHeight * 0.5;
        double verticalLineLength = boxWidth + spacing;
        double lineOffset = TextMapperLabel->GetTextProperty()->GetLineOffset();
        // Footer
        double size1[2];
        GenerateFooter(Padding, Padding, size1);
        size1[1] += spacing;
        // Labels
        double size2[2];
        GenerateLabels(Padding + verticalLineLength + spacing, size1[1], size2);
        size2[1] += spacing;
        // Bar
        double offsetY = -lineOffset + 2.0 / 7 * sizeOfOne[1];
        GenerateBarBorders(Padding, offsetY + size1[1], boxWidth, boxHeight, verticalLineLength);
        GenerateBarColors(boxWidth, boxHeight);
        // Title
        double size3[2];
        GenerateText(Padding, size2[1], size3);
        PlaceActors();
        //
        double maxX = size1[0];
        if (size2[0] > maxX) maxX = size2[0];
        if (size3[0] > maxX) maxX = size3[0];
        Size[0] = maxX + Padding;
        Size[1] = size3[1] + Padding;
    }

    void ScalarBarWidget::SetOffset(double offset[2], double x, double y)
    {
        offset[0] = x;
        offset[1] = y;
    }

    void ScalarBarWidget::UpdateBorderGeometry()
    {
        TextWidget::UpdateBorderGeometry();
        PlaceActors();
    }

    void ScalarBarWidget::PlaceActors()
    {
        PlaceActor(TextActorFooterCoordinate, FooterOffset, true);
        PlaceActor(TextActorLabelCoordinate, LabelOffset, true);
        PlaceActor(TextActorCoordinate, TextOffset, true);
        PlaceActor(BarBorderActorCoordinate, BarOffset, false);
        PlaceActor(BarColorsActorCoordinate, BarOffset, false);
    }

    void ScalarBarWidget::GenerateFooter(double offsetX, double offsetY, double size[2])
    {
        SetOffset(FooterOffset, offsetX, offsetY);
        int sizeFooter[2];
        GetTextSize(TextMapperFooter, Renderer, sizeFooter);
        size[0] = offsetX + sizeFooter[0];
        size[1] = offsetY + sizeFooter[1];
    }

    void ScalarBarWidget::GenerateLabels(double offsetX, double offsetY, double size[2])
    {
        std::string label;
        for (size_t i = 0; i < Labels.size(); i++)
        {
            if (i != 0) label += "\r\n";
            label += Labels[i];
        }
        TextMapperLabel->SetInput(ToVtkText(label).c_str());
        GetTextSize(TextMapperLabel, Renderer, LabelSize);
        SetOffset(LabelOffset, offsetX, offsetY);
        //
        MaximumLabelIndex = AddMaxColor && MaxColorVisible ? 1 : 0;
        MinimumLabelIndex = static_cast<int>(Labels.size()) - 1 - (AddMinColor && MinColorVisible ? 1 : 0);
        size[0] = offsetX + LabelSize[0];
        size[1] = offsetY + LabelSize[1];
    }

    void ScalarBarWidget::GenerateBarBorders(double offsetX, double offsetY, double boxWidth, double boxHeight,
                                             double verticalLineLength)
    {
        BarBorderLines->Reset();
        int numOfTableColors = NumberOfColors + VisibleExtraColors();
        BarBorderPoints->SetNumberOfPoints(2 * (numOfTableColors + 1) + 2);
        double h = 0;
        for (int i = 0; i <= numOfTableColors; i++)
        {
            h = i * boxHeight;
            BarBorderPoints->SetPoint(2 * i, 0, h, 0);
            BarBorderPoints->SetPoint(2 * i + 1, verticalLineLength, h, 0);
            BarBorderLines->InsertNextCell(2);
            BarBorderLines->InsertCellPoint(2 * i);
            BarBorderLines->InsertCellPoint(2 * i + 1);
        }
        BarBorderPoints->SetPoint(2 * (numOfTableColors + 1), boxWidth, 0, 0);
        BarBorderPoints->SetPoint(2 * (numOfTableColors + 1) + 1, boxWidth, h, 0);
        BarBorderLines->InsertNextCell(2);
        BarBorderLines->InsertCellPoint(0);
        BarBorderLines->InsertCellPoint(2 * numOfTableColors);
        BarBorderLines->InsertNextCell(2);
        BarBorderLines->InsertCellPoint(2 * (numOfTableColors + 1));
        BarBorderLines->InsertCellPoint(2 * (numOfTableColors + 1) + 1);
        BarBorderPoints->Modified();
        BarBorderPolyData->Modified();
        SetOffset(BarOffset, offsetX, offsetY);
    }

    void ScalarBarWidget::AddBarColorCell(int cellId, double boxWidth, double cellHeight, float scalar,
                                          double& height)
    {
        vtkIdType pointId = 4 * static_cast<vtkIdType>(cellId);
        BarColorsPoints->SetPoint(pointId + 0, 0, height, 0);
        BarColorsPoints->SetPoint(pointId + 1, boxWidth, height, 0);
        height += cellHeight;
        BarColorsPoints->SetPoint(pointId + 2, boxWidth, height, 0);
        BarColorsPoints->SetPoint(pointId + 3, 0, height, 0);
        BarColorsPolygons->InsertNextCell(4);
        for (vtkIdType i = 0; i < 4; i++)
        {
            BarColorsPolygons->InsertCellPoint(pointId + i);
            BarColorsScalars->SetValue(pointId + i, scalar);
        }
    }

    void ScalarBarWidget::GenerateBarColors(double boxWidth, double boxHeight)
    {
        BarColorsPolygons->Reset();
        int numOfAllTableColors = static_cast<int>(LookupTable->GetNumberOfTableValues());
        int numOfGradientColors = SmoothColors ? SmoothNumberOfColors : NumberOfColors;
        int numOfTableColors = numOfGradientColors + VisibleExtraColors();
        if (numOfAllTableColors < 2) numOfTableColors = 0;
        BarColorsPoints->SetNumberOfPoints(4 * static_cast<vtkIdType>(numOfTableColors));
        BarColorsScalars->SetNumberOfValues(4 * static_cast<vtkIdType>(numOfTableColors));
        //
        if (numOfTableColors > 0)
        {
            int cellId = 0;
            double height = 0;
            int firstGradientColor = AddMinColor ? 1 : 0;
            float scalarDelta = 1.0f / (numOfAllTableColors - 1);
            if (AddMinColor && MinColorVisible) AddBarColorCell(cellId++, boxWidth, boxHeight, 0, height);
            double gradientCellHeight = SmoothColors ? NumberOfColors * boxHeight / numOfGradientColors : boxHeight;
            for (int i = 0; i < numOfGradientColors; i++)
            {
                float scalar = (firstGradientColor + i) * scalarDelta;
                AddBarColorCell(cellId++, boxWidth, gradientCellHeight, scalar, height);
            }
            if (AddMaxColor && MaxColorVisible) AddBarColorCell(cellId, boxWidth, boxHeight, 1, height);
        }
        BarColorsPoints->Modified();
        BarColorsScalars->Modified();
        BarColorsPolyData->Modified();
        BarColorsMapper->SetScalarRange(0, 1);
        BarColorsMapper->SetLookupTable(LookupTable);

    }

    void ScalarBarWidget::GenerateText(double offsetX, double offsetY, double size[2])
    {
        SetOffset(TextOffset, offsetX, offsetY);
        int sizeText[2];
        GetTextSize(TextMapper, Renderer, sizeText);
        size[0] = offsetX + sizeText[0];
        size[1] = offsetY + sizeText[1];
    }

    Rect ScalarBarWidget::GetValueLabelRectangle(int labelIndex)
    {
        int count = static_cast<int>(Labels.size());
        if (count == 0 || labelIndex < 0 || labelIndex >= count) return Rect();
        //
        std::string allLabels = TextMapperLabel->GetInput() != nullptr ? TextMapperLabel->GetInput() : "";
        TextMapperLabel->SetInput(Labels[labelIndex].c_str());
        int labelSize[2];
        GetTextSize(TextMapperLabel, Renderer, labelSize);
        TextMapperLabel->SetInput(allLabels.c_str());
        //
        double lineStep = LabelLineStep > 0 ? LabelLineStep : static_cast<double>(LabelSize[1]) / count;
        double* widgetPosition = PositionCoordinate->GetComputedDoubleDisplayValue(Renderer);
        double x = widgetPosition[0] + LabelOffset[0];
        double y = widgetPosition[1] + LabelOffset[1] + (count - labelIndex - 1) * lineStep;
        Rect rectangle(static_cast<int>(std::floor(x)), static_cast<int>(std::floor(y)), std::max(1, labelSize[0]),
                       std::max(1, labelSize[1]));
        rectangle.Inflate(3, 1);
        return rectangle;
    }

    Rect ScalarBarWidget::GetValueLabelHighlightRectangle(Rect labelRectangle)
    {
        if (labelRectangle.IsEmpty()) return Rect();
        //
        double* widgetPosition = PositionCoordinate->GetComputedDoubleDisplayValue(Renderer);
        int widgetBottom = static_cast<int>(std::floor(widgetPosition[1]));
        int widgetTop = static_cast<int>(std::ceil(widgetPosition[1] + Size[1]));
        int height = std::max(1, static_cast<int>(std::round(ColorBoxHeight)));
        height = std::min(height, widgetTop - widgetBottom);
        int y = labelRectangle.Y + (labelRectangle.Height - height) / 2;
        labelRectangle.Y = std::max(widgetBottom, std::min(y, widgetTop - height));
        labelRectangle.Height = height;
        return labelRectangle;
    }

    // The whole widget highlight includes the title block with its unit line
    Rect ScalarBarWidget::GetDisplayRectangle()
    {
        Rect rectangle = TextWidget::GetDisplayRectangle();
        if (rectangle.IsEmpty()) return rectangle;
        //
        double* widgetPosition = PositionCoordinate->GetComputedDoubleDisplayValue(Renderer);
        int textX = static_cast<int>(std::floor(widgetPosition[0] + TextOffset[0]));
        int textY = static_cast<int>(std::floor(widgetPosition[1] + TextOffset[1]));
        int textSize[2];
        GetTextSize(TextMapper, Renderer, textSize);
        Rect textRectangle(textX, textY, std::max(1, textSize[0]), std::max(1, textSize[1]));
        textRectangle.Inflate(Padding, Padding);
        return Rect::Union(rectangle, textRectangle);
    }

    Rect ScalarBarWidget::GetHighlightRectangle(int x, int y)
    {
        if (Visibility)
        {
            Rect minimumRectangle = GetValueLabelHighlightRectangle(GetValueLabelRectangle(MinimumLabelIndex));
            Rect maximumRectangle = GetValueLabelHighlightRectangle(GetValueLabelRectangle(MaximumLabelIndex));
            if (minimumRectangle.Contains(x, y)) return minimumRectangle;
            if (maximumRectangle.Contains(x, y)) return maximumRectangle;
        }
        return TextWidget::GetHighlightRectangle(x, y);
    }

    bool ScalarBarWidget::GetValueLabelHighlightRectangle(bool minimum, Rect& rectangle)
    {
        rectangle = GetValueLabelHighlightRectangle(GetValueLabelRectangle(minimum ? MinimumLabelIndex
                                                                                   : MaximumLabelIndex));
        return Visibility && !rectangle.IsEmpty();
    }

    int ScalarBarWidget::HitValueLabel(int x, int y, Rect& rectangle)
    {
        rectangle = Rect();
        if (!Visibility) return VTKN_VALUE_LABEL_NONE;
        //
        Rect minimumRectangle = GetValueLabelHighlightRectangle(GetValueLabelRectangle(MinimumLabelIndex));
        Rect maximumRectangle = GetValueLabelHighlightRectangle(GetValueLabelRectangle(MaximumLabelIndex));
        if (minimumRectangle.Contains(x, y))
        {
            rectangle = minimumRectangle;
            return VTKN_VALUE_LABEL_MINIMUM;
        }
        if (maximumRectangle.Contains(x, y))
        {
            rectangle = maximumRectangle;
            return VTKN_VALUE_LABEL_MAXIMUM;
        }
        return VTKN_VALUE_LABEL_NONE;
    }
}
