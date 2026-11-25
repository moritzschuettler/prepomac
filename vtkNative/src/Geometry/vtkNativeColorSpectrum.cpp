// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeColorSpectrum.h"
#include "vtkNativeError.h"

#include <algorithm>
#include <array>
#include <vector>

namespace vtkn
{
    namespace
    {
        typedef std::vector<std::array<double, 4>> Points;

        // The points of each spectrum type: position in [0, 1] and RGB, from colorbrewer2.org and
        // kennethmoreland.com/color-maps as vtkControl lists them
        Points GetPoints(int type, vtkColorTransferFunction* ctf)
        {
            switch (type)
            {
                case VTKN_SPECTRUM_COOL_WARM:
                    ctf->SetColorSpaceToDiverging();
                    return { { 0, 0.230, 0.299, 0.754 }, { 1, 0.706, 0.016, 0.150 } };
                case VTKN_SPECTRUM_RAINBOW:
                    ctf->SetColorSpaceToHSV();
                    return { { 0.000, 0, 0, 1 }, { 0.250, 0, 1, 1 }, { 0.500, 0, 1, 0 }, { 0.750, 1, 1, 0 },
                             { 1.000, 1, 0, 0 } };
                case VTKN_SPECTRUM_RAINBOW_DESATURATED:
                    ctf->SetColorSpaceToRGB();
                    return { { 0, 0.278, 0.278, 0.788 }, { 0.143, 0.02, 0.02, 0.396 }, { 0.286, 0, 0.871, 0.918 },
                             { 0.429, 0, 0.604, 0.2 }, { 0.571, 1, 0.875, 0 }, { 0.714, 1, 0.329, 0 },
                             { 0.857, 0.451, 0.02, 0.02 }, { 1, 0.878, 0.302, 0.302 } };
                case VTKN_SPECTRUM_JET:
                    ctf->SetColorSpaceToRGB();
                    return { { 0.000, 0.000, 0.000, 0.500 }, { 0.125, 0.000, 0.000, 1.000 },
                             { 0.375, 0.000, 1.000, 1.000 }, { 0.500, 0.000, 1.000, 0.000 },
                             { 0.625, 1.000, 1.000, 0.000 }, { 0.875, 1.000, 0.000, 0.000 },
                             { 1.000, 0.500, 0.000, 0.000 } };
                case VTKN_SPECTRUM_TURBO:
                    ctf->SetColorSpaceToRGB();
                    return { { 0.000, 0.18995, 0.07176, 0.23217 }, { 0.067, 0.25107, 0.25237, 0.63374 },
                             { 0.133, 0.27698, 0.46153, 0.93309 }, { 0.200, 0.24427, 0.60937, 0.99697 },
                             { 0.267, 0.17223, 0.71680, 0.93981 }, { 0.333, 0.10342, 0.89600, 0.71500 },
                             { 0.400, 0.26180, 0.96765, 0.52981 }, { 0.467, 0.47375, 0.99755, 0.34963 },
                             { 0.533, 0.69525, 0.97610, 0.21294 }, { 0.600, 0.88331, 0.86553, 0.21719 },
                             { 0.667, 0.98000, 0.73000, 0.22161 }, { 0.733, 0.99297, 0.55214, 0.15417 },
                             { 0.800, 0.94084, 0.35566, 0.07031 }, { 0.867, 0.84662, 0.21407, 0.02487 },
                             { 0.933, 0.68602, 0.09536, 0.00481 }, { 1.000, 0.47960, 0.01583, 0.01055 } };
                case VTKN_SPECTRUM_WARM:
                    ctf->SetColorSpaceToRGB();
                    return { { 0, 0.549, 0.176, 0.016 }, { 0.143, 0.8, 0.298, 0.008 }, { 0.286, 0.925, 0.439, 0.078 },
                             { 0.429, 0.996, 0.6, 0.161 }, { 0.571, 0.996, 0.769, 0.31 }, { 0.714, 0.996, 0.89, 0.569 },
                             { 0.857, 1, 0.969, 0.737 }, { 1, 1, 1, 0.898 } };
                case VTKN_SPECTRUM_COOL:
                    ctf->SetColorSpaceToRGB();
                    return { { 0, 0.031, 0.271, 0.58 }, { 0.143, 0.129, 0.443, 0.71 }, { 0.286, 0.259, 0.573, 0.776 },
                             { 0.429, 0.42, 0.682, 0.839 }, { 0.571, 0.62, 0.792, 0.882 }, { 0.714, 0.776, 0.859, 0.937 },
                             { 0.857, 0.871, 0.922, 0.969 }, { 1, 0.969, 0.984, 1 } };
                case VTKN_SPECTRUM_CIVIDIS:
                    ctf->SetColorSpaceToRGB();
                    return { { 0, 0, 0.125, 0.297 }, { 0.125, 0.023, 0.211, 0.43 }, { 0.25, 0.254, 0.301, 0.418 },
                             { 0.375, 0.375, 0.391, 0.43 }, { 0.5, 0.484, 0.48, 0.469 }, { 0.625, 0.609, 0.578, 0.465 },
                             { 0.75, 0.738, 0.684, 0.43 }, { 0.875, 0.875, 0.793, 0.363 }, { 1, 0.996, 0.91, 0.27 } };
                case VTKN_SPECTRUM_VIRIDIS:
                    ctf->SetColorSpaceToRGB();
                    return { { 0, 0.267, 0.005, 0.329 }, { 0.143, 0.275, 0.197, 0.497 },
                             { 0.286, 0.213, 0.359, 0.552 }, { 0.429, 0.153, 0.498, 0.558 },
                             { 0.571, 0.122, 0.632, 0.531 }, { 0.714, 0.29, 0.759, 0.428 },
                             { 0.857, 0.622, 0.854, 0.226 }, { 1, 0.993, 0.906, 0.144 } };
                case VTKN_SPECTRUM_PLASMA:
                    ctf->SetColorSpaceToRGB();
                    return { { 0, 0.05, 0.03, 0.528 }, { 0.143, 0.328, 0.007, 0.64 }, { 0.286, 0.545, 0.038, 0.647 },
                             { 0.429, 0.725, 0.197, 0.538 }, { 0.571, 0.859, 0.359, 0.408 },
                             { 0.714, 0.956, 0.534, 0.285 }, { 0.857, 0.995, 0.738, 0.167 }, { 1, 0.94, 0.975, 0.131 } };
                case VTKN_SPECTRUM_BLACK_BODY:
                    ctf->SetColorSpaceToLab();
                    return { { 0, 0, 0, 0 }, { 0.143, 0.257, 0.089, 0.069 }, { 0.286, 0.502, 0.123, 0.107 },
                             { 0.429, 0.735, 0.198, 0.124 }, { 0.571, 0.877, 0.395, 0.038 }, { 0.714, 0.911, 0.632, 0.1 },
                             { 0.857, 0.907, 0.855, 0.189 }, { 1, 1, 1, 1 } };
                case VTKN_SPECTRUM_INFERNO:
                    ctf->SetColorSpaceToLab();
                    return { { 0, 0.001, 0, 0.014 }, { 0.143, 0.159, 0.044, 0.329 }, { 0.286, 0.397, 0.083, 0.433 },
                             { 0.429, 0.623, 0.165, 0.388 }, { 0.571, 0.831, 0.283, 0.259 },
                             { 0.714, 0.962, 0.49, 0.084 }, { 0.857, 0.982, 0.756, 0.153 }, { 1, 0.988, 0.998, 0.645 } };
                case VTKN_SPECTRUM_KINDLMANN:
                    ctf->SetColorSpaceToLab();
                    return { { 0, 0, 0, 0 }, { 0.143, 0.14, 0.022, 0.46 }, { 0.286, 0.028, 0.243, 0.587 },
                             { 0.429, 0.021, 0.449, 0.382 }, { 0.571, 0.031, 0.625, 0.083 }, { 0.714, 0.44, 0.767, 0.037 },
                             { 0.857, 0.98, 0.815, 0.572 }, { 1, 1, 1, 1 } };
                case VTKN_SPECTRUM_GRAYSCALE:
                    ctf->SetColorSpaceToLab();
                    return { { 0, 0.2, 0.2, 0.2 }, { 1, 1.0, 1.0, 1.0 } };
                default:
                    throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The color spectrum type is not one of vtkn_spectrum_type.");
            }
        }
    }

    vtkn_color_spectrum DefaultColorSpectrum()
    {
        vtkn_color_spectrum spectrum = {};
        spectrum.type = VTKN_SPECTRUM_COOL_WARM;
        spectrum.mode = VTKN_SPECTRUM_MODE_DISCRETE;
        spectrum.number_of_colors = 9;
        spectrum.max_user_value = 1;
        return spectrum;
    }

    vtkSmartPointer<vtkColorTransferFunction> CreateColorTransferFunction(const vtkn_color_spectrum& spectrum)
    {
        vtkSmartPointer<vtkColorTransferFunction> ctf = vtkSmartPointer<vtkColorTransferFunction>::New();
        Points points = GetPoints(spectrum.type, ctf);
        double n = spectrum.reverse_colors ? 1 : 0;
        double k = spectrum.reverse_colors ? -1 : 1;
        double x = spectrum.color_brightness;
        for (const auto& point : points)
            ctf->AddRGBPoint(k * point[0] + n, point[1] + x * (1 - point[1]), point[2] + x * (1 - point[2]),
                             point[3] + x * (1 - point[3]));
        return ctf;
    }

    // Port of PrepareActorLookupTable: the colors of the spectrum between the range ends, and below and above them
    // the min and max colors when the user range lies inside the scalar range
    vtkSmartPointer<vtkLookupTable> CreateActorLookupTable(const vtkn_color_spectrum& spectrum, double scalarRangeMin,
                                                           double scalarRangeMax)
    {
        double min = scalarRangeMin;
        double max = scalarRangeMax;
        bool addMinColor = false;
        bool addMaxColor = false;
        int numOfColors = spectrum.mode == VTKN_SPECTRUM_MODE_SMOOTH ? 256 : spectrum.number_of_colors;
        vtkSmartPointer<vtkColorTransferFunction> ctf = CreateColorTransferFunction(spectrum);
        if (spectrum.min_max_type == VTKN_SPECTRUM_MIN_MAX_MANUAL)
        {
            min = std::min(scalarRangeMin, spectrum.min_user_value);
            max = std::max(scalarRangeMax, spectrum.max_user_value);
            if (spectrum.min_user_value > min && max != min)
            {
                addMinColor = true;
                min = spectrum.min_user_value;
            }
            if (spectrum.max_user_value < max && max != min)
            {
                addMaxColor = true;
                max = spectrum.max_user_value;
            }
        }
        //
        vtkSmartPointer<vtkLookupTable> lookupTable = vtkSmartPointer<vtkLookupTable>::New();
        double color[3];
        if (addMinColor || addMaxColor)
        {
            int numOfAllColors = numOfColors;
            double delta = (max - min) / numOfColors;
            if (addMinColor)
            {
                numOfAllColors++;
                min -= delta;
            }
            if (addMaxColor)
            {
                numOfAllColors++;
                max += delta;
            }
            lookupTable->SetTableRange(min, max);
            lookupTable->SetNumberOfTableValues(numOfAllColors);
            int count = 0;
            if (addMinColor)
                lookupTable->SetTableValue(count++, spectrum.min_color[0], spectrum.min_color[1], spectrum.min_color[2], 1);
            double rangeDelta = 1.0 / (numOfColors - 1);
            for (int i = 0; i < numOfColors; i++)
            {
                ctf->GetColor(i * rangeDelta, color);
                lookupTable->SetTableValue(count++, color[0], color[1], color[2], 1);
            }
            if (addMaxColor)
                lookupTable->SetTableValue(count++, spectrum.max_color[0], spectrum.max_color[1], spectrum.max_color[2], 1);
        }
        else
        {
            lookupTable->SetTableRange(min, max);
            lookupTable->SetNumberOfTableValues(numOfColors);
            double delta = 1.0 / (lookupTable->GetNumberOfColors() - 1);
            for (int i = 0; i < lookupTable->GetNumberOfColors(); i++)
            {
                ctf->GetColor(i * delta, color);
                lookupTable->SetTableValue(i, color[0], color[1], color[2], 1);
            }
        }
        return lookupTable;
    }
}
