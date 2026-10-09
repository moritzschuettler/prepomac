// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    // The results name a deformation the way the dialog lists it, 'Surface Normals' or 'Disp&Def', which is no
    // option string a script can write, so the API takes the enumeration name and the results keep their own
    public static class PythonDeformationVariable
    {
        // Variables
        private static readonly Dictionary<DeformationVariableEnum, string> ModelNames =
            new Dictionary<DeformationVariableEnum, string>
            {
                { DeformationVariableEnum.Displacements, "Displacements" },
                { DeformationVariableEnum.Forces, "Forces" },
                { DeformationVariableEnum.SurfaceNormals, "Surface Normals" },
                { DeformationVariableEnum.WearDepths, "Wear Depths" },
                { DeformationVariableEnum.MeshDeformation, "Mesh Deformation" },
                { DeformationVariableEnum.DispDef, "Disp&Def" },
                { DeformationVariableEnum.DispDefDepth, "Disp&Def&Depth" }
            };


        // Static methods
        internal static string ToModelName(string value, string argumentName)
        {
            DeformationVariableEnum variable = PythonValueConverter.ToEnum<DeformationVariableEnum>(value,
                                                                                                   argumentName);
            string modelName = ModelNames[variable];
            if (!FeResults.GetPossibleDeformationFieldOutputNamesMap().ContainsKey(modelName))
                throw new PythonAPIException("The value '" + value + "' of the argument '" + argumentName +
                                             "' is not a deformation the results know.");
            //
            return modelName;
        }
        // A name the results keep that has no option string is returned as it is, so nothing is hidden
        public static string ToApiName(string modelName)
        {
            if (modelName == null) return null;
            //
            foreach (var entry in ModelNames)
            {
                if (string.Equals(entry.Value, modelName, StringComparison.OrdinalIgnoreCase))
                    return entry.Key.ToString();
            }
            return modelName;
        }
    }
}
