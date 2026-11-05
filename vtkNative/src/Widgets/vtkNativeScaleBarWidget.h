// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxScaleWidget: the scale bar. Its length in model units follows the camera, rounded to a value of
// five intervals, and the bar keeps its center while its length changes. The labels are formatted by the host,
// since their number format is a .NET one.
#pragma once

#include "vtkNativeBorderWidget.h"

#include <vtkCellArray.h>
#include <vtkTextMapper.h>
#include <vtkTextProperty.h>

#include <functional>
#include <string>
#include <vector>

namespace vtkn
{
    class ScaleBarWidget : public BorderWidget
    {
    public:
        ScaleBarWidget();

        void SetRenderer(vtkRenderer* renderer) override;
        void RemoveRenderer() override;
        void VisibilityOn() override;
        void VisibilityOff() override;
        void BringToFront() override;
        void OnRenderWindowModified() override;
        void CameraModified() override;
        void OnSizeChanged() override;
        void UpdateBorderGeometry() override;

        void SetTextStyle(const vtkn_text_style& style);
        void SetUnit(const std::string& unit);
        void SetWidth(int width);
        void SetPadding(int padding);
        void SetNumberFormatter(std::function<std::string(double)> formatter);
        std::vector<std::string> GetLabels() const;

    private:
        void GenerateGeometry();
        void UpdateSize();
        void GenerateLabelValues();
        void GenerateBackground();
        void GenerateBorders();
        void PositionLabels();
        void UpdateOuterBorder();
        void PlaceActors();
        std::vector<vtkActor2D*> GetActors() const;

        double Width = 300;
        double BarWidth = 300;
        double BarWidthInModelUnits = 0;
        double BarHeight = 10;
        int NumOfFields = 5;
        int Padding = 5;
        std::string Unit;
        std::function<std::string(double)> NumberFormatter;
        std::vector<double> LabelOffsets;
        //
        std::vector<vtkSmartPointer<vtkTextMapper>> ScaleTextMappers;
        std::vector<vtkSmartPointer<vtkActor2D>> ScaleTextActors;
        vtkSmartPointer<vtkPoints> ScaleBackgroundPoints;
        vtkSmartPointer<vtkCellArray> ScaleBackgroundPolygons;
        vtkSmartPointer<vtkPolyData> ScaleBackgroundPolyData;
        vtkSmartPointer<vtkActor2D> ScaleBackgroundActor;
        vtkSmartPointer<vtkCellArray> ScaleBorderLines;
        vtkSmartPointer<vtkPolyData> ScaleBorderPolyData;
        vtkSmartPointer<vtkActor2D> ScaleBorderActor;
    };
}
