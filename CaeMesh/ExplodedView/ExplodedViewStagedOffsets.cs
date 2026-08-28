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
    /// The offset of every part, split into the steps of the placement tree it was built from.
    ///
    /// A part does not carry one offset but the offset it takes over from each of its ancestors followed by
    /// its own, so the entry at index k holds how much the part moves while the parts of the k-th level
    /// come off. Adding all of them back up gives the offset at the full separation.
    ///
    /// Scaled all at once, every part moves at the same time. Scaled step by step, the scale factor range
    /// up to the full separation is divided among the levels: the first level comes off first and carries
    /// everything mounted on it along, then the parts mounted on it come off, and so on. The two agree at
    /// the full separation, and above it both scale the whole offset.
    /// </summary>
    public class ExplodedViewStagedOffsets
    {
        // Variables                                                                                                                
        private int _stepCount;
        private Dictionary<string, double[][]> _stepOffsetsByPart;


        // Properties                                                                                                               
        public int StepCount { get { return _stepCount; } }


        // Constructors                                                                                                             
        public ExplodedViewStagedOffsets(Dictionary<string, double[][]> stepOffsetsByPart)
        {
            _stepOffsetsByPart = stepOffsetsByPart;
            //
            _stepCount = 0;
            foreach (var entry in _stepOffsetsByPart)
            {
                if (entry.Value.Length > _stepCount) _stepCount = entry.Value.Length;
            }
        }


        // Methods                                                                                                                  
        public Dictionary<string, double[]> GetPartOffsets(double scaleFactor, bool sequential)
        {
            bool stepByStep = sequential && _stepCount > 1 && scaleFactor < 1;
            //
            Dictionary<string, double[]> partOffsets = new Dictionary<string, double[]>(_stepOffsetsByPart.Count);
            foreach (var entry in _stepOffsetsByPart)
            {
                double[][] stepOffsets = entry.Value;
                double[] partOffset = new double[3];
                //
                for (int i = 0; i < stepOffsets.Length; i++)
                {
                    double stepScaleFactor = stepByStep ? GetStepScaleFactor(i, scaleFactor) : scaleFactor;
                    if (stepScaleFactor == 0) continue;
                    //
                    partOffset[0] += stepOffsets[i][0] * stepScaleFactor;
                    partOffset[1] += stepOffsets[i][1] * stepScaleFactor;
                    partOffset[2] += stepOffsets[i][2] * stepScaleFactor;
                }
                //
                partOffsets.Add(entry.Key, partOffset);
            }
            //
            return partOffsets;
        }
        /// <summary>
        /// How far the given step has progressed: the steps are taken one after the other over the scale
        /// factor range up to the full separation, so the step is untouched before its turn, moves during
        /// it and stays where it arrived afterwards
        /// </summary>
        private double GetStepScaleFactor(int stepIndex, double scaleFactor)
        {
            double stepScaleFactor = scaleFactor * _stepCount - stepIndex;
            //
            if (stepScaleFactor < 0) return 0;
            else if (stepScaleFactor > 1) return 1;
            else return stepScaleFactor;
        }
    }
}
