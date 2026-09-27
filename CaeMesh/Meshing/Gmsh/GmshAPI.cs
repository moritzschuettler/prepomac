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
using System.Text.RegularExpressions;
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
        private HashSet<int> _transfiniteSurfaceIds = new HashSet<int>();
        private Dictionary<int, int> _transfiniteSourcePins = new Dictionary<int, int>();
        private HashSet<int> _sharedCrossEdgeIds = new HashSet<int>();
        private HashSet<int> _quadWildSourceIds = new HashSet<int>();
        private List<SweepQuadWildGroup> _quadWildGroups;
        private const int _maxQuadWildRounds = 4;
        private const double _sideTranslationTolerance = 0.02;
        private static readonly Regex _entityProgress =
            new Regex(@"Meshing (curve|surface|volume) \d+", RegexOptions.Compiled);
        private static readonly Regex _entityReference =
            new Regex(@"(?<![A-Za-z])(Surface|surface|Curve|curve|Edge|edge|Point|point)\s+(\d+)", RegexOptions.Compiled);
        // What a gmsh entity is in the model, so that a message of the mesher can name it the way the
        // interface does. Empty until the geometry has been renumbered, and empty for a part with no CAD
        private Dictionary<int, int> _gmshPointIdTopologyId = new Dictionary<int, int>();
        private Dictionary<int, int> _gmshCurveIdTopologyId = new Dictionary<int, int>();
        private Dictionary<int, int> _gmshSurfaceIdTopologyId = new Dictionary<int, int>();
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
        private const int _numLayerEdgeSamples = 256;
        private const double _cornerAngleToleranceDeg = 0.1;


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
                // Every message gmsh writes reaches the output twice, once printed by gmsh itself and once
                // read back from its log, and only the second can be named and filtered on its way out
                Gmsh.Option.SetNumber("General.Terminal", 0);
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
                    if (gsi is SweepMesh symmetricSweep && symmetricSweep.HasSymmetry) RemoveMirroredCells(symmetricSweep);
                    // PrepareData
                    PrepareData(gsi);
                    if (gsi is SweepMesh sweepSetupItem) PrepareSweepBlocks(sweepSetupItem);
                    // Mesh size
                    SetMeshSizes(gsi);
                    // 2D meshing algorithm
                    Gmsh.Option.SetNumber("Mesh.Algorithm", (int)GetAlgorithmMesh2D(gsi));
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
                                               gsi.TransfiniteAngleDeg, recombine, gsi.AlgorithmRecombine);
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
                _error = NameGmshEntities(ex.Message);
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
        /// <summary>
        /// The algorithm the surfaces are meshed with. A sweep whose sources QuadWild meshes has gmsh mesh only its
        /// walls, which are structured grids, the targets, which are thrown away, and the triangulation QuadWild
        /// starts from, so the quasi-structured quad algorithm - which meshes the curves of the surfaces again with
        /// sizes of its own - is replaced by the Frontal-Delaunay one.
        /// </summary>
        private static GmshAlgorithmMesh2DEnum GetAlgorithmMesh2D(GmshSetupItem gmshSetupItem)
        {
            if (gmshSetupItem is SweepMesh sweepMesh && sweepMesh.SourceMesher == SweepSourceMesherEnum.QuadWild &&
                sweepMesh.AlgorithmMesh2D == GmshAlgorithmMesh2DEnum.QuasiStructuredQuad)
                return GmshAlgorithmMesh2DEnum.FrontalDelaunay;
            return gmshSetupItem.AlgorithmMesh2D;
        }
        private void SetMeshSizes(GmshSetupItem gmshSetupItem)
        {
            GmshAlgorithmMesh2DEnum algorithm2D = GetAlgorithmMesh2D(gmshSetupItem);
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
                    // Sweep edge sizes. A layer whose sweep lines are cut into several edges is left to the
                    // sweep, which shares a count pinned on one piece out over the levels of the line in
                    // PrepareWallEdges; copied onto the full-length edges of such a layer the count would be
                    // shared out over their levels as well and override the pin on the piece it was set on
                    if (_gmshData.EdgeIdsBySweepLayer != null)
                    {
                        foreach (var edgeIds in _gmshData.EdgeIdsBySweepLayer)
                        {
                            if (IsSweepLineCutIntoEdges(edgeIds)) continue;
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
            MarkRingClosingBlocks(blocks);
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
                    foreach (var sourceSurfaceId in block.SourceSurfaceIds)
                    {
                        if (!_quadWildSourceIds.Contains(sourceSurfaceId)) Gmsh.Model.Mesh.SetRecombine(2, sourceSurfaceId);
                    }
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
                if (!block.ClosesRing) targetSurfaceIds.UnionWith(block.TargetSurfaceIds);
            }
            int[] tetrahedralVolumeIds = GetTetrahedralVolumeIds(blocks);
            if (GetAlgorithmMesh2D(sweepMesh) == GmshAlgorithmMesh2DEnum.QuasiStructuredQuad)
                MeshTetrahedralSurfacesByTriangles(tetrahedralVolumeIds, blocks);
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
                if (GetAlgorithmMesh2D(sweepMesh) != GmshAlgorithmMesh2DEnum.QuasiStructuredQuad) throw;
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
            // The cells no block covers are meshed by tetrahedra on what the sweep left on their boundaries
            if (!preview && tetrahedralVolumeIds.Length > 0)
            {
                string workDirectory = Path.GetDirectoryName(Path.GetFullPath(_gmshData.InpFileName));
                string[] cellNames = tetrahedralVolumeIds.Select(GetVolumePartName).ToArray();
                SweepTetrahedralFill.Fill(tetrahedralVolumeIds, cellNames, _gmshData.NetgenMesherFileName,
                                          _gmshData.NetgenMeshParametersFileName, workDirectory,
                                          message => _writeOutput?.Invoke(NameGmshEntities(message)));
            }
        }
        /// <summary>
        /// The part a volume of a compound was written out of, read off a boundary surface no other volume shares.
        /// </summary>
        private string GetVolumePartName(int volumeId)
        {
            int[] volumeIds;
            int topologyId;
            string partName;
            Tuple<int, int>[] surfaceDimTags;
            Gmsh.Model.GetBoundary(new Tuple<int, int>[] { new Tuple<int, int>(3, volumeId) }, out surfaceDimTags,
                                   false, false, false);
            foreach (var surfaceDimTag in surfaceDimTags)
            {
                int surfaceId = Math.Abs(surfaceDimTag.Item2);
                Gmsh.Model.GetAdjacencies(2, surfaceId, out volumeIds, out _);
                if (volumeIds.Length != 1 || !_gmshSurfaceIdTopologyId.TryGetValue(surfaceId, out topologyId)) continue;
                //
                int partId = FeMesh.GetPartIdFromGmshTopologyId(topologyId);
                if (_gmshData.PartIdPartName != null && _gmshData.PartIdPartName.TryGetValue(partId, out partName))
                    return partName;
            }
            return "the cell of gmsh volume " + volumeId;
        }
        /// <summary>
        /// The volumes of a compound that no sweep block covers: the cells that cannot be swept, which the model side
        /// leaves to tetrahedra rather than refusing the compound.
        /// </summary>
        private static int[] GetTetrahedralVolumeIds(List<SweepBlock> blocks)
        {
            Tuple<int, int>[] volumeDimTags;
            Gmsh.Model.GetEntities(out volumeDimTags, 3);
            HashSet<int> blockVolumeIds = new HashSet<int>(blocks.Select(block => block.VolumeId));
            return volumeDimTags.Select(dimTag => dimTag.Item2).Where(id => !blockVolumeIds.Contains(id)).ToArray();
        }
        /// <summary>
        /// The quasi-structured quad algorithm meshes every surface it reaches by quadrilaterals, and Netgen would stand
        /// a pyramid on each of them, so the surfaces that bound only tetrahedral cells are meshed by triangles.
        /// </summary>
        private static void MeshTetrahedralSurfacesByTriangles(int[] tetrahedralVolumeIds, List<SweepBlock> blocks)
        {
            if (tetrahedralVolumeIds.Length == 0) return;
            //
            HashSet<int> blockSurfaceIds = new HashSet<int>();
            foreach (var block in blocks)
            {
                blockSurfaceIds.UnionWith(block.SourceSurfaceIds);
                blockSurfaceIds.UnionWith(block.WallSurfaceIds);
                blockSurfaceIds.UnionWith(block.TargetSurfaceIds);
            }
            Tuple<int, int>[] surfaceDimTags;
            foreach (var volumeId in tetrahedralVolumeIds)
            {
                Gmsh.Model.GetBoundary(new Tuple<int, int>[] { new Tuple<int, int>(3, volumeId) }, out surfaceDimTags,
                                       false, false, false);
                foreach (var surfaceDimTag in surfaceDimTags)
                {
                    int surfaceId = Math.Abs(surfaceDimTag.Item2);
                    if (!blockSurfaceIds.Contains(surfaceId))
                        Gmsh.Model.Mesh.SetAlgorithm(2, surfaceId, (int)GmshAlgorithmMesh2DEnum.FrontalDelaunay);
                }
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
            // A target surface is meshed only to be thrown away, and the counts on its edges are set for the
            // walls, not for it: a structured grid on it would fail as often as not. Taken once the fragmentation
            // has given the faces their final tags - taken before it, the set named faces the renumbering had
            // since given to others, and a target the carrying had renumbered was made a structured grid
            // The target of a block closing a ring is the source the ring started from, and is meshed for real
            MarkRingClosingBlocks(_sweepBlocks);
            foreach (var block in _sweepBlocks)
            {
                if (!block.ClosesRing) _freeMeshedSurfaceIds.UnionWith(block.TargetSurfaceIds);
            }
            // A source QuadWild meshes is triangulated free for it, once the fragmentation has given the faces their
            // final tags. A source that is the target of another block is the cap that block's sweep leaves, and is
            // read back from there; one that is a wall of another block is meshed as the structured grid of that wall
            if (sweepMesh.SourceMesher == SweepSourceMesherEnum.QuadWild)
            {
                foreach (var block in _sweepBlocks) _quadWildSourceIds.UnionWith(block.SourceSurfaceIds);
                foreach (var block in _sweepBlocks)
                {
                    if (!block.ClosesRing) _quadWildSourceIds.ExceptWith(block.TargetSurfaceIds);
                }
                foreach (var block in _sweepBlocks) _quadWildSourceIds.ExceptWith(block.WallSurfaceIds);
                _freeMeshedSurfaceIds.UnionWith(_quadWildSourceIds);
            }
        }
        /// <summary>
        /// Marks the blocks that close a ring: every target of such a block is the source of a block swept before
        /// it, so the target is meshed before the block is swept and has to be kept, not rebuilt by the sweep.
        /// </summary>
        private static void MarkRingClosingBlocks(List<SweepBlock> blocks)
        {
            HashSet<int> earlierSourceIds = new HashSet<int>();
            foreach (var block in blocks)
            {
                block.ClosesRing = block.TargetSurfaceIds.Count > 0 && block.TargetSurfaceIds.IsSubsetOf(earlierSourceIds);
                earlierSourceIds.UnionWith(block.SourceSurfaceIds);
            }
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
            _writeOutput?.Invoke(NameGmshEntities("Sweep: " + block.GetDescription() + " - " +
                                                  DescribeCarriedEdges(toSourceDimTags) + " splitting the target carried " +
                                                  "to the source, " + DescribeCarriedEdges(toTargetDimTags) + " splitting " +
                                                  "the source carried to the target."));
        }
        private static string DescribeCarriedEdges(List<Tuple<int, int>> edgeDimTags)
        {
            if (edgeDimTags.Count == 0) return "0 edge/s";
            return edgeDimTags.Count + " edge/s (" + string.Join(", ", edgeDimTags.Select(dimTag => "edge " + dimTag.Item2)) +
                   ")";
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
            // The layer edges as polylines, so that one the operation splits can be told by its pieces
            Dictionary<int, double[][]> layerEdgeIdSamples = GetLayerEdgeSamples();
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
            int topologyId;
            HashSet<int> surfaceCopyIds = new HashSet<int>(surfaceCopyDimTags.Select(dimTag => dimTag.Item2));
            Dictionary<int, int> newSurfaceIdOriginalId = new Dictionary<int, int>();
            Dictionary<int, int> gmshSurfaceIdTopologyId = new Dictionary<int, int>();
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
                // A piece of a split face is named after the face the user sees
                if (_gmshSurfaceIdTopologyId.TryGetValue(oldSurfaceId, out topologyId))
                    gmshSurfaceIdTopologyId[dimTag.Item2] = topologyId;
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
                    throw new CaeException("The volume of " + block.GetDescription() + " was lost while the splits of " +
                                           "the sweep ends were carried across.");
                block.VolumeId = newVolumeId;
            }
            RemapGmshData(edgeIdMap, pointIdMap, GetSplitLayerEdgePieces(layerEdgeIdSamples, edgeIdMap, diagonal));
            // The messages name the entities by the tags they carry now. An error above still names them by the
            // tags they carried before, so the names follow only here
            if (_gmshSurfaceIdTopologyId.Count > 0) _gmshSurfaceIdTopologyId = gmshSurfaceIdTopologyId;
            _gmshCurveIdTopologyId = RemapEntityNames(_gmshCurveIdTopologyId, edgeIdMap);
            _gmshPointIdTopologyId = RemapEntityNames(_gmshPointIdTopologyId, pointIdMap);
        }
        /// <summary>
        /// Points along every layer edge of the sweep, taken before a fragmentation.
        /// </summary>
        private Dictionary<int, double[][]> GetLayerEdgeSamples()
        {
            Dictionary<int, double[][]> edgeIdSamples = new Dictionary<int, double[][]>();
            if (_gmshData.EdgeIdsBySweepLayer == null) return edgeIdSamples;
            //
            double[] min;
            double[] max;
            double[] point;
            foreach (var ring in _gmshData.EdgeIdsBySweepLayer)
            {
                foreach (var edgeId in ring)
                {
                    if (edgeIdSamples.ContainsKey(edgeId)) continue;
                    Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
                    double[][] samples = new double[_numLayerEdgeSamples + 1][];
                    for (int i = 0; i <= _numLayerEdgeSamples; i++)
                    {
                        Gmsh.Model.GetValue(1, edgeId, new double[] { min[0] + (max[0] - min[0]) * i / _numLayerEdgeSamples },
                                            out point);
                        samples[i] = point;
                    }
                    edgeIdSamples.Add(edgeId, samples);
                }
            }
            return edgeIdSamples;
        }
        /// <summary>
        /// The pieces of every layer edge a fragmentation split: the new edges whose two ends and middle lie on
        /// the edge as it was. A curve carried onto the end of one block can end on a layer edge of another -
        /// the end of a block swept along a plate lies in the face that a block swept through the plate has
        /// its layer edges on - and the edge then goes on as two layer edges meeting at the end of the curve.
        /// </summary>
        private static Dictionary<int, List<int>> GetSplitLayerEdgePieces(Dictionary<int, double[][]> layerEdgeIdSamples,
                                                                        Dictionary<int, int> edgeIdMap, double diagonal)
        {
            Dictionary<int, List<int>> edgeIdPieceIds = new Dictionary<int, List<int>>();
            if (layerEdgeIdSamples.Keys.All(edgeIdMap.ContainsKey)) return edgeIdPieceIds;
            //
            double[] min;
            double[] max;
            double[] point;
            Tuple<int, int>[] dimTags;
            HashSet<int> matchedIds = new HashSet<int>(edgeIdMap.Values);
            List<int> newEdgeIds = new List<int>();
            List<double[][]> newEdgePoints = new List<double[][]>();
            Gmsh.Model.GetEntities(out dimTags, 1);
            foreach (var dimTag in dimTags)
            {
                if (matchedIds.Contains(dimTag.Item2)) continue;
                Gmsh.Model.GetParametrizationBounds(1, dimTag.Item2, out min, out max);
                double[][] points = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    Gmsh.Model.GetValue(1, dimTag.Item2, new double[] { min[0] + (max[0] - min[0]) * i / 2.0 }, out point);
                    points[i] = point;
                }
                newEdgeIds.Add(dimTag.Item2);
                newEdgePoints.Add(points);
            }
            //
            foreach (var entry in layerEdgeIdSamples)
            {
                if (edgeIdMap.ContainsKey(entry.Key)) continue;
                //
                double length = 0;
                for (int i = 1; i < entry.Value.Length; i++) length += GetDistance(entry.Value[i - 1], entry.Value[i]);
                double tolerance = Math.Max(1E-6 * diagonal, 1E-4 * length);
                List<int> pieceIds = new List<int>();
                for (int i = 0; i < newEdgeIds.Count; i++)
                {
                    if (newEdgePoints[i].All(p => GetDistanceToPolyline(p, entry.Value) <= tolerance))
                        pieceIds.Add(newEdgeIds[i]);
                }
                if (pieceIds.Count > 1) edgeIdPieceIds.Add(entry.Key, pieceIds);
            }
            return edgeIdPieceIds;
        }
        private static double GetDistanceToPolyline(double[] point, double[][] polyline)
        {
            double distance = double.MaxValue;
            for (int i = 1; i < polyline.Length; i++)
            {
                double[] a = polyline[i - 1];
                double[] b = polyline[i];
                double[] ab = new double[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                double lengthSquared = ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2];
                double t = lengthSquared > 0 ? ((point[0] - a[0]) * ab[0] + (point[1] - a[1]) * ab[1] +
                                                (point[2] - a[2]) * ab[2]) / lengthSquared : 0;
                t = Math.Max(0, Math.Min(1, t));
                double[] closest = new double[] { a[0] + t * ab[0], a[1] + t * ab[1], a[2] + t * ab[2] };
                distance = Math.Min(distance, GetDistance(point, closest));
            }
            return distance;
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
        /// The surfaces with an edge lying inside them (an internal edge): a curve that is not on the boundary of the
        /// surface, although it may bound other surfaces (a rib standing on the edge), and lies inside it. The OCC
        /// import embeds such an edge in its face, and the face must not be meshed as transfinite, which ignores
        /// embedded curves.
        /// </summary>
        private static HashSet<int> GetSurfaceIdsWithInternalCurves(Tuple<int, int>[] surfaceDimTags)
        {
            HashSet<int> surfaceIdsWithInternalCurves = new HashSet<int>();
            if (surfaceDimTags.Length == 0) return surfaceIdsWithInternalCurves;
            //
            Tuple<int, int>[] edgeDimTags;
            Gmsh.Model.GetEntities(out edgeDimTags, 1);
            double tolerance = 1E-6 * GetModelDiagonal();
            double[][] curveBoxes = edgeDimTags.Select(dimTag => GetBoundingBox(1, dimTag.Item2)).ToArray();
            foreach (var surfaceDimTag in surfaceDimTags)
            {
                Tuple<int, int>[] boundaryDimTags;
                Gmsh.Model.GetBoundary(new Tuple<int, int>[] { surfaceDimTag }, out boundaryDimTags, false, false, false);
                HashSet<int> boundaryCurveIds = new HashSet<int>(boundaryDimTags.Select(dimTag => Math.Abs(dimTag.Item2)));
                double[] surfaceBox = GetBoundingBox(2, surfaceDimTag.Item2);
                for (int i = 0; i < edgeDimTags.Length; i++)
                {
                    int curveId = edgeDimTags[i].Item2;
                    if (boundaryCurveIds.Contains(curveId)) continue;
                    // The curve box must lie within the surface box
                    bool inBox = true;
                    for (int k = 0; k < 3 && inBox; k++)
                        inBox = curveBoxes[i][k] >= surfaceBox[k] - tolerance &&
                                curveBoxes[i][k + 3] <= surfaceBox[k + 3] + tolerance;
                    if (!inBox) continue;
                    //
                    if (IsCurveInsideSurface(curveId, surfaceDimTag.Item2))
                    {
                        surfaceIdsWithInternalCurves.Add(surfaceDimTag.Item2);
                        break;
                    }
                }
            }
            return surfaceIdsWithInternalCurves;
        }
        private static double[] GetBoundingBox(int dim, int tag)
        {
            double xMin, yMin, zMin, xMax, yMax, zMax;
            Gmsh.Model.GetBoundingBox(dim, tag, out xMin, out yMin, out zMin, out xMax, out yMax, out zMax);
            return new double[] { xMin, yMin, zMin, xMax, yMax, zMax };
        }
        /// <summary>
        /// True when three points of the curve lie on the surface and inside it.
        /// </summary>
        private static bool IsCurveInsideSurface(int curveId, int surfaceId)
        {
            double[] coor;
            double[] minimum;
            double[] maximum;
            Gmsh.Model.GetParametrizationBounds(1, curveId, out minimum, out maximum);
            if (minimum.Length < 1 || maximum.Length < 1) return false;
            //
            for (int i = 1; i <= 3; i++)
            {
                double t = minimum[0] + (maximum[0] - minimum[0]) * i / 4.0;
                Gmsh.Model.GetValue(1, curveId, new double[] { t }, out coor);
                if (coor.Length < 3) return false;
                if (!IsPointOnSurface(surfaceId, coor) || Gmsh.Model.IsInside(2, surfaceId, coor, false) <= 0)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// A point strictly inside a surface and lying on it: its centre of mass where that serves, else a
        /// point of its parameter box the inside test accepts. The inside test alone does not say that a point
        /// lies on the surface, since it projects the point onto it first, and the centre of mass of a bore
        /// lies on the axis of the bore rather than on the bore.
        ///
        /// The point also keeps clear of the surface's own boundary. The inside test accepts a point on the
        /// boundary, and such a point lies on the surface across the edge just as well, so the face it is asked
        /// to identify is taken for its neighbour: the parameter box of the L-shaped end of a bent bar puts a
        /// sample on the edge the end shares with a wall, and the end was then swept as a wall of its own
        /// block. A point on the boundary is kept only when the surface offers no other.
        /// </summary>
        private static double[] GetPointInsideSurface(int surfaceId)
        {
            double x;
            double y;
            double z;
            Gmsh.Model.OCC.GetCenterOfMass(2, surfaceId, out x, out y, out z);
            double[] center = new double[] { x, y, z };
            if (IsPointOnSurface(surfaceId, center) && Gmsh.Model.IsInside(2, surfaceId, center, false) > 0 &&
                IsPointClearOfSurfaceBoundary(surfaceId, center)) return center;
            //
            double[] boundaryCoor = null;
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
                        if (coor.Length < 3) continue;
                        if (IsPointClearOfSurfaceBoundary(surfaceId, coor)) return coor;
                        if (boundaryCoor == null) boundaryCoor = coor;
                    }
                }
            }
            if (boundaryCoor != null) return boundaryCoor;
            throw new CaeException("No point inside a surface of the sweep could be found.");
        }
        private static bool IsPointClearOfSurfaceBoundary(int surfaceId, double[] coor)
        {
            int[] curveIds;
            double[] closestCoor;
            double clearance = 1E-5 * GetModelDiagonal();
            Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out curveIds);
            foreach (var curveId in curveIds)
            {
                Gmsh.Model.GetClosestPoint(1, curveId, coor, out closestCoor, out _);
                if (closestCoor.Length >= 3 && GetDistance(coor, closestCoor) <= clearance) return false;
            }
            return true;
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
        /// one that does not is an error.
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
                        throw new CaeException("The edge " + entry.Key + " splitting the target of " +
                                               block.GetDescription() + " has no counterpart on the source after " +
                                               "the splits were carried across.");
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
        private void RemapGmshData(Dictionary<int, int> edgeIdMap, Dictionary<int, int> pointIdMap,
                                   Dictionary<int, List<int>> splitLayerEdgeIdPieceIds)
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
            // A layer edge follows the sweep. A curve carried onto the end of another block can still split it,
            // and it is then followed on by its pieces
            List<int> pieceIds;
            if (_gmshData.EdgeIdsBySweepLayer != null)
            {
                foreach (var ring in _gmshData.EdgeIdsBySweepLayer)
                {
                    List<int> remapped = new List<int>();
                    foreach (var edgeId in ring)
                    {
                        if (edgeIdMap.TryGetValue(edgeId, out newId)) remapped.Add(newId);
                        else if (splitLayerEdgeIdPieceIds.TryGetValue(edgeId, out pieceIds)) remapped.AddRange(pieceIds);
                        else throw new CaeException("A layer edge of the sweep, edge " + edgeId + ", was lost while the " +
                                                    "splits of the sweep ends were carried across.");
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
            if (_quadWildSourceIds.Count > 0)
            {
                MeshQuadWildSourceEdges(sweepMesh, blocks, walls, chains, surfaceIdEdgeIds, chainBlockIndices);
            }
            else
            {
                // Layer counts and the cross-sweep reconciliation, both measured from a 1D mesh
                PrepareWallEdges(walls, chains, chainBlockIndices);
                FreeSourcesWithUnequalSides(blocks, walls, surfaceIdEdgeIds,
                                            sweepMesh.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None);
                foreach (var wall in walls) Gmsh.Model.Mesh.SetRecombine(2, wall.SurfaceId);
                // The curves are meshed in a pass of their own. Measured: when the curves and the surfaces are
                // meshed in one call, a wall whose both cross-sweep sides are composite fails the transfinite
                // side check with node counts that are not the ones the constraints above set
                GenerateMesh(1);
                MatchOppositeSideSpacing(walls, null);
            }
            // A transfinite surface set a second time would add its corners to the ones gmsh holds, so a
            // later attempt keeps the constraints of the first
            if (setTransfiniteWalls) SetTransfiniteWalls(walls);
            // gmsh reports a wall it could not mesh with a structured grid, meshes it free and carries on, so
            // its log is read for the report. A surface whose corners were nominated it refuses with an exception
            // instead, which is caught for the same report
            int logLength = Gmsh.Logger.Get().Length;
            try
            {
                GenerateMesh(2);
            }
            catch (Exception ex) when (!(ex is CaeException))
            {
                if (!WereWallsRefused(logLength)) throw;
                throw new CaeException(GetRefusedSurfacesMessage(ReportRefusedSurfaces(walls, blocks, surfaceIdEdgeIds,
                                                                                        logLength)));
            }
            if (WereWallsRefused(logLength))
            {
                throw new CaeException(GetRefusedSurfacesMessage(ReportRefusedSurfaces(walls, blocks, surfaceIdEdgeIds,
                                                                                        logLength)));
            }
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
            // The QuadWild sources replace what gmsh meshed there, after the optimisation, which would move their nodes
            if (_quadWildGroups != null)
            {
                foreach (var group in _quadWildGroups) SweepQuadWildMesher.AddSurfaceMesh(group);
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
        /// Settles the counts of the walls and of the source edges QuadWild meshes, and leaves the curves meshed with
        /// them and the nodes of the source edges where QuadWild put them.
        ///
        /// The counts go round in a loop. The walls are settled first, with every free edge of a QuadWild source
        /// measured as gmsh sizes it; the sources are then triangulated and QuadWild meshes them, keeping the nodes
        /// of the fixed edges and choosing the counts of the free ones; the walls are settled again with those
        /// counts pinned. A run stays valid while every edge of its group carries the count it was meshed with. The
        /// settling may change a fixed edge - the target side of a wall beside a free edge whose count moved - or
        /// override the count of a free edge, and the group is then meshed again, with that free edge fixed at the
        /// count the walls gave it. The loop ends on the first round in which every run is valid. Every round starts
        /// from the pins the sweep was given and the counts QuadWild chose, since the counts a round settles would
        /// otherwise stay behind as pins of the next.
        /// </summary>
        private void MeshQuadWildSourceEdges(SweepMesh sweepMesh, List<SweepBlock> blocks, List<SweepWall> walls,
                                             List<SweepChain> chains, Dictionary<int, int[]> surfaceIdEdgeIds,
                                             List<int> chainBlockIndices)
        {
            if (_gmshData.QuadWildDirectory == null || !Directory.Exists(_gmshData.QuadWildDirectory))
                throw new CaeException("The QuadWild folder was not found: " + _gmshData.QuadWildDirectory);
            string workDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_gmshData.InpFileName)),
                                                "SweepQuadWild");
            Directory.CreateDirectory(workDirectory);
            Action<string> writeOutput = message => _writeOutput?.Invoke(NameGmshEntities(message));
            bool recombine = sweepMesh.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            //
            Dictionary<int, int> pinnedEdgeIdNumElements = new Dictionary<int, int>(_gmshData.EdgeIdNumElements);
            Dictionary<int, int> quadWildEdgeIdNumElements = new Dictionary<int, int>();
            Dictionary<int, int> directionEdgeIdNumOfElements = null;
            for (int round = 1; ; round++)
            {
                if (round > 1)
                {
                    _gmshData.EdgeIdNumElements.Clear();
                    foreach (var entry in pinnedEdgeIdNumElements) _gmshData.EdgeIdNumElements[entry.Key] = entry.Value;
                    foreach (var entry in quadWildEdgeIdNumElements) _gmshData.EdgeIdNumElements[entry.Key] = entry.Value;
                    Gmsh.Model.Mesh.Clear();
                    foreach (var entry in quadWildEdgeIdNumElements)
                        Gmsh.Model.Mesh.SetTransfiniteCurve(entry.Key, entry.Value + 1);
                }
                // QuadWild chooses no count along the sweep, so the layers of the first round hold for every round.
                // Resolved again, from the 1D mesh the counts of the round before shaped, they came out larger each time
                directionEdgeIdNumOfElements = PrepareWallEdges(walls, chains, chainBlockIndices,
                                                                directionEdgeIdNumOfElements);
                if (round == 1)
                {
                    FreeSourcesWithUnequalSides(blocks, walls, surfaceIdEdgeIds, recombine);
                    foreach (var wall in walls) Gmsh.Model.Mesh.SetRecombine(2, wall.SurfaceId);
                }
                GenerateMesh(1);
                if (round == 1) _quadWildGroups = GetQuadWildGroups(blocks, walls, pinnedEdgeIdNumElements, recombine,
                                                                    sweepMesh.QuadWildFallbackToGmsh);
                // A run whose counts still hold keeps its nodes, and the walls beside them are matched to them
                HashSet<int> fixedNodeEdgeIds = new HashSet<int>();
                foreach (var group in _quadWildGroups)
                {
                    if (group.Mesh == null) continue;
                    //
                    List<int> changedEdgeIds;
                    if (SweepQuadWildMesher.ApplyEdgeNodes(group, out changedEdgeIds))
                    {
                        fixedNodeEdgeIds.UnionWith(group.EdgeIds);
                        continue;
                    }
                    foreach (var edgeId in changedEdgeIds)
                    {
                        if (!quadWildEdgeIdNumElements.ContainsKey(edgeId)) continue;
                        group.FixedEdgeIds.Add(edgeId);
                        quadWildEdgeIdNumElements[edgeId] = SweepWallTopology.GetElementCount(edgeId);
                    }
                    writeOutput("Sweep: the side surfaces changed the element count of " +
                                String.Join(", ", changedEdgeIds.Select(id => "Edge " + id)) + ", so QuadWild meshes " +
                                group.GetDescription() + " again.");
                    group.Mesh = null;
                }
                MatchOppositeSideSpacing(walls, fixedNodeEdgeIds);
                //
                List<SweepQuadWildGroup> groupsToMesh = _quadWildGroups.Where(group => group.Mesh == null).ToList();
                if (groupsToMesh.Count == 0) break;
                if (round == _maxQuadWildRounds)
                    throw new CaeException("The element counts of the sweep side surfaces and of the source surfaces " +
                                           "QuadWild meshes did not settle in " + _maxQuadWildRounds + " rounds.");
                // The triangulation QuadWild starts from
                GenerateMesh(2);
                foreach (var group in groupsToMesh)
                {
                    // A group QuadWild cannot mesh, or meshes without following its edges - a thin wedge between a
                    // feature line and the boundary meeting at a small angle is one - is left to gmsh, as a source is
                    // without QuadWild. Its surfaces touch no other group, so no other run depends on its edges
                    try
                    {
                        group.Mesh = SweepQuadWildMesher.Mesh(group, sweepMesh, _gmshData.QuadWildDirectory,
                                                              workDirectory, writeOutput);
                    }
                    catch (CaeException ex)
                    {
                        if (!sweepMesh.QuadWildFallbackToGmsh)
                            throw new CaeException("Sweep: " + ex.Message + " QuadWild failed to mesh " +
                                                   group.GetDescription() + " and the fallback to gmsh is turned off.");
                        writeOutput("Sweep: " + ex.Message + " Gmsh meshes " + group.GetDescription() + " instead of " +
                                    "QuadWild.");
                        _quadWildGroups.Remove(group);
                        _quadWildSourceIds.ExceptWith(group.SurfaceIds);
                        foreach (var surfaceId in group.SurfaceIds)
                        {
                            if (recombine) Gmsh.Model.Mesh.SetRecombine(2, surfaceId);
                        }
                        foreach (var edgeId in group.EdgeIds) quadWildEdgeIdNumElements.Remove(edgeId);
                        continue;
                    }
                    foreach (var edgeId in group.FreeEdgeIds)
                        quadWildEdgeIdNumElements[edgeId] = group.Mesh.GetNumOfElements(edgeId);
                }
            }
        }
        /// <summary>
        /// The QuadWild sources in groups of surfaces that touch along an edge. An edge is fixed when the user pinned
        /// its count, when it follows the sweep or lies on the target side of a wall, when it is one of a pair of
        /// imprint edges, or when it bounds a surface that is neither a QuadWild source nor a wall - another part, a
        /// target, or a source left to gmsh; every other edge is free. A surface whose every edge is fixed needs an
        /// even number of elements around it, since every quadrilateral has four sides, so one with an odd number
        /// is left to gmsh, as a source is without QuadWild, and the groups are formed again without it.
        /// </summary>
        private List<SweepQuadWildGroup> GetQuadWildGroups(List<SweepBlock> blocks, List<SweepWall> walls,
                                                           Dictionary<int, int> pinnedEdgeIdNumElements, bool recombine,
                                                           bool fallbackToGmsh)
        {
            HashSet<int> wallSurfaceIds = new HashSet<int>(walls.Select(wall => wall.SurfaceId));
            // Counts no wall derives from another: the pins, the layers and the imprint pairs. A wall's side is set by
            // them when the other side is, so they spread over the walls; the sides no wall derives from them are
            // derived from a source side, and are fixed for QuadWild as target sides, but not in turn
            HashSet<int> setEdgeIds = new HashSet<int>(pinnedEdgeIdNumElements.Keys);
            foreach (var wall in walls) setEdgeIds.UnionWith(wall.DirectionEdgeIds);
            foreach (var block in blocks)
            {
                if (block.ImprintEdgeCopyIds == null) continue;
                foreach (var entry in block.ImprintEdgeCopyIds)
                {
                    setEdgeIds.Add(entry.Key);
                    setEdgeIds.Add(entry.Value);
                }
            }
            bool added = true;
            while (added)
            {
                added = false;
                foreach (var wall in walls)
                {
                    if (wall.SourceEdgeIds.Length == 0 || wall.TargetEdgeIds.Length == 0) continue;
                    if (wall.TargetEdgeIds.All(setEdgeIds.Contains))
                        foreach (var edgeId in wall.SourceEdgeIds) added |= setEdgeIds.Add(edgeId);
                    if (wall.SourceEdgeIds.All(setEdgeIds.Contains))
                        foreach (var edgeId in wall.TargetEdgeIds) added |= setEdgeIds.Add(edgeId);
                }
            }
            HashSet<int> alwaysFixedEdgeIds = new HashSet<int>(setEdgeIds);
            foreach (var wall in walls) alwaysFixedEdgeIds.UnionWith(wall.TargetEdgeIds);
            // A wall with one edge on each side carries one count across, so the walls join edges into runs that carry
            // a single count. A run through two free edges of QuadWild sources - the ends of two cells meeting, over
            // the walls of the cells between, on one edge of the part beside - would have QuadWild choose each of them
            // on its own, and counts no wall can take both of. The edges of such a run keep the count the walls settle.
            // A wall whose two sides are split at corresponding places carries each piece's count to the piece
            // opposite it, and two opposite edges of a source or a target face, both on the sides of walls, are brought
            // to one count as well, so both join the runs too. Left out, a wall split in two on both sides was fed from
            // two QuadWild sources through the face at its far end: QuadWild chose them apart, the face then held the
            // far piece to the other source's count, and every grid line of the wall leaned from one split to the other
            int[] orderedSourceEdgeIds;
            int[] orderedTargetEdgeIds;
            HashSet<int> crossSideEdgeIds = new HashSet<int>();
            Dictionary<int, int> runParent = new Dictionary<int, int>();
            foreach (var wall in walls)
            {
                crossSideEdgeIds.UnionWith(wall.SourceEdgeIds);
                crossSideEdgeIds.UnionWith(wall.TargetEdgeIds);
                if (wall.SourceEdgeIds.Length == 1 && wall.TargetEdgeIds.Length == 1)
                    Unite(runParent, wall.SourceEdgeIds[0], wall.TargetEdgeIds[0]);
                else if (wall.SourceEdgeIds.Length > 1 &&
                         SweepWallTopology.OrderWallSides(wall, out orderedSourceEdgeIds, out orderedTargetEdgeIds))
                {
                    for (int i = 0; i < orderedSourceEdgeIds.Length; i++)
                        Unite(runParent, orderedSourceEdgeIds[i], orderedTargetEdgeIds[i]);
                }
            }
            int[] adjacentFaceIds;
            int[] adjacentVertexIds;
            foreach (var edgeId in crossSideEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out adjacentFaceIds, out adjacentVertexIds);
                foreach (var faceId in adjacentFaceIds)
                {
                    int oppositeEdgeId;
                    if (wallSurfaceIds.Contains(faceId) || _quadWildSourceIds.Contains(faceId)) continue;
                    if (TryGetOppositeEdgeId(edgeId, faceId, adjacentVertexIds, out oppositeEdgeId) &&
                        crossSideEdgeIds.Contains(oppositeEdgeId))
                        Unite(runParent, edgeId, oppositeEdgeId);
                }
            }
            int[] surfaceEdgeIds;
            HashSet<int> quadWildEdgeIds = new HashSet<int>();
            foreach (var surfaceId in _quadWildSourceIds)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out surfaceEdgeIds);
                quadWildEdgeIds.UnionWith(surfaceEdgeIds);
            }
            foreach (var run in runParent.Keys.ToArray().GroupBy(id => Find(runParent, id)))
            {
                if (run.Count(id => quadWildEdgeIds.Contains(id) && !alwaysFixedEdgeIds.Contains(id)) > 1)
                    alwaysFixedEdgeIds.UnionWith(run);
            }
            // An edge splitting a target and the edge of the source below it that no pin and no wall gives a count take the
            // count of the edge opposite them in their face, for the face to be a grid. Chosen by QuadWild, that edge
            // moved the split every round, QuadWild chose it again for the split's new count, and the rounds never settled
            HashSet<int> wallSideEdgeIds = new HashSet<int>();
            foreach (var wall in walls)
            {
                wallSideEdgeIds.UnionWith(wall.SourceEdgeIds);
                wallSideEdgeIds.UnionWith(wall.TargetEdgeIds);
                wallSideEdgeIds.UnionWith(wall.DirectionEdgeIds);
            }
            foreach (var block in blocks)
            {
                if (block.ImprintEdgeCopyIds == null) continue;
                foreach (var entry in block.ImprintEdgeCopyIds)
                {
                    int[] pairEdgeIds = new int[] { entry.Key, entry.Value };
                    if (pairEdgeIds.Any(id => pinnedEdgeIdNumElements.ContainsKey(id) || wallSideEdgeIds.Contains(id)))
                        continue;
                    foreach (var pairEdgeId in pairEdgeIds)
                    {
                        Gmsh.Model.GetAdjacencies(1, pairEdgeId, out adjacentFaceIds, out adjacentVertexIds);
                        foreach (var faceId in adjacentFaceIds)
                        {
                            int oppositeEdgeId;
                            if (TryGetOppositeEdgeId(pairEdgeId, faceId, adjacentVertexIds, out oppositeEdgeId))
                                alwaysFixedEdgeIds.Add(oppositeEdgeId);
                        }
                    }
                }
            }
            //
            int[] edgeIds;
            int[] faceIds;
            int[] vertexIds;
            while (true)
            {
                Dictionary<int, int[]> surfaceIdEdgeIds = new Dictionary<int, int[]>();
                HashSet<int> fixedEdgeIds = new HashSet<int>();
                foreach (var surfaceId in _quadWildSourceIds.OrderBy(id => id))
                {
                    Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                    surfaceIdEdgeIds[surfaceId] = edgeIds;
                    foreach (var edgeId in edgeIds)
                    {
                        if (alwaysFixedEdgeIds.Contains(edgeId)) fixedEdgeIds.Add(edgeId);
                        else
                        {
                            Gmsh.Model.GetAdjacencies(1, edgeId, out faceIds, out _);
                            if (faceIds.Any(id => !_quadWildSourceIds.Contains(id) && !wallSurfaceIds.Contains(id)))
                                fixedEdgeIds.Add(edgeId);
                        }
                    }
                }
                //
                int oddSurfaceId = 0;
                int numOfElements = 0;
                foreach (var entry in surfaceIdEdgeIds)
                {
                    if (!entry.Value.All(fixedEdgeIds.Contains)) continue;
                    numOfElements = entry.Value.Sum(id => SweepWallTopology.GetElementCount(id));
                    if (numOfElements % 2 == 0) continue;
                    oddSurfaceId = entry.Key;
                    break;
                }
                if (oddSurfaceId != 0)
                {
                    if (!fallbackToGmsh)
                        throw new CaeException("Sweep: every edge of Surface " + oddSurfaceId + " is fixed and they " +
                                               "carry " + numOfElements + " elements together, an odd number no " +
                                               "quadrilateral mesh can have, and the fallback to gmsh is turned off.");
                    _quadWildSourceIds.Remove(oddSurfaceId);
                    if (recombine) Gmsh.Model.Mesh.SetRecombine(2, oddSurfaceId);
                    _writeOutput?.Invoke(NameGmshEntities("Sweep: every edge of Surface " + oddSurfaceId + " is fixed " +
                                                          "and they carry " + numOfElements + " elements together, an " +
                                                          "odd number no quadrilateral mesh can have, so the surface " +
                                                          "is meshed with gmsh instead of QuadWild."));
                    continue;
                }
                // Surfaces sharing an edge are meshed together
                Dictionary<int, int> parent = new Dictionary<int, int>();
                Dictionary<int, int> edgeIdSurfaceId = new Dictionary<int, int>();
                foreach (var entry in surfaceIdEdgeIds)
                {
                    Find(parent, entry.Key);
                    foreach (var edgeId in entry.Value)
                    {
                        int otherSurfaceId;
                        if (edgeIdSurfaceId.TryGetValue(edgeId, out otherSurfaceId)) Unite(parent, entry.Key, otherSurfaceId);
                        else edgeIdSurfaceId[edgeId] = entry.Key;
                    }
                }
                List<SweepQuadWildGroup> groups = new List<SweepQuadWildGroup>();
                foreach (var surfaceIds in surfaceIdEdgeIds.Keys.GroupBy(id => Find(parent, id))
                                                                .Select(group => group.OrderBy(id => id).ToArray())
                                                                .OrderBy(ids => ids[0]))
                {
                    SortedSet<int> groupEdgeIds = new SortedSet<int>();
                    foreach (var surfaceId in surfaceIds) groupEdgeIds.UnionWith(surfaceIdEdgeIds[surfaceId]);
                    SortedSet<int> groupVertexIds = new SortedSet<int>();
                    foreach (var edgeId in groupEdgeIds)
                    {
                        Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                        groupVertexIds.UnionWith(vertexIds);
                    }
                    groups.Add(new SweepQuadWildGroup(groups.Count + 1, surfaceIds, groupEdgeIds.ToArray(),
                                                      groupVertexIds.ToArray(),
                                                      new HashSet<int>(groupEdgeIds.Where(fixedEdgeIds.Contains))));
                }
                return groups;
            }
        }
        /// <summary>
        /// Places the nodes of the two opposite sides of every wall across from each other. A wall is meshed as a
        /// structured grid, which joins the k-th node of one side to the k-th node of the other, but the two
        /// sides are meshed as curves of their own: a side of several edges carries the density of each of them -
        /// a strip of a neighbouring part a fraction of a millimetre wide keeps the two elements its own walls
        /// need - while a side of one edge spreads the same number of nodes evenly. Past such an edge every grid
        /// line of the wall then leans over by the elements it is ahead, the face beside the wall follows the lean,
        /// and it folds against a hole whose own walls are straight. Both sides are treated alike: a node that
        /// cannot move - a vertex, or a node of an edge another wall uses as well - fixes the position of its
        /// partner on the other side, less the mismatch two fixed partners leave, which is spread evenly between
        /// such pairs. The source and target sides are matched, and so are the two sides along the sweep, but
        /// only on a wall whose one side is the other moved.
        /// </summary>
        private void MatchOppositeSideSpacing(List<SweepWall> walls, HashSet<int> fixedEdgeIds)
        {
            int count;
            Dictionary<int, int> edgeIdNumOfUses = new Dictionary<int, int>();
            foreach (var wall in walls)
            {
                foreach (var edgeId in wall.SourceEdgeIds.Concat(wall.TargetEdgeIds).Concat(wall.DirectionEdgeIds))
                {
                    edgeIdNumOfUses.TryGetValue(edgeId, out count);
                    edgeIdNumOfUses[edgeId] = count + 1;
                }
            }
            // The two cross-sweep sides of a wall are matched only where they lie on the source and on the target
            // of a block. A body whose walls stand in rings - a bar cut by the plates it runs through - has walls
            // whose sides are the boundaries between the rings, and the layers of the whole block are placed from
            // its source to its target: matching the last ring's end to the ring boundary above it, rather than to
            // the source, skews every ring in between
            int[] edgeIds;
            HashSet<int> sourceEdgeIds = new HashSet<int>();
            HashSet<int> targetEdgeIds = new HashSet<int>();
            if (_sweepBlocks != null)
            {
                foreach (var block in _sweepBlocks)
                {
                    foreach (var surfaceId in block.SourceSurfaceIds)
                    {
                        Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                        sourceEdgeIds.UnionWith(edgeIds);
                    }
                    foreach (var surfaceId in block.TargetSurfaceIds)
                    {
                        Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                        targetEdgeIds.UnionWith(edgeIds);
                    }
                }
            }
            //
            foreach (var wall in walls)
            {
                if (wall.SourceEdgeIds.All(sourceEdgeIds.Contains) && wall.TargetEdgeIds.All(targetEdgeIds.Contains))
                    MatchSideSpacing(wall.SourceEdgeIds, wall.TargetEdgeIds, true, edgeIdNumOfUses, fixedEdgeIds);
                List<int[]> directionSides = GetEdgeChains(wall.DirectionEdgeIds);
                if (directionSides.Count == 2)
                    MatchSideSpacing(directionSides[0], directionSides[1], false, edgeIdNumOfUses, fixedEdgeIds);
            }
        }
        private static void MatchSideSpacing(int[] edgeIds1, int[] edgeIds2, bool firstIsSource,
                                             Dictionary<int, int> edgeIdNumOfUses, HashSet<int> fixedEdgeIds)
        {
            Func<int, bool> isMovable = id => edgeIdNumOfUses[id] == 1 && (fixedEdgeIds == null || !fixedEdgeIds.Contains(id));
            if (!edgeIds1.Concat(edgeIds2).Any(isMovable)) return;
            SweepSideNodes side1 = SweepSideNodes.Create(edgeIds1);
            SweepSideNodes side2 = SweepSideNodes.Create(edgeIds2);
            if (side1 == null || side2 == null || side1.NumOfNodes != side2.NumOfNodes) return;
            int numOfNodes = side1.NumOfNodes;
            // Whether the second side runs the way the first does
            double along = 0;
            double reversed = 0;
            for (int i = 0; i < 3; i++)
            {
                double d1 = side1.EndPoint[i] - side1.StartPoint[i];
                double d2 = side2.EndPoint[i] - side2.StartPoint[i];
                along += Math.Pow(d1 - d2, 2);
                reversed += Math.Pow(d1 + d2, 2);
            }
            bool sameDirection = along <= reversed;
            // Only a wall whose one side is the other moved - both ends moved the same way - has a grid whose lines
            // can all run straight across. A tapering wall, whose one end leans along a chamfer while the other
            // stands upright, has none, and the even spacing gmsh gives its sides was measured to sweep better than
            // one that follows the fractions of the other side
            double[] startOffset = new double[3];
            double[] endOffset = new double[3];
            for (int i = 0; i < 3; i++)
            {
                startOffset[i] = (sameDirection ? side2.StartPoint[i] : side2.EndPoint[i]) - side1.StartPoint[i];
                endOffset[i] = (sameDirection ? side2.EndPoint[i] : side2.StartPoint[i]) - side1.EndPoint[i];
            }
            double sideLength = Math.Min(GetDistance(side1.StartPoint, side1.EndPoint),
                                         GetDistance(side2.StartPoint, side2.EndPoint));
            if (GetDistance(startOffset, endOffset) > _sideTranslationTolerance * sideLength) return;
            Func<int, int> partner = k => sameDirection ? k : numOfNodes - 1 - k;
            Func<int, double> fraction2 = k => sameDirection ? side2.GetFraction(partner(k))
                                                              : 1 - side2.GetFraction(partner(k));
            Func<SweepSideNodes, int, bool> isFixed = (side, k) =>
            {
                int edgeId = side.GetEdgeId(k);
                return edgeId == 0 || !isMovable(edgeId);
            };
            // The mismatch between the two sides is what a node that neither side can move leaves behind: a
            // vertex of one side opposite a node the other side shares with another wall. It is spread evenly
            // over the nodes up to the next such pair, and a node that can move is placed at its partner's
            // position plus the mismatch there. A node moved to lie exactly opposite its partner instead would
            // leave the whole mismatch to the one element next to the fixed pair - a sliver, where the even
            // spacing gmsh had given the side was already the best that could be done
            bool[] fixed1 = new bool[numOfNodes];
            bool[] fixed2 = new bool[numOfNodes];
            List<int> hardAnchors = new List<int>();
            for (int k = 0; k < numOfNodes; k++)
            {
                fixed1[k] = isFixed(side1, k);
                fixed2[k] = isFixed(side2, partner(k));
                if (fixed1[k] && fixed2[k]) hardAnchors.Add(k);
            }
            double[] mismatch = new double[numOfNodes];
            foreach (var k in hardAnchors) mismatch[k] = side1.GetFraction(k) - fraction2(k);
            for (int a = 0; a + 1 < hardAnchors.Count; a++)
            {
                int first = hardAnchors[a];
                int last = hardAnchors[a + 1];
                for (int k = first + 1; k < last; k++)
                {
                    double t = (double)(k - first) / (last - first);
                    mismatch[k] = mismatch[first] + (mismatch[last] - mismatch[first]) * t;
                }
            }
            // Where both nodes of a pair can move they keep a grading. Across the sweep it is the grading of the
            // source side: the source surface is meshed from its boundary nodes, and moving them to suit the
            // target - which is only ever projected onto - was measured to give a source mesh of more and worse
            // elements. The two sides along the sweep share the average of theirs
            // A source node moved opposite a node of the target that cannot move takes its neighbours along: the
            // correction it needs is spread evenly towards the next node that cannot move or is moved itself, so
            // that the source keeps an even spacing instead of one short element beside the moved node
            double[] sourceCorrections = new double[numOfNodes];
            if (firstIsSource)
            {
                List<int> correctionAnchors = new List<int>();
                for (int k = 0; k < numOfNodes; k++)
                {
                    if (fixed1[k]) correctionAnchors.Add(k);
                    else if (fixed2[k])
                    {
                        sourceCorrections[k] = fraction2(k) + mismatch[k] - side1.GetFraction(k);
                        correctionAnchors.Add(k);
                    }
                }
                for (int a = 0; a + 1 < correctionAnchors.Count; a++)
                {
                    int first = correctionAnchors[a];
                    int last = correctionAnchors[a + 1];
                    for (int k = first + 1; k < last; k++)
                    {
                        double t = (double)(k - first) / (last - first);
                        sourceCorrections[k] = sourceCorrections[first] +
                                               (sourceCorrections[last] - sourceCorrections[first]) * t;
                    }
                }
            }
            double[] fractions1 = new double[numOfNodes];
            double[] fractions2 = new double[numOfNodes];
            for (int k = 0; k < numOfNodes; k++)
            {
                double own1 = side1.GetFraction(k);
                double own2 = fraction2(k);
                if (fixed1[k] || fixed2[k])
                {
                    fractions1[k] = fixed1[k] ? own1 : own2 + mismatch[k];
                    fractions2[k] = fixed2[k] ? own2 : own1 - mismatch[k];
                }
                else if (firstIsSource)
                {
                    fractions1[k] = own1 + sourceCorrections[k];
                    fractions2[k] = fractions1[k] - mismatch[k];
                }
                else
                {
                    double average = 0.5 * (own1 + own2);
                    fractions1[k] = average + 0.5 * mismatch[k];
                    fractions2[k] = average - 0.5 * mismatch[k];
                }
            }
            // Each side takes its positions for the nodes it can move, an edge at a time, and keeps its own nodes on
            // an edge the positions would not keep in order inside it
            MoveSideNodes(side1, k => k, k => fractions1[k], fixed1);
            MoveSideNodes(side2, k => partner(k), k => sameDirection ? fractions2[k] : 1 - fractions2[k], fixed2);
        }
        private static void MoveSideNodes(SweepSideNodes side, Func<int, int> nodeIndex, Func<int, double> fraction,
                                          bool[] isFixed)
        {
            int numOfNodes = side.NumOfNodes;
            Dictionary<int, List<Tuple<int, double>>> edgeIdMoves = new Dictionary<int, List<Tuple<int, double>>>();
            for (int k = 0; k < numOfNodes; k++)
            {
                if (isFixed[k]) continue;
                int index = nodeIndex(k);
                int edgeId = side.GetEdgeId(index);
                if (!edgeIdMoves.ContainsKey(edgeId)) edgeIdMoves[edgeId] = new List<Tuple<int, double>>();
                edgeIdMoves[edgeId].Add(new Tuple<int, double>(index, fraction(k)));
            }
            foreach (var moves in edgeIdMoves.Values)
            {
                double start;
                double end;
                side.GetEdgeFractions(moves[0].Item1, out start, out end);
                List<Tuple<int, double>> ordered = moves.OrderBy(move => move.Item1).ToList();
                // The nodes only have to stay in order: spacing as tight as the other side's is what the other side
                // already has
                double minGap = 1E-6 * (end - start);
                bool valid = true;
                double previous = start;
                foreach (var move in ordered)
                {
                    if (move.Item2 - previous < minGap) { valid = false; break; }
                    previous = move.Item2;
                }
                if (!valid || end - previous < minGap) continue;
                // A node already where it should be is not set again, which would only move it by the error of
                // measuring the curve
                double tolerance = 0.05 * (end - start) / (ordered.Count + 1);
                foreach (var move in ordered)
                {
                    if (Math.Abs(move.Item2 - side.GetFraction(move.Item1)) > tolerance) side.MoveNode(move.Item1, move.Item2);
                }
            }
        }
        /// <summary>
        /// The edges split into the chains they form, the edges of one chain sharing their vertices.
        /// </summary>
        private static List<int[]> GetEdgeChains(int[] edgeIds)
        {
            int[] vertexIds;
            Dictionary<int, int> parent = new Dictionary<int, int>();
            foreach (var edgeId in edgeIds)
            {
                Find(parent, -edgeId);
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                foreach (var vertexId in vertexIds) Unite(parent, -edgeId, vertexId);
            }
            return edgeIds.GroupBy(id => Find(parent, -id)).Select(group => group.ToArray()).ToList();
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
                if (block.TargetSurfaceIds.Count < 1)
                    throw new CaeException("No target surface could be determined for " + block.GetDescription() + ".");
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
                throw new CaeException("The volume of " + block.GetDescription() + " could not be determined from its " +
                                       "surfaces.");
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
        private List<int[]> GetCrossSectionVertexIds(out List<int> groupBlockIndices)
        {
            int[] edgeIds;
            int[] vertexIds;
            List<int[]> groups = new List<int[]>();
            groupBlockIndices = new List<int>();
            if (_sweepBlocks == null) return groups;
            for (int blockIndex = 0; blockIndex < _sweepBlocks.Count; blockIndex++)
            {
                SweepBlock block = _sweepBlocks[blockIndex];
                foreach (var surfaceId in block.SourceSurfaceIds.Concat(block.TargetSurfaceIds))
                {
                    HashSet<int> group = new HashSet<int>();
                    Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                    foreach (var edgeId in edgeIds)
                    {
                        Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                        group.UnionWith(vertexIds);
                    }
                    if (group.Count > 1)
                    {
                        groups.Add(group.ToArray());
                        groupBlockIndices.Add(blockIndex);
                    }
                }
            }
            return groups;
        }
        /// <summary>
        /// The layer count of every edge on the source or target side of a wall that the levels along the sweep
        /// have already given a count, because it follows the sweep in a wall of another block.
        /// </summary>
        private static Dictionary<int, int> GetLayerCrossEdgePins(List<SweepWall> walls,
                                                                  Dictionary<int, int> directionEdgeIdNumOfElements)
        {
            int numOfElements;
            Dictionary<int, int> pins = new Dictionary<int, int>();
            foreach (var wall in walls)
            {
                foreach (var edgeId in wall.SourceEdgeIds.Concat(wall.TargetEdgeIds))
                {
                    if (directionEdgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) pins[edgeId] = numOfElements;
                }
            }
            return pins;
        }
        /// <summary>
        /// The classes of edges a transfinite source ties together: the two opposite sides of every four sided
        /// source the pass before the sweep made transfinite, and the two sides of every wall with one edge on
        /// each, carry one count. Only the classes holding an edge of such a source are returned.
        /// </summary>
        private List<int[]> GetTransfiniteSourceTiedEdgeGroups(List<SweepWall> walls)
        {
            List<int[]> groups = new List<int[]>();
            if (_sweepBlocks == null) return groups;
            //
            int[] edgeIds;
            int[] vertexIds;
            Dictionary<int, int> parent = new Dictionary<int, int>();
            HashSet<int> transfiniteSourceEdgeIds = new HashSet<int>();
            HashSet<int> checkedSurfaceIds = new HashSet<int>();
            foreach (var block in _sweepBlocks)
            {
                foreach (var surfaceId in block.SourceSurfaceIds)
                {
                    if (!checkedSurfaceIds.Add(surfaceId) || !_transfiniteSurfaceIds.Contains(surfaceId)) continue;
                    Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                    if (edgeIds.Length != 4) continue;
                    int[][] edgeVertexIds = new int[4][];
                    for (int j = 0; j < 4; j++)
                    {
                        Gmsh.Model.GetAdjacencies(1, edgeIds[j], out _, out vertexIds);
                        edgeVertexIds[j] = vertexIds;
                    }
                    int opposite = -1;
                    for (int j = 1; j < 4; j++)
                    {
                        if (!edgeVertexIds[0].Intersect(edgeVertexIds[j]).Any()) { opposite = j; break; }
                    }
                    if (opposite < 0) continue;
                    int[] others = Enumerable.Range(1, 3).Where(j => j != opposite).ToArray();
                    transfiniteSourceEdgeIds.UnionWith(edgeIds);
                    Unite(parent, edgeIds[0], edgeIds[opposite]);
                    Unite(parent, edgeIds[others[0]], edgeIds[others[1]]);
                }
            }
            if (transfiniteSourceEdgeIds.Count == 0) return groups;
            // A wall with one edge on each side carries one count across
            foreach (var wall in walls)
            {
                if (wall.SourceEdgeIds.Length == 1 && wall.TargetEdgeIds.Length == 1)
                    Unite(parent, wall.SourceEdgeIds[0], wall.TargetEdgeIds[0]);
            }
            //
            foreach (var group in parent.Keys.ToArray().GroupBy(id => Find(parent, id)))
            {
                int[] classEdgeIds = group.OrderBy(id => id).ToArray();
                if (classEdgeIds.Any(id => transfiniteSourceEdgeIds.Contains(id))) groups.Add(classEdgeIds);
            }
            return groups;
        }
        /// <summary>
        /// One count for every class of edges a transfinite source ties together. The pass before the sweep makes
        /// every four sided source transfinite, and such a surface needs the same count on its two opposite
        /// sides; a wall with one edge on each side needs the same count on both as well. The edges tied by
        /// these two relations form classes, and every class holding an edge of a transfinite source is given
        /// one count: the count the user pinned on one of its edges, else the count the levels along the sweep
        /// gave one of them - such an edge is a layer edge of a neighbouring block - else the smallest of the
        /// measured counts, since a user who wants the mesh finer refines it far more easily than the other way. A class whose fixed counts disagree is left alone, and the source falls to the free
        /// mesh later.
        /// </summary>
        private Dictionary<int, int> PinTransfiniteSourceSides(List<int[]> tiedEdgeGroups,
                                                               Dictionary<int, int> directionEdgeIdNumOfElements)
        {
            Dictionary<int, int> pins = new Dictionary<int, int>();
            foreach (var classEdgeIds in tiedEdgeGroups)
            {
                int count;
                int fixedCount = -1;
                string origin = null;
                bool conflict = false;
                foreach (var edgeId in classEdgeIds)
                {
                    if (!_gmshData.EdgeIdNumElements.TryGetValue(edgeId, out count)) continue;
                    if (fixedCount < 0) { fixedCount = count; origin = "the count the user set"; }
                    else if (fixedCount != count) conflict = true;
                }
                if (fixedCount < 0)
                {
                    foreach (var edgeId in classEdgeIds)
                    {
                        if (!directionEdgeIdNumOfElements.TryGetValue(edgeId, out count)) continue;
                        if (fixedCount < 0) { fixedCount = count; origin = "the layer count of the neighbouring block"; }
                        else if (fixedCount != count) conflict = true;
                    }
                }
                if (conflict) continue;
                bool hasLayerEdge = classEdgeIds.Any(id => directionEdgeIdNumOfElements.ContainsKey(id));
                int[] measured = classEdgeIds.Select(id => SweepWallTopology.GetElementCount(id)).ToArray();
                if (fixedCount < 0) { fixedCount = measured.Min(); origin = "the smallest of them"; }
                // A class whose edges already carry one count has nothing to enforce, and is left to the walls -
                // unless one of them is a layer edge, which the walls no longer move off its layer count, so that
                // the edges opposite it have to be held to that count as well
                if (measured.All(m => m == fixedCount) && !hasLayerEdge) continue;
                foreach (var edgeId in classEdgeIds) pins[edgeId] = fixedCount;
                //
                _writeOutput?.Invoke(NameGmshEntities("Sweep: " + string.Join(", ", classEdgeIds.Select(id => "Edge " + id)) +
                                                      " are tied together by the transfinite source surface/s " +
                                                      string.Join(", ", GetTransfiniteSourceIdsOfEdges(classEdgeIds)
                                                                            .Select(id => "Surface " + id)) +
                                                      ", measured " + string.Join(", ", measured) +
                                                      " element/s and are all given " + fixedCount + ", " + origin + "."));
            }
            return pins;
        }
        /// <summary>
        /// The transfinite sources the given edges lie on, for a message to name them.
        /// </summary>
        private SortedSet<int> GetTransfiniteSourceIdsOfEdges(int[] edgeIds)
        {
            int[] surfaceEdgeIds;
            SortedSet<int> surfaceIds = new SortedSet<int>();
            if (_sweepBlocks == null) return surfaceIds;
            //
            foreach (var block in _sweepBlocks)
            {
                foreach (var surfaceId in block.SourceSurfaceIds)
                {
                    if (!_transfiniteSurfaceIds.Contains(surfaceId)) continue;
                    Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out surfaceEdgeIds);
                    if (surfaceEdgeIds.Intersect(edgeIds).Any()) surfaceIds.Add(surfaceId);
                }
            }
            return surfaceIds;
        }
        private static void Unite(Dictionary<int, int> parent, int a, int b)
        {
            int ra = Find(parent, a);
            int rb = Find(parent, b);
            if (ra != rb) parent[ra] = rb;
        }
        private static int Find(Dictionary<int, int> parent, int id)
        {
            int p;
            if (!parent.TryGetValue(id, out p)) { parent[id] = id; return id; }
            while (p != id)
            {
                id = p;
                p = parent[id];
            }
            return id;
        }
        /// <summary>
        /// Meshes free every four sided source surface whose opposite sides were given different element counts.
        /// The pass before the sweep makes every four sided surface transfinite, but the counts on the edges of a
        /// source are settled wall by wall, and an edge shared with a neighbouring part carries the count that
        /// part's walls need, so two opposite edges settled by different walls need not agree. gmsh refuses such
        /// a surface with an exception rather than meshing it free, so the constraint is taken off first and the
        /// source is meshed the way a source carrying an imprint is. The recombination is set again, since the
        /// constraints go together.
        /// </summary>
        private void FreeSourcesWithUnequalSides(List<SweepBlock> blocks, List<SweepWall> walls,
                                                 Dictionary<int, int[]> surfaceIdEdgeIds, bool recombine)
        {
            int[] edgeIds;
            int[] vertexIds;
            HashSet<int> checkedSurfaceIds = new HashSet<int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                foreach (var surfaceId in blocks[i].SourceSurfaceIds)
                {
                    if (!checkedSurfaceIds.Add(surfaceId)) continue;
                    // A source that is the cap of the block before it is meshed free already
                    if (!_transfiniteSurfaceIds.Contains(surfaceId)) continue;
                    if (!surfaceIdEdgeIds.TryGetValue(surfaceId, out edgeIds) || edgeIds.Length != 4) continue;
                    // Every edge must carry a settled count
                    int[] counts = new int[4];
                    bool settled = true;
                    for (int j = 0; j < 4 && settled; j++) settled = _gmshData.EdgeIdNumElements.TryGetValue(edgeIds[j], out counts[j]);
                    if (!settled) continue;
                    // The edge opposite the first is the one sharing no vertex with it; the other two are opposite each other
                    int[][] edgeVertexIds = new int[4][];
                    for (int j = 0; j < 4; j++)
                    {
                        Gmsh.Model.GetAdjacencies(1, edgeIds[j], out _, out vertexIds);
                        edgeVertexIds[j] = vertexIds;
                    }
                    int opposite = -1;
                    for (int j = 1; j < 4; j++)
                    {
                        if (!edgeVertexIds[0].Intersect(edgeVertexIds[j]).Any()) { opposite = j; break; }
                    }
                    if (opposite < 0) continue;
                    int[] others = Enumerable.Range(1, 3).Where(j => j != opposite).ToArray();
                    if (counts[0] == counts[opposite] && counts[others[0]] == counts[others[1]]) continue;
                    //
                    Gmsh.Model.Mesh.RemoveConstraints(new Tuple<int, int>[] { new Tuple<int, int>(2, surfaceId) });
                    _transfiniteSurfaceIds.Remove(surfaceId);
                    if (recombine) Gmsh.Model.Mesh.SetRecombine(2, surfaceId);
                    _writeOutput?.Invoke(NameGmshEntities("Sweep: Surface " + surfaceId + ", the source of sweep block " + (i + 1) +
                                                          ", was given " + counts[0] + " and " + counts[opposite] + " elements on one " +
                                                          "pair of opposite sides and " + counts[others[0]] + " and " + counts[others[1]] +
                                                          " on the other, so it is meshed free rather than as a structured grid. " +
                                                          "Its edges: " + DescribeEdgeRoles(edgeIds, walls) + "."));
                }
            }
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
        /// <summary>
        /// Writes, for every surface gmsh refused as a transfinite surface, what the surface is to the sweep and
        /// the edges of each of its sides with the element count the sweep gave the edge and the count the curve
        /// mesh carries, so that the side the counts disagree on can be read off the output instead of guessed
        /// at. A wall is written side by side; a source or a target - constrained by the transfinite pass that
        /// runs before the sweep when it has four sides - is written with the blocks it belongs to and its edges.
        /// The refused surfaces are returned, for the error to name them.
        /// </summary>
        private List<int> ReportRefusedSurfaces(List<SweepWall> walls, List<SweepBlock> blocks,
                                                Dictionary<int, int[]> surfaceIdEdgeIds, int logLength)
        {
            List<int> refusedSurfaceIds = new List<int>();
            Regex refused = new Regex(@"Surface (\d+) cannot be meshed using the transfinite");
            foreach (var line in Gmsh.Logger.Get().Skip(logLength))
            {
                Match match = refused.Match(line);
                if (!match.Success) continue;
                int surfaceId = int.Parse(match.Groups[1].Value);
                if (!refusedSurfaceIds.Contains(surfaceId)) refusedSurfaceIds.Add(surfaceId);
                SweepWall wall = walls.FirstOrDefault(w => w.SurfaceId == surfaceId);
                if (wall != null)
                {
                    _writeOutput?.Invoke(NameGmshEntities("Sweep: Surface " + surfaceId + " is a side surface - source side: " +
                                                          DescribeWallSide(wall.SourceEdgeIds) + "; target side: " +
                                                          DescribeWallSide(wall.TargetEdgeIds) + "; along the sweep: " +
                                                          DescribeWallSide(wall.DirectionEdgeIds) + "."));
                    continue;
                }
                List<string> roles = new List<string>();
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].SourceSurfaceIds.Contains(surfaceId)) roles.Add("the source of sweep block " + (i + 1));
                    if (blocks[i].TargetSurfaceIds.Contains(surfaceId)) roles.Add("the target of sweep block " + (i + 1));
                }
                int[] edgeIds;
                string edges = surfaceIdEdgeIds.TryGetValue(surfaceId, out edgeIds) ? DescribeWallSide(edgeIds) : "unknown";
                _writeOutput?.Invoke(NameGmshEntities("Sweep: Surface " + surfaceId + " is " +
                                                      (roles.Count > 0 ? string.Join(" and ", roles) : "no surface of any sweep block") +
                                                      " - its edges: " + edges + "."));
            }
            return refusedSurfaceIds;
        }
        private static string GetRefusedSurfacesMessage(List<int> refusedSurfaceIds)
        {
            return "The sweep surface/s " + string.Join(", ", refusedSurfaceIds.Select(id => "surface " + id)) +
                   " could not be meshed with a structured grid. See the gmsh messages above.";
        }
        /// <summary>
        /// The roles of the given edges in the walls: which wall has each of them as its source side, its target
        /// side or one of its edges along the sweep.
        /// </summary>
        private string DescribeEdgeRoles(int[] edgeIds, List<SweepWall> walls)
        {
            List<string> items = new List<string>();
            foreach (var edgeId in edgeIds)
            {
                List<string> roles = new List<string>();
                foreach (var wall in walls)
                {
                    if (wall.SourceEdgeIds.Contains(edgeId)) roles.Add("source side of Surface " + wall.SurfaceId);
                    if (wall.TargetEdgeIds.Contains(edgeId)) roles.Add("target side of Surface " + wall.SurfaceId);
                    if (wall.DirectionEdgeIds.Contains(edgeId)) roles.Add("along the sweep on Surface " + wall.SurfaceId);
                }
                int pinned;
                string pin = _transfiniteSourcePins.TryGetValue(edgeId, out pinned) ? ", tied to " + pinned : ", not tied";
                items.Add("Edge " + edgeId + " (" + (roles.Count > 0 ? string.Join(", ", roles) : "on no wall") + pin + ")");
            }
            return string.Join("; ", items);
        }
        private string DescribeWallSide(int[] edgeIds)
        {
            if (edgeIds.Length == 0) return "no edges";
            //
            int given;
            List<string> items = new List<string>();
            foreach (var edgeId in edgeIds)
            {
                string givenText = _gmshData.EdgeIdNumElements.TryGetValue(edgeId, out given) ? given.ToString() : "no";
                items.Add("Edge " + edgeId + " (" + givenText + " element/s given, " +
                          SweepWallTopology.GetElementCount(edgeId) + " meshed)");
            }
            return string.Join(", ", items);
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
        private Dictionary<int, int> PrepareWallEdges(List<SweepWall> walls, List<SweepChain> chains,
                                                      List<int> chainBlockIndices,
                                                      Dictionary<int, int> directionEdgeIdNumOfElements = null)
        {
            GenerateMesh(1);
            // How many elements each edge following the sweep direction gets, from the levels along the sweep
            // the chains define - so that the two sides of every wall add up to the same total, however each
            // of them is split. A direction edge on no chain keeps the count the user pinned on it, else the
            // measured one. Measured while the 1D mesh from GenerateMesh(1) above is still there.
            int numOfElements;
            List<int[]> tiedEdgeGroups = GetTransfiniteSourceTiedEdgeGroups(walls);
            List<int> crossSectionBlockIndices;
            List<int[]> crossSectionVertexIds = GetCrossSectionVertexIds(out crossSectionBlockIndices);
            if (directionEdgeIdNumOfElements == null)
            {
                directionEdgeIdNumOfElements =
                    SweepWallTopology.ResolveDirectionEdgeElements(chains, walls, crossSectionVertexIds,
                                                                   crossSectionBlockIndices, _gmshData.EdgeIdNumElements,
                                                                   chainBlockIndices, tiedEdgeGroups);
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
            }
            // A four sided source the pass before the sweep made transfinite needs one count on its two opposite
            // sides, while the counts on its edges are settled wall by wall - an edge it shares with a
            // neighbouring block is a layer edge of that block and carries its layer count. So every pair of
            // opposite edges of such a source is brought to one count first, and the walls on both sides of the
            // edge follow it: the count the user pinned on either edge, else the one the levels along the
            // sweep gave either of them, else the larger of the two measured
            Dictionary<int, int> transfiniteSourcePins = PinTransfiniteSourceSides(tiedEdgeGroups, directionEdgeIdNumOfElements);
            _transfiniteSourcePins = transfiniteSourcePins;
            // An edge that follows the sweep in the walls of one block and crosses it in a wall of another - a
            // vertical edge of a plate swept through its thickness, shared with a cell beside it swept along
            // the plate - carries the layer count the whole family of the first block walks. The walls of the
            // second are resolved around it, the way they are around the edges a transfinite source ties
            // together; left to them, the count they measure for their own side replaced the layer count and
            // the walls of the first block came out with a different number of layers on their two sides
            Dictionary<int, int> seededEdgeIdNumOfElements =
                MergeEdgeElements(GetLayerCrossEdgePins(walls, directionEdgeIdNumOfElements), transfiniteSourcePins);
            // The two cross-sweep sides of every wall, also measured while the 1D mesh is still there. The
            // source side is pinned as well, so that every edge of every wall carries a count
            Dictionary<int, int> sourceEdgeIdNumOfElements;
            Dictionary<int, int> targetEdgeIdNumOfElements;
            _sharedCrossEdgeIds = GetSharedCrossEdgeIds(walls);
            ResolveCrossEdgeElements(walls, null, seededEdgeIdNumOfElements.Count > 0 ? seededEdgeIdNumOfElements : null,
                                     seededEdgeIdNumOfElements, out sourceEdgeIdNumOfElements, out targetEdgeIdNumOfElements);
            // Two edges of one source or target surface lying opposite each other belong to the target sides
            // of two different walls, which share their own totals out and round them apart; they are brought
            // to one count and the sides resolved again from it
            Dictionary<int, int> crossEdgePins =
                GetOppositeCrossEdgePins(walls, MergeEdgeElements(sourceEdgeIdNumOfElements,
                                                                  targetEdgeIdNumOfElements), _quadWildSourceIds);
            crossEdgePins = MergeEdgeElements(crossEdgePins, seededEdgeIdNumOfElements);
            if (crossEdgePins.Count > seededEdgeIdNumOfElements.Count)
                ResolveCrossEdgeElements(walls, null, crossEdgePins, seededEdgeIdNumOfElements, out sourceEdgeIdNumOfElements,
                                         out targetEdgeIdNumOfElements);
            // An edge splitting a target and the edge of the source below it pair their nodes one to one, so
            // they carry the same count: the one the user pinned on either, else the one the walls settled
            Dictionary<int, int> imprintEdgeIdNumOfElements = new Dictionary<int, int>();
            if (_sweepBlocks != null)
            {
                // The walls settle such an edge themselves where it lies on the side of one of them - the edge
                // along which a wall of the next part meets the target of this one - and that count is the one
                // the wall's grid is built on, so it has to be kept: measuring the edge again, as this used to,
                // puts a count back under the wall that its other side does not carry. An edge that lies inside
                // its face instead lies opposite an edge of the same face whose count the walls have settled,
                // and the face is a grid only when the two agree. The measured count is the last resort
                Dictionary<int, int> settledEdgeIdNumOfElements =
                    MergeEdgeElements(directionEdgeIdNumOfElements, sourceEdgeIdNumOfElements,
                                      targetEdgeIdNumOfElements);
                foreach (var block in _sweepBlocks)
                {
                    if (block.ImprintEdgeCopyIds == null) continue;
                    foreach (var entry in block.ImprintEdgeCopyIds)
                    {
                        if (!_gmshData.EdgeIdNumElements.TryGetValue(entry.Key, out numOfElements) &&
                            !_gmshData.EdgeIdNumElements.TryGetValue(entry.Value, out numOfElements) &&
                            !settledEdgeIdNumOfElements.TryGetValue(entry.Key, out numOfElements) &&
                            !settledEdgeIdNumOfElements.TryGetValue(entry.Value, out numOfElements) &&
                            !TryGetOppositeEdgeElements(entry.Key, settledEdgeIdNumOfElements, out numOfElements) &&
                            !TryGetOppositeEdgeElements(entry.Value, settledEdgeIdNumOfElements, out numOfElements))
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
                ResolveCrossEdgeElements(walls, recountedLoops, crossEdgePins, seededEdgeIdNumOfElements,
                                         out sourceEdgeIdNumOfElements, out targetEdgeIdNumOfElements);
                edgeIdNumOfElements = MergeEdgeElements(directionEdgeIdNumOfElements, sourceEdgeIdNumOfElements,
                                                        targetEdgeIdNumOfElements, imprintEdgeIdNumOfElements);
            }
            foreach (var entry in edgeIdNumOfElements) _gmshData.EdgeIdNumElements[entry.Key] = entry.Value;
            //
            CheckWallElementCounts(walls, chains, edgeIdNumOfElements);
            Gmsh.Model.Mesh.Clear();    // must clear the mesh before the constraints are changed
            foreach (var entry in edgeIdNumOfElements) Gmsh.Model.Mesh.SetTransfiniteCurve(entry.Key, entry.Value + 1);
            return directionEdgeIdNumOfElements;
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
                                              Dictionary<int, int> pinnedEdgeIdNumOfElements,
                                              Dictionary<int, int> assignedEdgeIdNumOfElements,
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
            // The edges a transfinite source ties together are assigned before any wall is, so that a wall reading
            // one of them as its target is resolved from that side and carries the count onto its source
            HashSet<int> seededEdgeIds = new HashSet<int>();
            if (assignedEdgeIdNumOfElements != null)
            {
                foreach (var entry in assignedEdgeIdNumOfElements)
                {
                    assignedTable[entry.Key] = entry.Value;
                    seededEdgeIds.Add(entry.Key);
                }
            }
            //
            List<SweepWall> waitingWalls = new List<SweepWall>();
            foreach (var wall in walls)
            {
                if (wall.SourceEdgeIds.Length == 0 || wall.TargetEdgeIds.Length == 0) continue;
                if (HasUnassignedEdge(wall.SourceEdgeIds, recountedEdgeIds, assignedTable)) waitingWalls.Add(wall);
                else if (wall.TargetEdgeIds.Any(id => seededEdgeIds.Contains(id)) &&
                         AreAllAssigned(wall.TargetEdgeIds, assignedTable) && !AreAllAssigned(wall.SourceEdgeIds, assignedTable))
                {
                    ResolveWallCrossEdges(new SweepWall(wall.SurfaceId, wall.TargetEdgeIds, wall.SourceEdgeIds,
                                                        wall.DirectionEdgeIds), true, pinnedEdgeIdNumOfElements,
                                          sourceTable, assignedTable);
                }
                else ResolveWallCrossEdges(wall, false, pinnedEdgeIdNumOfElements, sourceTable, assignedTable);
            }
            while (waitingWalls.Count > 0)
            {
                bool resolved = false;
                for (int i = 0; i < waitingWalls.Count; i++)
                {
                    SweepWall wall = waitingWalls[i];
                    bool sourceAssigned = AreAllAssigned(wall.SourceEdgeIds, assignedTable);
                    if (!sourceAssigned && !AreAllAssigned(wall.TargetEdgeIds, assignedTable)) continue;
                    if (sourceAssigned)
                        ResolveWallCrossEdges(wall, false, pinnedEdgeIdNumOfElements, sourceTable, assignedTable);
                    else
                    {
                        ResolveWallCrossEdges(new SweepWall(wall.SurfaceId, wall.TargetEdgeIds, wall.SourceEdgeIds,
                                                            wall.DirectionEdgeIds), true, pinnedEdgeIdNumOfElements,
                                              sourceTable, assignedTable);
                    }
                    waitingWalls.RemoveAt(i--);
                    resolved = true;
                }
                if (resolved) continue;
                //
                int[] loop = GetLoopToRecount(recountedLoops, assignedTable);
                if (loop == null)
                {
                    foreach (var wall in waitingWalls)
                        ResolveWallCrossEdges(wall, false, pinnedEdgeIdNumOfElements, sourceTable, assignedTable);
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
        private void ResolveWallCrossEdges(SweepWall wall, bool reversed,
                                           Dictionary<int, int> pinnedEdgeIdNumOfElements,
                                           Dictionary<int, int> sourceEdgeIdNumOfElements,
                                           Dictionary<int, int> targetEdgeIdNumOfElements)
        {
            int numOfElements;
            int sourceElements = 0;
            foreach (var edgeId in wall.SourceEdgeIds)
            {
                // The target side of an earlier wall keeps the count that wall was given, and the walls
                // of the block fed from it must start from that count rather than from the measured one
                if (pinnedEdgeIdNumOfElements == null ||
                    !pinnedEdgeIdNumOfElements.TryGetValue(edgeId, out numOfElements))
                {
                    if (!targetEdgeIdNumOfElements.TryGetValue(edgeId, out numOfElements))
                        numOfElements = SweepWallTopology.GetElementCount(edgeId);
                }
                sourceEdgeIdNumOfElements[edgeId] = numOfElements;
                sourceElements += numOfElements;
            }
            RaiseSourceToTargetEdgeCount(wall, pinnedEdgeIdNumOfElements, sourceEdgeIdNumOfElements,
                                         targetEdgeIdNumOfElements, ref sourceElements);
            // Where a split was carried across, both sides of the wall are split at the same places. The
            // counts of the target edges follow the source nodes above them, so that the grid column
            // through the split stays straight
            int[] targetElements = DistributeTargetElementsByProjection(wall, sourceElements, reversed);
            // Where the two sides are split at corresponding places, every target segment takes the count of
            // the source segment opposite it, so that the grid column through the split runs straight
            if (targetElements == null) targetElements = DistributeTargetElementsBySide(wall, sourceEdgeIdNumOfElements);
            // Where the target is cut into more pieces than the source, each edge of the source shares its own
            // count out over the run of target edges below it
            if (targetElements == null) targetElements = DistributeTargetElementsByGroup(wall, sourceEdgeIdNumOfElements);
            if (targetElements == null)
            {
                double[] lengths = SweepWallTopology.GetEdgeLengths(wall.TargetEdgeIds);
                targetElements = SweepWallTopology.DistributeElements(sourceElements, lengths);
            }
            // An edge shared with another wall's target side through a source or a target surface takes the
            // count the two of them settled on, and an edge an earlier wall has already built its grid on keeps
            // the count it had there; the rest of the side is shared out again
            ApplyPinnedTargetElements(wall, targetElements,
                                      GetSettledTargetElements(wall, pinnedEdgeIdNumOfElements,
                                                               sourceEdgeIdNumOfElements, targetEdgeIdNumOfElements));
            for (int i = 0; i < wall.TargetEdgeIds.Length; i++)
                targetEdgeIdNumOfElements[wall.TargetEdgeIds[i]] = targetElements[i];
        }
        /// <summary>
        /// The counts the edges of a wall's target side have to keep: the one a pin gives, else the one an
        /// earlier wall already used the edge with, as its source or as its target. An edge of a compound is
        /// shared by the faces of several parts, so the wall of one column can be resolved from it long before
        /// the wall of another column reaches it as the tail of its own target side - the plate beside a cell
        /// of a split bent plate, whose shared wall is resolved with the plate. Sharing that side out by length
        /// again moves the count under the earlier wall, and the two sides of that wall no longer agree. Null
        /// when no edge of the side carries a count.
        /// </summary>
        private static Dictionary<int, int> GetSettledTargetElements(SweepWall wall,
                                                                     Dictionary<int, int> pinnedEdgeIdNumOfElements,
                                                                     Dictionary<int, int> sourceEdgeIdNumOfElements,
                                                                     Dictionary<int, int> targetEdgeIdNumOfElements)
        {
            int numOfElements;
            Dictionary<int, int> settledEdgeIdNumOfElements = null;
            foreach (var edgeId in wall.TargetEdgeIds)
            {
                if ((pinnedEdgeIdNumOfElements == null ||
                     !pinnedEdgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) &&
                    !sourceEdgeIdNumOfElements.TryGetValue(edgeId, out numOfElements) &&
                    !targetEdgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) continue;
                //
                if (settledEdgeIdNumOfElements == null) settledEdgeIdNumOfElements = new Dictionary<int, int>();
                settledEdgeIdNumOfElements[edgeId] = numOfElements;
            }
            return settledEdgeIdNumOfElements;
        }
        /// <summary>
        /// Raises the source side of a wall to the number of edges its target side is cut into. Every edge
        /// carries at least one element, so a target cut in two by a chamfer needs two of them, and a source
        /// side gmsh measured as one element could never be the other side of that grid - the wall would be
        /// refused however fine the rest of the mesh is. The raise is only made where the source is free to
        /// take it: a source side the wall before it assigned as its target, or one the user pinned a count
        /// on, keeps the count it was given, and the wall is refused with the count message as before.
        /// </summary>
        private void RaiseSourceToTargetEdgeCount(SweepWall wall, Dictionary<int, int> pinnedEdgeIdNumOfElements,
                                                  Dictionary<int, int> sourceEdgeIdNumOfElements,
                                                  Dictionary<int, int> targetEdgeIdNumOfElements,
                                                  ref int sourceElements)
        {
            if (sourceElements >= wall.TargetEdgeIds.Length) return;
            //
            foreach (var edgeId in wall.SourceEdgeIds)
            {
                if (targetEdgeIdNumOfElements.ContainsKey(edgeId) ||
                    _gmshData.EdgeIdNumElements.ContainsKey(edgeId)) return;
                if (pinnedEdgeIdNumOfElements != null && pinnedEdgeIdNumOfElements.ContainsKey(edgeId)) return;
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
        /// The edges of the walls' cross-sweep sides that lie opposite another such edge across a source or a
        /// target surface. The two walls have to give such an edge one count, whatever each of them would
        /// share out on its own, so it is not an edge a single wall may place.
        /// </summary>
        private static HashSet<int> GetSharedCrossEdgeIds(List<SweepWall> walls)
        {
            HashSet<int> sideEdgeIds = new HashSet<int>();
            HashSet<int> wallSurfaceIds = new HashSet<int>();
            foreach (var wall in walls)
            {
                wallSurfaceIds.Add(wall.SurfaceId);
                sideEdgeIds.UnionWith(wall.SourceEdgeIds);
                sideEdgeIds.UnionWith(wall.TargetEdgeIds);
            }
            //
            int[] faceIds;
            int[] vertexIds;
            HashSet<int> sharedEdgeIds = new HashSet<int>();
            foreach (var edgeId in sideEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out faceIds, out vertexIds);
                foreach (var faceId in faceIds)
                {
                    if (wallSurfaceIds.Contains(faceId)) continue;
                    //
                    int oppositeEdgeId;
                    if (!TryGetOppositeEdgeId(edgeId, faceId, vertexIds, out oppositeEdgeId)) continue;
                    if (!sideEdgeIds.Contains(oppositeEdgeId)) continue;
                    //
                    sharedEdgeIds.Add(edgeId);
                    sharedEdgeIds.Add(oppositeEdgeId);
                }
            }
            return sharedEdgeIds;
        }
        /// <summary>
        /// The counts two edges of one source or target surface must share. Such a surface lies between the
        /// target sides of two different walls, each of which shares its own total out over its edges by
        /// length, and the two roundings need not agree: the chamfer at the tip of a plate is given two
        /// elements by the long wall beside it and one by the short one, and the column of the sweep through
        /// it is broken. The pair takes the larger of the two counts, held down to what the side with the
        /// least to give can spare, since every other edge of a side keeps an element of its own. An edge
        /// that is the only one of its side carries its wall's whole total and is not free, so it is pinned
        /// only at the count it already has. A source QuadWild meshes is no grid, and its edges keep their counts.
        /// </summary>
        private static Dictionary<int, int> GetOppositeCrossEdgePins(List<SweepWall> walls,
                                                                     Dictionary<int, int> edgeIdNumOfElements,
                                                                     HashSet<int> quadWildSourceIds)
        {
            HashSet<int> fixedEdgeIds = new HashSet<int>();
            HashSet<int> wallSurfaceIds = new HashSet<int>();
            Dictionary<int, int> edgeIdUpperBound = new Dictionary<int, int>();
            foreach (var wall in walls)
            {
                wallSurfaceIds.Add(wall.SurfaceId);
                AddCrossSideBounds(wall.SourceEdgeIds, edgeIdNumOfElements, edgeIdUpperBound, fixedEdgeIds);
                AddCrossSideBounds(wall.TargetEdgeIds, edgeIdNumOfElements, edgeIdUpperBound, fixedEdgeIds);
            }
            //
            int[] faceIds;
            int[] vertexIds;
            int numOfElements;
            int oppositeNumOfElements;
            int oppositeUpperBound;
            Dictionary<int, int> pins = new Dictionary<int, int>();
            foreach (var entry in edgeIdUpperBound)
            {
                if (!edgeIdNumOfElements.TryGetValue(entry.Key, out numOfElements)) continue;
                Gmsh.Model.GetAdjacencies(1, entry.Key, out faceIds, out vertexIds);
                //
                foreach (var faceId in faceIds)
                {
                    if (wallSurfaceIds.Contains(faceId) || quadWildSourceIds.Contains(faceId)) continue;
                    //
                    int oppositeEdgeId;
                    if (!TryGetOppositeEdgeId(entry.Key, faceId, vertexIds, out oppositeEdgeId)) continue;
                    if (!edgeIdNumOfElements.TryGetValue(oppositeEdgeId, out oppositeNumOfElements)) continue;
                    if (!edgeIdUpperBound.TryGetValue(oppositeEdgeId, out oppositeUpperBound)) continue;
                    if (oppositeNumOfElements == numOfElements) continue;
                    //
                    bool edgeFixed = fixedEdgeIds.Contains(entry.Key);
                    bool oppositeFixed = fixedEdgeIds.Contains(oppositeEdgeId);
                    if (edgeFixed && oppositeFixed) continue;
                    //
                    int count;
                    if (edgeFixed) count = numOfElements;
                    else if (oppositeFixed) count = oppositeNumOfElements;
                    else count = Math.Max(numOfElements, oppositeNumOfElements);
                    count = Math.Min(count, Math.Min(entry.Value, oppositeUpperBound));
                    //
                    if (count < 1) continue;
                    if (edgeFixed && count != numOfElements) continue;
                    if (oppositeFixed && count != oppositeNumOfElements) continue;
                    //
                    pins[entry.Key] = count;
                    pins[oppositeEdgeId] = count;
                }
            }
            return pins;
        }
        /// <summary>
        /// What one side of a wall leaves every edge of it free to take: the side's total less an element for
        /// each of its other edges. The tightest bound is kept where an edge lies on the sides of several
        /// walls, and an edge alone on a side is recorded as carrying a count that is not free at all.
        /// </summary>
        private static void AddCrossSideBounds(int[] sideEdgeIds, Dictionary<int, int> edgeIdNumOfElements,
                                               Dictionary<int, int> edgeIdUpperBound, HashSet<int> fixedEdgeIds)
        {
            int numOfElements;
            int sideElements = 0;
            foreach (var edgeId in sideEdgeIds)
            {
                if (!edgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) return;
                sideElements += numOfElements;
            }
            //
            int upperBound = sideElements - (sideEdgeIds.Length - 1);
            foreach (var edgeId in sideEdgeIds)
            {
                if (sideEdgeIds.Length == 1) fixedEdgeIds.Add(edgeId);
                if (!edgeIdUpperBound.TryGetValue(edgeId, out numOfElements) || upperBound < numOfElements)
                    edgeIdUpperBound[edgeId] = upperBound;
            }
        }
        /// <summary>
        /// Puts the pinned counts onto the edges of a wall's target side and shares the rest of the side's
        /// total out over the edges that are not pinned, by length. The side is left as the distribution made
        /// it when the pins would leave an edge without an element of its own.
        /// </summary>
        private static void ApplyPinnedTargetElements(SweepWall wall, int[] targetElements,
                                                      Dictionary<int, int> pinnedEdgeIdNumOfElements)
        {
            if (pinnedEdgeIdNumOfElements == null || pinnedEdgeIdNumOfElements.Count == 0) return;
            //
            int count;
            int total = 0;
            int pinnedElements = 0;
            List<int> freeIndices = new List<int>();
            int[] elements = new int[targetElements.Length];
            for (int i = 0; i < targetElements.Length; i++)
            {
                total += targetElements[i];
                if (pinnedEdgeIdNumOfElements.TryGetValue(wall.TargetEdgeIds[i], out count))
                {
                    elements[i] = count;
                    pinnedElements += count;
                }
                else freeIndices.Add(i);
            }
            if (freeIndices.Count == targetElements.Length) return;
            if (total - pinnedElements < freeIndices.Count) return;
            if (freeIndices.Count == 0 && pinnedElements != total) return;
            //
            if (freeIndices.Count > 0)
            {
                int[] freeEdgeIds = new int[freeIndices.Count];
                for (int i = 0; i < freeIndices.Count; i++) freeEdgeIds[i] = wall.TargetEdgeIds[freeIndices[i]];
                int[] sharedElements =
                    SweepWallTopology.DistributeElements(total - pinnedElements,
                                                         SweepWallTopology.GetEdgeLengths(freeEdgeIds));
                for (int i = 0; i < freeIndices.Count; i++) elements[freeIndices[i]] = sharedElements[i];
            }
            //
            for (int i = 0; i < targetElements.Length; i++) targetElements[i] = elements[i];
        }
        /// <summary>
        /// The count settled on an edge lying opposite the given one across a face it bounds. The opposite
        /// edge of a face is the one sharing neither of its vertices; a face whose opposite side is split
        /// into several edges names no single one and is left to the caller, which falls back on measuring.
        /// </summary>
        private static bool TryGetOppositeEdgeElements(int edgeId, Dictionary<int, int> settledEdgeIdNumOfElements,
                                                       out int numOfElements)
        {
            numOfElements = 0;
            int[] faceIds;
            int[] vertexIds;
            Gmsh.Model.GetAdjacencies(1, edgeId, out faceIds, out vertexIds);
            //
            foreach (var faceId in faceIds)
            {
                int oppositeEdgeId;
                if (!TryGetOppositeEdgeId(edgeId, faceId, vertexIds, out oppositeEdgeId)) continue;
                if (settledEdgeIdNumOfElements.TryGetValue(oppositeEdgeId, out numOfElements)) return true;
            }
            return false;
        }
        /// <summary>
        /// The edge lying opposite the given one across one face it bounds - the one sharing neither of its
        /// vertices. A side split into several edges names no single one and gives nothing.
        /// </summary>
        private static bool TryGetOppositeEdgeId(int edgeId, int faceId, int[] edgeVertexIds,
                                                 out int oppositeEdgeId)
        {
            oppositeEdgeId = -1;
            int[] faceEdgeIds;
            int[] vertexIds;
            Gmsh.Model.GetAdjacencies(2, faceId, out _, out faceEdgeIds);
            HashSet<int> vertices = new HashSet<int>(edgeVertexIds);
            //
            foreach (var otherEdgeId in faceEdgeIds)
            {
                if (otherEdgeId == edgeId) continue;
                Gmsh.Model.GetAdjacencies(1, otherEdgeId, out _, out vertexIds);
                bool touches = false;
                foreach (var vertexId in vertexIds)
                {
                    if (vertices.Contains(vertexId)) { touches = true; break; }
                }
                if (touches) continue;
                if (oppositeEdgeId >= 0) { oppositeEdgeId = -1; return false; }
                oppositeEdgeId = otherEdgeId;
            }
            return oppositeEdgeId >= 0;
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
                    throw new CaeException(GetWallCountMessage(wall.SurfaceId, "across the sweep direction",
                                                               sourceCount, targetCount, wall.SourceEdgeIds,
                                                               wall.TargetEdgeIds, edgeIdNumOfElements));
                // Along the sweep: the two sides, each of them one chain however many edges it is made of
                int chainIndex;
                List<int> sideEdgeIds;
                Dictionary<int, int> chainIndexCount = new Dictionary<int, int>();
                Dictionary<int, List<int>> chainIndexEdgeIds = new Dictionary<int, List<int>>();
                foreach (var edgeId in wall.DirectionEdgeIds)
                {
                    if (!edgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) continue;
                    if (!edgeIdChainIndex.TryGetValue(edgeId, out chainIndex)) chainIndex = -edgeId;
                    chainIndexCount.TryGetValue(chainIndex, out int count);
                    chainIndexCount[chainIndex] = count + numOfElements;
                    if (!chainIndexEdgeIds.TryGetValue(chainIndex, out sideEdgeIds))
                    {
                        sideEdgeIds = new List<int>();
                        chainIndexEdgeIds.Add(chainIndex, sideEdgeIds);
                    }
                    sideEdgeIds.Add(edgeId);
                }
                if (chainIndexCount.Count != 2) continue;
                //
                int[] chainIndices = chainIndexCount.Keys.ToArray();
                int firstCount = chainIndexCount[chainIndices[0]];
                int secondCount = chainIndexCount[chainIndices[1]];
                if (firstCount != secondCount)
                    throw new CaeException(GetWallCountMessage(wall.SurfaceId, "along the sweep direction",
                                                               firstCount, secondCount,
                                                               chainIndexEdgeIds[chainIndices[0]].ToArray(),
                                                               chainIndexEdgeIds[chainIndices[1]].ToArray(),
                                                               edgeIdNumOfElements));
            }
        }
        /// <summary>
        /// The message of a wall that is no grid, with the surface and the edges of the two sides named by their
        /// gmsh tags, which the message is renamed from on its way out.
        /// </summary>
        private static string GetWallCountMessage(int surfaceId, string what, int first, int second,
                                                  int[] firstEdgeIds, int[] secondEdgeIds,
                                                  Dictionary<int, int> edgeIdNumOfElements)
        {
            return "The sweep side surface " + surfaceId + " cannot be meshed: its two sides " + what +
                   " were given " + first + " and " + second + " elements (" +
                   DescribeEdgeCounts(firstEdgeIds, edgeIdNumOfElements) + "; " +
                   DescribeEdgeCounts(secondEdgeIds, edgeIdNumOfElements) + "), and a structured grid needs " +
                   "the same number on both. Select the source surfaces by hand, or split the part so that each " +
                   "piece is swept on its own.";
        }
        private static string DescribeEdgeCounts(int[] edgeIds, Dictionary<int, int> edgeIdNumOfElements)
        {
            int numOfElements;
            List<string> items = new List<string>();
            foreach (var edgeId in edgeIds)
            {
                if (!edgeIdNumOfElements.TryGetValue(edgeId, out numOfElements)) continue;
                items.Add("edge " + edgeId + ": " + numOfElements);
            }
            return string.Join(", ", items);
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
        /// <summary>
        /// The counts of a target side cut into more pieces than the source side, shared out run by run
        /// rather than over the side as a whole. Every edge of the source stands over a run of consecutive
        /// edges of the target - the flat of a bent plate is split in two on the inner contour and left whole
        /// on the outer one - and each source edge then shares its own count out over its own run, so that the
        /// split falls where the nodes above it are.
        ///
        /// Sharing the whole side out by length puts it elsewhere: the side of a bend is shorter on the inside
        /// than on the outside by the difference of the two arcs, so the share every piece takes of the side is
        /// not the share it takes of the source edge over it, and the grid column through the split comes out
        /// an element off. The runs are read from the lengths - the grouping whose runs come closest to the
        /// source edges over them, which leaves the arc of the bend matched to the arc however much shorter it
        /// is. Null when the sides cannot be ordered, or when no grouping leaves every piece an element.
        /// </summary>
        private int[] DistributeTargetElementsByGroup(SweepWall wall,
                                                      Dictionary<int, int> sourceEdgeIdNumOfElements)
        {
            if (wall.SourceEdgeIds.Length == 0 || wall.TargetEdgeIds.Length <= wall.SourceEdgeIds.Length) return null;
            //
            int[] orderedSourceEdgeIds;
            int[] orderedTargetEdgeIds;
            if (!SweepWallTopology.OrderWallSideChains(wall, out orderedSourceEdgeIds, out orderedTargetEdgeIds))
                return null;
            //
            int[] sourceElements = new int[orderedSourceEdgeIds.Length];
            for (int i = 0; i < orderedSourceEdgeIds.Length; i++)
            {
                if (!sourceEdgeIdNumOfElements.TryGetValue(orderedSourceEdgeIds[i], out sourceElements[i])) return null;
            }
            double[] sourceLengths = SweepWallTopology.GetEdgeLengths(orderedSourceEdgeIds);
            double[] targetLengths = SweepWallTopology.GetEdgeLengths(orderedTargetEdgeIds);
            //
            int[] runLengths = GetTargetRunLengths(sourceLengths, sourceElements, targetLengths);
            if (runLengths == null) return null;
            //
            int first = 0;
            Dictionary<int, int> targetEdgeIdNumOfElements = new Dictionary<int, int>();
            for (int i = 0; i < runLengths.Length; i++)
            {
                int[] runEdgeIds = new int[runLengths[i]];
                double[] lengths = new double[runLengths[i]];
                Array.Copy(orderedTargetEdgeIds, first, runEdgeIds, 0, runLengths[i]);
                Array.Copy(targetLengths, first, lengths, 0, runLengths[i]);
                // A run holding an edge whose count the wall beside it has a say in is not this method's to
                // place: what the run has left once that count is taken is not what the lengths ask for, and
                // the side is better shared out as a whole, which is what the caller falls back on
                if (runLengths[i] > 1 && runEdgeIds.Any(id => _sharedCrossEdgeIds.Contains(id))) return null;
                //
                int[] runElements = DistributeRunElements(sourceElements[i], lengths);
                for (int k = 0; k < runLengths[i]; k++)
                    targetEdgeIdNumOfElements[runEdgeIds[k]] = runElements[k];
                first += runLengths[i];
            }
            //
            int[] targetElements = new int[wall.TargetEdgeIds.Length];
            for (int i = 0; i < wall.TargetEdgeIds.Length; i++)
            {
                if (!targetEdgeIdNumOfElements.TryGetValue(wall.TargetEdgeIds[i], out targetElements[i])) return null;
            }
            return targetElements;
        }
        /// <summary>
        /// The count of one run shared out over its edges in proportion to their lengths, the edges with the
        /// largest remainder taking what the rounding leaves over, and every edge kept above nothing at the
        /// expense of the one with the most. Handing every edge an element first and sharing only the rest, as
        /// the distribution of a whole side does, lifts a short edge above its share - two edges of 20 and 100
        /// take three and eight of eleven elements that way instead of two and nine - and the split between
        /// them then sits an element away from the node of the source above it.
        /// </summary>
        private static int[] DistributeRunElements(int total, double[] lengths)
        {
            int numOfEdges = lengths.Length;
            if (numOfEdges == 0 || total < numOfEdges) return SweepWallTopology.DistributeElements(total, lengths);
            //
            double lengthSum = 0;
            foreach (var length in lengths) lengthSum += Math.Max(0, length);
            //
            int assigned = 0;
            double[] fraction = new double[numOfEdges];
            int[] elements = new int[numOfEdges];
            for (int i = 0; i < numOfEdges; i++)
            {
                double share = lengthSum > 0 ? total * Math.Max(0, lengths[i]) / lengthSum
                                             : (double)total / numOfEdges;
                elements[i] = (int)Math.Floor(share);
                fraction[i] = share - elements[i];
                assigned += elements[i];
            }
            int[] order = Enumerable.Range(0, numOfEdges).OrderByDescending(i => fraction[i]).ToArray();
            for (int k = 0; k < total - assigned; k++) elements[order[k % numOfEdges]]++;
            //
            while (true)
            {
                int smallest = 0;
                int largest = 0;
                for (int i = 0; i < numOfEdges; i++)
                {
                    if (elements[i] < elements[smallest]) smallest = i;
                    if (elements[i] > elements[largest]) largest = i;
                }
                if (elements[smallest] >= 1 || elements[largest] <= 1) break;
                elements[smallest]++;
                elements[largest]--;
            }
            return elements;
        }
        /// <summary>
        /// How many consecutive target edges stand under each source edge: the grouping whose runs add up
        /// closest to the lengths of the source edges over them, every run holding at least one edge and no
        /// more edges than the source edge over it has elements to give them. Null when there is no such
        /// grouping.
        /// </summary>
        private static int[] GetTargetRunLengths(double[] sourceLengths, int[] sourceElements, double[] targetLengths)
        {
            int numOfSource = sourceLengths.Length;
            int numOfTarget = targetLengths.Length;
            double[,] cost = new double[numOfSource + 1, numOfTarget + 1];
            int[,] runLength = new int[numOfSource + 1, numOfTarget + 1];
            for (int i = 0; i <= numOfSource; i++)
            {
                for (int j = 0; j <= numOfTarget; j++) cost[i, j] = double.MaxValue;
            }
            cost[0, 0] = 0;
            //
            for (int i = 1; i <= numOfSource; i++)
            {
                for (int j = 1; j <= numOfTarget; j++)
                {
                    double runLengthSum = 0;
                    int longestRun = Math.Min(j, sourceElements[i - 1]);
                    for (int run = 1; run <= longestRun; run++)
                    {
                        runLengthSum += targetLengths[j - run];
                        if (cost[i - 1, j - run] == double.MaxValue) continue;
                        //
                        double runCost = cost[i - 1, j - run] + Math.Abs(sourceLengths[i - 1] - runLengthSum);
                        if (runCost >= cost[i, j]) continue;
                        cost[i, j] = runCost;
                        runLength[i, j] = run;
                    }
                }
            }
            if (cost[numOfSource, numOfTarget] == double.MaxValue) return null;
            //
            int column = numOfTarget;
            int[] runLengths = new int[numOfSource];
            for (int i = numOfSource; i > 0; i--)
            {
                runLengths[i - 1] = runLength[i, column];
                column -= runLength[i, column];
            }
            return runLengths;
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
        /// element, 0 for a degenerate one and negative for an inverted one. The mesh is only
        /// reported on, whatever produced it, so that the inverted elements can be shown on it.
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
                                            double transfiniteAngleDeg, bool recombine,
                                            GmshAlgorithmRecombineEnum recombineAlgorithm)
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
            HashSet<int> surfaceIdsWithInternalCurves = GetSurfaceIdsWithInternalCurves(surfaceDimTags);
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
                if (surfaceIdsWithInternalCurves.Contains(surfaceId)) surface.Transfinite = false;
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
            // Corner angles
            WithdrawTransfiniteOnCornerAngles(surfaceIdSurface, volumeIdVolume, transfiniteThreeSided, transfiniteFourSided,
                                              transfiniteAngleDeg);
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
                    // A sweep line cut into several edges is not one layer, and the average would pin the
                    // same count on a segment of 2 and on a segment of 18. Such a layer is left to the sweep,
                    // which shares every line out over its segments by length in PrepareWallEdges
                    if (IsSweepLineCutIntoEdges(layer)) continue;
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
                    {
                        Gmsh.Model.Mesh.SetTransfiniteSurface(surface.Id, "AlternateLeft", surface.VertexIds);
                        _transfiniteSurfaceIds.Add(surface.Id);
                    }
                    else if (surface.FourSided && transfiniteFourSided)
                    {
                        Gmsh.Model.Mesh.SetTransfiniteSurface(surface.Id, "AlternateLeft");
                        _transfiniteSurfaceIds.Add(surface.Id);
                    }
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
        /// <summary>
        /// Meshes free every face with a corner sharper than 180° minus the threshold angle or flatter than the
        /// threshold angle. The faces of a transfinite volume keep their constraint, since the volume needs all
        /// of them, and so do the side surfaces of a sweep, which carry the structured grid the sweep runs along.
        /// </summary>
        private void WithdrawTransfiniteOnCornerAngles(Dictionary<int, GmshSurface> surfaceIdSurface,
                                                       Dictionary<int, GmshVolume> volumeIdVolume,
                                                       bool transfiniteThreeSided, bool transfiniteFourSided,
                                                       double transfiniteAngleDeg)
        {
            double minAngleDeg = 180 - transfiniteAngleDeg - _cornerAngleToleranceDeg;
            double maxAngleDeg = transfiniteAngleDeg + _cornerAngleToleranceDeg;
            if (minAngleDeg <= 0) return;
            //
            HashSet<int> keptSurfaceIds = new HashSet<int>(_freeMeshedSurfaceIds);
            foreach (var entry in volumeIdVolume)
            {
                if (entry.Value.Transfinite) keptSurfaceIds.UnionWith(entry.Value.SurfaceIds);
            }
            if (_sweepBlocks != null)
            {
                foreach (var block in _sweepBlocks) keptSurfaceIds.UnionWith(block.WallSurfaceIds);
            }
            //
            foreach (var entry in surfaceIdSurface)
            {
                GmshSurface surface = entry.Value;
                if (!surface.Transfinite || keptSurfaceIds.Contains(surface.Id)) continue;
                if (!(surface.ThreeSided && transfiniteThreeSided) && !(surface.FourSided && transfiniteFourSided)) continue;
                //
                Dictionary<int, double> vertexIdAngleDeg = GetCornerAnglesDeg(surface);
                if (!surface.WithdrawTransfiniteOnCornerAngles(vertexIdAngleDeg, minAngleDeg, maxAngleDeg)) continue;
                //
                double worstAngleDeg = vertexIdAngleDeg.Values.OrderByDescending(a => Math.Abs(a - 90)).First();
                _writeOutput?.Invoke(NameGmshEntities("Transfinite: Surface " + surface.Id + " is meshed without the " +
                                                      "transfinite constraint - it has a corner of " +
                                                      Math.Round(worstAngleDeg, 1) + " deg, outside the range from " +
                                                      Math.Round(180 - transfiniteAngleDeg, 1) + " to " +
                                                      Math.Round(transfiniteAngleDeg, 1) + " deg."));
            }
        }
        /// <summary>
        /// The angle between the two edges at each corner of the face, measured between the directions they leave
        /// the corner in on the CAD curves.
        /// </summary>
        private static Dictionary<int, double> GetCornerAnglesDeg(GmshSurface surface)
        {
            Dictionary<int, double> vertexIdAngleDeg = new Dictionary<int, double>();
            foreach (int vertexId in surface.VertexIds)
            {
                int[] cornerEdgeIds = surface.GetCornerEdgeIds(vertexId);
                if (cornerEdgeIds == null) continue;
                //
                double[] tangent1 = SweepWallTopology.GetEdgeTangent(cornerEdgeIds[0], vertexId);
                double[] tangent2 = SweepWallTopology.GetEdgeTangent(cornerEdgeIds[1], vertexId);
                double length1 = Math.Sqrt(tangent1[0] * tangent1[0] + tangent1[1] * tangent1[1] + tangent1[2] * tangent1[2]);
                double length2 = Math.Sqrt(tangent2[0] * tangent2[0] + tangent2[1] * tangent2[1] + tangent2[2] * tangent2[2]);
                if (length1 == 0 || length2 == 0) continue;
                //
                double cos = (tangent1[0] * tangent2[0] + tangent1[1] * tangent2[1] + tangent1[2] * tangent2[2]) /
                             (length1 * length2);
                vertexIdAngleDeg[vertexId] = Math.Acos(Math.Max(-1, Math.Min(1, cos))) * 180 / Math.PI;
            }
            return vertexIdAngleDeg;
        }
        /// <summary>
        /// Whether a sweep layer holds a sweep line cut into several edges, which two of its edges meeting at
        /// a vertex say. The direction edges of one ring run alongside each other and share no vertex, so a
        /// shared vertex is always the join of two segments of one line.
        /// </summary>
        private static bool IsSweepLineCutIntoEdges(HashSet<int> layerEdgeIds)
        {
            int[] vertexIds;
            HashSet<int> layerVertexIds = new HashSet<int>();
            foreach (var edgeId in layerEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                foreach (var vertexId in vertexIds)
                {
                    if (!layerVertexIds.Add(vertexId)) return true;
                }
            }
            return false;
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
            // Read the other way round, so that a gmsh entity in a message can be named
            _gmshPointIdTopologyId = InvertRenumbering(netgenVertexIdGmshVertexId);
            _gmshCurveIdTopologyId = InvertRenumbering(netgenEdgeIdGmshEdgeId);
            _gmshSurfaceIdTopologyId = InvertRenumbering(netgenFaceIdGmshFaceId);
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
        /// <summary>
        /// Removes the cells a symmetric sweep mirrors instead of meshing them: every volume no block lies in. The
        /// volume of a block is the one all of its surfaces bound. A surface a removed cell shares with a swept one
        /// stays, as an outer surface of the swept piece, and the counts and sizes of what is removed are dropped.
        /// </summary>
        private void RemoveMirroredCells(SweepMesh sweepMesh)
        {
            int[] volumeIds;
            HashSet<int> keptVolumeIds = new HashSet<int>();
            for (int i = 0; i < sweepMesh.BlockSourceSurfaceIds.Length; i++)
            {
                HashSet<int> candidateIds = null;
                foreach (var surfaceId in sweepMesh.BlockSourceSurfaceIds[i].Concat(sweepMesh.BlockSideSurfaceIds[i])
                                                                          .Concat(sweepMesh.BlockTargetSurfaceIds[i]))
                {
                    Gmsh.Model.GetAdjacencies(2, surfaceId, out volumeIds, out _);
                    if (candidateIds == null) candidateIds = new HashSet<int>(volumeIds);
                    else candidateIds.IntersectWith(volumeIds);
                }
                if (candidateIds != null && candidateIds.Count == 1) keptVolumeIds.Add(candidateIds.First());
            }
            Tuple<int, int>[] volumeDimTags;
            Gmsh.Model.GetEntities(out volumeDimTags, 3);
            Tuple<int, int>[] removedDimTags = volumeDimTags.Where(dimTag => !keptVolumeIds.Contains(dimTag.Item2)).ToArray();
            if (keptVolumeIds.Count == 0 || removedDimTags.Length == 0) return;
            //
            Gmsh.Model.OCC.Remove(removedDimTags, true);
            Synchronize();
            //
            Tuple<int, int>[] dimTags;
            Gmsh.Model.GetEntities(out dimTags, 0);
            HashSet<int> pointIds = new HashSet<int>(dimTags.Select(dimTag => dimTag.Item2));
            Gmsh.Model.GetEntities(out dimTags, 1);
            HashSet<int> curveIds = new HashSet<int>(dimTags.Select(dimTag => dimTag.Item2));
            Gmsh.Model.GetEntities(out dimTags, 2);
            HashSet<int> surfaceIds = new HashSet<int>(dimTags.Select(dimTag => dimTag.Item2));
            if (_gmshData.VertexNodeIdMeshSize != null)
                _gmshData.VertexNodeIdMeshSize = _gmshData.VertexNodeIdMeshSize.Where(entry => pointIds.Contains(entry.Key))
                                                                               .ToDictionary(entry => entry.Key,
                                                                                             entry => entry.Value);
            if (_gmshData.EdgeIdNumElements != null)
                _gmshData.EdgeIdNumElements = _gmshData.EdgeIdNumElements.Where(entry => curveIds.Contains(entry.Key))
                                                                         .ToDictionary(entry => entry.Key,
                                                                                       entry => entry.Value);
            if (_gmshData.FaceIdNodes != null)
                _gmshData.FaceIdNodes = _gmshData.FaceIdNodes.Where(entry => surfaceIds.Contains(entry.Key))
                                                             .ToDictionary(entry => entry.Key, entry => entry.Value);
            //
            _writeOutput?.Invoke("Sweep: the compound is symmetric across " + sweepMesh.SymmetryPlanes.Length +
                                 " plane/s - " + keptVolumeIds.Count + " of its " + volumeDimTags.Length + " cells are " +
                                 "swept and the mesh is mirrored onto the other " + removedDimTags.Length + ".");
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
                    {
                        if (IsEntityProgressLine(loggerLines[i])) continue;
                        sb.AppendLine(NameGmshEntities(loggerLines[i]));
                    }
                    //
                    _currentLogLine = loggerLines.Length;
                    //
                    if (sb.Length > 0) _writeOutput?.Invoke(sb.ToString());
                }
            }
        }
        /// <summary>
        /// One line per curve, surface or volume as the mesher walks them. They say nothing a reader acts
        /// on and there are thousands of them on a compound, which is enough to push everything the sweep
        /// reports about itself out of the output window before the run ends.
        /// </summary>
        private static bool IsEntityProgressLine(string line)
        {
            return _entityProgress.IsMatch(line);
        }
        /// <summary>
        /// Writes a gmsh entity in a message the way the interface names it - "Surface 5 of Solid_part-50"
        /// rather than the tag gmsh gave the imported geometry, which is a number the reader cannot look up
        /// anywhere. An entity the renumbering did not map is left as gmsh wrote it.
        /// </summary>
        private string NameGmshEntities(string line)
        {
            if (_gmshSurfaceIdTopologyId.Count == 0 && _gmshCurveIdTopologyId.Count == 0 &&
                _gmshPointIdTopologyId.Count == 0) return line;
            //
            return _entityReference.Replace(line, match =>
            {
                string word = match.Groups[1].Value;
                int gmshId;
                if (!int.TryParse(match.Groups[2].Value, out gmshId)) return match.Value;
                //
                Dictionary<int, int> map;
                string lower = word.ToLowerInvariant();
                if (lower == "surface") map = _gmshSurfaceIdTopologyId;
                else if (lower == "curve" || lower == "edge") map = _gmshCurveIdTopologyId;
                else map = _gmshPointIdTopologyId;
                //
                int topologyId;
                if (!map.TryGetValue(gmshId, out topologyId)) return match.Value;
                //
                string partName = null;
                int partId = FeMesh.GetPartIdFromGmshTopologyId(topologyId);
                if (_gmshData.PartIdPartName != null) _gmshData.PartIdPartName.TryGetValue(partId, out partName);
                if (partName == null) partName = "part " + partId;
                //
                return word + " " + (FeMesh.GetItemIdFromGmshTopologyId(topologyId) + 1) + " of " + partName;
            });
        }
        private static Dictionary<int, int> InvertRenumbering(Dictionary<int, int> topologyIdGmshId)
        {
            // Two sub-parts share the face between them, so one gmsh entity can stand for several of the
            // model's; the lowest is kept, which makes the name the same from one run to the next
            Dictionary<int, int> gmshIdTopologyId = new Dictionary<int, int>();
            foreach (var entry in topologyIdGmshId)
            {
                int topologyId;
                if (gmshIdTopologyId.TryGetValue(entry.Value, out topologyId) && topologyId <= entry.Key) continue;
                gmshIdTopologyId[entry.Value] = entry.Key;
            }
            return gmshIdTopologyId;
        }
        /// <summary>
        /// The names of the entities a fragmentation kept, under the tags it gave them. A split edge or vertex
        /// is no entity of the model any more and is left without a name.
        /// </summary>
        private static Dictionary<int, int> RemapEntityNames(Dictionary<int, int> gmshIdTopologyId,
                                                             Dictionary<int, int> oldIdNewId)
        {
            int topologyId;
            Dictionary<int, int> newGmshIdTopologyId = new Dictionary<int, int>();
            foreach (var entry in oldIdNewId)
            {
                if (gmshIdTopologyId.TryGetValue(entry.Key, out topologyId)) newGmshIdTopologyId[entry.Value] = topologyId;
            }
            return newGmshIdTopologyId;
        }
    }
}
