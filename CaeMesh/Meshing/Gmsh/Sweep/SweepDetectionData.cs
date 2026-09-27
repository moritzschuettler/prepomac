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
using System.Text;

namespace CaeMesh
{
    /// <summary>
    /// What the automatic sweep detection found for one group of parts - a single solid part, or the sub-parts
    /// of one compound part. The detection depends on the geometry alone and not on what the user selected, so
    /// the result is kept on the part leading the group and reused until the geometry changes. The stamp is
    /// what the result was detected from; a group whose stamp no longer matches is detected again.
    /// </summary>
    public class SweepDetectionData
    {
        // Variables
        private SweepBlocks _blocks;
        private SweepBlocks _partialBlocks;
        private string _error;
        private string _errorPartName;
        private HashSet<int> _sweepablePartIds;
        private string _stamp;


        // Properties
        /// <summary>
        /// The blocks of the whole group, or null when the group cannot be swept as a whole.
        /// </summary>
        public SweepBlocks Blocks { get { return _blocks; } }
        /// <summary>
        /// The blocks of the parts the detection did sweep, when the only thing that stops the group from being
        /// swept as a whole is that some of its parts are not reached; null otherwise. A compound sweeps these and
        /// leaves the parts that were not reached to tetrahedra.
        /// </summary>
        public SweepBlocks PartialBlocks { get { return _partialBlocks; } }
        /// <summary>
        /// Why the group cannot be swept as a whole, or null when it can.
        /// </summary>
        public string Error { get { return _error; } }
        /// <summary>
        /// The part the error was raised on, or null when the error names no part of its own.
        /// </summary>
        public string ErrorPartName { get { return _errorPartName; } }
        /// <summary>
        /// The parts the detection did sweep. A group in which one sub-part cannot be reached cannot be swept
        /// as a whole - but the sub-parts that were reached are still swept by the trial that reached them,
        /// and are the ones that need no splitting.
        /// </summary>
        public HashSet<int> SweepablePartIds { get { return _sweepablePartIds; } }
        public string Stamp { get { return _stamp; } }


        // Constructors
        public SweepDetectionData(SweepBlocks blocks, string error, string errorPartName,
                                  HashSet<int> sweepablePartIds, string stamp, SweepBlocks partialBlocks = null)
        {
            _blocks = blocks;
            _partialBlocks = partialBlocks;
            _error = error;
            _errorPartName = errorPartName;
            _sweepablePartIds = sweepablePartIds != null ? sweepablePartIds : new HashSet<int>();
            _stamp = stamp;
        }


        // Methods
        /// <summary>
        /// The geometry the detection is a function of: the topology counts of every part of the group and the
        /// box it occupies, the box taken without the offset an exploded view puts on the part.
        /// </summary>
        public static string GetStamp(IEnumerable<BasePart> parts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var part in parts)
            {
                VisualizationData vis = part.Visualization;
                sb.Append(part.PartId);
                sb.Append('|');
                sb.Append(vis == null ? 0 : vis.FaceCount);
                sb.Append('|');
                sb.Append(vis == null || vis.EdgeCellIdsByEdge == null ? 0 : vis.EdgeCellIdsByEdge.Length);
                sb.Append('|');
                sb.Append(vis == null || vis.VertexNodeIds == null ? 0 : vis.VertexNodeIds.Length);
                //
                BoundingBox bb = part.BoundingBox;
                if (bb != null)
                {
                    double[] offset = part.Offset != null ? part.Offset : new double[3];
                    double step = GetCoordinateStep(bb);
                    AppendCoordinate(sb, bb.MinX - offset[0], step);
                    AppendCoordinate(sb, bb.MinY - offset[1], step);
                    AppendCoordinate(sb, bb.MinZ - offset[2], step);
                    AppendCoordinate(sb, bb.MaxX - offset[0], step);
                    AppendCoordinate(sb, bb.MaxY - offset[1], step);
                    AppendCoordinate(sb, bb.MaxZ - offset[2], step);
                }
                sb.Append(';');
            }
            return sb.ToString();
        }
        /// <summary>
        /// A coordinate is written with the digits a changed geometry shows in and not with all of them.
        /// An exploded view adds an offset to the box and takes it away again, which does not give back the
        /// very same number, and a stamp written with every digit would throw the detection away each time
        /// a part is moved for looking at. The digits are counted from the size of the part and not from the
        /// coordinate, since a coordinate that should be zero comes out of CAD as a tiny number whose every
        /// significant digit is the noise the offset changes.
        /// </summary>
        private static double GetCoordinateStep(BoundingBox bb)
        {
            double size = Math.Max(bb.MaxX - bb.MinX, Math.Max(bb.MaxY - bb.MinY, bb.MaxZ - bb.MinZ));
            if (!(size > 0) || double.IsInfinity(size)) size = 1;
            return size * 1E-7;
        }
        private static void AppendCoordinate(StringBuilder sb, double value, double step)
        {
            sb.Append('|');
            sb.Append((Math.Round(value / step) + 0.0).ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
