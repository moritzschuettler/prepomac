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

namespace CaeMesh
{
    /// <summary>
    /// The layout of the disassembly method at the full separation, kept for as long as the parts it was
    /// built from do not change. The offsets are linear in the scale factor, so dragging the scale factor
    /// only rescales the stored layout instead of rebuilding the contact graph on every step. Whether the
    /// parts are scaled all at once or one level after the other does not change the layout, so it is not
    /// part of what the cache is keyed on.
    ///
    /// The cache is not invalidated by the callers but by the state of the parts themselves: the part
    /// names, their node counts and their bounding boxes. Anything that adds, removes, renames, moves or
    /// remeshes a part changes that state and the layout is rebuilt. A part state that changes without a
    /// reason - the bounding boxes carry the exploded view offset while the view is applied - only costs
    /// one rebuild and can never return a stale layout.
    ///
    /// A layout that could not be built is cached as well, so a model the method cannot work with does not
    /// pay for the whole analysis on every step only to fall back to another method again.
    /// </summary>
    public class DisassemblyExplodedViewCache
    {
        // Variables                                                                                                                
        private int _explodedDirection;
        private double _tolerance;
        private string[] _partNames;
        private double[] _partState;
        private ExplodedViewStagedOffsets _stagedOffsets;


        // Properties
        /// <summary>
        /// The number of steps a sequential explosion is taken in; one when no layout could be built
        /// </summary>
        public int StepCount { get { return _stagedOffsets != null ? _stagedOffsets.StepCount : 1; } }


        // Constructors                                                                                                             
        public DisassemblyExplodedViewCache(int explodedDirection, double tolerance, string[] sortedPartNames,
                                            double[] partState, ExplodedViewStagedOffsets stagedOffsets)
        {
            _explodedDirection = explodedDirection;
            _tolerance = tolerance;
            _partNames = sortedPartNames;
            _partState = partState;
            _stagedOffsets = stagedOffsets;
        }


        // Static methods                                                                                                           
        /// <summary>
        /// The node count and the bounding box of every part, in the order of the given part names
        /// </summary>
        public static double[] GetPartState(FeMesh mesh, string[] sortedPartNames)
        {
            double[] partState = new double[7 * sortedPartNames.Length];
            //
            int index = 0;
            BasePart part;
            for (int i = 0; i < sortedPartNames.Length; i++)
            {
                if (!mesh.Parts.TryGetValue(sortedPartNames[i], out part))
                {
                    index += 7;
                    continue;
                }
                //
                partState[index++] = part.NodeLabels != null ? part.NodeLabels.Length : 0;
                //
                BoundingBox boundingBox = part.BoundingBox;
                partState[index++] = boundingBox.MinX;
                partState[index++] = boundingBox.MinY;
                partState[index++] = boundingBox.MinZ;
                partState[index++] = boundingBox.MaxX;
                partState[index++] = boundingBox.MaxY;
                partState[index++] = boundingBox.MaxZ;
            }
            //
            return partState;
        }


        // Methods                                                                                                                  
        public bool IsValidFor(int explodedDirection, double tolerance, string[] sortedPartNames, double[] partState)
        {
            if (_explodedDirection != explodedDirection || _tolerance != tolerance) return false;
            if (_partNames.Length != sortedPartNames.Length || _partState.Length != partState.Length) return false;
            //
            for (int i = 0; i < _partNames.Length; i++)
            {
                if (!string.Equals(_partNames[i], sortedPartNames[i], StringComparison.Ordinal)) return false;
            }
            for (int i = 0; i < _partState.Length; i++)
            {
                if (_partState[i] != partState[i]) return false;
            }
            //
            return true;
        }
        /// <summary>
        /// The scaled offsets, or null when the cached outcome is that the method cannot be used
        /// </summary>
        public Dictionary<string, double[]> GetPartOffsets(double scaleFactor, bool sequential)
        {
            if (_stagedOffsets == null) return null;
            //
            return _stagedOffsets.GetPartOffsets(scaleFactor, sequential);
        }
    }
}
