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
        // Variables                                                                                                                
        // The pattern depends on the names of the parts only, and the output window asks for it on every move
        // of the mouse, so the last one is kept
        private static string _partNamesKey;
        private static Regex _reference;


        // Methods                                                                                                                  
        /// <summary>
        /// The geometry ids of every surface and edge the message names on a part of the geometry. A name of a
        /// part the geometry does not have, or of an item the part does not have, is skipped.
        /// </summary>
        public static int[] GetGeometryIds(string message, FeMesh geometry)
        {
            Regex reference = GetReferenceRegex(message, geometry);
            if (reference == null) return new int[0];
            //
            int geometryId;
            HashSet<int> geometryIds = new HashSet<int>();
            List<int> orderedGeometryIds = new List<int>();
            foreach (Match match in reference.Matches(message))
            {
                geometryId = GetGeometryId(match, geometry);
                if (geometryId >= 0 && geometryIds.Add(geometryId)) orderedGeometryIds.Add(geometryId);
            }
            return orderedGeometryIds.ToArray();
        }
        /// <summary>
        /// The geometry id of the surface or the edge whose name covers the given position of the message, or -1
        /// when no name of a surface or an edge of the geometry covers it.
        /// </summary>
        public static int GetGeometryIdAt(string message, int position, FeMesh geometry)
        {
            Regex reference = GetReferenceRegex(message, geometry);
            if (reference == null || position < 0 || position >= message.Length) return -1;
            //
            foreach (Match match in reference.Matches(message))
            {
                if (position < match.Index) break;
                if (position < match.Index + match.Length) return GetGeometryId(match, geometry);
            }
            return -1;
        }
        /// <summary>
        /// Every name of a surface or an edge of the geometry in the message, as its start and its length.
        /// </summary>
        public static int[][] GetGeometryNameRanges(string message, FeMesh geometry)
        {
            Regex reference = GetReferenceRegex(message, geometry);
            if (reference == null) return null;
            //
            List<int[]> ranges = new List<int[]>();
            foreach (Match match in reference.Matches(message))
            {
                if (GetGeometryId(match, geometry) >= 0) ranges.Add(new int[] { match.Index, match.Length });
            }
            return ranges.Count > 0 ? ranges.ToArray() : null;
        }
        private static Regex GetReferenceRegex(string message, FeMesh geometry)
        {
            if (string.IsNullOrEmpty(message) || geometry == null || geometry.Parts == null ||
                geometry.Parts.Count == 0) return null;
            // The longest name first, so that a name is not matched by another it starts with
            string[] partNames = geometry.Parts.Keys.OrderByDescending(name => name.Length).ToArray();
            string names = string.Join("|", partNames.Select(name => Regex.Escape(name)));
            //
            Regex reference = _reference;
            if (reference == null || _partNamesKey != names)
            {
                reference = new Regex(@"(?<![A-Za-z])(?i:(surface|edge|curve))\s+(\d+)\s+of\s+(" + names +
                                      @")(?![A-Za-z0-9_\-])");
                _reference = reference;
                _partNamesKey = names;
            }
            return reference;
        }
        private static int GetGeometryId(Match match, FeMesh geometry)
        {
            int itemId;
            BasePart part;
            if (!int.TryParse(match.Groups[2].Value, out itemId)) return -1;
            if (!geometry.Parts.TryGetValue(match.Groups[3].Value, out part) || part.Visualization == null) return -1;
            // The interface counts the items from one
            itemId--;
            string word = match.Groups[1].Value.ToLowerInvariant();
            if (word == "surface")
            {
                if (itemId < 0 || itemId >= part.Visualization.FaceCount) return -1;
                GeometryType surfaceType = part.PartType == PartType.Shell ? GeometryType.ShellFrontSurface
                                                                           : GeometryType.SolidSurface;
                return FeMesh.GetGeometryId(itemId, (int)surfaceType, part.PartId);
            }
            else
            {
                int[][] edgeCellIdsByEdge = part.Visualization.EdgeCellIdsByEdge;
                if (edgeCellIdsByEdge == null || itemId < 0 || itemId >= edgeCellIdsByEdge.Length) return -1;
                return FeMesh.GetGeometryId(itemId, (int)GeometryType.Edge, part.PartId);
            }
        }
    }
}
