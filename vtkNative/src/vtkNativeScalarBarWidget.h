// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxScalarBarWidget: the result legend. Its title is the text of the text widget; below it the color
// boxes with their border lines and, to their right, one label per box border. The labels arrive formatted, since
// their number formats are those of .NET, and the colors arrive as the lookup table of the legend.
#pragma once

#include "vtkNativeTextWidget.h"

#include <vtkCellArray.h>
#include <vtkFloatArray.h>
#include <vtkLookupTable.h>
#include <vtkPolyDataMapper2D.h>

#include <string>
#include <vector>

namespace vtkn
{
    class ScalarBarWidget : public TextWidget
    {
    public:
        // vtkMaxColorSpectrum.SmoothNumberOfColors
        static const int SmoothNumberOfColors = 256;

        ScalarBarWidget();

        void SetRenderer(vtkRenderer* renderer) override;
        void RemoveRenderer() override;
        void VisibilityOn() override;
        void VisibilityOff() override;
        void BringToFront() override;
        void OnSizeChanged() override;
        void SetTextStyle(const vtkn_text_style& style) override;
        void UpdateBorderGeometry() override;

        void SetTitle(const std::string& title);
        void SetColors(const vtkn_scalar_bar_colors& colors);
        // TryGetValueEditData: the highlight box of the minimum or maximum label
        bool GetValueLabelHighlightRectangle(bool minimum, Rect& rectangle);
        // TryGetValueEditData at a point: VTKN_VALUE_LABEL_NONE, _MINIMUM or _MAXIMUM
        int HitValueLabel(int x, int y, Rect& rectangle);

    protected:
        Rect GetDisplayRectangle() override;
        Rect GetHighlightRectangle(int x, int y) override;

    private:
        void GenerateGeometry();
        void GenerateFooter(double offsetX, double offsetY, double size[2]);
        void GenerateLabels(double offsetX, double offsetY, double size[2]);
        void GenerateBarBorders(double offsetX, double offsetY, double boxWidth, double boxHeight,
                                double verticalLineLength);
        void GenerateBarColors(double offsetX, double offsetY, double boxWidth, double boxHeight);
        void AddBarColorCell(int cellId, double boxWidth, double cellHeight, float scalar, double& height);
        void GenerateText(double offsetX, double offsetY, double size[2]);
        void SetOffset(double offset[2], double x, double y);
        void PlaceActors();
        Rect GetValueLabelRectangle(int labelIndex);
        Rect GetValueLabelHighlightRectangle(Rect labelRectangle);
        int VisibleExtraColors() const;

        vtkSmartPointer<vtkPoints> BarBorderPoints;
        vtkSmartPointer<vtkCellArray> BarBorderLines;
        vtkSmartPointer<vtkPolyData> BarBorderPolyData;
        vtkSmartPointer<vtkActor2D> BarBorderActor;
        vtkSmartPointer<vtkCoordinate> BarBorderActorCoordinate;
        vtkSmartPointer<vtkPoints> BarColorsPoints;
        vtkSmartPointer<vtkCellArray> BarColorsPolygons;
        vtkSmartPointer<vtkFloatArray> BarColorsScalars;
        vtkSmartPointer<vtkPolyData> BarColorsPolyData;
        vtkSmartPointer<vtkPolyDataMapper2D> BarColorsMapper;
        vtkSmartPointer<vtkActor2D> BarColorsActor;
        vtkSmartPointer<vtkCoordinate> BarColorsActorCoordinate;
        vtkSmartPointer<vtkLookupTable> LookupTable;
        vtkSmartPointer<vtkActor2D> TextActorFooter;
        vtkSmartPointer<vtkCoordinate> TextActorFooterCoordinate;
        vtkSmartPointer<vtkTextMapper> TextMapperFooter;
        vtkSmartPointer<vtkActor2D> TextActorLabel;
        vtkSmartPointer<vtkCoordinate> TextActorLabelCoordinate;
        vtkSmartPointer<vtkTextMapper> TextMapperLabel;
        //
        int NumberOfColors = 12;
        bool SmoothColors = false;
        bool AddMinColor = false;
        bool AddMaxColor = false;
        bool MinColorVisible = true;
        bool MaxColorVisible = true;
        std::vector<std::string> Labels;
        int LabelSize[2] = { 0, 0 };
        double LabelLineStep = 0;
        double ColorBoxWidth = 0;
        double ColorBoxHeight = 0;
        int MinimumLabelIndex = -1;
        int MaximumLabelIndex = -1;
        double FooterOffset[2] = { 0, 0 };
        double LabelOffset[2] = { 0, 0 };
        double TextOffset[2] = { 0, 0 };
        double BarOffset[2] = { 0, 0 };
        // VTK 9 draws a text bitmap one pixel left of and one pixel above where VTK 5.8 drew its glyphs
        static constexpr double TextShiftX = 1;
        static constexpr double TextShiftY = -1;
    };
}
