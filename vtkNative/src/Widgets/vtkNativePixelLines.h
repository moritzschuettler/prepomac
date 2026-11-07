// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The thin lines of the widgets. VTK 9 maps a whole display coordinate onto the border between two pixels, so with
// multisampling a one pixel line drawn there covers two rows at half intensity and comes out gray; VTK 5.8 drew it into
// one row. Moved by half a pixel, the line covers the row it was meant for.
#pragma once

#include <vtkAlgorithmOutput.h>
#include <vtkNew.h>
#include <vtkPolyData.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkTransform.h>
#include <vtkTransformFilter.h>

namespace vtkn
{
    inline void SetPixelCenteredInput(vtkPolyDataMapper2D* mapper, vtkPolyData* polyData)
    {
        vtkNew<vtkTransform> transform;
        transform->Translate(0.5, 0.5, 0);
        vtkNew<vtkTransformFilter> filter;
        filter->SetInputData(polyData);
        filter->SetTransform(transform);
        mapper->SetInputConnection(filter->GetOutputPort());
    }
}
