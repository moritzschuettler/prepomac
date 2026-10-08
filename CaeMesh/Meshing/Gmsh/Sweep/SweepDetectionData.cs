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
        private SweepBlocks[] _directions;
        private double[] _directionScores;
        private Dictionary<int, string> _unreachedPartReasons;


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
        /// as a whole - but the sub-parts that were reached are still swept by the ways chosen for them, and
        /// are the ones that need no splitting.
        /// </summary>
        public HashSet<int> SweepablePartIds { get { return _sweepablePartIds; } }
        public string Stamp { get { return _stamp; } }
        /// <summary>
        /// Every distinct way the detection found to sweep the group as a whole, best first, with their scores in
        /// DirectionScores. Blocks is the first of them. For a single part these are the directions it can be
        /// swept in on its own, one per pair of source and target surfaces, a revolved part having its one
        /// revolution; a compound has the one way chosen for it; empty when the group cannot be swept.
        /// </summary>
        public SweepBlocks[] Directions { get { return _directions; } }
        public double[] DirectionScores { get { return _directionScores; } }
        /// <summary>
        /// Why a part the detection did not sweep cannot be swept with the others although it can be on its own, by
        /// part id: the surface on which its best way disagrees with the way a neighbour is swept, or what the source
        /// it is swept from alone ran into against its neighbours. A part that cannot be swept on its own is not
        /// here; its own detection says why.
        /// </summary>
        public Dictionary<int, string> UnreachedPartReasons { get { return _unreachedPartReasons; } }
        /// <summary>
        /// Whether the group is not swept but revolved: a single part turned about an axis it touches.
        /// </summary>
        public bool IsRevolved { get { return _blocks != null && _blocks.HasRevolvedBlocks; } }


        // Constructors
        public SweepDetectionData(SweepBlocks blocks, string error, string errorPartName,
                                  HashSet<int> sweepablePartIds, string stamp, SweepBlocks partialBlocks = null,
                                  IList<SweepBlocks> directions = null, IList<double> directionScores = null,
                                  Dictionary<int, string> unreachedPartReasons = null)
        {
            _blocks = blocks;
            _partialBlocks = partialBlocks;
            _error = error;
            _errorPartName = errorPartName;
            _sweepablePartIds = sweepablePartIds != null ? sweepablePartIds : new HashSet<int>();
            _stamp = stamp;
            _unreachedPartReasons = unreachedPartReasons != null ? unreachedPartReasons : new Dictionary<int, string>();
            //
            if (directions == null || directionScores == null)
            {
                _directions = blocks != null ? new SweepBlocks[] { blocks } : new SweepBlocks[0];
                _directionScores = blocks != null ? new double[] { 0 } : new double[0];
            }
            else
            {
                int[] order = Enumerable.Range(0, directions.Count).OrderByDescending(i => directionScores[i]).ToArray();
                _directions = order.Select(i => directions[i]).ToArray();
                _directionScores = order.Select(i => directionScores[i]).ToArray();
            }
        }


        // Methods
        /// <summary>
        /// The geometry the detection is a function of: the topology counts of every part of the group, the box it
        /// occupies, the box taken without the offset an exploded view puts on the part, and the lengths of its
        /// edges and the areas of its faces, so that a shape changed within its box and its counts - a split moved,
        /// a face repaired - is a changed geometry as well.
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
                    AppendMeasures(sb, vis, step, step * GetBoxSize(bb));
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
            return 1E-7 * GetBoxSize(bb);
        }
        private static double GetBoxSize(BoundingBox bb)
        {
            double size = Math.Max(bb.MaxX - bb.MinX, Math.Max(bb.MaxY - bb.MinY, bb.MaxZ - bb.MinZ));
            if (!(size > 0) || double.IsInfinity(size)) size = 1;
            return size;
        }
        /// <summary>
        /// The lengths of the edges and the areas of the faces folded into one number each, every one written with
        /// the digits the coordinates are, lengths by the length step and areas by the area step. The measures do
        /// not move with the part, so the offset of an exploded view leaves them alone.
        /// </summary>
        private static void AppendMeasures(StringBuilder sb, VisualizationData vis, double lengthStep, double areaStep)
        {
            if (vis == null) return;
            sb.Append('|');
            sb.Append(FoldMeasures(vis.EdgeLengths, lengthStep));
            sb.Append('|');
            sb.Append(FoldMeasures(vis.FaceAreas, areaStep));
        }
        private static long FoldMeasures(double[] measures, double step)
        {
            long fold = 0;
            if (measures == null) return fold;
            foreach (var measure in measures)
            {
                if (double.IsNaN(measure) || double.IsInfinity(measure)) continue;
                fold = unchecked(fold * 31 + (long)Math.Round(measure / step));
            }
            return fold;
        }
        private static void AppendCoordinate(StringBuilder sb, double value, double step)
        {
            sb.Append('|');
            sb.Append((Math.Round(value / step) + 0.0).ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
