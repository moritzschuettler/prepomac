// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxTextWidget: a border widget around a block of text, padded on every side.
#pragma once

#include "vtkNativeBorderWidget.h"

#include <vtkTextMapper.h>
#include <vtkTextProperty.h>

#include <string>

namespace vtkn
{
    class TextWidget : public BorderWidget
    {
    public:
        TextWidget();

        void SetRenderer(vtkRenderer* renderer) override;
        void RemoveRenderer() override;
        void VisibilityOn() override;
        void VisibilityOff() override;
        void BringToFront() override;
        void OnSizeChanged() override;
        void UpdateBorderGeometry() override;

        virtual void SetTextStyle(const vtkn_text_style& style);
        virtual void SetText(const std::string& text);
        void SetPadding(int padding);
        std::string GetText() const;

        static void ApplyTextStyle(vtkTextProperty* property, const vtkn_text_style& style);
        // vtkMaxWidgetTools.GetTextSize
        static void GetTextSize(vtkTextMapper* mapper, vtkRenderer* renderer, int size[2]);
        // The C# widgets end their lines with Environment.NewLine, and VTK 5.8 measured the carriage return as the
        // glyph Arial has for it, invisible and as wide as a space. VTK 9 measures it as nothing, so it becomes a
        // space and every line keeps the width it had
        static std::string ToVtkText(std::string text);

    protected:
        vtkSmartPointer<vtkActor2D> TextActor;
        vtkSmartPointer<vtkCoordinate> TextActorCoordinate;
        vtkSmartPointer<vtkTextMapper> TextMapper;
        int Padding = 0;
    };
}
