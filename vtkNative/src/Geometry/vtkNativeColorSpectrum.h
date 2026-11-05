// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The color spectrum of the results: the color transfer function of each spectrum type and the lookup table the
// result actors are colored with. Port of GetColorTransferFunction and PrepareActorLookupTable of vtkControl.
#pragma once

#include "vtkNative.h"

#include <vtkColorTransferFunction.h>
#include <vtkLookupTable.h>
#include <vtkSmartPointer.h>

namespace vtkn
{
    vtkn_color_spectrum DefaultColorSpectrum();
    vtkSmartPointer<vtkColorTransferFunction> CreateColorTransferFunction(const vtkn_color_spectrum& spectrum);
    vtkSmartPointer<vtkLookupTable> CreateActorLookupTable(const vtkn_color_spectrum& spectrum, double scalarRangeMin,
                                                           double scalarRangeMax);
}
