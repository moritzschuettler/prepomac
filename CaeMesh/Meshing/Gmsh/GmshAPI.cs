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
using CaeMesh.Sweep;
using GmshCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaeMesh
{
    public class GmshAPI
    {
        // Variables                                                                                                                
        private GmshData _gmshData;
        private Action<string> _writeOutput;
        private List<SweepBlock> _sweepBlocks;
        private HashSet<int> _freeMeshedSurfaceIds = new HashSet<int>();
        private HashSet<int> _scaffoldEdgeIds;
        private bool _isOCC;
        private string _error;
        private Thread _thread;
        private int _currentLogLine;
        private int _parameterizationULineCount;
        private int _parameterizationVLineCount;
        private const int _parameterizationLinePointCount = 11;
        private const int _parameterizationLineSubdivisionCount = 2;
        private const int _numStraightEdgeSamples = 9;
        private const int _numSurfaceParameterSamples = 16;


        // Properties                                                                                                               
        public GmshData GmshData { get { return _gmshData; } }
        public GmshAPI(GmshData gmshData, Action<string> writeOutput)
        {
            _gmshData = gmshData;
            _writeOutput = writeOutput;
            //
            if (_gmshData.MeshFileName != null) _isOCC = false;
            if (_gmshData.GeometryFileName != null)
            {
                if (_gmshData.GeometryFileName.EndsWith("brep")) _isOCC = true;
                else if (_gmshData.GeometryFileName.EndsWith("stl")) _isOCC = false;
                else throw new NotSupportedException();
            }
        }
        public string CreateMesh()
        {
            return RunInBackground(CreateMeshBackground);
        }
        public string GetOccNormals()
        {
            return RunInBackground(GetOccNormalsBackground);
        }
        public string GetElementQualities()
        {
            return RunInBackground(GetElementQualitiesBackground);
        }
        public string Defeature()
        {
            return RunInBackground(DefeatureBackground);
        }
        public string GetCoordinatesFromParameterization()
        {
            return GetCoordinatesFromParameterization(3, 3);
        }
        public string GetCoordinatesFromParameterization(int uLineCount, int vLineCount)
        {
            if (uLineCount < 1) throw new ArgumentOutOfRangeException("uLineCount");
            if (vLineCount < 1) throw new ArgumentOutOfRangeException("vLineCount");
            _parameterizationULineCount = uLineCount;
            _parameterizationVLineCount = vLineCount;
            //
            return RunInBackground(GetCoordinatesFromParameterizationBackground);
        }
        //
        private string RunInBackground(Action action)
        {
            try
            {
                Gmsh.Initialize();
                Gmsh.Model.Add("Model-1");
                Gmsh.Logger.Start();
                _currentLogLine = 0;
                // Common options
                Gmsh.Option.SetNumber("Geometry.OCCScaling", 1);
                //
                _thread = new Thread(new ThreadStart(() => action()));
                _thread.Start();
                //
                int count = 0;
                while (_thread.ThreadState == System.Threading.ThreadState.Running)
                {
                    if (count++ % 10 == 0) WriteLog();
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
                // Wait for the worker thread to finish
                _thread.Join();
                //
                WriteLog();
                //
                return _error;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _writeOutput?.Invoke(_error);
                _thread?.Abort();
                //
                return _error;
            }
            finally
            {
                Gmsh.Logger.Stop();
                Gmsh.FinalizeAll();
                // Check if the thread was not aborted on error
                if (_error != null && _gmshData.InpFileName != null && File.Exists(_gmshData.InpFileName))
                    File.Delete(_gmshData.InpFileName);
            }
        }
        // Meshing                                                                                                                  
        private void CreateMeshBackground()
        {
            try
            {
                if (_gmshData.GmshSetupItems.Length != 1)
                    throw new CaeException("Currently, for a single part, only one active mesh setup item of the type: " +
                        "Shell gmsh, Tetrahedral gmsh, Transfinite mesh, Extrude mesh, Sweep mesh or Revolve mesh is possible.");
                //
                MeshSetupItem meshSetupItem = _gmshData.GmshSetupItems[0];
                //
                if (meshSetupItem is GmshSetupItem gsi)
                {
                    Tuple<int, int>[] outDimTags;
                    if (_isOCC) Gmsh.Model.OCC.ImportShapes(_gmshData.GeometryFileName, out outDimTags, false, "");
                    else
                    {   // Discrete stl geometry
                        double angleDeg = _gmshData.StlFeatureAngleDeg;
                        if (angleDeg < 0) angleDeg = 0;
                        double angleRad = angleDeg * Math.PI / 180;
                        Gmsh.Merge(_gmshData.GeometryFileName);
                        Gmsh.Model.Mesh.RemoveDuplicateNodes();
                        Gmsh.Model.Mesh.ClassifySurfaces(angleRad, true, true, angleRad, true);
                        Gmsh.Model.Mesh.CreateGeometry();
                        Gmsh.Model.GetEntities(out outDimTags, 2);
                        int[] surfaceIds = new int[outDimTags.Length];
                        for (int i = 0; i < outDimTags.Length; i++) surfaceIds[i] = outDimTags[i].Item2;
                        int volumeId = Gmsh.Model.Geo.AddSurfaceLoop(surfaceIds);
                        Gmsh.Model.Geo.AddVolume(new int[] { volumeId });
                    }
                    //
                    Synchronize(); // must be here
                    // Renumber Gmsh items
                    RenumberGmshDataByCoor();
                    // PrepareData
                    PrepareData(gsi);
                    if (gsi is SweepMesh sweepSetupItem) PrepareSweepBlocks(sweepSetupItem);
                    // Mesh size
                    SetMeshSizes(gsi);
                    // 2D meshing algorithm
                    Gmsh.Option.SetNumber("Mesh.Algorithm", (int)gsi.AlgorithmMesh2D);
                    // 3D meshing algorithm
                    Gmsh.Option.SetNumber("Mesh.Algorithm3D", (int)gsi.AlgorithmMesh3D);
                    // Recombine algorithm
                    bool recombine = gsi.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
                    if (recombine)
                    {
                        Gmsh.Option.SetNumber("Mesh.RecombinationAlgorithm", (int)gsi.AlgorithmRecombine);
                        Gmsh.Option.SetNumber("Mesh.RecombineMinimumQuality", gsi.RecombineMinQuality);
                    }
                    // Transfinite
                    bool transfiniteVolume = gsi is TransfiniteMesh && gsi.TransfiniteThreeSided && gsi.TransfiniteFourSided;
                    bool sweepMesh = _gmshData.EdgeIdsBySweepLayer != null;
                    if (_isOCC && (gsi.TransfiniteThreeSided || gsi.TransfiniteFourSided || sweepMesh))
                        //Gmsh.Mesh.SetTransfiniteAutomatic(gsi.TransfiniteAngleRad, recombine);
                        SetTransfiniteSurfaces(gsi.TransfiniteThreeSided, gsi.TransfiniteFourSided, transfiniteVolume,
                                               recombine, gsi.AlgorithmRecombine);
                    // Optimization On/Off
                    int optimize = 1;
                    if (gsi.OptimizeFirstOrderShell == GmshOptimizeFirstOrderShellEnum.None &&
                        gsi.OptimizeFirstOrderSolid == GmshOptimizeFirstOrderSolidEnum.None &&
                        gsi.OptimizeHighOrder == GmshOptimizeHighOrderEnum.None) optimize = 0;  
                    Gmsh.Option.SetNumber("Mesh.Optimize", optimize);
                    //
                    if (gsi is ShellGmsh)
                        ShellGmsh(gsi, _gmshData.Preview);
                    else if (gsi is TetrahedralGmsh)
                        TetrahedralGmsh(gsi, _gmshData.Preview);
                    else if (gsi is TransfiniteMesh)
                        TransfiniteMesh(_gmshData.Preview);
                    else if (gsi is ExtrudeMesh em)
                        ExtrudeMesh(em, _gmshData.PartMeshingParameters, _gmshData.Preview);
                    else if (gsi is RevolveMesh rm)
                        RevolveMesh(rm, _gmshData.PartMeshingParameters, _gmshData.Preview);
                    else if (gsi is SweepMesh sm)
                        SweepMesh(sm, _gmshData.PartMeshingParameters, _gmshData.Preview);
                    else throw new NotSupportedException("MeshSetupItemTypeException");
                }
                else throw new NotSupportedException("MeshSetupItemTypeException");
                // Element order
                if (!_gmshData.Preview && _gmshData.PartMeshingParameters.SecondOrder)
                {
                    if (!_gmshData.PartMeshingParameters.MidsideNodesOnGeometry) 
                        Gmsh.Option.SetNumber("Mesh.SecondOrderLinear", 1);     // first
                    // Create incomplete second order elements: 8-node quads, 20-node hexas, etc.
                    Gmsh.Option.SetNumber("Mesh.SecondOrderIncomplete", 1);     // second
                    Gmsh.Model.Mesh.SetOrder(2);                                // third
                    // Optimize high order
                    if (gsi.OptimizeHighOrder != GmshOptimizeHighOrderEnum.None)
                    {
                        Tuple<int, int>[] dimTags = new Tuple<int, int>[0];
                        Gmsh.Model.Mesh.Optimize(gsi.OptimizeHighOrder.ToString(), false, 1, dimTags);
                    }
                }
                // Revolving a surface that touches the axis can create duplicate nodes on collapsed edges.
                // Merge them after all mesh operations and before exporting, so visualization topology sees merged nodes.
                if (!_gmshData.Preview && gsi is RevolveMesh) Gmsh.Model.Mesh.RemoveDuplicateNodes();
                // Quality of the mesh that is about to be written, for every setup item type
                if (!_gmshData.Preview) ReportMeshQuality();
                // Output
                Gmsh.Write(_gmshData.InpFileName);
                //Gmsh.Write(@"c:\Temp\mesh.mesh");
                //
                _writeOutput?.Invoke("Meshing done.");
                _writeOutput?.Invoke("");
                double elapsedTime = Gmsh.Option.GetNumber("Mesh.CpuTime");
                _writeOutput?.Invoke("Elapsed time [s]: " + Math.Round(elapsedTime, 5));
                _writeOutput?.Invoke("");
                //
                _error = null;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }
        /// <summary>
        /// How much coarser than asked for the mesh is made, the quasi-structured quad algorithm subdividing
        /// what it meshes.
        /// </summary>
        private static double GetMeshSizeFactor(GmshAlgorithmMesh2DEnum algorithm2D)
        {
            return algorithm2D == GmshAlgorithmMesh2DEnum.QuasiStructuredQuad ? 2 : 1;
        }
        /// <summary>
        /// The global sizes, twice as coarse for the quasi-structured quad algorithm, which subdivides what
        /// it meshes. Set again whenever the algorithm changes, or the mesh stays as coarse as the algorithm
        /// that is no longer running asked for.
        /// </summary>
        private void SetGlobalMeshSizes(GmshAlgorithmMesh2DEnum algorithm2D)
        {
            double sf = GetMeshSizeFactor(algorithm2D);
            Gmsh.Option.SetNumber("Mesh.MeshSizeMin", _gmshData.PartMeshingParameters.MinH * sf);
            Gmsh.Option.SetNumber("Mesh.MeshSizeMax", _gmshData.PartMeshingParameters.MaxH * sf);
            Gmsh.Option.SetNumber("Mesh.MeshSizeFromCurvature",
                                  2 * Math.PI * _gmshData.PartMeshingParameters.ElementsPerCurve / sf);
        }
        private void SetMeshSizes(GmshSetupItem gmshSetupItem)
        {
            GmshAlgorithmMesh2DEnum algorithm2D = gmshSetupItem.AlgorithmMesh2D;
            // Mesh size
            //Tuple<int, int>[] surfaceDimTags;
            //Gmsh.GetEntities(out surfaceDimTags, 2);
            //foreach (var surfaceDimTag in surfaceDimTags) Gmsh.Mesh.SetSizeFromBoundary(2, surfaceDimTag.Item2, 0);
            //
            double sf = GetMeshSizeFactor(algorithm2D);
            SetGlobalMeshSizes(algorithm2D);
            // Local vertex mesh size
            Tuple<int, int>[] dimTags = new Tuple<int, int>[1];
            foreach (var entry in _gmshData.VertexNodeIdMeshSize)
            {
                dimTags[0] = new Tuple<int, int>(0, entry.Key);
                Gmsh.Model.OCC.SetSize(dimTags, entry.Value);
            }
            // Local edge mesh size
            int edgeId;
            int numOfElements;
            // Get all edges
            Gmsh.Model.GetEntities(out dimTags, 1);
            // Check all edges
            Dictionary<int, int> edgeIdNumberOfElements = new Dictionary<int, int>();
            foreach (var entry in dimTags)
            {
                edgeId = entry.Item2;
                //
                if (_gmshData.EdgeIdNumElements.TryGetValue(edgeId, out numOfElements))
                {
                    numOfElements = (int)(numOfElements / sf);
                    edgeIdNumberOfElements[edgeId] = numOfElements;
                    // Sweep edge sizes
                    if (_gmshData.EdgeIdsBySweepLayer != null)
                    {
                        foreach (var edgeIds in _gmshData.EdgeIdsBySweepLayer)
                        {
                            if (edgeIds.Contains(edgeId))
                            {
                                foreach (var sweepEdgeId in edgeIds)
                                {
                                    edgeIdNumberOfElements[sweepEdgeId] = numOfElements;
                                }
                            }
                        }
                    }
                }
            }
            // Overwrite all edge sizes
            int numOfNodes;
            foreach (var entry in edgeIdNumberOfElements)
            {
                edgeId = entry.Key;
                numOfElements = entry.Value;
                //
                _gmshData.EdgeIdNumElements[edgeId] = numOfElements;
                //
                numOfNodes = numOfElements + 1;
                Gmsh.Model.Mesh.SetTransfiniteCurve(edgeId, numOfNodes);
            }
            //
            Synchronize(); // must be here for mesh refinement
        }
        //
        private void ShellGmsh(GmshSetupItem gmshSetupItem, bool preview)
        {
            bool recombine = gmshSetupItem.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            // Recombine all
            if (recombine) Gmsh.Option.SetNumber("Mesh.RecombineAll", 1);
            //
            if (preview) GenerateMesh(2);
            else GenerateMesh(2);
            // Optimize first order
            if (gmshSetupItem.OptimizeFirstOrderShell != GmshOptimizeFirstOrderShellEnum.None)
            {
                Tuple<int, int>[] dimTags = new Tuple<int, int>[0];
                Gmsh.Model.Mesh.Optimize(gmshSetupItem.OptimizeFirstOrderShell.ToString(), false, 10, dimTags);
            }
        }
        private void TetrahedralGmsh(GmshSetupItem gmshSetupItem, bool preview)
        {
            bool recombine = gmshSetupItem.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            // Recombine all
            if (recombine) Gmsh.Option.SetNumber("Mesh.RecombineAll", 1);
            //
            if (preview) GenerateMesh(1);
            else GenerateMesh(3);
            // Optimize first order
            if (gmshSetupItem.OptimizeFirstOrderSolid != GmshOptimizeFirstOrderSolidEnum.None)
            {
                Tuple<int, int>[] dimTags = new Tuple<int, int>[0];
                Gmsh.Model.Mesh.Optimize(gmshSetupItem.OptimizeFirstOrderSolid.ToString(), false, 10, dimTags);
            }
        }
        private void TransfiniteMesh(bool preview)
        {
            if (preview) GenerateMesh(1);
            else GenerateMesh(3);
        }
        private void ExtrudeMesh(ExtrudeMesh extrudeMesh, MeshingParameters meshingParameters, bool preview)
        {
            if (extrudeMesh == null) throw new NotSupportedException();
            if (extrudeMesh.ExtrudeCenter == null || extrudeMesh.Direction == null)
                throw new CaeException("The extrude direction could not be determined.");
            //
            int surfaceId;
            HashSet<int> sourceSurfaceIds = new HashSet<int>();
            bool recombine = extrudeMesh.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            Tuple<int, int>[] extrudeDimTags = new Tuple<int, int>[extrudeMesh.CreationIds.Length];
            // Collect surfaces
            for (int i = 0; i < extrudeMesh.CreationIds.Length; i++)
            {
                surfaceId = FeMesh.GetItemIdFromGeometryId(extrudeMesh.CreationIds[i]) + 1;
                extrudeDimTags[i] = new Tuple<int, int>(2, surfaceId);
                sourceSurfaceIds.Add(surfaceId);
                if (recombine) Gmsh.Model.Mesh.SetRecombine(2, surfaceId);
            }
            // Layers - size
            int[] numElements;
            double[] height;
            double edgeLength = Math.Sqrt(Math.Pow(extrudeMesh.Direction[0], 2) +
                                          Math.Pow(extrudeMesh.Direction[1], 2) +
                                          Math.Pow(extrudeMesh.Direction[2], 2));
            GetExtrudeRevolveLayers(extrudeMesh, meshingParameters, edgeLength, out numElements, out height);
            //
            if (preview)
            {
                PreviewExtrudeRevolveMesh(sourceSurfaceIds);
            }
            else
            {
                Tuple<int, int>[] outDimTags;
                Gmsh.Model.OCC.Extrude(extrudeDimTags,
                                       extrudeMesh.Direction[0],
                                       extrudeMesh.Direction[1],
                                       extrudeMesh.Direction[2],
                                       out outDimTags, numElements, height, true);
                // Volume check
                if (CheckMeshVolume(outDimTags))
                {
                    Gmsh.Model.OCC.Remove(new Tuple<int, int>[] { new Tuple<int, int>(3, 1) }, true);
                    Gmsh.Model.OCC.Synchronize();
                    GenerateMesh(3);
                }
                else
                {
                    throw new CaeException("The volume of the extruded mesh is not equal to the volume " +
                        "of the geometry it represents. Try selecting other surfaces for the extrusion.");
                }
            }
        }
        private void RevolveMesh(RevolveMesh revolveMesh, MeshingParameters meshingParameters, bool preview)
        {
            if (revolveMesh == null) throw new NotSupportedException();
            if (revolveMesh.AxisCenter == null || revolveMesh.AxisDirection == null)
                throw new CaeException("The revolve direction could not be determined.");
            //
            int surfaceId;
            HashSet<int> sourceSurfaceIds = new HashSet<int>();
            bool recombine = revolveMesh.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            for (int i = 0; i < revolveMesh.CreationIds.Length; i++)
            {
                surfaceId = FeMesh.GetItemIdFromGeometryId(revolveMesh.CreationIds[i]) + 1;
                sourceSurfaceIds.Add(surfaceId);
                if (recombine) Gmsh.Model.Mesh.SetRecombine(2, surfaceId);
            }
            // Layers - the cumulative heights are converted to angular positions by RevolveMethods.
            int[] numElements;
            double[] height;
            double angleRad = revolveMesh.AngleDeg * Math.PI / 180;
            double edgeLength = revolveMesh.MiddleR * angleRad;
            GetExtrudeRevolveLayers(revolveMesh, meshingParameters, edgeLength, out numElements, out height);
            //
            if (preview)
            {
                PreviewExtrudeRevolveMesh(sourceSurfaceIds);
            }
            else
            {
                // Only the source faces are meshed. The solid, target and side meshes are built analytically from
                // the source-surface mesh, so the sizing of the other faces and the volume is irrelevant.
                // RevolveMethods captures the topology, removes the volume and the non-source faces, and then calls
                // back to mesh the source faces in isolation (a neighbour-aware 2D algorithm such as the
                // Quasi-structured quad meshes a face differently when its neighbours are still present).
                Action meshSourceSurfaces = () =>
                {
                    GenerateMesh(2);
                    // Optimize first order shell
                    if (revolveMesh.OptimizeFirstOrderShell != GmshOptimizeFirstOrderShellEnum.None)
                    {
                        Tuple<int, int>[] dimTags = new Tuple<int, int>[0];
                        Gmsh.Model.Mesh.Optimize(revolveMesh.OptimizeFirstOrderShell.ToString(), false, 10, dimTags);
                    }
                };
                RevolveMethods.CreateRevolveMesh(sourceSurfaceIds, revolveMesh.AxisCenter,
                                                  revolveMesh.AxisDirection, angleRad,
                                                  numElements, height, meshSourceSurfaces);
            }
        }
        private static void GetExtrudeRevolveLayers(GmshSetupItem gmshSetupItem,
                                                    MeshingParameters meshingParameters,
                                                    double edgeLength, out int[] numElements,
                                                    out double[] height)
        {
            int numEl;
            if (gmshSetupItem.ElementSizeType == ElementSizeTypeEnum.ScaleFactor)
            {
                numEl = (int)Math.Round(edgeLength / meshingParameters.MaxH /
                                        gmshSetupItem.ElementScaleFactor, 0);
                if (numEl < 1) numEl = 1;
                numElements = new int[] { numEl };
                height = new double[] { 1 };
            }
            else if (gmshSetupItem.ElementSizeType == ElementSizeTypeEnum.NumberOfElements)
            {
                numEl = gmshSetupItem.NumberOfElements;
                if (numEl < 1) numEl = 1;
                numElements = new int[] { numEl };
                height = new double[] { 1 };
            }
            else if (gmshSetupItem.ElementSizeType == ElementSizeTypeEnum.MultiLayerd)
            {
                if (gmshSetupItem.LayerSizes == null || gmshSetupItem.NumOfElementsPerLayer == null ||
                    gmshSetupItem.LayerSizes.Length == 0 ||
                    gmshSetupItem.LayerSizes.Length != gmshSetupItem.NumOfElementsPerLayer.Length)
                    throw new CaeException("The mesh layers are not properly defined.");
                numElements = gmshSetupItem.NumOfElementsPerLayer.ToArray();
                double sumSizes = gmshSetupItem.LayerSizes.Sum();
                if (sumSizes <= 0) throw new CaeException("The sum of the mesh layer thicknesses must be positive.");
                double sumHeight = 0;
                height = new double[gmshSetupItem.LayerSizes.Length];
                for (int i = 0; i < height.Length; i++)
                {
                    sumHeight += gmshSetupItem.LayerSizes[i] / sumSizes;
                    height[i] = sumHeight;
                }
                height[height.Length - 1] = 1;
            }
            else throw new NotSupportedException("ExtrudedElementSizeTypeEnumException");
        }
        private void PreviewExtrudeRevolveMesh(HashSet<int> sourceSurfaceIds)
        {
            Tuple<int, int>[] allDimTags = Gmsh.Model.OCC.GetEntities(2);
            List<Tuple<int, int>> toRemoveDimTags = new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(3, 1)
            };
            for (int i = 0; i < allDimTags.Length; i++)
            {
                if (!sourceSurfaceIds.Contains(allDimTags[i].Item2)) toRemoveDimTags.Add(allDimTags[i]);
            }
            Gmsh.Model.OCC.Remove(toRemoveDimTags.ToArray(), false);
            Gmsh.Model.OCC.Synchronize();
            GenerateMesh(2);
        }
        private void SweepMesh(SweepMesh sweepMesh, MeshingParameters meshingParameters, bool preview)
        {
            if (sweepMesh == null) throw new NotSupportedException();
            //
            bool recombine = sweepMesh.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            // Get surface item ids
            Dictionary<int, int[]> surfaceIdEdgeIds;
            Dictionary<int, int[]> surfaceIdVertexIds;
            GetSurfaceItems(out surfaceIdEdgeIds, out surfaceIdVertexIds);
            // One block per swept volume, in the order in which the blocks feed each other
            List<SweepBlock> blocks = _sweepBlocks ?? GetSweepBlocks(sweepMesh, surfaceIdVertexIds.Keys);
            //
            List<SweepWall> walls = new List<SweepWall>();
            List<SweepChain> chains = new List<SweepChain>();
            List<int> chainBlockIndices = new List<int>();
            HashSet<int> wallSurfaceIds = new HashSet<int>();
            HashSet<int> targetSurfaceIds = new HashSet<int>();
            foreach (var block in blocks)
            {
                // Set source surfaces
                if (recombine)
                {
                    foreach (var sourceSurfaceId in block.SourceSurfaceIds) Gmsh.Model.Mesh.SetRecombine(2, sourceSurfaceId);
                }
                // Set side surfaces. A wall whose side is built from several edges - which is what a
                // split source face produces - needs its four corners nominated by hand, and needs the
                // element counts on its opposite sides reconciled first. A wall two blocks share is one
                // wall, with the same edges in the same roles seen from either side.
                List<SweepChain> blockChains;
                HashSet<int> directionEdgeIds = GetBlockDirectionEdgeIds(block, surfaceIdEdgeIds, out blockChains);
                chains.AddRange(blockChains);
                for (int i = 0; i < blockChains.Count; i++) chainBlockIndices.Add(blocks.IndexOf(block));
                foreach (var wall in SweepWallTopology.Classify(block.SourceSurfaceIds, block.WallSurfaceIds,
                                                                block.TargetSurfaceIds, surfaceIdEdgeIds, directionEdgeIds))
                {
                    if (wallSurfaceIds.Add(wall.SurfaceId)) walls.Add(wall);
                }
                targetSurfaceIds.UnionWith(block.TargetSurfaceIds);
            }
            // Whether the walls came out of the 2D pass as the structured grid the sweep needs shows only
            // when that grid is walked, so the whole pass is one attempt that either builds the sweep or
            // fails. The counts the item itself carries are kept, since a second attempt has to resolve its
            // own and not read back what the first one wrote
            Dictionary<int, int> pinnedEdgeIdNumElements = new Dictionary<int, int>(_gmshData.EdgeIdNumElements);
            try
            {
                MeshSweepBlocks(sweepMesh, blocks, walls, chains, targetSurfaceIds, surfaceIdEdgeIds,
                                chainBlockIndices, preview, true);
            }
            catch (CaeException)
            {
                // The quasi-structured quad algorithm meshes the curves of the surfaces it meshes again, with
                // sizes of its own and regardless of the counts pinned on them, so a wall's source side may no
                // longer match its target side, and one column of the wall grid may come out with fewer layers
                // than the next. Measured: neither meshing the sources first nor rebuilding the walls to its
                // counts survives its final pass over every quad surface, so the source is meshed with the
                // Frontal-Delaunay algorithm and recombined instead
                if (sweepMesh.AlgorithmMesh2D != GmshAlgorithmMesh2DEnum.QuasiStructuredQuad) throw;
                _writeOutput?.Invoke("Sweep: the side surfaces could not be meshed with the structured grid the sweep needs " +
                                     "under the quasi-structured quad algorithm. The source surfaces are meshed with the " +
                                     "Frontal-Delaunay algorithm and recombined instead.");
                Gmsh.Option.SetNumber("Mesh.Algorithm", (int)GmshAlgorithmMesh2DEnum.FrontalDelaunay);
                SetGlobalMeshSizes(GmshAlgorithmMesh2DEnum.FrontalDelaunay);
                foreach (var block in blocks)
                {
                    foreach (var sourceSurfaceId in block.SourceSurfaceIds) Gmsh.Model.Mesh.SetRecombine(2, sourceSurfaceId);
                }
                // The second attempt measures the edges as the other algorithm meshes them. The counts of the
                // first attempt would reach it twice - through the table it wrote and through the transfinite
                // constraints it set, which a fresh 1D mesh would simply reproduce - so both are taken back and
                // the sweep is prepared the way it would have been had this algorithm been chosen from the start
                _gmshData.EdgeIdNumElements.Clear();
                foreach (var entry in pinnedEdgeIdNumElements) _gmshData.EdgeIdNumElements.Add(entry.Key, entry.Value);
                RemoveWallEdgeConstraints(walls);
                MeshSweepBlocks(sweepMesh, blocks, walls, chains, targetSurfaceIds, surfaceIdEdgeIds,
                                chainBlockIndices, preview, false);
            }
            // The edges made by carrying the splits across are scaffolding: their nodes stay, being nodes of
            // the surface meshes, but their line elements would become model edges splitting the faces once
            // the mesh is read - which is what the item asks for when it keeps them
            if (_scaffoldEdgeIds != null && !sweepMesh.KeepImprintEdges)
            {
                foreach (var edgeId in _scaffoldEdgeIds) Gmsh.Model.Mesh.RemoveElements(1, edgeId);
            }
        }
        /// <summary>
        /// Builds the blocks of a sweep item before the mesh sizes are set, and carries the imprints that split
        /// an end of a prismatic block inside its boundary across to its other end, since that changes the
        /// geometry and the sizes and the transfinite constraints have to see the result.
        ///
        /// A target split inside its boundary - by the footprint of a neighbour standing on it - has edges
        /// that belong to no wall of the block. The cap the sweep leaves would not follow them, and the
        /// neighbour's walls start from their nodes, so the mesh would not be conformal there. For a prism the
        /// split is carried across: every edge splitting the target inside is copied and moved onto the
        /// source, every edge splitting the source inside is copied and moved onto the target, and all the
        /// volumes are fragmented with the copies, so that they become real edges, cross where two footprints
        /// overlap and end on vertices where they meet a boundary. A carried edge landing on a face that a
        /// neighbouring prism shares splits that prism's end too, so the rounds repeat until no block has an
        /// edge without its counterpart on the other end. Source and target then carry the same split, piece
        /// for piece, and the sweep lines between a piece and the piece above it are pinned. The fragmentation
        /// renumbers what it touches, so every id held anywhere is matched to its new tag afterwards: the
        /// volumes, the vertices and the unsplit edges by their centre of mass and size, the faces by lying
        /// inside a copy of the face they came from, kept aside for that purpose. The edges the carrying made
        /// exist for the sweep only; their line elements are dropped once the mesh is built. A block that is
        /// not prismatic is left alone; the model side refuses it before it reaches here.
        /// </summary>
        private void PrepareSweepBlocks(SweepMesh sweepMesh)
        {
            Dictionary<int, int[]> surfaceIdEdgeIds;
            Dictionary<int, int[]> surfaceIdVertexIds;
            GetSurfaceItems(out surfaceIdEdgeIds, out surfaceIdVertexIds);
            _sweepBlocks = GetSweepBlocks(sweepMesh, surfaceIdVertexIds.Keys);
            // A target surface is meshed only to be thrown away, and the counts on its edges are set for the
            // walls, not for it: a structured grid on it would fail as often as not
            foreach (var block in _sweepBlocks) _freeMeshedSurfaceIds.UnionWith(block.TargetSurfaceIds);
            // The translation of every prism, and the faces every block started with
            Dictionary<SweepBlock, int[][]> blockOriginalSurfaceIds = new Dictionary<SweepBlock, int[][]>();
            foreach (var block in _sweepBlocks)
            {
                block.ImprintTranslation = GetPrismTranslation(block, surfaceIdEdgeIds);
                blockOriginalSurfaceIds.Add(block, new int[][] { block.SourceSurfaceIds.ToArray(),
                                                                block.WallSurfaceIds.ToArray(),
                                                                block.TargetSurfaceIds.ToArray() });
            }
            // Every face and every edge stands for itself until a fragmentation changes it
            Tuple<int, int>[] dimTags;
            Dictionary<int, int> surfaceIdOriginalId = new Dictionary<int, int>();
            Dictionary<int, int> edgeIdOriginalId = new Dictionary<int, int>();
            Gmsh.Model.GetEntities(out dimTags, 2);
            foreach (var dimTag in dimTags) surfaceIdOriginalId.Add(dimTag.Item2, dimTag.Item2);
            Gmsh.Model.GetEntities(out dimTags, 1);
            foreach (var dimTag in dimTags) edgeIdOriginalId.Add(dimTag.Item2, dimTag.Item2);
            //
            int round = 0;
            List<Tuple<int, int>> copyDimTags = new List<Tuple<int, int>>();
            while (true)
            {
                copyDimTags.Clear();
                foreach (var block in _sweepBlocks) CarryUnmatchedImprints(block, surfaceIdEdgeIds, copyDimTags);
                if (copyDimTags.Count == 0) break;
                if (++round > _sweepBlocks.Count + 1)
                    throw new CaeException("The splits of the sweep sources and targets could not be carried across the " +
                                           "volumes.");
                //
                FragmentVolumesWithCurves(copyDimTags, ref surfaceIdOriginalId, ref edgeIdOriginalId);
                GetSurfaceItems(out surfaceIdEdgeIds, out surfaceIdVertexIds);
                foreach (var block in _sweepBlocks)
                    RefreshBlockSurfaces(block, blockOriginalSurfaceIds[block], surfaceIdOriginalId);
            }
            // Also for a block that had nothing to carry: its two ends may already be split at matching
            // places, and the lines between them are pinned just the same
            foreach (var block in _sweepBlocks) PairImprintEdges(block, surfaceIdEdgeIds);
            if (round > 0) _scaffoldEdgeIds = GetScaffoldEdgeIds(surfaceIdOriginalId, edgeIdOriginalId);
        }
        private double[] GetPrismTranslation(SweepBlock block, Dictionary<int, int[]> surfaceIdEdgeIds)
        {
            int[] edgeIds;
            HashSet<int> sourceEdgeIds = new HashSet<int>();
            foreach (var id in block.SourceSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) sourceEdgeIds.UnionWith(edgeIds);
            }
            double[] translation;
            List<SweepChain> chains;
            List<SweepWall> walls = SweepWallTopology.Classify(block.SourceSurfaceIds, block.WallSurfaceIds,
                                                               block.TargetSurfaceIds, surfaceIdEdgeIds,
                                                               GetBlockDirectionEdgeIds(block, surfaceIdEdgeIds, out chains));
            return IsPrismaticBlock(block, walls, sourceEdgeIds, out translation) ? translation : null;
        }
        /// <summary>
        /// The edges of a block that follow the sweep direction, as the chains of edges running from its source
        /// along its walls, among the edges the model recorded as following the sweep direction. A block whose
        /// source no chain leaves - an item without recorded edges, say - gets null, and its walls are sorted
        /// by their source and target edges alone.
        /// </summary>
        private HashSet<int> GetBlockDirectionEdgeIds(SweepBlock block, Dictionary<int, int[]> surfaceIdEdgeIds,
                                                      out List<SweepChain> chains)
        {
            int[] edgeIds;
            HashSet<int> sourceEdgeIds = new HashSet<int>();
            HashSet<int> wallEdgeIds = new HashSet<int>();
            foreach (var id in block.SourceSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) sourceEdgeIds.UnionWith(edgeIds);
            }
            foreach (var id in block.WallSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) wallEdgeIds.UnionWith(edgeIds);
            }
            HashSet<int> allowedEdgeIds = null;
            if (_gmshData.EdgeIdsBySweepLayer != null)
            {
                allowedEdgeIds = new HashSet<int>();
                foreach (var ring in _gmshData.EdgeIdsBySweepLayer) allowedEdgeIds.UnionWith(ring);
            }
            chains = SweepWallTopology.FollowChains(sourceEdgeIds, wallEdgeIds, allowedEdgeIds);
            if (chains.Count == 0) return null;
            //
            HashSet<int> directionEdgeIds = new HashSet<int>();
            foreach (var chain in chains) directionEdgeIds.UnionWith(chain.EdgeIds);
            return directionEdgeIds;
        }
        /// <summary>
        /// The edges splitting the source and the target of a block inside their boundaries: the edges of the
        /// end faces that belong to no wall of the block.
        /// </summary>
        private static void GetInteriorEdgeIds(SweepBlock block, Dictionary<int, int[]> surfaceIdEdgeIds,
                                               out HashSet<int> sourceEdgeIds, out HashSet<int> targetEdgeIds)
        {
            int[] edgeIds;
            HashSet<int> wallEdgeIds = new HashSet<int>();
            sourceEdgeIds = new HashSet<int>();
            targetEdgeIds = new HashSet<int>();
            foreach (var id in block.WallSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) wallEdgeIds.UnionWith(edgeIds);
            }
            foreach (var id in block.SourceSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) sourceEdgeIds.UnionWith(edgeIds);
            }
            foreach (var id in block.TargetSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) targetEdgeIds.UnionWith(edgeIds);
            }
            sourceEdgeIds.ExceptWith(wallEdgeIds);
            targetEdgeIds.ExceptWith(wallEdgeIds);
        }
        private static Dictionary<int, double[]> GetEdgeSignatures(IEnumerable<int> edgeIds)
        {
            double x;
            double y;
            double z;
            double length;
            Dictionary<int, double[]> signatures = new Dictionary<int, double[]>();
            foreach (var edgeId in edgeIds)
            {
                Gmsh.Model.OCC.GetCenterOfMass(1, edgeId, out x, out y, out z);
                Gmsh.Model.OCC.GetMass(1, edgeId, out length);
                signatures.Add(edgeId, new double[] { x, y, z, length });
            }
            return signatures;
        }
        /// <summary>
        /// The edge among the candidates that the given edge lands on when moved by the translation, or -1.
        /// </summary>
        private static int FindTranslatedEdge(double[] signature, double[] translation,
                                              Dictionary<int, double[]> candidateSignatures, double tolerance)
        {
            double[] carried = new double[] { signature[0] + translation[0], signature[1] + translation[1],
                                              signature[2] + translation[2] };
            foreach (var entry in candidateSignatures)
            {
                if (GetDistance(carried, entry.Value) > tolerance) continue;
                if (Math.Abs(signature[3] - entry.Value[3]) > 1E-6 * Math.Max(signature[3], 1E-30)) continue;
                return entry.Key;
            }
            return -1;
        }
        /// <summary>
        /// Copies every edge splitting an end of a prismatic block inside that has no counterpart on the other
        /// end, and moves the copy onto that other end.
        /// </summary>
        private void CarryUnmatchedImprints(SweepBlock block, Dictionary<int, int[]> surfaceIdEdgeIds,
                                            List<Tuple<int, int>> copyDimTags)
        {
            if (block.ImprintTranslation == null) return;
            //
            HashSet<int> sourceEdgeIds;
            HashSet<int> targetEdgeIds;
            GetInteriorEdgeIds(block, surfaceIdEdgeIds, out sourceEdgeIds, out targetEdgeIds);
            if (sourceEdgeIds.Count == 0 && targetEdgeIds.Count == 0) return;
            //
            double[] translation = block.ImprintTranslation;
            double[] back = new double[] { -translation[0], -translation[1], -translation[2] };
            double tolerance = 1E-6 * GetModelDiagonal();
            Dictionary<int, double[]> sourceSignatures = GetEdgeSignatures(sourceEdgeIds);
            Dictionary<int, double[]> targetSignatures = GetEdgeSignatures(targetEdgeIds);
            List<Tuple<int, int>> toSourceDimTags = new List<Tuple<int, int>>();
            List<Tuple<int, int>> toTargetDimTags = new List<Tuple<int, int>>();
            foreach (var entry in targetSignatures)
            {
                if (FindTranslatedEdge(entry.Value, translation, sourceSignatures, tolerance) < 0)
                    toSourceDimTags.Add(new Tuple<int, int>(1, entry.Key));
            }
            foreach (var entry in sourceSignatures)
            {
                if (FindTranslatedEdge(entry.Value, back, targetSignatures, tolerance) < 0)
                    toTargetDimTags.Add(new Tuple<int, int>(1, entry.Key));
            }
            if (toSourceDimTags.Count == 0 && toTargetDimTags.Count == 0) return;
            //
            Tuple<int, int>[] outDimTags;
            if (toSourceDimTags.Count > 0)
            {
                Gmsh.Model.OCC.Copy(toSourceDimTags.ToArray(), out outDimTags);
                Gmsh.Model.OCC.Translate(outDimTags, translation[0], translation[1], translation[2]);
                copyDimTags.AddRange(outDimTags);
            }
            if (toTargetDimTags.Count > 0)
            {
                Gmsh.Model.OCC.Copy(toTargetDimTags.ToArray(), out outDimTags);
                Gmsh.Model.OCC.Translate(outDimTags, back[0], back[1], back[2]);
                copyDimTags.AddRange(outDimTags);
            }
            _writeOutput?.Invoke("Sweep volume " + block.VolumeId + ": " + toSourceDimTags.Count + " edge/s splitting the " +
                                 "target carried to the source, " + toTargetDimTags.Count + " edge/s splitting the source " +
                                 "carried to the target.");
        }
        /// <summary>
        /// Fragments every volume with the carried curves, so that they become real edges of the faces they lie
        /// on. Every volume goes in, so that a face shared by two blocks stays one face. The operation renumbers
        /// the entities it touches, and OpenCASCADE reports the new tags only for the inputs: the faces are
        /// matched by lying inside a copy of the face they came from, kept aside during the operation, and the
        /// volumes, the vertices and the unsplit edges by their centre of mass and size. The two maps are
        /// rewritten to lead from the new tags to the original ones; an edge the fragmentation made has no
        /// entry.
        /// </summary>
        private void FragmentVolumesWithCurves(List<Tuple<int, int>> curveDimTags, ref Dictionary<int, int> surfaceIdOriginalId,
                                               ref Dictionary<int, int> edgeIdOriginalId)
        {
            double diagonal = GetModelDiagonal();
            Dictionary<int, double[]> oldPointSignatures = GetEntitySignatures(0);
            Dictionary<int, double[]> oldEdgeSignatures = GetEntitySignatures(1);
            Dictionary<int, double[]> oldVolumeSignatures = GetEntitySignatures(3);
            // A copy of every face, kept out of the operation, tells afterwards which face a piece came from
            int[] entityCounts = GetEntityCounts();
            Tuple<int, int>[] surfaceDimTags;
            Tuple<int, int>[] surfaceCopyDimTags;
            Gmsh.Model.GetEntities(out surfaceDimTags, 2);
            Gmsh.Model.OCC.Copy(surfaceDimTags, out surfaceCopyDimTags);
            //
            Tuple<int, int>[] volumeDimTags;
            Tuple<int, int>[] outDimTags;
            Tuple<int, int>[][] outDimTagsMap;
            Gmsh.Model.GetEntities(out volumeDimTags, 3);
            Gmsh.Model.OCC.Fragment(volumeDimTags, curveDimTags.ToArray(), out outDimTags, out outDimTagsMap, true, true);
            Gmsh.Model.OCC.Synchronize();
            RemoveFreeCurves();
            //
            double[] closestCoord;
            double[] parametricCoord;
            double tolerance = 1E-6 * diagonal;
            HashSet<int> surfaceCopyIds = new HashSet<int>(surfaceCopyDimTags.Select(dimTag => dimTag.Item2));
            Dictionary<int, int> newSurfaceIdOriginalId = new Dictionary<int, int>();
            Gmsh.Model.GetEntities(out outDimTags, 2);
            foreach (var dimTag in outDimTags)
            {
                if (surfaceCopyIds.Contains(dimTag.Item2)) continue;
                //
                // The inside test projects the point onto the surface first, so the distance to it is checked too
                double[] inside = GetPointInsideSurface(dimTag.Item2);
                int oldSurfaceId = -1;
                for (int i = 0; i < surfaceCopyDimTags.Length; i++)
                {
                    Gmsh.Model.GetClosestPoint(2, surfaceCopyDimTags[i].Item2, inside, out closestCoord, out parametricCoord);
                    if (closestCoord.Length < 3 || GetDistance(inside, closestCoord) > tolerance) continue;
                    if (Gmsh.Model.IsInside(2, surfaceCopyDimTags[i].Item2, inside, false) > 0)
                    {
                        oldSurfaceId = surfaceDimTags[i].Item2;
                        break;
                    }
                }
                if (oldSurfaceId < 0)
                    throw new CaeException("A surface made by carrying the splits of the sweep ends across lies in no surface " +
                                           "that was there before.");
                newSurfaceIdOriginalId.Add(dimTag.Item2, surfaceIdOriginalId[oldSurfaceId]);
            }
            RemoveSurfaceCopies(surfaceCopyDimTags, entityCounts);
            surfaceIdOriginalId = newSurfaceIdOriginalId;
            //
            Dictionary<int, int> pointIdMap = MatchEntitiesByGeometry(0, oldPointSignatures, diagonal);
            Dictionary<int, int> edgeIdMap = MatchEntitiesByGeometry(1, oldEdgeSignatures, diagonal);
            Dictionary<int, int> volumeIdMap = MatchEntitiesByGeometry(3, oldVolumeSignatures, diagonal);
            //
            int originalId;
            Dictionary<int, int> newEdgeIdOriginalId = new Dictionary<int, int>();
            foreach (var entry in edgeIdMap)
            {
                if (edgeIdOriginalId.TryGetValue(entry.Key, out originalId)) newEdgeIdOriginalId[entry.Value] = originalId;
            }
            edgeIdOriginalId = newEdgeIdOriginalId;
            //
            int newVolumeId;
            foreach (var block in _sweepBlocks)
            {
                if (!volumeIdMap.TryGetValue(block.VolumeId, out newVolumeId))
                    throw new CaeException("A sweep volume was lost while the splits of the sweep ends were carried across.");
                block.VolumeId = newVolumeId;
            }
            RemapGmshData(edgeIdMap, pointIdMap);
        }
        /// <summary>
        /// Removes the surfaces kept aside, with their edges and vertices. OpenCASCADE warns about an unknown
        /// vertex for every one of them although they are all gone afterwards, so the warnings are kept out of
        /// the log, and the model is checked to hold as many entities as before the copies were made, the
        /// pieces of the split faces and edges apart.
        /// </summary>
        private static void RemoveSurfaceCopies(Tuple<int, int>[] surfaceDimTags, int[] entityCountsBeforeCopies)
        {
            double verbosity = Gmsh.Option.GetNumber("General.Verbosity");
            Gmsh.Option.SetNumber("General.Verbosity", 1);
            try
            {
                Gmsh.Model.OCC.Remove(surfaceDimTags, true);
                Gmsh.Model.OCC.Synchronize();
            }
            finally
            {
                Gmsh.Option.SetNumber("General.Verbosity", verbosity);
            }
            // A surface copy has no volume; its edges and vertices are counted with those the split made
            int[] entityCounts = GetEntityCounts();
            if (entityCounts[3] != entityCountsBeforeCopies[3] || entityCounts[2] < entityCountsBeforeCopies[2] ||
                entityCounts[2] >= entityCountsBeforeCopies[2] + surfaceDimTags.Length)
                throw new CaeException("The surfaces kept aside while the splits of the sweep ends were carried across " +
                                       "could not be removed again.");
        }
        private static int[] GetEntityCounts()
        {
            Tuple<int, int>[] dimTags;
            int[] counts = new int[4];
            for (int dim = 0; dim < 4; dim++)
            {
                Gmsh.Model.GetEntities(out dimTags, dim);
                counts[dim] = dimTags.Length;
            }
            return counts;
        }
        /// <summary>
        /// A piece of a carried curve that fell outside every face is of no use and would be meshed as a
        /// dangling edge.
        /// </summary>
        private static void RemoveFreeCurves()
        {
            int[] surfaceIds;
            Tuple<int, int>[] edgeDimTags;
            List<Tuple<int, int>> freeDimTags = new List<Tuple<int, int>>();
            Gmsh.Model.GetEntities(out edgeDimTags, 1);
            foreach (var dimTag in edgeDimTags)
            {
                Gmsh.Model.GetAdjacencies(1, dimTag.Item2, out surfaceIds, out _);
                if (surfaceIds.Length == 0) freeDimTags.Add(dimTag);
            }
            if (freeDimTags.Count == 0) return;
            Gmsh.Model.OCC.Remove(freeDimTags.ToArray(), true);
            Gmsh.Model.OCC.Synchronize();
        }
        /// <summary>
        /// A point strictly inside a surface and lying on it: its centre of mass where that serves, else a
        /// point of its parameter box the inside test accepts. The inside test alone does not say that a point
        /// lies on the surface, since it projects the point onto it first, and the centre of mass of a bore
        /// lies on the axis of the bore rather than on the bore.
        /// </summary>
        private static double[] GetPointInsideSurface(int surfaceId)
        {
            double x;
            double y;
            double z;
            Gmsh.Model.OCC.GetCenterOfMass(2, surfaceId, out x, out y, out z);
            double[] center = new double[] { x, y, z };
            if (IsPointOnSurface(surfaceId, center) && Gmsh.Model.IsInside(2, surfaceId, center, false) > 0) return center;
            //
            double[] coor;
            double[] minimum;
            double[] maximum;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out minimum, out maximum);
            if (minimum.Length >= 2 && maximum.Length >= 2)
            {
                double[] parametricCoor = new double[2];
                for (int i = 1; i < _numSurfaceParameterSamples; i++)
                {
                    for (int j = 1; j < _numSurfaceParameterSamples; j++)
                    {
                        parametricCoor[0] = minimum[0] + (maximum[0] - minimum[0]) * i / (double)_numSurfaceParameterSamples;
                        parametricCoor[1] = minimum[1] + (maximum[1] - minimum[1]) * j / (double)_numSurfaceParameterSamples;
                        if (Gmsh.Model.IsInside(2, surfaceId, parametricCoor, true) <= 0) continue;
                        //
                        Gmsh.Model.GetValue(2, surfaceId, parametricCoor, out coor);
                        if (coor.Length >= 3) return coor;
                    }
                }
            }
            throw new CaeException("No point inside a surface of the sweep could be found.");
        }
        private static bool IsPointOnSurface(int surfaceId, double[] coor)
        {
            double[] closestCoor;
            Gmsh.Model.GetClosestPoint(2, surfaceId, coor, out closestCoor, out _);
            return closestCoor.Length >= 3 && GetDistance(coor, closestCoor) <= 1E-6 * GetModelDiagonal();
        }
        /// <summary>
        /// Reads the faces of a block back after a fragmentation: every current face that came from one of the
        /// faces the block started with.
        /// </summary>
        private static void RefreshBlockSurfaces(SweepBlock block, int[][] originalSurfaceIds,
                                                 Dictionary<int, int> surfaceIdOriginalId)
        {
            HashSet<int> originalSourceIds = new HashSet<int>(originalSurfaceIds[0]);
            HashSet<int> originalWallIds = new HashSet<int>(originalSurfaceIds[1]);
            HashSet<int> originalTargetIds = new HashSet<int>(originalSurfaceIds[2]);
            List<int> sourceSurfaceIds = new List<int>();
            List<int> wallSurfaceIds = new List<int>();
            List<int> targetSurfaceIds = new List<int>();
            foreach (var entry in surfaceIdOriginalId)
            {
                if (originalSourceIds.Contains(entry.Value)) sourceSurfaceIds.Add(entry.Key);
                else if (originalWallIds.Contains(entry.Value)) wallSurfaceIds.Add(entry.Key);
                else if (originalTargetIds.Contains(entry.Value)) targetSurfaceIds.Add(entry.Key);
            }
            block.SetSurfaces(sourceSurfaceIds, wallSurfaceIds, targetSurfaceIds);
        }
        /// <summary>
        /// Pairs every edge splitting the target of a prism inside with the edge of the source below it, which
        /// is what the sweep lines are pinned between. After the carrying every such edge has its counterpart;
        /// one that does not is an error. The source of a block that carries imprints is meshed free, since
        /// its pieces have their counts dictated.
        /// </summary>
        private void PairImprintEdges(SweepBlock block, Dictionary<int, int[]> surfaceIdEdgeIds)
        {
            HashSet<int> sourceEdgeIds;
            HashSet<int> targetEdgeIds;
            GetInteriorEdgeIds(block, surfaceIdEdgeIds, out sourceEdgeIds, out targetEdgeIds);
            if (targetEdgeIds.Count == 0) return;
            //
            int sourceEdgeId;
            double tolerance = 1E-6 * GetModelDiagonal();
            Dictionary<int, double[]> sourceSignatures = GetEdgeSignatures(sourceEdgeIds);
            Dictionary<int, double[]> targetSignatures = GetEdgeSignatures(targetEdgeIds);
            Dictionary<int, int> imprintEdgeCopyIds = new Dictionary<int, int>();
            if (block.ImprintTranslation != null)
            {
                foreach (var entry in targetSignatures)
                {
                    sourceEdgeId = FindTranslatedEdge(entry.Value, block.ImprintTranslation, sourceSignatures, tolerance);
                    if (sourceEdgeId < 0)
                        throw new CaeException("Sweep volume " + block.VolumeId + ": an edge splitting the target has no " +
                                               "counterpart on the source after the splits were carried across.");
                    imprintEdgeCopyIds.Add(entry.Key, sourceEdgeId);
                }
            }
            else
            {
                // A block that is not a prism has had nothing carried across, so it is pinned only where both
                // of its ends are already split at matching places - as the two tangency lines of a bent plate
                // split its front face and its back face alike. Every edge splitting the target then stands
                // above an edge splitting the source, one for one; where that does not hold the block sweeps
                // unpinned, as it did before, rather than failing
                if (sourceEdgeIds.Count != targetEdgeIds.Count) return;
                foreach (var entry in targetSignatures)
                {
                    sourceEdgeId = FindEdgeUnderTarget(entry.Key, entry.Value, sourceSignatures, tolerance);
                    if (sourceEdgeId < 0 || imprintEdgeCopyIds.ContainsValue(sourceEdgeId)) return;
                    imprintEdgeCopyIds.Add(entry.Key, sourceEdgeId);
                }
            }
            block.ImprintEdgeCopyIds = imprintEdgeCopyIds;
            _freeMeshedSurfaceIds.UnionWith(block.SourceSurfaceIds);
        }
        /// <summary>
        /// The edge splitting the source that the given edge splitting the target stands above, for a block
        /// that is not a prism and so carries no translation of its own: the nearest candidate the target edge
        /// lands on when it is moved by the vector between the two centres of mass, measured on samples of the
        /// curve and not on its centre and its length alone. A pair found this way is a translation of the
        /// other even where the block is not, which is all that pinning the lines between them needs.
        /// </summary>
        private static int FindEdgeUnderTarget(int targetEdgeId, double[] targetSignature,
                                               Dictionary<int, double[]> sourceSignatures, double tolerance)
        {
            double[] samples = GetCurveSamples(targetEdgeId);
            if (samples == null) return -1;
            //
            bool onEdge;
            double distance;
            double[] closestCoor;
            double[] parametricCoor;
            double[] carried = new double[samples.Length];
            int bestEdgeId = -1;
            double bestDistance = double.MaxValue;
            foreach (var entry in sourceSignatures)
            {
                if (Math.Abs(targetSignature[3] - entry.Value[3]) > 1E-6 * Math.Max(targetSignature[3], 1E-30)) continue;
                distance = GetDistance(targetSignature, entry.Value);
                if (distance >= bestDistance) continue;
                //
                for (int i = 0; i < samples.Length; i += 3)
                {
                    for (int k = 0; k < 3; k++) carried[i + k] = samples[i + k] + entry.Value[k] - targetSignature[k];
                }
                Gmsh.Model.GetClosestPoint(1, entry.Key, carried, out closestCoor, out parametricCoor);
                if (closestCoor.Length != carried.Length) continue;
                //
                onEdge = true;
                for (int i = 0; i < carried.Length && onEdge; i += 3)
                {
                    onEdge = Math.Sqrt(Math.Pow(closestCoor[i] - carried[i], 2) +
                                       Math.Pow(closestCoor[i + 1] - carried[i + 1], 2) +
                                       Math.Pow(closestCoor[i + 2] - carried[i + 2], 2)) <= tolerance;
                }
                if (!onEdge) continue;
                //
                bestEdgeId = entry.Key;
                bestDistance = distance;
            }
            return bestEdgeId;
        }
        /// <summary>
        /// Points along a curve, at equally spaced parameters strictly inside its bounds.
        /// </summary>
        private static double[] GetCurveSamples(int edgeId)
        {
            double[] minimum;
            double[] maximum;
            Gmsh.Model.GetParametrizationBounds(1, edgeId, out minimum, out maximum);
            if (minimum.Length < 1 || maximum.Length < 1) return null;
            //
            double[] parametricCoor = new double[_numStraightEdgeSamples];
            for (int i = 0; i < _numStraightEdgeSamples; i++)
                parametricCoor[i] = minimum[0] + (maximum[0] - minimum[0]) * (i + 1.0) / (_numStraightEdgeSamples + 1.0);
            //
            double[] coor;
            Gmsh.Model.GetValue(1, edgeId, parametricCoor, out coor);
            return coor;
        }
        /// <summary>
        /// The edges the carrying made: an edge that was not there before and has only pieces of one face on
        /// its sides. A piece of an edge that was there before separates pieces of two different faces.
        /// </summary>
        private static HashSet<int> GetScaffoldEdgeIds(Dictionary<int, int> surfaceIdOriginalId,
                                                       Dictionary<int, int> edgeIdOriginalId)
        {
            int[] surfaceIds;
            Tuple<int, int>[] edgeDimTags;
            HashSet<int> originalIds = new HashSet<int>();
            HashSet<int> scaffoldEdgeIds = new HashSet<int>();
            Gmsh.Model.GetEntities(out edgeDimTags, 1);
            foreach (var dimTag in edgeDimTags)
            {
                if (edgeIdOriginalId.ContainsKey(dimTag.Item2)) continue;
                Gmsh.Model.GetAdjacencies(1, dimTag.Item2, out surfaceIds, out _);
                originalIds.Clear();
                foreach (var surfaceId in surfaceIds) originalIds.Add(surfaceIdOriginalId[surfaceId]);
                if (surfaceIds.Length > 0 && originalIds.Count == 1) scaffoldEdgeIds.Add(dimTag.Item2);
            }
            return scaffoldEdgeIds;
        }
        private void RemapGmshData(Dictionary<int, int> edgeIdMap, Dictionary<int, int> pointIdMap)
        {
            int newId;
            // An edge that was split has no successor; its pinned count is lost with it
            if (_gmshData.EdgeIdNumElements != null)
            {
                Dictionary<int, int> edgeIdNumElements = new Dictionary<int, int>();
                foreach (var entry in _gmshData.EdgeIdNumElements)
                {
                    if (edgeIdMap.TryGetValue(entry.Key, out newId)) edgeIdNumElements[newId] = entry.Value;
                }
                _gmshData.EdgeIdNumElements = edgeIdNumElements;
            }
            // A layer edge follows the sweep and is never split by a curve carried onto an end
            if (_gmshData.EdgeIdsBySweepLayer != null)
            {
                foreach (var ring in _gmshData.EdgeIdsBySweepLayer)
                {
                    List<int> remapped = new List<int>();
                    foreach (var edgeId in ring)
                    {
                        if (!edgeIdMap.TryGetValue(edgeId, out newId))
                            throw new CaeException("A layer edge of the sweep was lost while the splits of the sweep ends " +
                                                   "were carried across.");
                        remapped.Add(newId);
                    }
                    ring.Clear();
                    ring.UnionWith(remapped);
                }
            }
            if (_gmshData.VertexNodeIdMeshSize != null)
            {
                Dictionary<int, double> vertexNodeIdMeshSize = new Dictionary<int, double>();
                foreach (var entry in _gmshData.VertexNodeIdMeshSize)
                {
                    if (pointIdMap.TryGetValue(entry.Key, out newId)) vertexNodeIdMeshSize[newId] = entry.Value;
                }
                _gmshData.VertexNodeIdMeshSize = vertexNodeIdMeshSize;
            }
        }
        /// <summary>
        /// The centre of mass and the size of every entity of a dimension - the coordinates of a point.
        /// </summary>
        private static Dictionary<int, double[]> GetEntitySignatures(int dim)
        {
            double x;
            double y;
            double z;
            double mass;
            double[] coor;
            Tuple<int, int>[] dimTags;
            Dictionary<int, double[]> signatures = new Dictionary<int, double[]>();
            Gmsh.Model.GetEntities(out dimTags, dim);
            foreach (var dimTag in dimTags)
            {
                if (dim == 0)
                {
                    coor = GetVertexCoor(dimTag.Item2);
                    signatures.Add(dimTag.Item2, new double[] { coor[0], coor[1], coor[2], 0 });
                }
                else
                {
                    Gmsh.Model.OCC.GetCenterOfMass(dim, dimTag.Item2, out x, out y, out z);
                    Gmsh.Model.OCC.GetMass(dim, dimTag.Item2, out mass);
                    signatures.Add(dimTag.Item2, new double[] { x, y, z, mass });
                }
            }
            return signatures;
        }
        private static Dictionary<int, int> MatchEntitiesByGeometry(int dim, Dictionary<int, double[]> oldSignatures,
                                                                    double diagonal)
        {
            double tolerance = 1E-6 * diagonal;
            Dictionary<int, double[]> newSignatures = GetEntitySignatures(dim);
            Dictionary<int, int> map = new Dictionary<int, int>();
            foreach (var oldEntry in oldSignatures)
            {
                foreach (var newEntry in newSignatures)
                {
                    if (GetDistance(oldEntry.Value, newEntry.Value) > tolerance) continue;
                    if (Math.Abs(oldEntry.Value[3] - newEntry.Value[3]) >
                        1E-6 * Math.Max(Math.Abs(oldEntry.Value[3]), 1E-30)) continue;
                    //
                    map[oldEntry.Key] = newEntry.Key;
                    break;
                }
            }
            return map;
        }
        private static double GetModelDiagonal()
        {
            BoundingBox bb = new BoundingBox();
            Gmsh.Model.GetBoundingBox(-1, -1, out bb.MinX, out bb.MinY, out bb.MinZ, out bb.MaxX, out bb.MaxY, out bb.MaxZ);
            return bb.GetDiagonal();
        }
        private static double GetDistance(double[] coor1, double[] coor2)
        {
            return Math.Sqrt(Math.Pow(coor1[0] - coor2[0], 2) + Math.Pow(coor1[1] - coor2[1], 2) +
                             Math.Pow(coor1[2] - coor2[2], 2));
        }
        private static double GetBoundingBoxDiagonal(int dim, int tag)
        {
            BoundingBox bb = new BoundingBox();
            Gmsh.Model.GetBoundingBox(dim, tag, out bb.MinX, out bb.MinY, out bb.MinZ, out bb.MaxX, out bb.MaxY, out bb.MaxZ);
            return bb.GetDiagonal();
        }
        /// <summary>
        /// Whether the block is a prism: planar source and target, straight and parallel direction edges. The
        /// translation returned carries a point of the target onto the source.
        /// </summary>
        private static bool IsPrismaticBlock(SweepBlock block, List<SweepWall> walls, HashSet<int> sourceEdgeIds,
                                             out double[] translation)
        {
            translation = null;
            foreach (var id in block.SourceSurfaceIds.Concat(block.TargetSurfaceIds))
            {
                if (Gmsh.Model.GetType(2, id) != "Plane") return false;
            }
            // The vertices on the source side, to orient every direction edge from the target to the source
            int[] vertexIds;
            HashSet<int> sourceVertexIds = new HashSet<int>();
            foreach (var edgeId in sourceEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                sourceVertexIds.UnionWith(vertexIds);
            }
            //
            Vec3D reference = null;
            foreach (var wall in walls)
            {
                foreach (var edgeId in wall.DirectionEdgeIds)
                {
                    Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                    if (vertexIds.Length != 2 || vertexIds[0] == vertexIds[1]) return false;
                    int sourceVertexId = sourceVertexIds.Contains(vertexIds[0]) ? vertexIds[0] : vertexIds[1];
                    int targetVertexId = sourceVertexId == vertexIds[0] ? vertexIds[1] : vertexIds[0];
                    if (!sourceVertexIds.Contains(sourceVertexId)) return false;
                    //
                    double[] targetCoor = GetVertexCoor(targetVertexId);
                    Vec3D vector = new Vec3D(GetVertexCoor(sourceVertexId)) - new Vec3D(targetCoor);
                    if (!IsStraightEdge(edgeId, targetCoor, vector)) return false;
                    if (reference == null)
                    {
                        reference = vector;
                        translation = vector.Coor;
                        continue;
                    }
                    if ((vector - reference).Len > 1E-6 * reference.Len) return false;
                }
            }
            return reference != null;
        }
        private static double[] GetVertexCoor(int vertexId)
        {
            double[] coor;
            Gmsh.Model.GetValue(0, vertexId, new double[3], out coor);
            return coor;
        }
        /// <summary>
        /// Whether an edge runs straight between its end vertices, judged by its shape and not by the type the
        /// geometry kernel reports: an edge that is geometrically a segment arrives from an imported file as a
        /// spline often enough, and the model side, which decides whether a block may be swept at all, measures
        /// the same thing on the visualization nodes of the edge.
        /// </summary>
        private static bool IsStraightEdge(int edgeId, double[] start, Vec3D chord)
        {
            double[] minimum;
            double[] maximum;
            Gmsh.Model.GetParametrizationBounds(1, edgeId, out minimum, out maximum);
            if (minimum.Length < 1 || maximum.Length < 1) return false;
            //
            double[] parametricCoor = new double[_numStraightEdgeSamples];
            for (int i = 0; i < _numStraightEdgeSamples; i++)
                parametricCoor[i] = minimum[0] + (maximum[0] - minimum[0]) * (i + 1.0) / (_numStraightEdgeSamples + 1.0);
            //
            double[] coor;
            Gmsh.Model.GetValue(1, edgeId, parametricCoor, out coor);
            double tolerance = 1E-6 * chord.Len * chord.Len;
            for (int i = 0; i < _numStraightEdgeSamples; i++)
            {
                Vec3D offset = new Vec3D(coor[3 * i] - start[0], coor[3 * i + 1] - start[1], coor[3 * i + 2] - start[2]);
                if (Vec3D.CrossProduct(chord, offset).Len > tolerance) return false;
            }
            return true;
        }
        /// <summary>
        /// One attempt at the sweep: the element counts, the 2D mesh, and the sweep built on the grid the
        /// walls came out with. Everything that can tell the attempt apart from a good one raises, so the
        /// caller can take the whole thing back and try another 2D algorithm - the mesh is cleared at the
        /// start of every attempt, so a half built sweep leaves nothing behind.
        /// </summary>
        private void MeshSweepBlocks(SweepMesh sweepMesh, List<SweepBlock> blocks, List<SweepWall> walls,
                                     List<SweepChain> chains, HashSet<int> targetSurfaceIds,
                                     Dictionary<int, int[]> surfaceIdEdgeIds, List<int> chainBlockIndices,
                                     bool preview, bool setTransfiniteWalls)
        {
            // Layer counts and the cross-sweep reconciliation, both measured from a 1D mesh
            PrepareWallEdges(walls, chains, chainBlockIndices);
            foreach (var wall in walls) Gmsh.Model.Mesh.SetRecombine(2, wall.SurfaceId);
            // The curves are meshed in a pass of their own. Measured: when the curves and the surfaces are
            // meshed in one call, a wall whose both cross-sweep sides are composite fails the transfinite
            // side check with node counts that are not the ones the constraints above set
            GenerateMesh(1);
            // A transfinite surface set a second time would add its corners to the ones gmsh holds, so a
            // later attempt keeps the constraints of the first
            if (setTransfiniteWalls) SetTransfiniteWalls(walls);
            // gmsh reports a wall it could not mesh with a structured grid, meshes it free and carries on, so
            // its log is read for the report
            int logLength = Gmsh.Logger.Get().Length;
            GenerateMesh(2);
            if (WereWallsRefused(logLength))
                throw new CaeException("A sweep side surface could not be meshed with a structured grid. See the gmsh " +
                                       "messages above.");
            // Remove the volumes and the target surface meshes. A target shared with the next block is
            // rebuilt by the sweep of this block and read back as the source of the next one.
            List<Tuple<int, int>> toRemoveDimTags = new List<Tuple<int, int>>();
            foreach (var block in blocks) toRemoveDimTags.Add(new Tuple<int, int>(3, block.VolumeId));
            foreach (var targetSurfaceId in targetSurfaceIds) toRemoveDimTags.Add(new Tuple<int, int>(2, targetSurfaceId));
            //
            Gmsh.Model.Mesh.Clear(toRemoveDimTags.ToArray());
            // Optimize first order. This reaches the walls as well as the source surfaces, because
            // gmsh ignores the entity list for the 2D relocation methods; Laplace 2D is the only one
            // of the three that leaves a transfinite wall alone.
            if (sweepMesh.OptimizeFirstOrderShell != GmshOptimizeFirstOrderShellEnum.None)
            {
                Tuple<int, int>[] dimTags = new Tuple<int, int>[0];
                Gmsh.Model.Mesh.Optimize(sweepMesh.OptimizeFirstOrderShell.ToString(), false, 10, dimTags);
            }
            // Debug
            if (Debugger.IsAttached) Gmsh.Write(_gmshData.InpFileName);
            //
            if (preview)
            {
                // Remove the side surface mesh
                toRemoveDimTags = new List<Tuple<int, int>>();
                foreach (var block in blocks)
                {
                    foreach (var sideSurfaceId in block.WallSurfaceIds) toRemoveDimTags.Add(new Tuple<int, int>(2, sideSurfaceId));
                }
                //
                Gmsh.Model.Mesh.Clear(toRemoveDimTags.ToArray());
            }
            else
            {
                SweepMethods.CreateSweepMesh(blocks, sweepMesh.NumberOfGlobalSmoothSteps, surfaceIdEdgeIds, _writeOutput);
            }
        }
        /// <summary>
        /// The sweep blocks of the item, in gmsh numbering. An item saved before the blocks were recorded
        /// carries only its side surfaces and describes a single volume, so that block is rebuilt the way
        /// it used to be: the source from the selection and the target as whatever surfaces are left.
        /// </summary>
        private List<SweepBlock> GetSweepBlocks(SweepMesh sweepMesh, IEnumerable<int> allSurfaceIds)
        {
            List<SweepBlock> blocks = new List<SweepBlock>();
            if (sweepMesh.BlockSourceSurfaceIds != null)
            {
                for (int i = 0; i < sweepMesh.BlockSourceSurfaceIds.Length; i++)
                {
                    blocks.Add(new SweepBlock(sweepMesh.BlockSourceSurfaceIds[i], sweepMesh.BlockSideSurfaceIds[i],
                                              sweepMesh.BlockTargetSurfaceIds[i]));
                }
            }
            else
            {
                HashSet<int> sourceSurfaceIds = new HashSet<int>();
                for (int i = 0; i < sweepMesh.CreationIds.Length; i++)
                    sourceSurfaceIds.Add(FeMesh.GetItemIdFromGeometryId(sweepMesh.CreationIds[i]) + 1);
                HashSet<int> sideSurfaceIds = new HashSet<int>();
                if (sweepMesh.BlockSideSurfaceIds != null && sweepMesh.BlockSideSurfaceIds.Length > 0)
                    sideSurfaceIds.UnionWith(sweepMesh.BlockSideSurfaceIds[0]);
                HashSet<int> targetSurfaceIds = new HashSet<int>(allSurfaceIds);
                targetSurfaceIds.ExceptWith(sourceSurfaceIds);
                targetSurfaceIds.ExceptWith(sideSurfaceIds);
                blocks.Add(new SweepBlock(sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds));
            }
            //
            foreach (var block in blocks)
            {
                if (block.TargetSurfaceIds.Count < 1) throw new CaeException("No sweep target surface could be determined.");
                block.VolumeId = GetSweepBlockVolumeId(block);
            }
            //
            return blocks;
        }
        /// <summary>
        /// The volume bounded by the surfaces of a block. A surface shared with a neighbouring block touches
        /// two volumes, so the volume of the block is the one that every one of its surfaces touches.
        /// </summary>
        private static int GetSweepBlockVolumeId(SweepBlock block)
        {
            int[] volumeIds;
            HashSet<int> candidateIds = null;
            IEnumerable<int> surfaceIds = block.SourceSurfaceIds.Concat(block.WallSurfaceIds).Concat(block.TargetSurfaceIds);
            foreach (var surfaceId in surfaceIds)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceId, out volumeIds, out _);
                if (candidateIds == null) candidateIds = new HashSet<int>(volumeIds);
                else candidateIds.IntersectWith(volumeIds);
            }
            if (candidateIds == null || candidateIds.Count != 1)
                throw new CaeException("The volume of a sweep block could not be determined from its surfaces.");
            //
            return candidateIds.First();
        }
        //
        public bool CheckMeshVolume(Tuple<int, int>[] outDimTags)
        {
            double volumeOut;
            double initialVolume;
            double extrudedVolume = 0;
            //
            Gmsh.Model.OCC.GetMass(3, 1, out initialVolume);
            //
            foreach (var outDimTag in outDimTags)
            {
                if (outDimTag.Item1 == 3)
                {
                    Gmsh.Model.OCC.GetMass(3, outDimTag.Item2, out volumeOut);
                    extrudedVolume += volumeOut;
                }
            }
            double maxVolume = Math.Max(initialVolume, extrudedVolume);
            if (Math.Abs(initialVolume - extrudedVolume) > 1E-2 * maxVolume) return false;
            //
            return true;
                
        }
        public bool CheckMeshVolume(GeometryPart part, ExtrudeMesh extrudeMesh, 
                                           Func<GeometryPart, string> ExportCADPartGeometryToDefaultFile)
        {
            int surfaceId;
            string brepFileName = ExportCADPartGeometryToDefaultFile(part);
            // Initialize
            Gmsh.Initialize();
            Gmsh.Model.Add("Brep_model");
            // Import
            Tuple<int, int>[] outDimTags;
            Gmsh.Model.OCC.ImportShapes(brepFileName, out outDimTags, false, "");
            Gmsh.Model.OCC.Synchronize(); // must be here
            // Get surfaces to extrude
            Tuple<int, int>[] dimTags = new Tuple<int, int>[extrudeMesh.CreationIds.Length];
            for (int i = 0; i < extrudeMesh.CreationIds.Length; i++)
            {
                surfaceId = FeMesh.GetItemIdFromGeometryId(extrudeMesh.CreationIds[i]) + 1;
                dimTags[i] = new Tuple<int, int>(2, surfaceId);
            }
            // Setup layers
            int[] numElements = new int[] { 1 };
            double[] height = new double[] { 1 };
            // Extrude
            Gmsh.Model.OCC.Extrude(dimTags, 0, 0, -50, out outDimTags, numElements, height, false);
            //
            double massOut;
            double mass1 = 0;
            double massExtruded = 0;
            //
            Gmsh.Model.OCC.GetMass(3, 1, out massOut);
            mass1 += massOut;
            //
            foreach (var outDimTag in outDimTags)
            {
                if (outDimTag.Item1 == 3)
                {
                    Gmsh.Model.OCC.GetMass(3, outDimTag.Item2, out massOut);
                    massExtruded += massOut;
                }
            }
            return false;
        }
        /// <summary>
        /// Fixes the element counts on every edge of every wall, immediately before the walls are
        /// meshed.
        ///
        /// This has to happen here rather than in SetMeshSizes, because SetTransfiniteSurfaces runs
        /// in between and reassigns the edges that follow the sweep direction from its own averaged
        /// edge groups. That silently discarded the layer grading, and it produces nothing at all
        /// for a wall that is neither 3 nor 4 sided, which is what a split source face creates.
        ///
        /// Two constraints are imposed:
        ///
        ///   - The edges following the sweep direction get their counts from the levels along the
        ///     sweep (SweepWallTopology.ResolveDirectionEdgeElements): the chains of edges running from
        ///     the source are cut into levels wherever a chain has a vertex, every interval between two
        ///     levels carries one count, and an edge carries the sum over the intervals it spans. The
        ///     two sides of every wall therefore add up to the same total however each of them is
        ///     split, whether by loops running around the body, which give rings with one count each,
        ///     or by a split on one wall alone, which the walls beside it run past unsplit.
        ///   - The two cross-sweep sides of each wall are made to agree. gmsh raises an error rather
        ///     than falling back when a transfinite surface has different node counts on opposite
        ///     sides, and the sweep needs the agreement in any case: the source and target boundary
        ///     loops must carry the same number of nodes for the sweep lines to pair up. The source
        ///     side wins, because that is the mesh the user is tuning, and the target side is
        ///     redistributed to match in proportion to the lengths of its edges.
        ///
        /// Counts the user did not pin down are read back from a one dimensional mesh rather than
        /// predicted, which is the approach SetTransfiniteSurfaces already takes.
        /// </summary>
        /// <summary>
        /// The vertices of every source and target surface of the blocks, surface by surface: the vertices of
        /// one such surface lie on one level of the sweep.
        /// </summary>
        private List<int[]> GetCrossSectionVertexIds()
        {
            int[] edgeIds;
            int[] vertexIds;
            List<int[]> groups = new List<int[]>();
            if (_sweepBlocks == null) return groups;
            foreach (var block in _sweepBlocks)
            {
                foreach (var surfaceId in block.SourceSurfaceIds.Concat(block.TargetSurfaceIds))
                {
                    HashSet<int> group = new HashSet<int>();
                    Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                    foreach (var edgeId in edgeIds)
                    {
                        Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                        group.UnionWith(vertexIds);
                    }
                    if (group.Count > 1) groups.Add(group.ToArray());
                }
            }
            return groups;
        }
        /// <summary>
        /// Takes back the transfinite constraints the first attempt set on the edges of the walls, so that the
        /// 1D mesh the second attempt measures follows the sizes and not those counts. The surfaces keep their
        /// constraints: a transfinite surface set a second time would add its corners to the ones gmsh holds.
        /// </summary>
        private static void RemoveWallEdgeConstraints(List<SweepWall> walls)
        {
            HashSet<int> edgeIds = new HashSet<int>();
            foreach (var wall in walls)
            {
                edgeIds.UnionWith(wall.SourceEdgeIds);
                edgeIds.UnionWith(wall.TargetEdgeIds);
                edgeIds.UnionWith(wall.DirectionEdgeIds);
            }
            List<Tuple<int, int>> dimTags = new List<Tuple<int, int>>();
            foreach (var edgeId in edgeIds) dimTags.Add(new Tuple<int, int>(1, edgeId));
            Gmsh.Model.Mesh.RemoveConstraints(dimTags.ToArray());
        }
        private static bool WereWallsRefused(int logLength)
        {
            return Gmsh.Logger.Get().Skip(logLength).Any(line => line.Contains("cannot be meshed using the transfinite"));
        }
        private static void SetTransfiniteWalls(List<SweepWall> walls)
        {
            foreach (var wall in walls)
            {
                int[] corners = SweepWallTopology.GetCorners(wall);
                if (corners != null) Gmsh.Model.Mesh.SetTransfiniteSurface(wall.SurfaceId, "AlternateLeft", corners);
                else Gmsh.Model.Mesh.SetTransfiniteSurface(wall.SurfaceId, "AlternateLeft");
            }
        }
        private void PrepareWallEdges(List<SweepWall> walls, List<SweepChain> chains, List<int> chainBlockIndices)
        {
            GenerateMesh(1);
            // How many elements each edge following the sweep direction gets, from the levels along the sweep
            // the chains define - so that the two sides of every wall add up to the same total, however each
            // of them is split. A direction edge on no chain keeps the count the user pinned on it, else the
            // measured one. Measured while the 1D mesh from GenerateMesh(1) above is still there.
            int numOfElements;
            Dictionary<int, int> directionEdgeIdNumOfElements =
                SweepWallTopology.ResolveDirectionEdgeElements(chains, walls, GetCrossSectionVertexIds(),
                                                               _gmshData.EdgeIdNumElements, chainBlockIndices);
            foreach (var wall in walls)
            {
                foreach (var edgeId in wall.DirectionEdgeIds)
                {
                    if (directionEdgeIdNumOfElements.ContainsKey(edgeId)) continue;
                    if (!_gmshData.EdgeIdNumElements.TryGetValue(edgeId, out numOfElements))
                        numOfElements = SweepWallTopology.GetElementCount(edgeId);
                    directionEdgeIdNumOfElements[edgeId] = Math.Max(1, numOfElements);
                }
            }
            // The two cross-sweep sides of every wall, also measured while the 1D mesh is still there. The
            // source side is pinned as well, so that every edge of every wall carries a count
            Dictionary<int, int> sourceEdgeIdNumOfElements;
            Dictionary<int, int> targetEdgeIdNumOfElements;
            ResolveCrossEdgeElements(walls, null, out sourceEdgeIdNumOfElements, out targetEdgeIdNumOfElements);
            // An edge splitting a target and the edge of the source below it pair their nodes one to one, so
            // they carry the same count: the one the user pinned on either, else the measured one
            Dictionary<int, int> imprintEdgeIdNumOfElements = new Dictionary<int, int>();
            if (_sweepBlocks != null)
            {
                foreach (var block in _sweepBlocks)
                {
                    if (block.ImprintEdgeCopyIds == null) continue;
                    foreach (var entry in block.ImprintEdgeCopyIds)
                    {
                        if (!_gmshData.EdgeIdNumElements.TryGetValue(entry.Key, out numOfElements) &&
                            !_gmshData.EdgeIdNumElements.TryGetValue(entry.Value, out numOfElements))
                            numOfElements = SweepWallTopology.GetElementCount(entry.Key);
                        imprintEdgeIdNumOfElements[entry.Key] = numOfElements;
                        imprintEdgeIdNumOfElements[entry.Value] = numOfElements;
                    }
                }
            }
            // Every count in one table, the later ones overriding the earlier
            Dictionary<int, int> edgeIdNumOfElements = MergeEdgeElements(directionEdgeIdNumOfElements, sourceEdgeIdNumOfElements,
                                                                         targetEdgeIdNumOfElements, imprintEdgeIdNumOfElements);
            // A wall whose two sides came out unequal stands between two cuts of one closed curve that gmsh
            // counted differently, and the column those cuts belong to is resolved again, from the cut counted
            // by its length. Every other edge keeps the count measured above
            List<int[]> recountedLoops = GetDifferentlyCountedCrossSections(walls, edgeIdNumOfElements);
            if (recountedLoops.Count > 0)
            {
                _writeOutput?.Invoke("Sweep: " + GetRecountedCrossSectionsMessage(recountedLoops));
                ResolveCrossEdgeElements(walls, recountedLoops, out sourceEdgeIdNumOfElements, out targetEdgeIdNumOfElements);
                edgeIdNumOfElements = MergeEdgeElements(directionEdgeIdNumOfElements, sourceEdgeIdNumOfElements,
                                                        targetEdgeIdNumOfElements, imprintEdgeIdNumOfElements);
            }
            foreach (var entry in edgeIdNumOfElements) _gmshData.EdgeIdNumElements[entry.Key] = entry.Value;
            //
            CheckWallElementCounts(walls, chains, edgeIdNumOfElements);
            Gmsh.Model.Mesh.Clear();    // must clear the mesh before the constraints are changed
            foreach (var entry in edgeIdNumOfElements) Gmsh.Model.Mesh.SetTransfiniteCurve(entry.Key, entry.Value + 1);
        }
        /// <summary>
        /// The element counts of the two cross-sweep sides of every wall, resolved from the source side to the
        /// target side: the source side keeps the count an earlier wall gave the edge as its target, else the
        /// measured one, and the target side is distributed from the source total. The walls come block after
        /// block and ring after ring, so a target one wall assigns is the source the next one reads.
        ///
        /// A wall two columns of blocks share is one wall, sided by the column that reached it first, and its
        /// count may have to come from the other: the four walls of a bore that a post stands in are sided from
        /// the plate, and it is the post that carries the count of the bore. The loops given are the cuts of
        /// one closed curve that gmsh counted differently, and a wall whose source side lies on one of them
        /// waits until another wall has assigned one of its sides - the source, as usual, or the target when
        /// the other column got there first, and the wall is then resolved the other way round. When no
        /// waiting wall can be resolved, the coarsest of the loops still unassigned is counted from its length,
        /// with every count the user seeded on it kept exactly, and the rest of the column follows from it wall
        /// by wall. Without loops every wall is resolved in its turn.
        /// </summary>
        private void ResolveCrossEdgeElements(List<SweepWall> walls, List<int[]> recountedLoops,
                                              out Dictionary<int, int> sourceEdgeIdNumOfElements,
                                              out Dictionary<int, int> targetEdgeIdNumOfElements)
        {
            Dictionary<int, int> sourceTable = new Dictionary<int, int>();
            Dictionary<int, int> assignedTable = new Dictionary<int, int>();
            HashSet<int> recountedEdgeIds = new HashSet<int>();
            if (recountedLoops != null)
            {
                foreach (var loop in recountedLoops) recountedEdgeIds.UnionWith(loop);
            }
            //
            List<SweepWall> waitingWalls = new List<SweepWall>();
            foreach (var wall in walls)
            {
                if (wall.SourceEdgeIds.Length == 0 || wall.TargetEdgeIds.Length == 0) continue;
                if (HasUnassignedEdge(wall.SourceEdgeIds, recountedEdgeIds, assignedTable)) waitingWalls.Add(wall);
                else ResolveWallCrossEdges(wall, false, sourceTable, assignedTable);
            }
            while (waitingWalls.Count > 0)
            {
                bool resolved = false;
                for (int i = 0; i < waitingWalls.Count; i++)
                {
                    SweepWall wall = waitingWalls[i];
                    bool sourceAssigned = AreAllAssigned(wall.SourceEdgeIds, assignedTable);
                    if (!sourceAssigned && !AreAllAssigned(wall.TargetEdgeIds, assignedTable)) continue;
                    if (sourceAssigned) ResolveWallCrossEdges(wall, false, sourceTable, assignedTable);
                    else
                    {
                        ResolveWallCrossEdges(new SweepWall(wall.SurfaceId, wall.TargetEdgeIds, wall.SourceEdgeIds,
                                                            wall.DirectionEdgeIds), true, sourceTable, assignedTable);
                    }
                    waitingWalls.RemoveAt(i--);
                    resolved = true;
                }
                if (resolved) continue;
                //
                int[] loop = GetLoopToRecount(recountedLoops, assignedTable);
                if (loop == null)
                {
                    foreach (var wall in waitingWalls) ResolveWallCrossEdges(wall, false, sourceTable, assignedTable);
                    break;
                }
                SweepWallTopology.RecountCrossSection(loop, recountedLoops, _gmshData.EdgeIdNumElements, assignedTable);
            }
            sourceEdgeIdNumOfElements = sourceTable;
            targetEdgeIdNumOfElements = assignedTable;
        }
        private static bool HasUnassignedEdge(int[] edgeIds, HashSet<int> recountedEdgeIds,
                                              Dictionary<int, int> assignedEdgeIdNumOfElements)
        {
            foreach (var edgeId in edgeIds)
            {
                if (recountedEdgeIds.Contains(edgeId) && !assignedEdgeIdNumOfElements.ContainsKey(edgeId)) return true;
            }
            return false;
        }
        private static bool AreAllAssigned(int[] edgeIds, Dictionary<int, int> assignedEdgeIdNumOfElements)
        {
            foreach (var edgeId in edgeIds)
            {
                if (!assignedEdgeIdNumOfElements.ContainsKey(edgeId)) return false;
            }
            return true;
        }
        /// <summary>
        /// The loop to count from its length when no waiting wall can be resolved: the one carrying the most
        /// seeds of the user's, else the one cut into the fewest edges, whose count the rounding inflated least.
        /// Null when every loop is assigned.
        /// </summary>
        private int[] GetLoopToRecount(List<int[]> loops, Dictionary<int, int> assignedEdgeIdNumOfElements)
        {
            int[] best = null;
            int bestSeeded = -1;
            int bestUnassigned = int.MaxValue;
            foreach (var loop in loops)
            {
                int seeded = 0;
                int unassigned = 0;
                foreach (var edgeId in loop)
                {
                    if (assignedEdgeIdNumOfElements.ContainsKey(edgeId)) continue;
                    unassigned++;
                    if (_gmshData.EdgeIdNumElements.ContainsKey(edgeId)) seeded++;
                }
                if (unassigned == 0) continue;
                if (seeded > bestSeeded || (seeded == bestSeeded && unassigned < bestUnassigned))
                {
                    best = loop;
                    bestSeeded = seeded;
                    bestUnassigned = unassigned;
                }
            }
            return best;
        }
        private void ResolveWallCrossEdges(SweepWall wall, bool reversed, Dictionary<int, int> sourceEdgeIdNumOfElements,
                                           Dictionary<int, int> targetEdgeIdNumOfElements)
        {
            int numOfElements;
            int sourceElements = 0;
            foreach (var edgeId in wall.SourceEdgeIds)
            {
                // The target side of an earlier wall keeps the count that wall was given, and the walls
                // of the block fed from it must start from that count rather than from the measured one
                if (!targetEdgeIdNumOfElements.TryGetValue(edgeId, out numOfElements))
                    numOfElements = SweepWallTopology.GetElementCount(edgeId);
                sourceEdgeIdNumOfElements[edgeId] = numOfElements;
                sourceElements += numOfElements;
            }
            RaiseSourceToTargetEdgeCount(wall, sourceEdgeIdNumOfElements, targetEdgeIdNumOfElements,
                                         ref sourceElements);
            // Where a split was carried across, both sides of the wall are split at the same places. The
            // counts of the target edges follow the source nodes above them, so that the grid column
            // through the split stays straight
            int[] targetElements = DistributeTargetElementsByProjection(wall, sourceElements, reversed);
            // Where the two sides are split at corresponding places, every target segment takes the count of
            // the source segment opposite it, so that the grid column through the split runs straight
            if (targetElements == null) targetElements = DistributeTargetElementsBySide(wall, sourceEdgeIdNumOfElements);
            if (targetElements == null)
            {
                double[] lengths = SweepWallTopology.GetEdgeLengths(wall.TargetEdgeIds);
                targetElements = SweepWallTopology.DistributeElements(sourceElements, lengths);
            }
            for (int i = 0; i < wall.TargetEdgeIds.Length; i++)
                targetEdgeIdNumOfElements[wall.TargetEdgeIds[i]] = targetElements[i];
        }
        /// <summary>
        /// Raises the source side of a wall to the number of edges its target side is cut into. Every edge
        /// carries at least one element, so a target cut in two by a chamfer needs two of them, and a source
        /// side gmsh measured as one element could never be the other side of that grid - the wall would be
        /// refused however fine the rest of the mesh is. The raise is only made where the source is free to
        /// take it: a source side the wall before it assigned as its target, or one the user pinned a count
        /// on, keeps the count it was given, and the wall is refused with the count message as before.
        /// </summary>
        private void RaiseSourceToTargetEdgeCount(SweepWall wall, Dictionary<int, int> sourceEdgeIdNumOfElements,
                                                  Dictionary<int, int> targetEdgeIdNumOfElements,
                                                  ref int sourceElements)
        {
            if (sourceElements >= wall.TargetEdgeIds.Length) return;
            //
            foreach (var edgeId in wall.SourceEdgeIds)
            {
                if (targetEdgeIdNumOfElements.ContainsKey(edgeId) ||
                    _gmshData.EdgeIdNumElements.ContainsKey(edgeId)) return;
            }
            //
            double[] lengths = SweepWallTopology.GetEdgeLengths(wall.SourceEdgeIds);
            int[] raisedElements = SweepWallTopology.DistributeElements(wall.TargetEdgeIds.Length, lengths);
            sourceElements = 0;
            for (int i = 0; i < wall.SourceEdgeIds.Length; i++)
            {
                sourceEdgeIdNumOfElements[wall.SourceEdgeIds[i]] = raisedElements[i];
                sourceElements += raisedElements[i];
            }
        }
        /// <summary>
        /// The cross section loops of one column that gmsh counted differently, found from the walls whose two
        /// sides came out unequal: the loops the two sides of such a wall lie on, when they are the same closed
        /// curve - of the same length - cut differently, and from there every loop of that length that a wall
        /// joins to one of them, since a column carries one count through all its cross sections. Gmsh meshes
        /// every curve on its own and rounds its count up, so a circle cut into two half arcs and the same
        /// circle cut into four unequal arcs measure differently, and the walls between them cannot both be
        /// grids. Empty when every wall is a grid, and when the sides of no unequal wall are cuts of one curve.
        /// </summary>
        private static List<int[]> GetDifferentlyCountedCrossSections(List<SweepWall> walls,
                                                                      Dictionary<int, int> edgeIdNumOfElements)
        {
            List<int[]> loops = new List<int[]>();
            List<SweepWall> refusedWalls = GetWallsRefusedAcrossSweep(walls, edgeIdNumOfElements);
            if (refusedWalls.Count == 0) return loops;
            //
            Dictionary<int, int[]> edgeIdLoop = SweepWallTopology.GetCrossSectionLoops(walls);
            Dictionary<int[], double> loopLengths = new Dictionary<int[], double>();
            foreach (var wall in refusedWalls)
            {
                int[] sourceLoop = edgeIdLoop[wall.SourceEdgeIds[0]];
                int[] targetLoop = edgeIdLoop[wall.TargetEdgeIds[0]];
                if (!AreSameClosedCurve(sourceLoop, targetLoop, loopLengths)) continue;
                if (!loops.Contains(sourceLoop)) loops.Add(sourceLoop);
                if (!loops.Contains(targetLoop)) loops.Add(targetLoop);
            }
            // The rest of the column: across every wall standing on a loop of the set to the loop of its other
            // side, as long as that is the same curve
            bool added = loops.Count > 0;
            while (added)
            {
                added = false;
                foreach (var wall in walls)
                {
                    if (wall.SourceEdgeIds.Length == 0 || wall.TargetEdgeIds.Length == 0) continue;
                    int[] sourceLoop = edgeIdLoop[wall.SourceEdgeIds[0]];
                    int[] targetLoop = edgeIdLoop[wall.TargetEdgeIds[0]];
                    bool sourceIn = loops.Contains(sourceLoop);
                    if (sourceIn == loops.Contains(targetLoop)) continue;
                    if (!AreSameClosedCurve(sourceLoop, targetLoop, loopLengths)) continue;
                    loops.Add(sourceIn ? targetLoop : sourceLoop);
                    added = true;
                }
            }
            return loops;
        }
        private static bool AreSameClosedCurve(int[] loop1, int[] loop2, Dictionary<int[], double> loopLengths)
        {
            if (loop1 == loop2) return false;
            double length1 = GetLoopLength(loop1, loopLengths);
            double length2 = GetLoopLength(loop2, loopLengths);
            return Math.Abs(length1 - length2) <= 1E-3 * Math.Max(length1, length2);
        }
        private static double GetLoopLength(int[] loop, Dictionary<int[], double> loopLengths)
        {
            double length;
            if (loopLengths.TryGetValue(loop, out length)) return length;
            length = SweepWallTopology.GetEdgeLengths(loop).Sum();
            loopLengths.Add(loop, length);
            return length;
        }
        private static string GetRecountedCrossSectionsMessage(List<int[]> loops)
        {
            List<string> cuts = new List<string>();
            foreach (var loop in loops)
            {
                int total = 0;
                foreach (var edgeId in loop) total += SweepWallTopology.GetElementCount(edgeId);
                string cut = total + " elements over " + loop.Length + (loop.Length == 1 ? " edge" : " edges");
                if (!cuts.Contains(cut)) cuts.Add(cut);
            }
            double length = SweepWallTopology.GetEdgeLengths(loops[0]).Sum();
            return "the cross sections of one closed curve of length " + Math.Round(length, 3) + " were given " +
                   string.Join(" and ", cuts) + " by gmsh, which rounds the count of every edge up; the curve is counted " +
                   "from its length instead.";
        }
        /// <summary>
        /// Every count in one table, the later tables overriding the earlier.
        /// </summary>
        private static Dictionary<int, int> MergeEdgeElements(params Dictionary<int, int>[] tables)
        {
            Dictionary<int, int> edgeIdNumOfElements = new Dictionary<int, int>();
            foreach (var table in tables)
            {
                foreach (var entry in table) edgeIdNumOfElements[entry.Key] = entry.Value;
            }
            return edgeIdNumOfElements;
        }
        /// <summary>
        /// The walls whose source side and target side carry a different number of elements between them.
        /// </summary>
        private static List<SweepWall> GetWallsRefusedAcrossSweep(List<SweepWall> walls, Dictionary<int, int> edgeIdNumOfElements)
        {
            List<SweepWall> refusedWalls = new List<SweepWall>();
            foreach (var wall in walls)
            {
                if (wall.SourceEdgeIds.Length == 0 || wall.TargetEdgeIds.Length == 0) continue;
                if (GetSideElementCount(wall.SourceEdgeIds, edgeIdNumOfElements) !=
                    GetSideElementCount(wall.TargetEdgeIds, edgeIdNumOfElements)) refusedWalls.Add(wall);
            }
            return refusedWalls;
        }
        private static int GetSideElementCount(int[] edgeIds, Dictionary<int, int> edgeIdNumOfElements)
        {
            int numOfElements;
            int count = 0;
            foreach (var edgeId in edgeIds)
            {
                if (edgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) count += numOfElements;
            }
            return count;
        }
        /// <summary>
        /// Checks that every wall is a grid before gmsh is asked to lay one over it: the two sides across the
        /// sweep carry the same number of elements between them, and so do the two sides along it. Gmsh
        /// refuses such a wall with a message naming a surface number and nothing else, and carries on to
        /// fail elsewhere, so the counts are checked here where the wall can still be named by what it is.
        /// </summary>
        private static void CheckWallElementCounts(List<SweepWall> walls, List<SweepChain> chains,
                                                   Dictionary<int, int> edgeIdNumOfElements)
        {
            int numOfElements;
            Dictionary<int, int> edgeIdChainIndex = new Dictionary<int, int>();
            for (int c = 0; c < chains.Count; c++)
            {
                foreach (var edgeId in chains[c].EdgeIds) edgeIdChainIndex[edgeId] = c;
            }
            foreach (var wall in walls)
            {
                // Across the sweep: the source side against the target side
                int sourceCount = GetSideElementCount(wall.SourceEdgeIds, edgeIdNumOfElements);
                int targetCount = GetSideElementCount(wall.TargetEdgeIds, edgeIdNumOfElements);
                if (wall.SourceEdgeIds.Length > 0 && wall.TargetEdgeIds.Length > 0 && sourceCount != targetCount)
                    throw new CaeException(GetWallCountMessage("across the sweep direction", sourceCount, targetCount));
                // Along the sweep: the two sides, each of them one chain however many edges it is made of
                int chainIndex;
                Dictionary<int, int> chainIndexCount = new Dictionary<int, int>();
                foreach (var edgeId in wall.DirectionEdgeIds)
                {
                    if (!edgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) continue;
                    if (!edgeIdChainIndex.TryGetValue(edgeId, out chainIndex)) chainIndex = -edgeId;
                    chainIndexCount.TryGetValue(chainIndex, out int count);
                    chainIndexCount[chainIndex] = count + numOfElements;
                }
                if (chainIndexCount.Count != 2) continue;
                //
                int[] counts = chainIndexCount.Values.ToArray();
                if (counts[0] != counts[1])
                    throw new CaeException(GetWallCountMessage("along the sweep direction", counts[0], counts[1]));
            }
        }
        private static string GetWallCountMessage(string what, int first, int second)
        {
            return "A sweep side surface cannot be meshed: its two sides " + what + " were given " + first +
                   " and " + second + " elements, and a structured grid needs the same number on both. " +
                   "Select the source surfaces by hand, or split the part so that each piece is swept on " +
                   "its own.";
        }
        /// <summary>
        /// The counts of a wall's target side taken one for one from its source side, in the order the two
        /// sides run. Null when the two sides cannot be matched - a different number of edges, or a side that
        /// is no open chain - and the distribution by length decides then.
        /// </summary>
        private static int[] DistributeTargetElementsBySide(SweepWall wall,
                                                            Dictionary<int, int> sourceEdgeIdNumOfElements)
        {
            int[] orderedSourceEdgeIds;
            int[] orderedTargetEdgeIds;
            if (!SweepWallTopology.OrderWallSides(wall, out orderedSourceEdgeIds, out orderedTargetEdgeIds)) return null;
            //
            int numOfElements;
            Dictionary<int, int> targetEdgeIdNumOfElements = new Dictionary<int, int>();
            for (int i = 0; i < orderedTargetEdgeIds.Length; i++)
            {
                if (!sourceEdgeIdNumOfElements.TryGetValue(orderedSourceEdgeIds[i], out numOfElements)) return null;
                targetEdgeIdNumOfElements[orderedTargetEdgeIds[i]] = numOfElements;
            }
            int[] targetElements = new int[wall.TargetEdgeIds.Length];
            for (int i = 0; i < wall.TargetEdgeIds.Length; i++)
                targetElements[i] = targetEdgeIdNumOfElements[wall.TargetEdgeIds[i]];
            return targetElements;
        }
        private int[] DistributeTargetElementsByProjection(SweepWall wall, int sourceElements, bool reversed)
        {
            if (_sweepBlocks == null || wall.TargetEdgeIds.Length < 2) return null;
            //
            SweepBlock block = null;
            foreach (var candidate in _sweepBlocks)
            {
                if (candidate.ImprintTranslation != null && candidate.ImprintEdgeCopyIds != null &&
                    candidate.WallSurfaceIds.Contains(wall.SurfaceId))
                {
                    block = candidate;
                    break;
                }
            }
            if (block == null) return null;
            // The source side nodes, carried onto the target plane - the other way for a wall resolved from
            // its target side
            IntPtr[] tags;
            double[] coor;
            double sign = reversed ? -1 : 1;
            List<double> projected = new List<double>();
            HashSet<IntPtr> seen = new HashSet<IntPtr>();
            foreach (var edgeId in wall.SourceEdgeIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, edgeId, true, false);
                for (int i = 0; i < tags.Length; i++)
                {
                    if (!seen.Add(tags[i])) continue;
                    projected.Add(coor[3 * i] - sign * block.ImprintTranslation[0]);
                    projected.Add(coor[3 * i + 1] - sign * block.ImprintTranslation[1]);
                    projected.Add(coor[3 * i + 2] - sign * block.ImprintTranslation[2]);
                }
            }
            // Each target edge gets the nodes that land on it, an end node of two edges counting for both
            double[] closestCoord;
            double[] parametricCoord;
            double tolerance = 1E-6 * GetBoundingBoxDiagonal(3, block.VolumeId);
            int[] targetElements = new int[wall.TargetEdgeIds.Length];
            int total = 0;
            for (int i = 0; i < wall.TargetEdgeIds.Length; i++)
            {
                Gmsh.Model.GetClosestPoint(1, wall.TargetEdgeIds[i], projected.ToArray(), out closestCoord, out parametricCoord);
                if (closestCoord.Length != projected.Count) return null;
                int count = 0;
                for (int k = 0; k < projected.Count / 3; k++)
                {
                    if (Math.Sqrt(Math.Pow(closestCoord[3 * k] - projected[3 * k], 2) +
                                  Math.Pow(closestCoord[3 * k + 1] - projected[3 * k + 1], 2) +
                                  Math.Pow(closestCoord[3 * k + 2] - projected[3 * k + 2], 2)) <= tolerance) count++;
                }
                targetElements[i] = Math.Max(1, count - 1);
                total += targetElements[i];
            }
            // Only a split that lines up with the source nodes is worth following
            return total == sourceElements ? targetElements : null;
        }
        /// <summary>
        /// Reports the quality of the finished mesh, whatever produced it.
        ///
        /// The measure is gmsh's own minSICN, the signed inverse condition number: 1 for an ideal
        /// element, 0 for a degenerate one and negative for an inverted one. The sweep additionally
        /// refuses to emit inverted elements, because it builds those elements itself; a mesh that
        /// came out of gmsh is only reported on, since rejecting it would throw away a mesh the
        /// mesher considers acceptable.
        /// </summary>
        private void ReportMeshQuality()
        {
            try
            {
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] nodeTags;
                string elementKind = "solid";
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 3, -1);
                if (elementTags.Length == 0)
                {
                    Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, -1);
                    elementKind = "shell";
                }
                //
                int count = 0;
                for (int i = 0; i < elementTags.Length; i++) if (elementTags[i] != null) count += elementTags[i].Length;
                if (count == 0) return;
                //
                int index = 0;
                IntPtr[] allElementTags = new IntPtr[count];
                for (int i = 0; i < elementTags.Length; i++)
                {
                    Array.Copy(elementTags[i], 0, allElementTags, index, elementTags[i].Length);
                    index += elementTags[i].Length;
                }
                //
                double[] qualities;
                Gmsh.Model.Mesh.GetElementQualities(allElementTags, out qualities, "minSICN");
                if (qualities == null || qualities.Length != count) return;
                //
                double sum = 0;
                double minimum = double.MaxValue;
                int numInvalid = 0;
                for (int i = 0; i < qualities.Length; i++)
                {
                    if (qualities[i] < minimum) minimum = qualities[i];
                    sum += qualities[i];
                    if (qualities[i] <= 0) numInvalid++;
                }
                //
                _writeOutput?.Invoke("Number of " + elementKind + " elements: " + count);
                _writeOutput?.Invoke("Element quality (min SICN): min " + Math.Round(minimum, 4) +
                                     ", average " + Math.Round(sum / count, 4));
                if (numInvalid > 0)
                    _writeOutput?.Invoke("Warning: " + numInvalid + " inverted or degenerate element/s.");
            }
            catch (Exception ex)
            {
                _writeOutput?.Invoke("The mesh quality could not be evaluated: " + ex.Message);
            }
        }
        public static void GetSurfaceItems(out Dictionary<int, int[]> surfaceIdEdgeIds,
                                            out Dictionary<int, int[]> surfaceIdVertexIds)
        {
            Tuple<int, int>[] surfaceDimTags = Gmsh.Model.OCC.GetEntities(2);
            //
            int[] edgeIds;
            int[] vertexIds;
            HashSet<int> allVertexIds;
            surfaceIdEdgeIds = new Dictionary<int, int[]>();
            surfaceIdVertexIds = new Dictionary<int, int[]>();
            //
            foreach (var surfaceDimTag in surfaceDimTags)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceDimTag.Item2, out _, out edgeIds);
                surfaceIdEdgeIds.Add(surfaceDimTag.Item2, edgeIds);
                //
                allVertexIds = new HashSet<int>();
                foreach (var edgeId in edgeIds)
                {
                    Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                    allVertexIds.UnionWith(vertexIds);
                }
                surfaceIdVertexIds.Add(surfaceDimTag.Item2, allVertexIds.ToArray());
            }
        }
        private void SetTransfiniteSurfaces(bool transfiniteThreeSided, bool transfiniteFourSided, bool transfiniteVolume,
                                            bool recombine, GmshAlgorithmRecombineEnum recombineAlgorithm)
        {
            int[] volumeIds;
            int[] edgeIds;
            int[][] edgeVertexIds;
            int[] vertexIds;
            HashSet<int> surfaceVertexIds = new HashSet<int>();
            //bool invertEdge;
            int edgeDim = 1;
            int edgeId;
            int surfaceDim = 2;
            int surfaceId;
            int volumeId;
            double edgeLength;
            double[] edgeLengths;
            //
            GmshEdge edge;
            GmshEdge existingEdge;
            Dictionary<int, GmshEdge> edgeIdEdge = new Dictionary<int, GmshEdge>();
            GmshSurface surface;
            Dictionary<int, GmshSurface> surfaceIdSurface = new Dictionary<int, GmshSurface>();
            GmshVolume volume;
            GmshVolume existingVolume;
            Dictionary<int, GmshVolume> volumeIdVolume = new Dictionary<int, GmshVolume>();
            //
            Tuple<int, int>[] vertexDimTags;
            Tuple<int, int>[] edgeDimTags;
            Tuple<int, int>[] surfaceDimTags;
            Tuple<int, int>[] volumeDimTags;
            //
            Gmsh.Model.GetEntities(out vertexDimTags, 0);
            Gmsh.Model.GetEntities(out edgeDimTags, 1);
            Gmsh.Model.GetEntities(out surfaceDimTags, 2);
            Gmsh.Model.GetEntities(out volumeDimTags, 3);
            // Collect volume faces
            foreach (var surfaceDimTag in surfaceDimTags)
            {
                surfaceId = surfaceDimTag.Item2;
                if (surfaceIdSurface.ContainsKey(surfaceId)) continue;
                //
                Gmsh.Model.GetAdjacencies(surfaceDim, surfaceId, out volumeIds, out edgeIds);
                // Get edge orientations
                //Gmsh.GetBoundary(new Tuple<int, int>[] { new Tuple<int, int>(surfaceDim, surfaceId) },
                //                 out edgeDimTags, false, true, false);
                // Surface
                surfaceVertexIds.Clear();
                edgeVertexIds = new int[edgeIds.Length][];
                edgeLengths = new double[edgeIds.Length];
                for (int j = 0; j < edgeIds.Length; j++)
                {
                    edgeId = edgeIds[j];
                    //
                    Gmsh.Model.GetAdjacencies(edgeDim, edgeId, out _, out vertexIds);
                    //
                    Array.Sort(vertexIds);
                    //
                    if (_isOCC) Gmsh.Model.OCC.GetMass(edgeDim, edgeId, out edgeLength);
                    else edgeLength = 1;
                    edge = new GmshEdge(edgeId, vertexIds, surfaceId, edgeLength);
                    edgeLengths[j] = edgeLength;
                    //
                    if (edgeIdEdge.TryGetValue(edgeId, out existingEdge))
                        existingEdge.SurfaceIds.UnionWith(edge.SurfaceIds);
                    else edgeIdEdge.Add(edge.Id, edge);
                    //
                    edgeVertexIds[j] = edge.VertexIds;
                    surfaceVertexIds.UnionWith(vertexIds);
                }
                //
                for (int i = 0; i < volumeIds.Length; i++)
                {
                    volumeId = volumeIds[i];
                    volume = new GmshVolume(volumeId, surfaceId);
                    //
                    if (volumeIdVolume.TryGetValue(volumeId, out existingVolume))
                        existingVolume.SurfaceIds.UnionWith(volume.SurfaceIds);
                    else volumeIdVolume.Add(volume.Id, volume);
                }
                //
                surface = new GmshSurface(surfaceId, surfaceVertexIds.ToArray(), edgeIds, edgeLengths, edgeVertexIds);
                surfaceIdSurface.Add(surface.Id, surface);
            }
            // Check if a volume is a transfinite volume                                                                            
            foreach (var entry in volumeIdVolume)
            {
                volumeId = entry.Key;
                volume = entry.Value;
                //
                if (!transfiniteVolume) volume.Transfinite = false;
                else if (volumeId > 0) volume.Transfinite = true;
                else volume.Transfinite = false;
                //
                if (volume.SurfaceIds.Count == 5 || volume.SurfaceIds.Count == 6)
                {
                    foreach (var volSurfaceId in volume.SurfaceIds)
                    {
                        surface = surfaceIdSurface[volSurfaceId];
                        if (surface.Transfinite)
                        {
                            if (surface.ThreeSided && transfiniteThreeSided) volume.TriSurfIds.Add(volSurfaceId);
                            else if (surface.FourSided && transfiniteFourSided) volume.QuadSurfIds.Add(volSurfaceId);
                        }
                        else { volume.Transfinite = false; break; }
                    }
                    if (volume.Transfinite)
                    {
                        if (!(volume.NumQuadSurfaces == 6 || (volume.NumTriSurfaces == 2 && volume.NumQuadSurfaces == 3)))
                            volume.Transfinite = false;
                    }
                }
                else volume.Transfinite = false;
                //
                if (volume.Transfinite)
                {
                    foreach (var volumeSurfaceId in volume.SurfaceIds) surfaceIdSurface[volumeSurfaceId].Recombine = true;
                }
                // Fix 5 sided volumes
                if (volume.Transfinite && volume.SurfaceIds.Count == 5)
                {
                    HashSet<int> connectingEdgeIds = new HashSet<int>();
                    foreach (var quadSurfaceId in volume.QuadSurfIds)
                        connectingEdgeIds.UnionWith(surfaceIdSurface[quadSurfaceId].EdgeIds);
                    foreach (var triSurfaceId in volume.TriSurfIds)
                        connectingEdgeIds.ExceptWith(surfaceIdSurface[triSurfaceId].EdgeIds);
                    //
                    surface = surfaceIdSurface[volume.TriSurfIds[0]];
                    int firstSurfaceVertexId = surface.VertexIds[0];
                    int secondSurfaceVertexId = -1;
                    //
                    foreach (var connectingEdgeId in connectingEdgeIds)
                    {
                        edge = edgeIdEdge[connectingEdgeId];
                        if (edge.VertexIds[0] == firstSurfaceVertexId) { secondSurfaceVertexId = edge.VertexIds[1]; break; }
                        else if (edge.VertexIds[1] == firstSurfaceVertexId) { secondSurfaceVertexId = edge.VertexIds[0]; break; }
                    }
                    //
                    if (secondSurfaceVertexId != -1)
                    {
                        surface = surfaceIdSurface[volume.TriSurfIds[1]];
                        surface.SetFirstVertexId(secondSurfaceVertexId);
                    }
                }
            }
            // Edge graph
            Node<GmshEdge> edgeNode;
            Graph<GmshEdge> edgeGraph = new Graph<GmshEdge>();
            Dictionary<int, Node<GmshEdge>> edgeIdNodeEdge = new Dictionary<int, Node<GmshEdge>>();
            // Add opposite edges as connections to graph
            foreach (var entry in surfaceIdSurface)
            {
                surface = entry.Value;
                if (surface.Transfinite)
                {
                    // Add nodes to graph
                    for (int i = 0; i < surface.EdgeIds.Length; i++)
                    {
                        edgeId = surface.EdgeIds[i];
                        if (!edgeIdNodeEdge.ContainsKey(edgeId))
                        {
                            edgeNode = new Node<GmshEdge>(edgeIdEdge[edgeId]);
                            edgeIdNodeEdge.Add(edgeId, edgeNode);
                            edgeGraph.AddNode(edgeNode);
                        }
                    }
                    // Add connections to graph
                    edgeGraph.AddUndirectedEdge(edgeIdNodeEdge[surface.OppositeEdgesA[0]],
                                                edgeIdNodeEdge[surface.OppositeEdgesA[1]]);
                    if (surface.OppositeEdgesB != null)
                    {
                        edgeGraph.AddUndirectedEdge(edgeIdNodeEdge[surface.OppositeEdgesB[0]],
                                                    edgeIdNodeEdge[surface.OppositeEdgesB[1]]);
                    }
                }
                
            }
            // Get groups of connected edges
            List<Graph<GmshEdge>> edgeGroups = edgeGraph.GetConnectedSubgraphs();
            //
            int min;
            int max;
            int sum;
            int maxByRefinement;
            int numOfElements;
            int numOfNodes;
            int avgNumOfNodes;
            HashSet<int> groupEdgeIds;
            IntPtr[] nodeTagsIntPtr;
            double[] coor;
            // Create edge mesh to get the number of nodes for each edge
            GenerateMesh(1);
            //
            Dictionary<int, int> edgeIdNumOfNodes = new Dictionary<int, int>();
            foreach (var edgeGroup in edgeGroups)
            {
                if (edgeGroup.Nodes.Count <= 1) continue;
                //
                min = int.MaxValue;
                max = -int.MaxValue;
                maxByRefinement = -int.MaxValue;
                sum = 0;
                groupEdgeIds = new HashSet<int>();
                //
                foreach (var edgeNodeFromGroup in edgeGroup.Nodes)
                {
                    // Mesh refinement
                    if (_gmshData.EdgeIdNumElements.TryGetValue(edgeNodeFromGroup.Value.Id, out numOfElements)) // must be here
                    {
                        numOfNodes = numOfElements + 1;
                        if (numOfNodes > maxByRefinement) maxByRefinement = numOfNodes;
                    }
                    else
                    {
                        // Only consider if there is no refinement found jet
                        if (maxByRefinement < 0)
                        {
                            Gmsh.Model.Mesh.GetNodes(out nodeTagsIntPtr, out coor, 1, edgeNodeFromGroup.Value.Id, true, false);
                            //
                            numOfNodes = nodeTagsIntPtr.Length;      // for 2 nodes there is only 1 element
                            if (numOfNodes < min) min = numOfNodes;
                            if (numOfNodes > max) max = numOfNodes;
                            sum += numOfNodes;
                        }
                    }
                }
                //
                if (maxByRefinement > 0) avgNumOfNodes = maxByRefinement;
                else avgNumOfNodes = (int)Math.Round((double)sum / edgeGroup.Nodes.Count, 0, MidpointRounding.AwayFromZero);
                //avgNumOfNodes = (int)Math.Round((min + max) / 2.0, 0, MidpointRounding.AwayFromZero);
                //
                foreach (var edgeNodeFromGroup in edgeGroup.Nodes)
                {
                    //if (avgNumOfNodes % 2 == 0)
                    //{
                    //    if (avgNumOfNodes >= 3) avgNumOfNodes -= 1;
                    //    else avgNumOfNodes += 1;
                    //}
                    // Change number of elements to an even number
                    if (recombine && (recombineAlgorithm == GmshAlgorithmRecombineEnum.SimpleFullQuad ||
                                      recombineAlgorithm == GmshAlgorithmRecombineEnum.BlossomFullQuad))
                    {
                        if (avgNumOfNodes % 2 == 0) avgNumOfNodes--;
                        if (avgNumOfNodes < 3) avgNumOfNodes = 3;
                    }
                    //
                    Gmsh.Model.Mesh.SetTransfiniteCurve(edgeNodeFromGroup.Value.Id, avgNumOfNodes);
                    edgeIdNumOfNodes[edgeNodeFromGroup.Value.Id] = avgNumOfNodes;
                }
            }
            // Sweep mesh layers
            int count;
            if (_gmshData.EdgeIdsBySweepLayer != null)
            {
                foreach (var layer in _gmshData.EdgeIdsBySweepLayer)
                {
                    sum = 0;
                    count = 0;
                    foreach (var sweepEdgeId in layer)
                    {
                        if (edgeIdNumOfNodes.TryGetValue(sweepEdgeId, out numOfNodes))
                        {
                            sum += numOfNodes;
                            count++;
                        }
                    }
                    // A sweep layer whose edges all sit on faces that are neither 3 nor 4 sided -
                    // which is what a split source face produces - contributes nothing here. Without
                    // the guard the average is 0/0, and the resulting node count collapses the whole
                    // sweep direction to a single element. The sweep sets these edges itself later.
                    if (count == 0) continue;
                    avgNumOfNodes = (int)Math.Round((double)sum / count, 0, MidpointRounding.AwayFromZero);
                    //
                    foreach (var sweepEdgeId in layer)
                    {
                        Gmsh.Model.Mesh.SetTransfiniteCurve(sweepEdgeId, avgNumOfNodes);
                    }
                }
            }
            //
            Gmsh.Model.Mesh.Clear(); // must clear the mesh
            //
            foreach (var entry in surfaceIdSurface)
            {
                surface = entry.Value;
                // The source of a block that carries imprints, and every target, is meshed free, whatever its shape
                if (_freeMeshedSurfaceIds.Contains(surface.Id)) continue;
                //
                if (surface.Transfinite)
                {
                    // "Left", "Right", "AlternateLeft" and "AlternateRight"
                    if (surface.ThreeSided && transfiniteThreeSided)
                        Gmsh.Model.Mesh.SetTransfiniteSurface(surface.Id, "AlternateLeft", surface.VertexIds);
                    else if (surface.FourSided && transfiniteFourSided)
                        Gmsh.Model.Mesh.SetTransfiniteSurface(surface.Id, "AlternateLeft");
                    //
                    if (recombine && surface.Recombine) Gmsh.Model.Mesh.SetRecombine(2, surface.Id);
                    //
                    //Gmsh.Model.Mesh.SetSmoothing(2, surface.Id, 100);
                }
            }
            //
            foreach (var entry in volumeIdVolume)
            {
                if (entry.Value.Transfinite) Gmsh.Model.Mesh.SetTransfiniteVolume(entry.Key);
                //Gmsh.Mesh.SetOutwardOrientation(volumeDimTag.Item2);
            }
        }
        private void GenerateMesh(int dim)
        {
            Gmsh.Model.Mesh.Generate(0);
            if (dim == 1) Gmsh.Model.Mesh.Generate(1);
            if (dim == 2) Gmsh.Model.Mesh.Generate(2);
            if (dim == 3) Gmsh.Model.Mesh.Generate(3);
            return;
        }
        // Background methods
        private void GetOccNormalsBackground()
        {
            try
            {
                if (_gmshData.FaceIdNodes == null || _gmshData.FaceIdNodes.Count == 0) return;
                //
                Tuple<int, int>[] outDimTags;
                Gmsh.Model.OCC.ImportShapes(_gmshData.GeometryFileName, out outDimTags, false, "");
                //
                Synchronize(); // must be here
                //
                RenumberGmshDataByCoor();
                //
                int faceTag;
                FeNode[] nodes;
                double[] normal;
                double[] concatenatedCoor;
                List<Vec3D> normals;
                Dictionary<int, List<Vec3D>> nodeIdNormals = new Dictionary<int, List<Vec3D>>();
                //
                foreach (var entry in _gmshData.FaceIdNodes)
                {
                    faceTag = entry.Key;
                    nodes = entry.Value;
                    //
                    concatenatedCoor = new double[3 * nodes.Length];
                    for (int i = 0; i < nodes.Length; i++)
                        Array.Copy(nodes[i].Coor, 0, concatenatedCoor, 3 * i, 3);
                    double[] concatenatedParametricCoord;
                    Gmsh.Model.GetParametrization(2, faceTag, concatenatedCoor, out concatenatedParametricCoord);
                    double[] concatenatedNormals;
                    Gmsh.Model.GetNormal(faceTag, concatenatedParametricCoord, out concatenatedNormals);
                    //
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        normal = new double[3];
                        Array.Copy(concatenatedNormals, 3 * i, normal, 0, 3);
                        //
                        if (nodeIdNormals.TryGetValue(nodes[i].Id, out normals)) normals.Add(new Vec3D(normal));
                        else nodeIdNormals.Add(nodes[i].Id, new List<Vec3D>() { new Vec3D(normal) });
                    }
                }
                _gmshData.NodeIdNormals = nodeIdNormals;
                _error = null;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }
        private void GetElementQualitiesBackground()
        {
            try
            {
                Gmsh.Open(_gmshData.MeshFileName);
                //
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] nodeTags;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, -1, -1);
                // Merge elements of different types
                HashSet<IntPtr> allElementTags = new HashSet<IntPtr> { };
                for (int i = 0; i < elementTags.Length; i++) allElementTags.UnionWith(elementTags[i]);
                // Get quality
                double[] qualities;
                IntPtr[] elementIntPtr = allElementTags.ToArray();
                Gmsh.Model.Mesh.GetElementQualities(elementIntPtr, out qualities, _gmshData.ElementQualityMetric);
                // Create result
                _gmshData.ElementQuality = new Dictionary<int, double>();
                for (int i = 0; i < allElementTags.Count; i++)
                {
                    _gmshData.ElementQuality.Add(elementIntPtr[i].ToInt32(), qualities[i]);
                }
                //
                _error = null;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }
        private void DefeatureBackground()
        {
            try
            {
                if (_gmshData.SurfaceIds == null || _gmshData.SurfaceIds.Length == 0) return;
                // With OCCAutoFix on, Gmsh defeatures a ShapeFix_Solid copy that no longer contains the faces to remove
                Gmsh.Option.SetNumber("Geometry.OCCAutoFix", 0);
                //
                Tuple<int, int>[] outDimTags;
                Gmsh.Model.OCC.ImportShapes(_gmshData.GeometryFileName, out outDimTags, false, "");
                //
                Synchronize(); // must be here
                //
                RenumberGmshDataByCoor();
                //
                int[] volumeTags = new int[] { 1 };
                int[] surfaceTags = _gmshData.SurfaceIds;
                //
                Gmsh.Model.OCC.Defeature(volumeTags, surfaceTags, out outDimTags, true);
                //
                Synchronize();
                //
                Gmsh.Write(_gmshData.GeometryFileName);
                //
                _error = null;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }
        private void GetCoordinatesFromParameterizationBackground()
        {
            try
            {
                Gmsh.Open(_gmshData.GeometryFileName);
                //
                Tuple<int, int>[] surfaceDimTags;
                Gmsh.Model.GetEntities(out surfaceDimTags, 2);
                //
                int count = 0;
                int tag;
                double[] minBounds;
                double[] maxBounds;
                _gmshData.ParameterizationLinesU = new double[surfaceDimTags.Length][][][];
                _gmshData.ParameterizationLinesV = new double[surfaceDimTags.Length][][][];
                foreach (var entry in surfaceDimTags)
                {
                    tag = entry.Item2;
                    //
                    Gmsh.Model.GetParametrizationBounds(2, tag, out minBounds, out maxBounds);
                    //
                    _gmshData.ParameterizationLinesU[count] =
                        GetParameterizationLines(tag, true, _parameterizationULineCount, minBounds, maxBounds);
                    _gmshData.ParameterizationLinesV[count] =
                        GetParameterizationLines(tag, false, _parameterizationVLineCount, minBounds, maxBounds);
                    count += 1;
                }
                _error = null;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }
        private double[][][] GetParameterizationLines(int tag, bool constantU, int lineCount,
                                                      double[] minBounds, double[] maxBounds)
        {
            List<double[][]> lines = new List<double[][]>();
            double fixedMin = constantU ? minBounds[0] : minBounds[1];
            double fixedMax = constantU ? maxBounds[0] : maxBounds[1];
            double variableMin = constantU ? minBounds[1] : minBounds[0];
            double variableMax = constantU ? maxBounds[1] : maxBounds[0];
            double fixedStep = (fixedMax - fixedMin) / (lineCount + 1);
            double variableStep = (variableMax - variableMin) / (_parameterizationLinePointCount - 1);
            //
            double fixedCoor;
            double variableCoor;
            double[] parametricCoor;
            double[] previousParametricCoor;
            bool inside;
            bool previousInside;
            List<double[]> parametricLine;
            for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
                fixedCoor = fixedMin + fixedStep * (lineIndex + 1);
                previousParametricCoor = null;
                previousInside = false;
                parametricLine = null;
                //
                for (int pointIndex = 0; pointIndex < _parameterizationLinePointCount; pointIndex++)
                {
                    variableCoor = variableMin + variableStep * pointIndex;
                    if (constantU) parametricCoor = new double[] { fixedCoor, variableCoor };
                    else parametricCoor = new double[] { variableCoor, fixedCoor };
                    inside = Gmsh.Model.IsInside(2, tag, parametricCoor, true) > 0;
                    //
                    if (inside)
                    {
                        if (!previousInside)
                        {
                            parametricLine = new List<double[]>();
                            if (previousParametricCoor != null)
                            {
                                parametricLine.Add(GetInsideBoundaryParameterization(tag, parametricCoor,
                                                                                    previousParametricCoor));
                            }
                        }
                        parametricLine.Add(parametricCoor);
                    }
                    else if (previousInside)
                    {
                        parametricLine.Add(GetInsideBoundaryParameterization(tag, previousParametricCoor,
                                                                            parametricCoor));
                        AddParameterizationLine(tag, parametricLine, lines);
                        parametricLine = null;
                    }
                    //
                    previousParametricCoor = parametricCoor;
                    previousInside = inside;
                }
                if (parametricLine != null) AddParameterizationLine(tag, parametricLine, lines);
            }
            return lines.ToArray();
        }
        private double[] GetInsideBoundaryParameterization(int tag, double[] insideCoor, double[] outsideCoor)
        {
            insideCoor = insideCoor.ToArray();
            outsideCoor = outsideCoor.ToArray();
            double[] middleCoor = new double[2];
            for (int i = 0; i < 30; i++)
            {
                middleCoor[0] = 0.5 * (insideCoor[0] + outsideCoor[0]);
                middleCoor[1] = 0.5 * (insideCoor[1] + outsideCoor[1]);
                if (Gmsh.Model.IsInside(2, tag, middleCoor, true) > 0)
                {
                    insideCoor[0] = middleCoor[0];
                    insideCoor[1] = middleCoor[1];
                }
                else
                {
                    outsideCoor[0] = middleCoor[0];
                    outsideCoor[1] = middleCoor[1];
                }
            }
            return insideCoor;
        }
        private void AddParameterizationLine(int tag, List<double[]> parametricLine, List<double[][]> lines)
        {
            if (parametricLine.Count < 2) return;
            //
            int pointCount = (parametricLine.Count - 1) * _parameterizationLineSubdivisionCount + 1;
            double[] parametricCoor = new double[2 * pointCount];
            int count = 0;
            double ratio;
            for (int i = 0; i < parametricLine.Count - 1; i++)
            {
                for (int j = 0; j < _parameterizationLineSubdivisionCount; j++)
                {
                    ratio = j / (double)_parameterizationLineSubdivisionCount;
                    parametricCoor[2 * count] = parametricLine[i][0] * (1 - ratio) +
                                                parametricLine[i + 1][0] * ratio;
                    parametricCoor[2 * count + 1] = parametricLine[i][1] * (1 - ratio) +
                                                    parametricLine[i + 1][1] * ratio;
                    count++;
                }
            }
            parametricCoor[2 * count] = parametricLine[parametricLine.Count - 1][0];
            parametricCoor[2 * count + 1] = parametricLine[parametricLine.Count - 1][1];
            //
            double[] coor1D;
            Gmsh.Model.GetValue(2, tag, parametricCoor, out coor1D);
            //
            double[][] coor = new double[coor1D.Length / 3][];
            for (int i = 0; i < coor.Length; i++)
            {
                coor[i] = new double[] { coor1D[3 * i], coor1D[3 * i + 1], coor1D[3 * i + 2] };
            }
            lines.Add(coor);
        }
        // Tools                                                                                                                    
        private void RenumberGmshDataByCoor()
        {
            Dictionary<int, int> netgenVertexIdGmshVertexId = RenumberVertices();
            Dictionary<int, int> netgenEdgeIdGmshEdgeId = RenumberEdges(netgenVertexIdGmshVertexId);
            Dictionary<int, int> netgenFaceIdGmshFaceId = RenumberFaces(netgenVertexIdGmshVertexId);
            // Renumber mesh data                                                                                                   
            // Vertex mesh size
            if (_gmshData.VertexNodeIdMeshSize != null)
            {
                int vertexId;
                Dictionary<int, double> vertexIdMeshSize = new Dictionary<int, double>();
                foreach (var entry in _gmshData.VertexNodeIdMeshSize)
                {
                    if (netgenVertexIdGmshVertexId.TryGetValue(entry.Key, out vertexId))
                        vertexIdMeshSize[vertexId] = entry.Value;
                    else if (Debugger.IsAttached) Debugger.Break();
                }
                _gmshData.VertexNodeIdMeshSize = vertexIdMeshSize;
            }
            // Edge mesh size
            if (_gmshData.EdgeIdNumElements != null)
            {
                int edgeId;
                int numOfUnmappedEdges = 0;
                Dictionary<int, int> edgeIdNumElements = new Dictionary<int, int>();
                foreach (var entry in _gmshData.EdgeIdNumElements)
                {
                    if (netgenEdgeIdGmshEdgeId.TryGetValue(entry.Key, out edgeId))
                        edgeIdNumElements[edgeId] = entry.Value;
                    else numOfUnmappedEdges++;  // an edge of a part that is not being meshed
                }
                // The unmapped edges are silently dropped, so report the count the user can check against
                if (_gmshData.EdgeIdNumElements.Count > 0)
                {
                    _writeOutput?.Invoke("Local mesh size: " + edgeIdNumElements.Count + " edge/s constrained, " +
                                         numOfUnmappedEdges + " edge/s not on the meshed part.");
                }
                _gmshData.EdgeIdNumElements = edgeIdNumElements;
            }
            // Normals
            if (_gmshData.FaceIdNodes != null)
            {
                int faceId;
                Dictionary<int, FeNode[]> faceIdNodes = new Dictionary<int, FeNode[]>();
                foreach (var entry in _gmshData.FaceIdNodes)
                {
                    if (netgenFaceIdGmshFaceId.TryGetValue(entry.Key, out faceId))
                        faceIdNodes[faceId] = entry.Value;
                    else if (Debugger.IsAttached) Debugger.Break();
                }
                _gmshData.FaceIdNodes = faceIdNodes;
            }
            // Defeature
            if (_gmshData.SurfaceIds != null)
            {
                int faceId;
                List<int> surfaceIdsList = new List<int>();
                foreach (var surfaceId in _gmshData.SurfaceIds)
                {
                    if (netgenFaceIdGmshFaceId.TryGetValue(surfaceId, out faceId))
                        surfaceIdsList.Add(faceId);
                    else if (Debugger.IsAttached) Debugger.Break();
                }
                _gmshData.SurfaceIds = surfaceIdsList.ToArray();
            }
            // Sweep data
            if (_gmshData.GmshSetupItems != null)
            {
                foreach (var setupItem in _gmshData.GmshSetupItems)
                {
                    if (setupItem is SweepMesh sw)
                    {
                        RenumberSurfaceIds(sw.BlockSourceSurfaceIds, netgenFaceIdGmshFaceId);
                        RenumberSurfaceIds(sw.BlockSideSurfaceIds, netgenFaceIdGmshFaceId);
                        RenumberSurfaceIds(sw.BlockTargetSurfaceIds, netgenFaceIdGmshFaceId);
                        //
                        int edgeId;
                        for (int i = 0; i < sw.LayerGroupEdgeIds.Length; i++)
                        {
                            for (int j = 0; j < sw.LayerGroupEdgeIds[i].Length; j++)
                            {
                                for (int z = 0; z < sw.LayerGroupEdgeIds[i][j].Length; z++)
                                {
                                    if (netgenEdgeIdGmshEdgeId.TryGetValue(sw.LayerGroupEdgeIds[i][j][z], out edgeId))
                                        sw.LayerGroupEdgeIds[i][j][z] = edgeId;
                                    else if (Debugger.IsAttached) Debugger.Break();
                                }
                            }
                        }
                    }
                }
            }
        }
        private static void RenumberSurfaceIds(int[][] surfaceIds, Dictionary<int, int> netgenFaceIdGmshFaceId)
        {
            if (surfaceIds == null) return;
            //
            int faceId;
            for (int i = 0; i < surfaceIds.Length; i++)
            {
                for (int j = 0; j < surfaceIds[i].Length; j++)
                {
                    if (netgenFaceIdGmshFaceId.TryGetValue(surfaceIds[i][j], out faceId)) surfaceIds[i][j] = faceId;
                    else if (Debugger.IsAttached) Debugger.Break();
                }
            }
        }
        private void PrepareData(GmshSetupItem gmshSetupItem)
        {
            // Gather sweep edge ids by layer
            _gmshData.EdgeIdsBySweepLayer = null;
            if (gmshSetupItem is SweepMesh sm)
            {
                List<HashSet<int>> rings = new List<HashSet<int>>();
                for (int i = 0; i < sm.LayerGroupEdgeIds.Length; i++)
                {
                    HashSet<int> ring = new HashSet<int>();
                    for (int j = 0; j < sm.LayerGroupEdgeIds[i].Length; j++) ring.UnionWith(sm.LayerGroupEdgeIds[i][j]);
                    rings.Add(ring);
                }
                // Rings of two blocks that share a direction edge - the wall the blocks have in common -
                // are the same sweep lines, so they are one ring and get one layer count
                MergeOverlappingRings(rings);
                _gmshData.EdgeIdsBySweepLayer = rings.ToArray();
            }
        }
        private static void MergeOverlappingRings(List<HashSet<int>> rings)
        {
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < rings.Count && !merged; i++)
                {
                    for (int j = i + 1; j < rings.Count; j++)
                    {
                        if (!rings[i].Overlaps(rings[j])) continue;
                        //
                        rings[i].UnionWith(rings[j]);
                        rings.RemoveAt(j);
                        merged = true;
                        break;
                    }
                }
            }
        }
        private Dictionary<int, int> RenumberVertices()
        {
            // Vertices                                                                                                             
            double[] coor;
            Tuple<int, int>[] pointDimTags;
            Gmsh.Model.GetEntities(out pointDimTags, 0);
            Dictionary<int, double[]> pointIdCoor = new Dictionary<int, double[]>();
            foreach (var item in pointDimTags)
            {
                Gmsh.Model.GetValue(item.Item1, item.Item2, new double[3], out coor);
                pointIdCoor.Add(item.Item2, coor);
            }
            int minId;
            double minDistance;
            double dx;
            double dy;
            double dz;
            double[] coorN;
            double[] coorG;
            Dictionary<int, int> netgenVertexIdGmshVertexId = new Dictionary<int, int>();
            foreach (var netGenEntry in _gmshData.VertexNodes)
            {
                minId = -1;
                minDistance = double.MaxValue;
                coorN = netGenEntry.Value.Coor;
                foreach (var gmshEntry in pointIdCoor)
                {
                    coorG = gmshEntry.Value;
                    dx = Math.Abs(coorN[0] - coorG[0]);
                    if (dx <= minDistance)
                    {
                        dy = Math.Abs(coorN[1] - coorG[1]);
                        if (dy <= minDistance)
                        {
                            dz = Math.Abs(coorN[2] - coorG[2]);
                            if (dz <= minDistance)
                            {
                                minDistance = Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2);
                                minId = gmshEntry.Key;
                                if (minDistance == 0) break;
                            }
                        }
                    }
                }
                //
                netgenVertexIdGmshVertexId.Add(netGenEntry.Key, minId);
            }
            //
            return netgenVertexIdGmshVertexId;
        }
        private Dictionary<int, int> RenumberEdges(Dictionary<int, int> netgenVertexIdGmshVertexId)
        {
            // Edges                                                                                                                
            int[] vertexIds;
            CompareIntArray comparer = new CompareIntArray();
            List<GmshIdLocation> edgeIdDataList;
            Dictionary<int[], List<GmshIdLocation>> edgeVertexNodeIdsEdgeId =
                new Dictionary<int[], List<GmshIdLocation>>(comparer);
            // Renumber edge vertex node ids as keys
            foreach (var entry in _gmshData.EdgeVertexNodeIdsEdgeId)
            {
                vertexIds = new int[entry.Key.Length];
                for (int i = 0; i < vertexIds.Length; i++) vertexIds[i] = netgenVertexIdGmshVertexId[entry.Key[i]];
                Array.Sort(vertexIds);
                //
                if (edgeVertexNodeIdsEdgeId.TryGetValue(vertexIds, out edgeIdDataList)) edgeIdDataList.AddRange(entry.Value);
                else edgeVertexNodeIdsEdgeId.Add(vertexIds, entry.Value.ToList());  // create a copy to ba able to AddRange later
            }
            _gmshData.EdgeVertexNodeIdsEdgeId = edgeVertexNodeIdsEdgeId;
            // Edges - prepare a map of netgen edge id -> gmsh edge id                      
            int edgeId;
            int netgenEdgeId = -1;
            int gmshEdgeId = -1;
            double min;
            double d2;
            double x;
            double y;
            double z;
            double[] center;
            BoundingBox bb = new BoundingBox();
            Vec3D xyz;
            Vec3D cog;
            GmshIdLocation edgeIdData;
            List<GmshIdLocation> idLocationList;
            Tuple<int, int>[] dimTags;
            Gmsh.Model.GetEntities(out dimTags, 1);
            // Prepare Gmsh edge data by vertex ids
            Dictionary<int[], List<GmshIdLocation>> gmshEdgeVertexNodeIdsEdgeId =
                new Dictionary<int[], List<GmshIdLocation>>(comparer);
            foreach (var entry in dimTags)
            {
                edgeId = entry.Item2;
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                Array.Sort(vertexIds);
                //
                if (_isOCC) Gmsh.Model.OCC.GetCenterOfMass(1, edgeId, out x, out y, out z);
                else
                {
                    Gmsh.Model.GetBoundingBox(1, edgeId, out bb.MinX, out bb.MinY, out bb.MinZ,
                                              out bb.MaxX, out bb.MaxY, out bb.MaxZ);
                    center = bb.GetCenter();
                    x = center[0];
                    y = center[1];
                    z = center[2];
                }
                xyz = new Vec3D(x, y, z);
                //
                edgeIdData = new GmshIdLocation() { Id = edgeId, Location = xyz.Coor };
                if (gmshEdgeVertexNodeIdsEdgeId.TryGetValue(vertexIds, out edgeIdDataList)) edgeIdDataList.Add(edgeIdData);
                else gmshEdgeVertexNodeIdsEdgeId.Add(vertexIds, new List<GmshIdLocation>() { edgeIdData });
            }
            // Renumber edge ids
            Dictionary<int, int> netgenEdgeIdGmshEdgeId = new Dictionary<int, int>();
            foreach (var entry in _gmshData.EdgeVertexNodeIdsEdgeId)
            {
                vertexIds = entry.Key;
                //
                foreach (var edgeDataEntry in entry.Value)
                {
                    netgenEdgeId = edgeDataEntry.Id;
                    xyz = new Vec3D(edgeDataEntry.Location);
                    //
                    if (gmshEdgeVertexNodeIdsEdgeId.TryGetValue(vertexIds, out idLocationList))
                    {
                        if (idLocationList.Count() == 1) gmshEdgeId = idLocationList[0].Id;
                        else
                        {
                            min = double.MaxValue;
                            foreach (var idLocation in idLocationList)
                            {
                                cog = new Vec3D(idLocation.Location);
                                d2 = (cog - xyz).Len2;
                                if (d2 < min)
                                {
                                    min = d2;
                                    gmshEdgeId = idLocation.Id;
                                }
                            }
                        }
                        //
                        netgenEdgeIdGmshEdgeId[netgenEdgeId] = gmshEdgeId;
                    }
                }
            }
            return netgenEdgeIdGmshEdgeId;
        }
        private Dictionary<int, int> RenumberFaces(Dictionary<int, int> netgenVertexIdGmshVertexId)
        {
            // Faces                                                                                                                
            int[] vertexIds;
            CompareIntArray comparer = new CompareIntArray();
            List<GmshIdLocation> faceIdDataList;
            Dictionary<int[], List<GmshIdLocation>> faceVertexNodeIdsFaceId =
                new Dictionary<int[], List<GmshIdLocation>>(comparer);
            // Renumber face vertex node ids as keys
            foreach (var entry in _gmshData.FaceVertexNodeIdsFaceId)
            {
                vertexIds = new int[entry.Key.Length];
                for (int i = 0; i < vertexIds.Length; i++) vertexIds[i] = netgenVertexIdGmshVertexId[entry.Key[i]];
                Array.Sort(vertexIds);
                //
                if (faceVertexNodeIdsFaceId.TryGetValue(vertexIds, out faceIdDataList)) faceIdDataList.AddRange(entry.Value);
                else faceVertexNodeIdsFaceId.Add(vertexIds, entry.Value.ToList());  // create a copy to ba able to AddRange later
            }
            _gmshData.FaceVertexNodeIdsFaceId = faceVertexNodeIdsFaceId;
            // Vertices - prepare a map of netgen edge id -> gmsh edge id                      
            int faceId;
            int netgenFaceId = -1;
            int gmshFaceId = -1;
            int[] edgeIds;
            HashSet<int> faceVertexIds;
            double d2;
            double x;
            double y;
            double z;
            double size;
            double[] center;
            BoundingBox bb = new BoundingBox();
            Vec3D xyz;
            Vec3D cog;
            GmshIdLocation faceIdData;
            List<GmshIdLocation> idLocationList;
            Tuple<int, int>[] dimTags;
            Gmsh.Model.GetEntities(out dimTags, 2);
            // Prepare Gmsh edge data by vertex ids
            Dictionary<int[], List<GmshIdLocation>> gmshFaceVertexNodeIdsFaceId =
                new Dictionary<int[], List<GmshIdLocation>>(comparer);
            foreach (var entry in dimTags)
            {
                faceId = entry.Item2;
                faceVertexIds = new HashSet<int>();
                Gmsh.Model.GetAdjacencies(2, faceId, out _, out edgeIds);
                for (int i = 0; i < edgeIds.Length; i++)
                {
                    Gmsh.Model.GetAdjacencies(1, edgeIds[i], out _, out vertexIds);
                    faceVertexIds.UnionWith(vertexIds);
                }
                vertexIds = faceVertexIds.ToArray();
                Array.Sort(vertexIds);
                //
                if (_isOCC) Gmsh.Model.OCC.GetCenterOfMass(2, faceId, out x, out y, out z);
                else
                {
                    Gmsh.Model.GetBoundingBox(2, faceId, out bb.MinX, out bb.MinY, out bb.MinZ,
                                              out bb.MaxX, out bb.MaxY, out bb.MaxZ);
                    center = bb.GetCenter();
                    x = center[0];
                    y = center[1];
                    z = center[2];
                }
                xyz = new Vec3D(x, y, z);
                //
                if (_isOCC) Gmsh.Model.OCC.GetMass(2, faceId, out size);
                else size = -1;
                //
                faceIdData = new GmshIdLocation();
                faceIdData.Id = faceId;
                faceIdData.Size = size;
                faceIdData .Location = xyz.Coor;
                //
                if (gmshFaceVertexNodeIdsFaceId.TryGetValue(vertexIds, out faceIdDataList)) faceIdDataList.Add(faceIdData);
                else gmshFaceVertexNodeIdsFaceId.Add(vertexIds, new List<GmshIdLocation>() { faceIdData });
            }
            // Renumber face ids based on the most simillar of two criteria
            int count;
            int[] locationIdDiff;
            int[] sizeIdDiff;
            double[] locationDiff;
            double[] sizeDiff;
            Dictionary<int, int> netgenFaceIdGmshFaceId = new Dictionary<int, int>();
            foreach (var entry in _gmshData.FaceVertexNodeIdsFaceId)
            {
                vertexIds = entry.Key;
                //
                foreach (var faceDataEntry in entry.Value)
                {
                    netgenFaceId = faceDataEntry.Id;
                    xyz = new Vec3D(faceDataEntry.Location);
                    //
                    if (gmshFaceVertexNodeIdsFaceId.TryGetValue(vertexIds, out idLocationList))
                    {
                        if (idLocationList.Count() == 1) gmshFaceId = idLocationList[0].Id;
                        else
                        {
                            locationIdDiff = new int[idLocationList.Count()];
                            sizeIdDiff = new int[idLocationList.Count()];
                            locationDiff = new double[idLocationList.Count()];
                            sizeDiff = new double[idLocationList.Count()];
                            // Collect all differences
                            count = 0;
                            foreach (var idLocation in idLocationList)
                            {
                                cog = new Vec3D(idLocation.Location);
                                d2 = (cog - xyz).Len2;
                                locationIdDiff[count] = idLocation.Id;
                                locationDiff[count] = d2;
                                //
                                d2 = Math.Abs(faceDataEntry.Size - idLocation.Size);
                                sizeIdDiff[count] = idLocation.Id;
                                sizeDiff[count] = d2;
                                //
                                count++;
                            }
                            // Sort differences
                            Array.Sort(locationDiff, locationIdDiff);
                            Array.Sort(sizeDiff, sizeIdDiff);
                            // If different surfaces are found select by the smallest normalized criteria. Faces of
                            // the same size - the two halves of a cylinder split by its seam - leave the size
                            // criterion nothing to decide on, and normalizing by its zero spread would compare
                            // against NaN and let it win regardless; the location decides alone then
                            if (sizeDiff[sizeDiff.Length - 1] <= 1E-6 * Math.Max(Math.Abs(faceDataEntry.Size), 1E-30))
                                gmshFaceId = locationIdDiff[0];
                            else if (locationIdDiff[0] != sizeIdDiff[0])
                            {
                                // Normalize differences
                                for (int i = 0; i < locationDiff.Length; i++)
                                {
                                    locationDiff[i] /= locationDiff[locationDiff.Length - 1];
                                    sizeDiff[i] /= sizeDiff[locationIdDiff.Length - 1];
                                }
                                //
                                if (locationDiff[0] < sizeDiff[0]) gmshFaceId = locationIdDiff[0];
                                else gmshFaceId = sizeIdDiff[0];
                            }
                            else gmshFaceId = locationIdDiff[0];
                        }
                        //
                        netgenFaceIdGmshFaceId[netgenFaceId] = gmshFaceId;
                    }
                }
            }
            return netgenFaceIdGmshFaceId;
        }
        private void Synchronize()
        {
            if (_isOCC) Gmsh.Model.OCC.Synchronize();
            else Gmsh.Model.Geo.Synchronize();
        }
        private void WriteLog()
        {
            if (Gmsh.IsInitialized() == 1)
            {
                string[] loggerLines = Gmsh.Logger.Get();
                if (loggerLines != null && loggerLines.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = _currentLogLine; i < loggerLines.Length; i++)
                        sb.AppendLine(loggerLines[i]);
                    //
                    _currentLogLine = loggerLines.Length;
                    //
                    _writeOutput?.Invoke(sb.ToString());
                }
            }
        }
    }
}
