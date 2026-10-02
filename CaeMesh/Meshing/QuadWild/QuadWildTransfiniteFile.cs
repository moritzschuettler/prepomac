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
using System.Globalization;
using System.Linq;
using System.Text;

namespace CaeMesh
{
    /// <summary>
    /// The structured faces QuadWild meshes, by the one definition all meshers follow, written as &lt;input&gt;.tfaces for
    /// quad_from_patches: every face as a point inside it, the positions of its corners in loop order and its name. QuadWild
    /// never sees face ids and renumbers the triangulation, so the faces are matched by position, as the prescribed nodes
    /// are. With the file QuadWild finds no structured faces of its own.
    /// </summary>
    public static class QuadWildTransfiniteFile
    {
        // Methods
        public static int Write(string fileName, FeMesh geometry, IEnumerable<BasePart> parts, bool threeSided,
                                bool fourSided, double transfiniteAngleDeg)
        {
            List<string> lines = new List<string>();
            foreach (var part in parts)
            {
                if (part.PartType != PartType.Shell || part.Visualization == null) continue;
                //
                VisualizationData vis = part.Visualization;
                foreach (var face in TransfiniteFaceDetector.Detect(geometry, part, threeSided, fourSided,
                                                                    transfiniteAngleDeg))
                {
                    int[] cellIds = vis.CellIdsByFace[face.FaceId];
                    if (cellIds == null || cellIds.Length == 0) continue;
                    // The centre of a cell of the face lies inside the face
                    int[] cell = vis.Cells[cellIds[cellIds.Length / 2]];
                    double[] inside = new double[3];
                    foreach (var nodeId in cell)
                    {
                        for (int i = 0; i < 3; i++) inside[i] += geometry.Nodes[nodeId].Coor[i] / cell.Length;
                    }
                    //
                    StringBuilder sb = new StringBuilder();
                    sb.Append("face ").Append(face.NumberOfSides).Append(' ').Append(Format(inside));
                    foreach (var cornerNodeId in face.CornerNodeIds)
                        sb.Append(' ').Append(Format(geometry.Nodes[cornerNodeId].Coor));
                    sb.Append(" surface ").Append(face.FaceId + 1).Append(" of ").Append(part.Name);
                    lines.Add(sb.ToString());
                }
            }
            System.IO.File.WriteAllText(fileName, "faces " + lines.Count + Environment.NewLine +
                                                  string.Join(Environment.NewLine, lines) + Environment.NewLine);
            return lines.Count;
        }
        private static string Format(double[] coor)
        {
            return string.Join(" ", coor.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
        }
    }
}
