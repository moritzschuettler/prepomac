// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CaeMesh
{
    /// <summary>
    /// Reads back the surfaces and the edges a message names the way the interface does - "surface 5 of
    /// Solid_part-50", "edge 12 of Solid_part-50" - so that the geometry a meshing error is about can be shown.
    /// A message is the one thing that comes back from a mesher running in a process of its own, and what it
    /// names is exactly what it is about, so the names are read rather than carried a second time as ids.
    /// </summary>
    public static class MessageGeometryReferences
    {
        // Methods
        /// <summary>
        /// The geometry ids of every surface and edge the message names on a part of the geometry. A name of a
        /// part the geometry does not have, or of an item the part does not have, is skipped.
        /// </summary>
        public static int[] GetGeometryIds(string message, FeMesh geometry)
        {
            if (string.IsNullOrEmpty(message) || geometry == null || geometry.Parts == null ||
                geometry.Parts.Count == 0) return new int[0];
            // The longest name first, so that a name is not matched by another it starts with
            string[] partNames = geometry.Parts.Keys.OrderByDescending(name => name.Length).ToArray();
            string names = string.Join("|", partNames.Select(name => Regex.Escape(name)));
            Regex reference = new Regex(@"(?<![A-Za-z])(?i:(surface|edge|curve))\s+(\d+)\s+of\s+(" + names +
                                        @")(?![A-Za-z0-9_\-])");
            //
            int itemId;
            BasePart part;
            HashSet<int> geometryIds = new HashSet<int>();
            List<int> orderedGeometryIds = new List<int>();
            foreach (Match match in reference.Matches(message))
            {
                if (!int.TryParse(match.Groups[2].Value, out itemId)) continue;
                if (!geometry.Parts.TryGetValue(match.Groups[3].Value, out part) || part.Visualization == null) continue;
                // The interface counts the items from one
                itemId--;
                int geometryId;
                string word = match.Groups[1].Value.ToLowerInvariant();
                if (word == "surface")
                {
                    if (itemId < 0 || itemId >= part.Visualization.FaceCount) continue;
                    GeometryType surfaceType = part.PartType == PartType.Shell ? GeometryType.ShellFrontSurface
                                                                               : GeometryType.SolidSurface;
                    geometryId = FeMesh.GetGeometryId(itemId, (int)surfaceType, part.PartId);
                }
                else
                {
                    int[][] edgeCellIdsByEdge = part.Visualization.EdgeCellIdsByEdge;
                    if (edgeCellIdsByEdge == null || itemId < 0 || itemId >= edgeCellIdsByEdge.Length) continue;
                    geometryId = FeMesh.GetGeometryId(itemId, (int)GeometryType.Edge, part.PartId);
                }
                if (geometryIds.Add(geometryId)) orderedGeometryIds.Add(geometryId);
            }
            return orderedGeometryIds.ToArray();
        }
    }
}
