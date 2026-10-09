// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    // The deformation a From Integration history output integrates over. The results name these with spaces
    // and ampersands, so PythonDeformationVariable maps each to the name the results keep
    public enum DeformationVariableEnum
    {
        Displacements,
        Forces,
        SurfaceNormals,
        WearDepths,
        MeshDeformation,
        DispDef,
        DispDefDepth
    }
}
