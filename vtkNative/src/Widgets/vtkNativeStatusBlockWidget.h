// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxStatusBlockWidget: the block with the result name, step, increment, time and deformation scale
// factor. Its lines arrive formatted, since the dates and number formats are those of .NET; the widget lays them
// out and finds the box of the deformation scale factor, where a click opens the scale factor setting.
#pragma once

#include "vtkNativeTextWidget.h"

#include <string>

namespace vtkn
{
    class StatusBlockWidget : public TextWidget
    {
    public:
        // line4 is the animation line; it is left out when it is empty
        void SetLines(const std::string& line1, const std::string& line2, const std::string& line3Fixed,
                      const std::string& line3Factor, const std::string& line4);
        bool LeftButtonPress(int x, int y, int clicks) override;
        bool GetDeformationScaleFactorTextClicked() const { return DeformationScaleFactorTextClicked; }
        Rect GetDeformationScaleFactorRectangle() const { return DeformationScaleFactorRect; }

    private:
        void MeasureText(const std::string& text, double size[2]);

        bool DeformationScaleFactorTextClicked = false;
        Rect DeformationScaleFactorRect;
    };
}
