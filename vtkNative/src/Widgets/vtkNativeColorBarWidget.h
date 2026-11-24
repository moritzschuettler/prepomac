// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxColorBarWidget: the legend of colors by part, material or section, one colored box and one label
// per entry above the title text.
#pragma once

#include "vtkNativeTextWidget.h"

#include <vtkLookupTable.h>
#include <vtkPolyDataMapper2D.h>

#include <string>
#include <vector>

namespace vtkn
{
    class ColorBarWidget : public TextWidget
    {
    public:
        ColorBarWidget();

        void SetRenderer(vtkRenderer* renderer) override;
        void RemoveRenderer() override;
        void VisibilityOn() override;
        void VisibilityOff() override;
        void BringToFront() override;
        void OnSizeChanged() override;
        void UpdateBorderGeometry() override;

        // colors: count * 3 values from 0 to 1
        void SetColorsAndLabels(const double* colors, const std::vector<std::string>& labels);
        // The box of the title in display coordinates; false when the bar or its title is not shown
        bool GetTitleRectangle(Rect& rectangle) const;
        // The title is highlighted on its own, as it is edited on its own
        Rect GetHighlightRectangle(int x, int y) override;

    private:
        void GenerateGeometry();
        void GenerateBarBorders(double boxWidth, double boxHeight);
        void GenerateBarColors(double boxWidth, double boxHeight);
        void PlaceActors();
        std::vector<vtkActor2D*> GetActors() const;

        vtkSmartPointer<vtkPolyData> BarBorderPolyData;
        vtkSmartPointer<vtkActor2D> BarBorderActor;
        vtkSmartPointer<vtkPolyData> BarColorsPolyData;
        vtkSmartPointer<vtkPolyDataMapper2D> BarColorsMapper;
        vtkSmartPointer<vtkActor2D> BarColorsActor;
        vtkSmartPointer<vtkTextMapper> TextMapperFooter;
        vtkSmartPointer<vtkActor2D> TextActorFooter;
        vtkSmartPointer<vtkTextMapper> TextMapperLabel;
        vtkSmartPointer<vtkActor2D> TextActorLabel;
        vtkSmartPointer<vtkLookupTable> LookupTable;
        std::vector<double> Colors;
        std::vector<std::string> Labels;
        double FooterOffset[2] = { 0, 0 };
        double LabelOffset[2] = { 0, 0 };
        double TextOffset[2] = { 0, 0 };
        double TitleSize[2] = { 0, 0 };
        double BarOffset[2] = { 0, 0 };
    };
}
