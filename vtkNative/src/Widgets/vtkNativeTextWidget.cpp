// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeTextWidget.h"
#include "vtkNativeError.h"

#include <vtkNew.h>

#include <algorithm>

namespace vtkn
{
    TextWidget::TextWidget()
    {
        vtkNew<vtkTextProperty> textProperty;
        TextMapper = vtkSmartPointer<vtkTextMapper>::New();
        TextMapper->SetTextProperty(textProperty);
        TextActor = vtkSmartPointer<vtkActor2D>::New();
        TextActor->SetMapper(TextMapper);
        // The text sits at a pixel offset from the widget corner
        TextActorCoordinate = TextActor->GetPositionCoordinate();
        TextActorCoordinate->SetCoordinateSystemToDisplay();
        TextActorCoordinate->SetReferenceCoordinate(PositionCoordinate);
        TextActorCoordinate->SetValue(Padding, Padding);
    }

    void TextWidget::SetRenderer(vtkRenderer* renderer)
    {
        BorderWidget::SetRenderer(renderer);
        if (Visibility) Renderer->AddActor(TextActor);
    }

    void TextWidget::RemoveRenderer()
    {
        if (Renderer != nullptr) Renderer->RemoveActor(TextActor);
        BorderWidget::RemoveRenderer();
    }

    void TextWidget::VisibilityOn()
    {
        if (Visibility) return;
        // The text may have changed while the widget was hidden
        OnSizeChanged();
        BorderWidget::VisibilityOn();
        Renderer->AddActor(TextActor);
    }

    void TextWidget::VisibilityOff()
    {
        if (!Visibility) return;
        BorderWidget::VisibilityOff();
        Renderer->RemoveActor(TextActor);
    }

    void TextWidget::BringToFront()
    {
        if (!Visibility || Renderer == nullptr) return;
        BorderWidget::BringToFront();
        Renderer->RemoveActor(TextActor);
        Renderer->AddActor(TextActor);
    }

    void TextWidget::OnSizeChanged()
    {
        int textSize[2];
        GetTextSize(TextMapper, Renderer, textSize);
        Size[0] = textSize[0] + 2 * Padding;
        Size[1] = textSize[1] + 2 * Padding;
        BorderWidget::OnSizeChanged();
    }

    void TextWidget::SetTextStyle(const vtkn_text_style& style)
    {
        ApplyTextStyle(TextMapper->GetTextProperty(), style);
    }

    void TextWidget::SetText(const std::string& text)
    {
        TextMapper->SetInput(ToVtkText(text).c_str());
        OnSizeChanged();
    }

    void TextWidget::UpdateBorderGeometry()
    {
        BorderWidget::UpdateBorderGeometry();
        const double offset[2] = { static_cast<double>(Padding), static_cast<double>(Padding) };
        PlaceActor(TextActorCoordinate, offset, true);
    }

    std::string TextWidget::GetText() const
    {
        const char* text = TextMapper->GetInput();
        return text == nullptr ? std::string() : std::string(text);
    }

    std::string TextWidget::ToVtkText(std::string text)
    {
        std::replace(text.begin(), text.end(), '\r', ' ');
        return text;
    }

    void TextWidget::SetPadding(int padding)
    {
        if (padding == Padding) return;
        Padding = padding;
        OnSizeChanged();
    }

    void TextWidget::ApplyTextStyle(vtkTextProperty* property, const vtkn_text_style& style)
    {
        Require(style.font_size > 0, "The font size must be positive.");
        switch (style.font_family)
        {
            case VTKN_FONT_ARIAL: property->SetFontFamilyToArial(); break;
            case VTKN_FONT_COURIER: property->SetFontFamilyToCourier(); break;
            case VTKN_FONT_TIMES: property->SetFontFamilyToTimes(); break;
            default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The font family is not one of vtkn_font_family.");
        }
        property->SetFontSize(style.font_size);
        property->SetColor(style.color[0], style.color[1], style.color[2]);
        property->SetLineOffset(style.line_offset);
        property->SetLineSpacing(style.line_spacing);
        property->SetBold(style.bold != 0);
        property->SetItalic(style.italic != 0);
    }

    void TextWidget::GetTextSize(vtkTextMapper* mapper, vtkRenderer* renderer, int size[2])
    {
        if (renderer == nullptr)
        {
            size[0] = size[1] = 50;
            return;
        }
        size[0] = size[1] = 0;
        mapper->GetSize(renderer, size);
    }
}
