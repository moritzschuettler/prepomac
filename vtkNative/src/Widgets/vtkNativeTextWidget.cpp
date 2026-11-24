// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeTextWidget.h"
#include "vtkNativeError.h"
#include "vtkNativeFonts.h"

#include <vtkImageData.h>
#include <vtkNew.h>
#include <vtkStdString.h>
#include <vtkTextRenderer.h>

#include <algorithm>
#include <cmath>
#include <cstring>

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
        OnSizeChanged();
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
            case VTKN_FONT_VIEW: Fonts::Apply(property, style.bold != 0, style.italic != 0); break;
            default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The font family is not one of vtkn_font_family.");
        }
        property->SetFontSize(style.font_size);
        property->SetColor(style.color[0], style.color[1], style.color[2]);
        property->SetLineOffset(style.line_offset);
        property->SetLineSpacing(style.line_spacing);
        property->SetBold(style.bold != 0);
        property->SetItalic(style.italic != 0);
    }

    double TextWidget::GetLinePitch(const vtkn_text_style& style)
    {
        vtkNew<vtkTextProperty> property;
        ApplyTextStyle(property, style);
        double linePitch;
        double baselineHeight;
        GetTextLayout(property, linePitch, baselineHeight);
        return linePitch;
    }

    // The views keep the 72 DPI of vtkWindow, at which a font size is a size in pixels. The baseline is where the
    // flat bottom of a capital H is drawn, counted from the bottom of the text box: the bottom keeps its distance to
    // the baseline of the last line, while the top follows the tallest glyph of the first line
    void TextWidget::GetTextLayout(vtkTextProperty* textProperty, double& linePitch, double& baselineHeight)
    {
        const int viewDpi = 72;
        vtkNew<vtkTextProperty> property;
        property->ShallowCopy(textProperty);
        vtkTextRenderer* renderer = vtkTextRenderer::GetInstance();
        if (renderer == nullptr) throw Error(VTKN_ERROR_INTERNAL, "No text renderer is available.");
        int twoLines[4];
        int threeLines[4];
        if (!renderer->GetBoundingBox(property, vtkStdString("H\nH"), twoLines, viewDpi) ||
            !renderer->GetBoundingBox(property, vtkStdString("H\nH\nH"), threeLines, viewDpi))
            throw Error(VTKN_ERROR_INTERNAL, "The text could not be measured.");
        linePitch = static_cast<double>((threeLines[3] - threeLines[2]) - (twoLines[3] - twoLines[2]));
        //
        property->SetBackgroundOpacity(0);
        int oneLine[4];
        vtkNew<vtkImageData> image;
        if (!renderer->GetBoundingBox(property, vtkStdString("H"), oneLine, viewDpi) ||
            !renderer->RenderString(property, vtkStdString("H"), image, nullptr, viewDpi))
            throw Error(VTKN_ERROR_INTERNAL, "The text could not be drawn.");
        int extent[6];
        image->GetExtent(extent);
        int alphaComponent = image->GetNumberOfScalarComponents() - 1;
        auto rowHasInk = [&](int y) {
            for (int x = extent[0]; x <= extent[1]; x++)
            {
                if (image->GetScalarComponentAsDouble(x, y, 0, alphaComponent) > 63) return true;
            }
            return false;
        };
        int top = oneLine[3];
        int inkTop = std::min(top, extent[3]);
        while (inkTop >= extent[2] && !rowHasInk(inkTop)) inkTop--;
        if (inkTop < extent[2]) throw Error(VTKN_ERROR_INTERNAL, "The text could not be measured.");
        int inkBottom = inkTop;
        while (inkBottom > extent[2] && rowHasInk(inkBottom - 1)) inkBottom--;
        baselineHeight = static_cast<double>(inkBottom - oneLine[2]);
    }

    // PlaceActor puts every text of the widgets at a whole pixel, from which the lines are drawn the way the
    // annotations show: they start one pixel left of the actor, and the bottom of the text box lies one pixel below
    // the actor
    bool TextWidget::GetTextOrigin(vtkTextMapper* mapper, vtkActor2D* actor, int line, int origin[2]) const
    {
        const char* text = mapper->GetInput();
        if (Renderer == nullptr || text == nullptr || text[0] == '\0') return false;
        //
        double linePitch;
        double baselineHeight;
        GetTextLayout(mapper->GetTextProperty(), linePitch, baselineHeight);
        int lineCount = 1 + static_cast<int>(std::count(text, text + std::strlen(text), '\n'));
        if (line < 0 || line >= lineCount) return false;
        int* anchor = actor->GetPositionCoordinate()->GetComputedDisplayValue(Renderer);
        origin[0] = anchor[0] - 1;
        origin[1] = anchor[1] + static_cast<int>(std::round(baselineHeight)) - 1 +
                    static_cast<int>(std::round((lineCount - 1 - line) * linePitch));
        return true;
    }

    bool TextWidget::GetTextOrigin(int origin[2]) const
    {
        return Visibility && GetTextOrigin(TextMapper, TextActor, 0, origin);
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
