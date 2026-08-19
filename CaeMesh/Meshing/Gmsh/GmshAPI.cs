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
using CaeMesh.Meshing;
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
        private bool _isOCC;
        private string _error;
        private Thread _thread;
        private int _currentLogLine;
        private int _parameterizationULineCount;
        private int _parameterizationVLineCount;
        private const int _parameterizationLinePointCount = 11;
        private const int _parameterizationLineSubdivisionCount = 2;


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
                if (_error != null) File.Delete(_gmshData.InpFileName);
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
        private void SetMeshSizes(GmshSetupItem gmshSetupItem)
        {
            GmshAlgorithmMesh2DEnum algorithm2D = gmshSetupItem.AlgorithmMesh2D;
            // Mesh size
            //Tuple<int, int>[] surfaceDimTags;
            //Gmsh.GetEntities(out surfaceDimTags, 2);
            //foreach (var surfaceDimTag in surfaceDimTags) Gmsh.Mesh.SetSizeFromBoundary(2, surfaceDimTag.Item2, 0);
            //
            double sf = 1;
            if (algorithm2D == GmshAlgorithmMesh2DEnum.QuasiStructuredQuad) sf = 2;
            //
            Gmsh.Option.SetNumber("Mesh.MeshSizeMin", _gmshData.PartMeshingParameters.MinH * sf);
            Gmsh.Option.SetNumber("Mesh.MeshSizeMax", _gmshData.PartMeshingParameters.MaxH * sf);
            double cs = 2 * Math.PI * _gmshData.PartMeshingParameters.ElementsPerCurve / sf;
            Gmsh.Option.SetNumber("Mesh.MeshSizeFromCurvature", cs);
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
                ApplyTransfiniteCurve(edgeId, numOfNodes, null);
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
            int surfaceId;
            HashSet<int> sourceSurfaceIds = new HashSet<int>();
            bool recombine = sweepMesh.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            // Get source surfaces
            for (int i = 0; i < sweepMesh.CreationIds.Length; i++)
            {
                surfaceId = FeMesh.GetItemIdFromGeometryId(sweepMesh.CreationIds[i]) + 1;
                sourceSurfaceIds.Add(surfaceId);
                //Gmsh.Model.Mesh.SetSmoothing(2, surfaceId, 100); // smoothing
            }
            // Get surface item ids
            Dictionary<int, int[]> surfaceIdEdgeIds;
            Dictionary<int, int[]> surfaceIdVertexIds;
            GetSurfaceItems(out surfaceIdEdgeIds, out surfaceIdVertexIds);
            // Get side surfaces
            HashSet<int> sideSurfaceIds = new HashSet<int>(sweepMesh.SideSurfaceIds);
            HashSet<int> targetSurfaceIds = new HashSet<int>();
            // Get target surfaces
            targetSurfaceIds.UnionWith(surfaceIdVertexIds.Keys);
            targetSurfaceIds.ExceptWith(sourceSurfaceIds);
            targetSurfaceIds.ExceptWith(sideSurfaceIds);
            // The mesh is built by projecting onto every target surface and keeping the nearest hit,
            // so a target that is split into several faces is allowed
            if (targetSurfaceIds.Count < 1)
                throw new CaeException("No sweep target surface could be determined.");
            // The volume the solid elements are classified on
            Tuple<int, int>[] volumeDimTags;
            Gmsh.Model.GetEntities(out volumeDimTags, 3);
            if (volumeDimTags.Length != 1)
                throw new CaeException("The sweep mesh setup item requires a part with a single volume.");
            int volumeId = volumeDimTags[0].Item2;
            // Set source surfaces
            if (recombine)
            {
                foreach (var sourceSurfaceId in sourceSurfaceIds) Gmsh.Model.Mesh.SetRecombine(2, sourceSurfaceId);
            }
            // Set side surfaces. A wall whose side is built from several edges - which is what a
            // split source face produces - needs its four corners nominated by hand, and needs the
            // element counts on its opposite sides reconciled first.
            List<SweepWall> walls = SweepWallTopology.Classify(sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds,
                                                               surfaceIdEdgeIds);
            //
            int[] sourceVertexIdsOfSurface;
            HashSet<int> sourceVertexIds = new HashSet<int>();
            foreach (var sourceSurfaceId in sourceSurfaceIds)
            {
                if (surfaceIdVertexIds.TryGetValue(sourceSurfaceId, out sourceVertexIdsOfSurface))
                    sourceVertexIds.UnionWith(sourceVertexIdsOfSurface);
            }
            // Layer counts, grading, and the cross-sweep reconciliation, all measured from a 1D mesh
            PrepareWallEdges(walls, sourceVertexIds);
            //
            foreach (var wall in walls)
            {
                Gmsh.Model.Mesh.SetRecombine(2, wall.SurfaceId);
                //
                int[] corners = wall.HasCompositeSide ? SweepWallTopology.GetCorners(wall) : null;
                if (corners != null) Gmsh.Model.Mesh.SetTransfiniteSurface(wall.SurfaceId, "AlternateLeft", corners);
                else Gmsh.Model.Mesh.SetTransfiniteSurface(wall.SurfaceId, "AlternateLeft");
            }
            //
            GenerateMesh(2);
            // Remove the volume and the target surface mesh
            List<Tuple<int, int>> toRemoveDimTags = new List<Tuple<int, int>>() { new Tuple<int, int>(3, volumeId) };
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
                foreach (var sideSurfaceId in sideSurfaceIds) toRemoveDimTags.Add(new Tuple<int, int>(2, sideSurfaceId));
                //
                Gmsh.Model.Mesh.Clear(toRemoveDimTags.ToArray());
            }
            else
            {
                SweepMethods.CreateSweepMesh(sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds,
                                             sweepMesh.NumberOfGlobalSmoothSteps, surfaceIdEdgeIds, volumeId,
                                             _writeOutput);
            }
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
        ///   - Every direction edge within the same sweep-direction ring (`_gmshData.EdgeIdsBySweepLayer`)
        ///     gets the same number of elements, since they are the same sweep lines viewed one ring
        ///     apart. Different rings are free to disagree - there is no physical constraint between
        ///     them - so a body whose walls are already split along the sweep direction gets a
        ///     non-monotonic layer stack for free, one count per ring, with no piecewise transfinite
        ///     law needed. The layer grading is applied here too, so that nothing can overwrite it
        ///     afterwards.
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
        private void PrepareWallEdges(List<SweepWall> walls, HashSet<int> sourceVertexIds)
        {
            HashSet<int> directionEdgeIds = new HashSet<int>();
            foreach (var wall in walls) directionEdgeIds.UnionWith(wall.DirectionEdgeIds);
            //
            GenerateMesh(1);
            // Group the direction edges by sweep ring. A direction edge not covered by any ring -
            // older data, or none computed - falls back into one group of its own, so it is still
            // handled, just without the per-ring guarantee.
            List<HashSet<int>> ringGroups = new List<HashSet<int>>();
            HashSet<int> unassigned = new HashSet<int>(directionEdgeIds);
            if (_gmshData.EdgeIdsBySweepLayer != null)
            {
                foreach (var ring in _gmshData.EdgeIdsBySweepLayer)
                {
                    HashSet<int> ringDirectionEdgeIds = new HashSet<int>(ring);
                    ringDirectionEdgeIds.IntersectWith(directionEdgeIds);
                    if (ringDirectionEdgeIds.Count == 0) continue;
                    ringGroups.Add(ringDirectionEdgeIds);
                    unassigned.ExceptWith(ringDirectionEdgeIds);
                }
            }
            if (unassigned.Count > 0) ringGroups.Add(unassigned);
            // How many element layers each ring has. A count the user pinned down on any of the
            // ring's direction edges wins; otherwise the meshed counts decide. Measured while the 1D
            // mesh from GenerateMesh(1) above is still there.
            Dictionary<HashSet<int>, int> ringLayerElements = new Dictionary<HashSet<int>, int>();
            foreach (var ringEdgeIds in ringGroups)
                ringLayerElements[ringEdgeIds] = ResolveRingLayerElements(ringEdgeIds);
            // The two cross-sweep sides of every wall, also measured while the 1D mesh is still there
            int sourceElements;
            Dictionary<int, int> targetEdgeIdNumOfElements = new Dictionary<int, int>();
            foreach (var wall in walls)
            {
                if (wall.SourceEdgeIds.Length == 0 || wall.TargetEdgeIds.Length == 0) continue;
                //
                sourceElements = 0;
                foreach (var edgeId in wall.SourceEdgeIds)
                    sourceElements += SweepWallTopology.GetElementCount(edgeId);
                //
                double[] lengths = SweepWallTopology.GetEdgeLengths(wall.TargetEdgeIds);
                int[] targetElements = SweepWallTopology.DistributeElements(sourceElements, lengths);
                for (int i = 0; i < wall.TargetEdgeIds.Length; i++)
                    targetEdgeIdNumOfElements[wall.TargetEdgeIds[i]] = targetElements[i];
            }
            //
            Gmsh.Model.Mesh.Clear();    // must clear the mesh before the constraints are changed
            //
            foreach (var ringEdgeIds in ringGroups)
            {
                int layerElements = ringLayerElements[ringEdgeIds];
                foreach (var edgeId in ringEdgeIds)
                {
                    _gmshData.EdgeIdNumElements[edgeId] = layerElements;
                    ApplyTransfiniteCurve(edgeId, layerElements + 1, sourceVertexIds);
                }
            }
            foreach (var entry in targetEdgeIdNumOfElements)
            {
                _gmshData.EdgeIdNumElements[entry.Key] = entry.Value;
                ApplyTransfiniteCurve(entry.Key, entry.Value + 1, sourceVertexIds);
            }
        }
        /// <summary>
        /// The single call site every SetTransfiniteCurve constraint in the sweep/transfinite
        /// pipeline goes through, so a LocalMeshSize grading on an edge cannot be silently discarded
        /// by whichever call happens to run last for that edge.
        /// </summary>
        private void ApplyTransfiniteCurve(int edgeId, int numOfNodes, HashSet<int> sourceVertexIds)
        {
            EdgeBias bias;
            if (_gmshData.EdgeIdBias != null && _gmshData.EdgeIdBias.TryGetValue(edgeId, out bias))
                SetGradedTransfiniteCurve(edgeId, numOfNodes, bias.BiasType, bias.BiasFactor, bias.Reversed, sourceVertexIds);
            else
                Gmsh.Model.Mesh.SetTransfiniteCurve(edgeId, numOfNodes);
        }
        private int ResolveRingLayerElements(HashSet<int> ringEdgeIds)
        {
            int numOfElements;
            int layerElements = 0;
            int measuredSum = 0;
            int measuredCount = 0;
            foreach (var edgeId in ringEdgeIds)
            {
                if (_gmshData.EdgeIdNumElements.TryGetValue(edgeId, out numOfElements))
                {
                    if (numOfElements > layerElements) layerElements = numOfElements;
                }
                else
                {
                    measuredSum += SweepWallTopology.GetElementCount(edgeId);
                    measuredCount++;
                }
            }
            if (layerElements <= 0 && measuredCount > 0)
                layerElements = (int)Math.Round((double)measuredSum / measuredCount, 0, MidpointRounding.AwayFromZero);
            if (layerElements < 1) layerElements = 1;
            return layerElements;
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
                for (int i = 0; i < elementTags.Length; i++) count += elementTags[i].Length;
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
        /// <summary>
        /// sourceVertexIds is the sweep source surface's vertices, used to auto-orient a direction
        /// edge's grading towards the target regardless of the curve's own parametrization; pass null
        /// for a plain LocalMeshSize edge with no such preference, where the curve's own parametric
        /// start is the "forward" direction the editor's arrow shows. Either way, reversed flips it.
        /// </summary>
        private static void SetGradedTransfiniteCurve(int edgeId, int numOfNodes, SweepBiasTypeEnum biasType,
                                                      double biasFactor, bool reversed, HashSet<int> sourceVertexIds)
        {
            if (biasType == SweepBiasTypeEnum.None)
            {
                Gmsh.Model.Mesh.SetTransfiniteCurve(edgeId, numOfNodes);
                return;
            }
            if (biasType == SweepBiasTypeEnum.Bump)
            {
                // A bump is symmetric about the middle of the curve, so its direction is irrelevant
                Gmsh.Model.Mesh.SetTransfiniteCurve(edgeId, numOfNodes, "Bump", biasFactor);
                return;
            }
            // A geometric progression runs from the parametric start of the curve. An edge that runs
            // from the target surface back to the source gets the reciprocal ratio, which is the same
            // distribution walked the other way, so every wall ends up graded from the source surface
            // towards the target one. reversed flips whichever direction that resolves to.
            bool forward = sourceVertexIds == null || CurveStartsOnSurfaceVertex(edgeId, sourceVertexIds);
            if (reversed) forward = !forward;
            double coefficient = forward ? biasFactor : 1 / biasFactor;
            //
            Gmsh.Model.Mesh.SetTransfiniteCurve(edgeId, numOfNodes, "Progression", coefficient);
        }
        /// <summary>
        /// Whether the parametric start of a curve is one of the given vertices. The start is found
        /// by evaluating the curve at its lower parameter bound and taking the nearer of the two end
        /// vertices, rather than by relying on the sign convention of GetBoundary.
        /// </summary>
        private static bool CurveStartsOnSurfaceVertex(int edgeId, HashSet<int> vertexIds)
        {
            double[] minParameter;
            double[] maxParameter;
            Gmsh.Model.GetParametrizationBounds(1, edgeId, out minParameter, out maxParameter);
            if (minParameter == null || minParameter.Length < 1) return true;
            //
            double[] startCoor;
            Gmsh.Model.GetValue(1, edgeId, new double[] { minParameter[0] }, out startCoor);
            if (startCoor == null || startCoor.Length < 3) return true;
            //
            Tuple<int, int>[] vertexDimTags;
            Gmsh.Model.GetBoundary(new Tuple<int, int>[] { new Tuple<int, int>(1, edgeId) }, out vertexDimTags,
                                   false, false, false);
            //
            int nearestVertexId = -1;
            double nearestDistance = double.MaxValue;
            foreach (var vertexDimTag in vertexDimTags)
            {
                if (vertexDimTag.Item1 != 0) continue;
                //
                int vertexId = Math.Abs(vertexDimTag.Item2);
                double[] coor;
                Gmsh.Model.GetValue(0, vertexId, new double[0], out coor);
                if (coor == null || coor.Length < 3) continue;
                //
                double distance = Math.Pow(coor[0] - startCoor[0], 2) +
                                  Math.Pow(coor[1] - startCoor[1], 2) +
                                  Math.Pow(coor[2] - startCoor[2], 2);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestVertexId = vertexId;
                }
            }
            // A closed curve has no distinct start vertex; leave its grading as it comes
            if (nearestVertexId < 0) return true;
            //
            return vertexIds.Contains(nearestVertexId);
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
                    ApplyTransfiniteCurve(edgeNodeFromGroup.Value.Id, avgNumOfNodes, null);
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
                        ApplyTransfiniteCurve(sweepEdgeId, avgNumOfNodes, null);
                    }
                }
            }
            //
            Gmsh.Model.Mesh.Clear(); // must clear the mesh
            //
            foreach (var entry in surfaceIdSurface)
            {
                surface = entry.Value;
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
            if (_gmshData.SurfaceIds == null || _gmshData.SurfaceIds.Length == 0) return;
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
            return;
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
                Dictionary<int, int> edgeIdNumElements = new Dictionary<int, int>();
                foreach (var entry in _gmshData.EdgeIdNumElements)
                {
                    if (netgenEdgeIdGmshEdgeId.TryGetValue(entry.Key, out edgeId))
                        edgeIdNumElements[edgeId] = entry.Value;
                    else if (Debugger.IsAttached)
                    {
                        // Other part
                        //Debugger.Break();
                    }
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
                    int faceId;
                    if (setupItem is SweepMesh sw)
                    {
                        for (int i = 0; i < sw.SideSurfaceIds.Length; i++)
                        {
                            if (netgenFaceIdGmshFaceId.TryGetValue(sw.SideSurfaceIds[i], out faceId))
                                sw.SideSurfaceIds[i] = faceId;
                            else if (Debugger.IsAttached) Debugger.Break();
                        }
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
        private void PrepareData(GmshSetupItem gmshSetupItem)
        {
            // Gather sweep edge ids by layer
            _gmshData.EdgeIdsBySweepLayer = null;
            if (gmshSetupItem is SweepMesh sm)
            {
                _gmshData.EdgeIdsBySweepLayer = new HashSet<int>[sm.LayerGroupEdgeIds.Length];
                for (int i = 0; i < sm.LayerGroupEdgeIds.Length; i++)
                {
                    _gmshData.EdgeIdsBySweepLayer[i] = new HashSet<int>();
                    for (int j = 0; j < sm.LayerGroupEdgeIds[i].Length; j++)
                    {
                        _gmshData.EdgeIdsBySweepLayer[i].UnionWith(sm.LayerGroupEdgeIds[i][j]);
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
                            // If different surfaces are found select by the smallest normalized criteria
                            if (locationIdDiff[0] != sizeIdDiff[0])
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
