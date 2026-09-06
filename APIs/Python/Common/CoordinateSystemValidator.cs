// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;

namespace APIs.Python
{
    // The axes of a coordinate system can only be checked once every defining point is a coordinate,
    // so a point built from a selection is first resolved by the mesh the selection was made on. The
    // check runs on a copy, which leaves the proposed system with the selection descriptions a replay
    // needs, and it uses the domain check so a script is refused exactly what the dialog refuses.
    internal static class CoordinateSystemValidator
    {
        // Methods
        public static void ValidateAxes(CoordinateSystem coordinateSystem, FeMesh mesh)
        {
            CoordinateSystem resolved = coordinateSystem;
            if (UsesSelectedPoints(coordinateSystem))
            {
                if (mesh == null || !AreCreationIdsInMesh(coordinateSystem, mesh)) return;
                //
                resolved = coordinateSystem.DeepClone();
                mesh.UpdateCoordinateSystem(resolved);
            }
            //
            string error;
            if (!resolved.IsProperlyDefined(out error)) throw new PythonAPIException(error);
        }
        private static bool UsesSelectedPoints(CoordinateSystem coordinateSystem)
        {
            return coordinateSystem.CenterCreatedFrom != CsPointCreatedFromEnum.Coordinates ||
                   coordinateSystem.PointXCreatedFrom != CsPointCreatedFromEnum.Coordinates ||
                   coordinateSystem.PointXYCreatedFrom != CsPointCreatedFromEnum.Coordinates;
        }
        // A point kept from an existing system can name a node an intervening mesh change removed;
        // the mesh then cannot complete the axes, so there is nothing to check here
        private static bool AreCreationIdsInMesh(CoordinateSystem coordinateSystem, FeMesh mesh)
        {
            return AreNodeIdsInMesh(coordinateSystem.CenterCreationIds, mesh) &&
                   AreNodeIdsInMesh(coordinateSystem.PointXCreationIds, mesh) &&
                   AreNodeIdsInMesh(coordinateSystem.PointXYCreationIds, mesh);
        }
        private static bool AreNodeIdsInMesh(int[] nodeIds, FeMesh mesh)
        {
            if (nodeIds == null) return true;
            //
            foreach (int nodeId in nodeIds)
            {
                if (!mesh.Nodes.ContainsKey(nodeId)) return false;
            }
            //
            return true;
        }
    }
}
