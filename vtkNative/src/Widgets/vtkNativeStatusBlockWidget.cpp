// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeStatusBlockWidget.h"

namespace vtkn
{
    namespace
    {
        const char* const NewLine = "\r\n";
    }

    void StatusBlockWidget::MeasureText(const std::string& text, double size[2])
    {
        SetText(text);
        size[0] = Size[0];
        size[1] = Size[1];
    }

    // The C# widget set each partial text and read the widget size to find the box of the scale factor
    void StatusBlockWidget::SetLines(const std::string& line1, const std::string& line2,
                                     const std::string& line3Fixed, const std::string& line3Factor,
                                     const std::string& line4)
    {
        std::string text = line1 + NewLine + line2 + NewLine + line3Fixed + line3Factor;
        if (!line4.empty()) text += NewLine + line4;
        //
        double line1Size[2];
        double line1And2Size[2];
        double line1And2And3Size[2];
        double line3FixedSize[2];
        double line3FixedAndFactorSize[2];
        MeasureText(line1, line1Size);
        MeasureText(line1 + NewLine + line2, line1And2Size);
        MeasureText(line1 + NewLine + line2 + NewLine + line3Fixed, line1And2And3Size);
        MeasureText(line3Fixed, line3FixedSize);
        MeasureText(line3Fixed + line3Factor, line3FixedAndFactorSize);
        //
        DeformationScaleFactorRect = Rect(static_cast<int>(line3FixedSize[0]), static_cast<int>(line1And2Size[1]),
                                          static_cast<int>(line3FixedAndFactorSize[0] - line3FixedSize[0]),
                                          static_cast<int>(line1And2And3Size[1] - line1And2Size[1]));
        //
        SetText(text);
    }

    // The box is measured from the top left corner of the widget
    bool StatusBlockWidget::LeftButtonPress(int x, int y, int clicks)
    {
        if (Renderer != nullptr)
        {
            int* size = Renderer->GetSize();
            int localX = x - static_cast<int>(Position[0]);
            int localY = size[1] - y - static_cast<int>(Position[1]);
            DeformationScaleFactorTextClicked = DeformationScaleFactorRect.Contains(localX, localY);
        }
        return TextWidget::LeftButtonPress(x, y, clicks);
    }
}
