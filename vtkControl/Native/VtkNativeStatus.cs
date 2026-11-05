// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace vtkControl.Native
{
    // Mirrors vtkn_status of vtkNative.h
    public enum VtkNativeStatus
    {
        Ok = 0,
        InvalidArgument = 1,
        InvalidHandle = 2,
        BufferTooSmall = 3,
        NotFound = 4,
        Vtk = 5,
        Internal = 6,
        LibraryNotLoaded = -1
    }
}
