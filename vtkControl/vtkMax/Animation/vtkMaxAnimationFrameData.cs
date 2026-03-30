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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Kitware.VTK;
using CaeGlobals;

namespace vtkControl
{
    public class vtkMaxAnimationFrameData
    {
        // Variables                                                                                                                
        public float[] Time;
        public int[] StepId;
        public int[] StepIncrementId;
        public float[] ScaleFactor;
        public double[] AllFramesScalarRange;
        public vtkMaxAnimationType AnimationType;
        public bool UseAllFrameData;
        public List<Dictionary<int, string>> AnimatedActorNames;
        public Dictionary<string, bool> ActorVisible;
        public HashSet<string> InitializedActorNames;
        public double MemMb;

        // Constructors                                                                                                             
        public vtkMaxAnimationFrameData()
            : this(null, null, null, null, null, vtkMaxAnimationType.ScaleFactor)
        {
        }

        public vtkMaxAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, float[] scale, double[] scalarRange,
                                        vtkMaxAnimationType animationType)
        {
            Time = time;
            StepId = stepId;
            StepIncrementId = stepIncrementId;
            ScaleFactor = scale;
            AllFramesScalarRange = scalarRange;
            AnimationType = animationType;
            AnimatedActorNames = new List<Dictionary<int, string>>();
            ActorVisible = new Dictionary<string, bool>();
            InitializedActorNames = new HashSet<string>();
            MemMb = 0;
        }
    }
}
