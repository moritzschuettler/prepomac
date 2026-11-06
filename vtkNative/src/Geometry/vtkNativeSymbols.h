// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The symbol sources of vtkControl: the shapes the AddOriented* methods and AddSphereActor glyph onto the points of
// a symbol actor, each in glyph space (the x axis along the point normal) with the scale factor of its glyph.
#pragma once

#include "vtkNative.h"

#include <vtkPolyData.h>
#include <vtkSmartPointer.h>

namespace vtkn
{
    struct SymbolSource
    {
        vtkSmartPointer<vtkPolyData> Source;
        double ScaleFactor = 1;
        bool Flat = false;
    };

    SymbolSource CreateSymbolSource(const vtkn_symbol_parameters& parameters);
}
