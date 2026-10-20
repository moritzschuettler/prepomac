// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CaeGlobals
{
    public class RenamedTypeSerializationBinder : SerializationBinder
    {
        // Variables                                                                                                                
        private static readonly Dictionary<string, string> _renamedTypeNames = GetRenamedTypeNames();
        private static readonly RenamedTypeSerializationBinder _instance = new RenamedTypeSerializationBinder();


        // Properties                                                                                                               
        public static RenamedTypeSerializationBinder Instance { get { return _instance; } }


        // Constructors                                                                                                             
        private RenamedTypeSerializationBinder()
        {
        }


        // Methods                                                                                                                  
        private static Dictionary<string, string> GetRenamedTypeNames()
        {
            // Add one entry for each renamed or moved serializable type:
            // { "OldNamespace.OldTypeName", "NewNamespace.NewTypeName, NewAssemblyName" }
            return new Dictionary<string, string>()
            {
                { "CaeMesh.Meshing.ElementSizeTypeEnum", "CaeMesh.ElementSizeTypeEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshAlgorithmMesh2DEnum", "CaeMesh.GmshAlgorithmMesh2DEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshAlgorithmMesh3DEnum", "CaeMesh.GmshAlgorithmMesh3DEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshAlgorithmRecombineEnum", "CaeMesh.GmshAlgorithmRecombineEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshCommandEnum", "CaeMesh.GmshCommandEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshOptimizeFirstOrderShellEnum", "CaeMesh.GmshOptimizeFirstOrderShellEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshOptimizeFirstOrderSolidEnum", "CaeMesh.GmshOptimizeFirstOrderSolidEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshOptimizeHighOrderEnum", "CaeMesh.GmshOptimizeHighOrderEnum, CaeMesh" },
                { "CaeMesh.TetrahedralNetgen", "CaeMesh.NetgenMesh, CaeMesh" },
                { "PrePoMax.Commands.CAddTetrahedralNetgenToUncoveredParts",
                  "PrePoMax.Commands.CAddNetgenMeshToUncoveredParts, PrePoMax" },
                { "PrePoMax.SplitPartMeshData", "CaeMesh.SplitPartMeshData, CaeMesh" },
                { "CaeMesh.SurfaceExtraction", "CaeMesh.FaceOffset, CaeMesh" },
                // The extrude mesh item was removed; its selected source surfaces are swept by a sweep mesh item
                { "CaeMesh.ExtrudeMesh", "CaeMesh.SweepMesh, CaeMesh" }
            };
        }
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type unityHolder = BinaryFormatterTools.BindUnityHolder(typeName);
            if (unityHolder != null) return unityHolder;
            if (_renamedTypeNames.Count == 0 || typeName == null) return null;
            //
            string renamedTypeName;
            if (_renamedTypeNames.TryGetValue(typeName, out renamedTypeName)) return Type.GetType(renamedTypeName, false);
            //
            renamedTypeName = GetRenamedGenericArgumentTypeNames(typeName);
            if (renamedTypeName == null) return null;
            //
            return Type.GetType(renamedTypeName + ", " + assemblyName, false);
        }
        private static string GetRenamedGenericArgumentTypeNames(string typeName)
        {
            string renamedTypeName = typeName;
            //
            foreach (var entry in _renamedTypeNames)
            {
                int index = 0;
                while ((index = renamedTypeName.IndexOf(entry.Key, index, StringComparison.Ordinal)) >= 0)
                {
                    int nameEnd = index + entry.Key.Length;
                    bool isArgumentStart = index > 0 && renamedTypeName[index - 1] == '[';
                    bool isArgumentEnd = nameEnd < renamedTypeName.Length &&
                                         (renamedTypeName[nameEnd] == ',' || renamedTypeName[nameEnd] == ']');
                    //
                    if (isArgumentStart && isArgumentEnd)
                    {
                        int argumentEnd = GetArgumentEnd(renamedTypeName, nameEnd);
                        renamedTypeName = renamedTypeName.Substring(0, index) + entry.Value +
                                          renamedTypeName.Substring(argumentEnd);
                        index += entry.Value.Length;
                    }
                    else index = nameEnd;
                }
            }
            //
            if (renamedTypeName == typeName) return null;
            else return renamedTypeName;
        }
        private static int GetArgumentEnd(string typeName, int start)
        {
            int depth = 0;
            //
            for (int i = start; i < typeName.Length; i++)
            {
                if (typeName[i] == '[') depth++;
                else if (typeName[i] == ']')
                {
                    if (depth == 0) return i;
                    else depth--;
                }
            }
            //
            return typeName.Length;
        }
    }
}
