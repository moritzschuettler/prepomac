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
using CaeGlobals;
using CaeMesh.Sweep;

namespace CaeMesh
{
    /// <summary>
    /// Builds the solid mesh of a swept volume from the meshed source and wall surfaces.
    ///
    /// The pipeline for one block is:
    ///
    ///   1. Read the source mesh and walk the structured wall grid to get the exact position of
    ///      every boundary node on every layer.               (SweepMeshData)
    ///   2. Fill the interior of every layer by fitting an affine transform to the layer boundary
    ///      and extending the leftover boundary residual harmonically into the interior.
    ///                                                        (SweepLayerMorph, SweepLaplacian)
    ///   3. Project the target layer onto the target surfaces and spread the correction back along
    ///      the sweep lines so that no single element layer absorbs it.   (SweepProjector)
    ///   4. Relax the generated nodes, rejecting any move that would degrade an element.
    ///                                                        (SweepSmoother, SweepQuality)
    ///   5. Emit nodes and elements with a verified orientation.          (LayeredMeshBuilder)
    ///
    /// Everything below the top level entry point works on a SweepBlock, so a many-to-one sweep
    /// only has to supply an ordered list of blocks instead of a single one.
    /// </summary>
    public static class SweepMethods
    {
        // Variables
        private const int _maxSolverIterations = 500;
        private const double _solverTolerance = 1E-8;
        private const double _relaxation = 0.5;


        // Methods
        public static void CreateSweepMesh(HashSet<int> sourceSurfaceIds, HashSet<int> sideSurfaceIds,
                                           HashSet<int> targetSurfaceIds, int numGlobalSmoothSteps,
                                           Dictionary<int, int[]> surfaceIdEdgeIds, int volumeId,
                                           Action<string> writeOutput)
        {
            SweepBlock block = new SweepBlock(sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds);
            block.VolumeId = volumeId;
            CreateSweepMesh(new SweepBlock[] { block }, numGlobalSmoothSteps, surfaceIdEdgeIds, writeOutput);
        }
        /// <summary>
        /// Sweeps an ordered list of blocks. A one-to-one sweep is the single block case; a
        /// many-to-one sweep passes the blocks in the order they stack along the sweep direction.
        /// </summary>
        public static void CreateSweepMesh(IList<SweepBlock> blocks, int numGlobalSmoothSteps,
                                           Dictionary<int, int[]> surfaceIdEdgeIds, Action<string> writeOutput)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks.Count > 1) writeOutput?.Invoke("Sweep block " + (i + 1) + " of " + blocks.Count + ".");
                CreateBlockMesh(blocks[i], numGlobalSmoothSteps, surfaceIdEdgeIds, writeOutput);
            }
        }
        private static void CreateBlockMesh(SweepBlock block, int numGlobalSmoothSteps,
                                            Dictionary<int, int[]> surfaceIdEdgeIds, Action<string> writeOutput)
        {
            // Source mesh, wall grid and the sweep line table
            SweepMeshData data = SweepMeshData.Create(block, surfaceIdEdgeIds);
            writeOutput?.Invoke("Sweep lines: " + data.NumNodes + ", element layers: " + (data.NumLayers - 1) + ".");
            // Interior of every layer
            SweepLaplacian laplacian = SweepLaplacian.Create(data);
            SweepLayerMorph.MorphLayers(data, laplacian, _maxSolverIterations, _solverTolerance);
            // Target layer onto the geometry, correction spread back along the sweep lines
            TargetProjection projection = SweepProjector.ProjectTargetLayer(data, block.TargetSurfaceIds);
            double edgeLength = data.GetAverageSourceEdgeLength();
            if (edgeLength > 0)
            {
                writeOutput?.Invoke("Largest target surface correction: " +
                                    Math.Round(projection.MaxDistance / edgeLength, 3) +
                                    " average source element edges.");
            }
            // Orientation is measured, not assumed
            bool flip = SweepQuality.ComputeOrientationFlip(data);
            // Optional relaxation. The morph already leaves the layer interiors smooth, so this only
            // has something to add where the target surface is strongly inclined or curved and the
            // orthogonal projection bunched the target layer up; it defaults to off.
            SweepSmoother.Smooth(data, projection, flip, numGlobalSmoothSteps, _relaxation);
            // Report before emitting anything
            double minimum;
            double maximum;
            int numInverted;
            SweepQuality.GetQualityRange(data, flip, out minimum, out maximum, out numInverted);
            writeOutput?.Invoke("Scaled Jacobian: min " + Math.Round(minimum, 4) + ", max " + Math.Round(maximum, 4) +
                                ".");
            if (numInverted > 0)
            {
                writeOutput?.Invoke("Inverted elements: " + numInverted + ".");
                throw new CaeException("The sweep produced " + numInverted + " inverted element/s. Try a coarser or " +
                                       "more regular source surface mesh, fewer element layers, or split the volume " +
                                       "so that each part is swept separately.");
            }
            //
            LayeredMeshBuilder.Build(data, block, projection, flip);
        }
    }
}
