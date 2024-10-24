using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CaeMesh.Meshing;
using GmshCommon;
using CaeMesh;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using CaeGlobals;
using System.Drawing.Printing;
using System.Windows.Forms.VisualStyles;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Drawing;

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
                while (_thread.ThreadState == ThreadState.Running)
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
                        "Shell gmsh, Tetrahedral gmsh, Transfinite mesh, Extrude mesh or Revolve mesh is possible.");
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
                    // Mesh size
                    SetMeshSizes();
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
                    if (_isOCC && (gsi.TransfiniteThreeSided || gsi.TransfiniteFourSided))
                        //Gmsh.Mesh.SetTransfiniteAutomatic(gsi.TransfiniteAngleRad, recombine);
                        SetTransfiniteSurfaces(gsi.TransfiniteThreeSided, gsi.TransfiniteFourSided, transfiniteVolume, recombine);
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
                    else if (gsi is ExtrudeMesh || gsi is RevolveMesh)
                        ExtrudeRevolveMesh(gsi, _gmshData.PartMeshingParameters, _gmshData.Preview);
                    else if (gsi is SweepMesh sm)
                        SweepMesh(sm, _gmshData.PartMeshingParameters, _gmshData.Preview);
                    else throw new NotSupportedException("MeshSetupItemTypeException");
                }
                else throw new NotSupportedException("MeshSetupItemTypeException");
                // Element order
                if (!_gmshData.Preview && _gmshData.PartMeshingParameters.SecondOrder)
                {
                    if (!_gmshData.PartMeshingParameters.MidsideNodesOnGeometry) 
                        Gmsh.Option.SetNumber("Mesh.SecondOrderLinear", 1); // first
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
        private void SetMeshSizes()
        {
            RenumberGmshDataByCoor();
            // Mesh size
            //Tuple<int, int>[] surfaceDimTags;
            //Gmsh.GetEntities(out surfaceDimTags, 2);
            //foreach (var surfaceDimTag in surfaceDimTags) Gmsh.Mesh.SetSizeFromBoundary(2, surfaceDimTag.Item2, 0);
            //
            double scaleFactor = 1;
            //
            Gmsh.Option.SetNumber("Mesh.MeshSizeMin", _gmshData.PartMeshingParameters.MinH * scaleFactor);
            Gmsh.Option.SetNumber("Mesh.MeshSizeMax", _gmshData.PartMeshingParameters.MaxH * scaleFactor);
            Gmsh.Option.SetNumber("Mesh.MeshSizeFromCurvature", 2 * Math.PI * _gmshData.PartMeshingParameters.ElementsPerCurve);
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
            int numOfNodes;
            //
            Gmsh.Model.GetEntities(out dimTags, 1);
            //
            foreach (var entry in dimTags)
            {
                edgeId = entry.Item2;
                if (_gmshData.EdgeIdNumElements.TryGetValue(edgeId, out numOfElements))
                {
                    numOfNodes = numOfElements + 1;
                    Gmsh.Model.Mesh.SetTransfiniteCurve(edgeId, numOfNodes);
                }
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
            if (preview) Gmsh.Model.Mesh.Generate(2);
            else Gmsh.Model.Mesh.Generate(2);
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
            if (preview) Gmsh.Model.Mesh.Generate(1);
            else Gmsh.Model.Mesh.Generate(3);
            // Optimize first order
            if (gmshSetupItem.OptimizeFirstOrderSolid != GmshOptimizeFirstOrderSolidEnum.None)
            {
                Tuple<int, int>[] dimTags = new Tuple<int, int>[0];
                Gmsh.Model.Mesh.Optimize(gmshSetupItem.OptimizeFirstOrderSolid.ToString(), false, 10, dimTags);
            }
        }
        private void TransfiniteMesh(bool preview)
        {
            if (preview) Gmsh.Model.Mesh.Generate(1);
            else Gmsh.Model.Mesh.Generate(3);
        }
        private void ExtrudeRevolveMesh(GmshSetupItem gmshSetupItem, MeshingParameters meshingParameters, bool preview)
        {
            ExtrudeMesh extrudeMesh = null;
            RevolveMesh revolveMesh = null;
            //
            if (gmshSetupItem is ExtrudeMesh em) extrudeMesh = em;
            else if (gmshSetupItem is RevolveMesh rm) revolveMesh = rm;
            else throw new NotSupportedException();
            //
            if (extrudeMesh != null)
            {
                if (extrudeMesh.ExtrudeCenter == null || extrudeMesh.Direction == null)
                    throw new CaeException("The extrude direction could not be determined.");
            }
            else if (revolveMesh != null)
            {
                if (revolveMesh.AxisCenter == null || revolveMesh.AxisDirection == null)
                    throw new CaeException("The revolve direction could not be determined.");
            }
            //
            int surfaceId;
            HashSet<int> allExtrudeSurfaceIds = new HashSet<int>();
            bool recombine = gmshSetupItem.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            Tuple<int, int>[] extrudeDimTags = new Tuple<int, int>[gmshSetupItem.CreationIds.Length];            
            // Collect surfaces
            for (int i = 0; i < gmshSetupItem.CreationIds.Length; i++)
            {
                surfaceId = FeMesh.GetItemIdFromGeometryId(gmshSetupItem.CreationIds[i]) + 1;
                extrudeDimTags[i] = new Tuple<int, int>(2, surfaceId);
                allExtrudeSurfaceIds.Add(surfaceId);
                // Recombine surfaces
                if (recombine) Gmsh.Model.Mesh.SetRecombine(2, surfaceId);
            }
            // Layers - size
            int numEl;
            int[] numElements;
            double[] height;
            if (gmshSetupItem.ElementSizeType == ElementSizeTypeEnum.ScaleFactor)
            {
                double edgeLength;
                if (extrudeMesh != null)
                {
                    edgeLength = Math.Sqrt(Math.Pow(extrudeMesh.Direction[0], 2) +
                                           Math.Pow(extrudeMesh.Direction[1], 2) +
                                           Math.Pow(extrudeMesh.Direction[2], 2));

                }
                else if (revolveMesh != null)
                {
                    edgeLength = revolveMesh.MiddleR * revolveMesh.AngleDeg * Math.PI / 180;
                }
                else throw new NotSupportedException();
                //
                numEl = (int)Math.Round(edgeLength / meshingParameters.MaxH / gmshSetupItem.ElementScaleFactor, 0);
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
                numElements = gmshSetupItem.NumOfElementsPerLayer;
                //
                double sumSizes = 0;
                for (int i = 0; i < gmshSetupItem.LayerSizes.Length; i++) sumSizes += gmshSetupItem.LayerSizes[i];
                //
                double sumHeight = 0;
                height = new double[gmshSetupItem.LayerSizes.Length];
                for (int i = 0; i < height.Length; i++)
                {
                    sumHeight += gmshSetupItem.LayerSizes[i] / sumSizes;
                    height[i] = sumHeight;
                }
            }
            else throw new NotSupportedException("ExtrudedElementSizeTypeEnumException");
            //
            if (preview)
            {
                Tuple<int, int>[] allDimTags = Gmsh.Model.OCC.GetEntities(2);
                List<Tuple<int, int>> toRemoveDimTags = new List<Tuple<int, int>>() { new Tuple<int, int>(3, 1) };  // add solid
                for (int i = 0; i < allDimTags.Length; i++)
                {
                    if (!allExtrudeSurfaceIds.Contains(allDimTags[i].Item2)) toRemoveDimTags.Add(allDimTags[i]);
                }
                //
                Gmsh.Model.OCC.Remove(toRemoveDimTags.ToArray(), false);
                //
                Gmsh.Model.OCC.Synchronize(); // must be here
                //
                Gmsh.Model.Mesh.Generate(2);
            }
            else
            {
                Tuple<int, int>[] outDimTags;
                // Extrude
                if (extrudeMesh != null)
                {
                    Gmsh.Model.OCC.Extrude(extrudeDimTags,
                                           extrudeMesh.Direction[0],
                                           extrudeMesh.Direction[1],
                                           extrudeMesh.Direction[2],
                                           out outDimTags, numElements, height, true);
                }
                // Revolve
                else if (revolveMesh != null)
                {
                    Gmsh.Model.OCC.Revolve(extrudeDimTags,
                                           revolveMesh.AxisCenter[0],
                                           revolveMesh.AxisCenter[1],
                                           revolveMesh.AxisCenter[2],
                                           revolveMesh.AxisDirection[0],
                                           revolveMesh.AxisDirection[1],
                                           revolveMesh.AxisDirection[2],
                                           revolveMesh.AngleDeg * Math.PI / 180,
                                           out outDimTags, numElements, height, true);
                }
                else throw new NotSupportedException();
                // Volume check 
                if (CheckMeshVolume(outDimTags))
                {
                    Gmsh.Model.OCC.Remove(new Tuple<int, int>[] { new Tuple<int, int>(3, 1) }, true);
                    //
                    Gmsh.Model.OCC.Synchronize(); // must be here
                    //
                    Gmsh.Model.Mesh.Generate(3);
                }
                else
                {
                    throw new CaeGlobals.CaeException("The volume of the extruded mesh is not equal to the volume " +
                        "of the geometry it represents. Try selecting other surfaces for the extrusion.");
                }
            }
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
            }
            // Get side surfaces
            Dictionary<int, int[]> surfaceIdEdgeIds;
            Dictionary<int, int[]> surfaceIdVertexIds;
            GetSurfaceItems(out surfaceIdEdgeIds, out surfaceIdVertexIds);
            // Find side surfaces
            HashSet<int> vertexSurfaceIds;
            Dictionary<int, HashSet<int>> vertexIdSurfaceId = new Dictionary<int, HashSet<int>>();
            HashSet<int> sourceSurfaceVertices = new HashSet<int>();
            HashSet<int> targetSurfaceIds = new HashSet<int>();
            //
            foreach (var surfaceEntry in surfaceIdVertexIds)
            {
                surfaceId = surfaceEntry.Key;
                foreach (var vertexId in surfaceEntry.Value)
                {
                    if (vertexIdSurfaceId.TryGetValue(vertexId, out vertexSurfaceIds)) vertexSurfaceIds.Add(surfaceId);
                    else vertexIdSurfaceId.Add(vertexId, new HashSet<int> { surfaceId });
                }
                //
                if (sourceSurfaceIds.Contains(surfaceId)) sourceSurfaceVertices.UnionWith(surfaceEntry.Value);
                //
                targetSurfaceIds.Add(surfaceId);
            }
            HashSet<int> sideSurfaceIds = new HashSet<int>();
            foreach (var entry in vertexIdSurfaceId)
            {
                if (sourceSurfaceVertices.Contains(entry.Key)) sideSurfaceIds.UnionWith(entry.Value);
            }
            // Remove the source surfaces from the side surfaces
            sideSurfaceIds.ExceptWith(sourceSurfaceIds);
            // Get target surfaces
            targetSurfaceIds.ExceptWith(sourceSurfaceIds);
            targetSurfaceIds.ExceptWith(sideSurfaceIds);
            if (targetSurfaceIds.Count != 1) throw new NotSupportedException();
            // Set source surfaces
            if (recombine)
            {
                foreach (var sourceSurfaceId in sourceSurfaceIds) Gmsh.Model.Mesh.SetRecombine(2, sourceSurfaceId);
            }
            // Set side surfaces
            foreach (var sideSurfaceId in sideSurfaceIds)
            {
                Gmsh.Model.Mesh.SetRecombine(2, sideSurfaceId);
                Gmsh.Model.Mesh.SetTransfiniteSurface(sideSurfaceId, "AlternateLeft");
            }
            //
            Gmsh.Model.Mesh.Generate(2);
            // Remove the volume and the target surface mesh
            List<Tuple<int, int>> toRemoveDimTags = new List<Tuple<int, int>>() { new Tuple<int, int>(3, 1) };  // volume
            foreach (var targetSurfaceId in targetSurfaceIds) toRemoveDimTags.Add(new Tuple<int, int>(2, targetSurfaceId));
            //
            Gmsh.Model.Mesh.Clear(toRemoveDimTags.ToArray());
            // Create sweep lines and their nodes
            Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine;
            Dictionary<IntPtr, double[]> nodeIdCoor;
            HashSet<IntPtr> addedNodeIds;
            HashSet<IntPtr> nodeIdsOfBoundarySweepLines;
            Dictionary<IntPtr, HashSet<IntPtr>> sweepLineNeighbours;
            CreateSweepLines(sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds, surfaceIdEdgeIds, surfaceIdVertexIds,
                             out addedNodeIds, out nodeIdSweepLine, out nodeIdsOfBoundarySweepLines, out sweepLineNeighbours,
                             out nodeIdCoor);
            //
            SmoothSweepLines(nodeIdSweepLine, nodeIdCoor, nodeIdsOfBoundarySweepLines, sweepLineNeighbours);
            //
            ProjectSweepLineEndNodesToFaces(nodeIdSweepLine, nodeIdCoor, nodeIdsOfBoundarySweepLines, targetSurfaceIds);


            // Add nodes
            foreach (var entry in nodeIdSweepLine)
            {
                if (!nodeIdsOfBoundarySweepLines.Contains(entry.Key))
                {
                    for (int i = 1; i < entry.Value.Length; i++)
                    {
                        if (i == entry.Value.Length - 1)
                            Gmsh.Model.Mesh.AddNodes(2, targetSurfaceIds.First(), new IntPtr[1] { entry.Value[i] }, nodeIdCoor[entry.Value[i]]);
                        else
                            Gmsh.Model.Mesh.AddNodes(3, 1, new IntPtr[1] { entry.Value[i] }, nodeIdCoor[entry.Value[i]]);
                    }
                }
            }

            //foreach (var nodeId in addedNodeIds)
            //{
            //    Gmsh.Model.Mesh.AddNodes(3, 1, new IntPtr[1] { nodeId }, nodeIdCoor[nodeId]);
            //}
            


            // Get max element id
            int currElementId;
            int maxElementId = 0;
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, -1, -1);
            for (int i = 0; i < elementTags.Length; i++)
            {
                for (int j = 0; j < elementTags[i].Length; j++)
                {
                    currElementId = elementTags[i][j].ToInt32();
                    if (currElementId > maxElementId) maxElementId = currElementId;
                }
            }
            //


            int numNodes;
            int newElementId = maxElementId + 1;
            int numLayers = nodeIdSweepLine.First().Value.Length - 1;
            IntPtr[] nodeIdsArr;
            IntPtr[][] solidNodeIdsArr;

            double[] coor1;
            double[] coor2;
            double[] coor3;
            double[] u = new double[3];
            double[] v = new double[3];
            double[] n = new double[3];
            double[] d = new double[3];
            bool reverse;
            int delta1;
            int delta2;

            foreach (var id in sourceSurfaceIds)
            {
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, id);
                //
                for (int i = 0; i < numLayers; i++) // layer id
                {
                    for (int j = 0; j < elementTags.Length; j++) // element type id
                    {
                        Gmsh.Model.Mesh.GetElementProperties(elementTypes[j], out _, out _, out numNodes, out _, out _);
                        //
                        for (int k = 0; k < elementTags[j].Length; k++) // element id
                        {
                            nodeIdsArr = new IntPtr[numNodes];
                            Array.Copy(nodeTags[j], k * numNodes, nodeIdsArr, 0, numNodes);
                            //
                            if (numNodes < 3) throw new NotSupportedException();
                            //
                            coor1 = nodeIdCoor[nodeIdsArr[0]];
                            coor2 = nodeIdCoor[nodeIdsArr[1]];
                            coor3 = nodeIdCoor[nodeIdsArr[2]];
                            //
                            u[0] = coor2[0] - coor1[0];
                            u[1] = coor2[1] - coor1[1];
                            u[2] = coor2[2] - coor1[2];
                            //
                            v[0] = coor3[0] - coor1[0];
                            v[1] = coor3[1] - coor1[1];
                            v[2] = coor3[2] - coor1[2];
                            //
                            n[0] = u[1] * v[2] - u[2] * v[1];
                            n[1] = u[2] * v[0] - u[0] * v[2];
                            n[2] = u[0] * v[1] - u[1] * v[0];
                            //
                            coor1 = nodeIdCoor[nodeIdSweepLine.First().Value[0]];
                            coor2 = nodeIdCoor[nodeIdSweepLine.First().Value[1]];
                            //
                            d[0] = coor2[0] - coor1[0];
                            d[1] = coor2[1] - coor1[1];
                            d[2] = coor2[2] - coor1[2];
                            //
                            reverse = n[0] * d[0] + n[1] * d[1] + n[2] * d[2] < 0;
                            if (reverse)
                            {
                                delta1 = 1;
                                delta2 = 0;
                            }
                            else
                            {
                                delta1 = 0;
                                delta2 = 1;
                            }
                            if (numNodes == 3)
                            {
                                solidNodeIdsArr = new IntPtr[][] { new IntPtr[6] };
                                solidNodeIdsArr[0][0] = nodeIdSweepLine[nodeIdsArr[0]][i + delta1];
                                solidNodeIdsArr[0][1] = nodeIdSweepLine[nodeIdsArr[1]][i + delta1];
                                solidNodeIdsArr[0][2] = nodeIdSweepLine[nodeIdsArr[2]][i + delta1];
                                solidNodeIdsArr[0][3] = nodeIdSweepLine[nodeIdsArr[0]][i + delta2];
                                solidNodeIdsArr[0][4] = nodeIdSweepLine[nodeIdsArr[1]][i + delta2];
                                solidNodeIdsArr[0][5] = nodeIdSweepLine[nodeIdsArr[2]][i + delta2];
                                //
                                Gmsh.Model.Mesh.AddElements(3, 1, new int[] { 6 },
                                                            new IntPtr[][] { new IntPtr[] { (IntPtr)newElementId } },
                                                            solidNodeIdsArr);
                                newElementId++;
                                //
                                if (i == numLayers - 1)
                                {
                                    solidNodeIdsArr = new IntPtr[][] { new IntPtr[3] };
                                    solidNodeIdsArr[0][0] = nodeIdSweepLine[nodeIdsArr[0]][i + 1];
                                    solidNodeIdsArr[0][1] = nodeIdSweepLine[nodeIdsArr[2]][i + 1];
                                    solidNodeIdsArr[0][2] = nodeIdSweepLine[nodeIdsArr[1]][i + 1];

                                    //
                                    Gmsh.Model.Mesh.AddElements(2, targetSurfaceIds.First(), new int[] { 2 },
                                                                new IntPtr[][] { new IntPtr[] { (IntPtr)newElementId } },
                                                                solidNodeIdsArr);
                                    newElementId++;
                                }
                            }
                            else if (numNodes == 4)
                            {

                            }
                            else throw new NotSupportedException();
                        }
                    }
                }
            }

            //Tuple<int, int>[] dimTags = new Tuple<int, int>[0];
            //Gmsh.Model.Mesh.Optimize(GmshOptimizeFirstOrderSolidEnum.Gmsh.ToString(), true, 5, dimTags);
        }
        //
        private void GetSurfaceItems(out Dictionary<int, int[]> surfaceIdEdgeIds, out Dictionary<int, int[]> surfaceIdVertexIds)
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
        private void CreateSweepLines(HashSet<int> sourceSurfaceIds, HashSet<int> sideSurfaceIds, HashSet<int> targetSurfaceIds,
                                      Dictionary<int, int[]> surfaceIdEdgeIds, Dictionary<int, int[]> surfaceIdVertexIds,
                                      out HashSet<IntPtr> addedNodeIds, out Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine,
                                      out HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                      out Dictionary<IntPtr, HashSet<IntPtr>> sweepLineNeighbours,
                                      out Dictionary<IntPtr, double[]> nodeIdCoor)
        {
            HashSet<int> sourceEdgeIds = new HashSet<int>();
            HashSet<int> sideEdgeIds = new HashSet<int>();
            HashSet<int> sourceSideBoundaryEdgeIds;
            //
            foreach (var id in sourceSurfaceIds) sourceEdgeIds.UnionWith(surfaceIdEdgeIds[id]);
            foreach (var id in sideSurfaceIds) sideEdgeIds.UnionWith(surfaceIdEdgeIds[id]);
            sourceSideBoundaryEdgeIds = sourceEdgeIds.Intersect(sideEdgeIds).ToHashSet();
            //
            // Side surface sweep lines                                                                     
            //
            // Get side nodes neighbours map
            Dictionary<IntPtr, HashSet<IntPtr>> nodeIdNeighbourIds = GetNodesNeighbours(sideSurfaceIds);
            // Get first nodes of the sweep lines
            IntPtr[] nodeIdsArr;
            nodeIdsOfBoundarySweepLines = new HashSet<IntPtr>();
            foreach (var id in sourceSideBoundaryEdgeIds)
            {
                Gmsh.Model.Mesh.GetNodes(out nodeIdsArr, out _, 1, id, true, false);
                nodeIdsOfBoundarySweepLines.UnionWith(nodeIdsArr);
            }
            // Get sweep lines on side surfaces
            IntPtr firstNodeId;
            IntPtr neighbourId;
            HashSet<IntPtr> currentLayerNodeIds = nodeIdsOfBoundarySweepLines;
            HashSet<IntPtr> prevTwoLayerNodeIds = new HashSet<IntPtr>(nodeIdsOfBoundarySweepLines);
            HashSet<IntPtr> nextLayerNodeIds;
            Dictionary<IntPtr, IntPtr> nodeIdFirstNodeId = new Dictionary<IntPtr, IntPtr>();
            Dictionary<IntPtr, List<IntPtr>> nodeIdSweepLineList = new Dictionary<IntPtr, List<IntPtr>>();
            foreach (var nodeId in nodeIdsOfBoundarySweepLines)
                nodeIdSweepLineList.Add(nodeId, new List<IntPtr>() { nodeId });
            //
            int numOfLayers = 1;
            HashSet<IntPtr> neighbourIds;
            int visitedNodesCount = currentLayerNodeIds.Count;
            while (visitedNodesCount < nodeIdNeighbourIds.Count)
            {
                nextLayerNodeIds = new HashSet<IntPtr>();
                foreach (var nodeId in currentLayerNodeIds)
                {
                    neighbourIds = nodeIdNeighbourIds[nodeId].Except(prevTwoLayerNodeIds).ToHashSet();
                    if (neighbourIds.Count != 1) throw new NotSupportedException();
                    //
                    neighbourId = neighbourIds.First();
                    // Get first node id of the sweep line
                    if (!nodeIdFirstNodeId.TryGetValue(nodeId, out firstNodeId)) firstNodeId = nodeId;
                    nodeIdFirstNodeId[neighbourId] = firstNodeId;
                    //
                    nodeIdSweepLineList[firstNodeId].Add(neighbourId);
                    //
                    nextLayerNodeIds.Add(neighbourId);
                }
                prevTwoLayerNodeIds = new HashSet<IntPtr>(currentLayerNodeIds);
                prevTwoLayerNodeIds.UnionWith(nextLayerNodeIds);
                //
                currentLayerNodeIds = nextLayerNodeIds;
                //
                numOfLayers++;
                visitedNodesCount += currentLayerNodeIds.Count;
            }
            //
            nodeIdSweepLine = new Dictionary<IntPtr, IntPtr[]>();
            foreach (var entry in nodeIdSweepLineList) nodeIdSweepLine.Add(entry.Key, entry.Value.ToArray());
            //
            // Source surface sweep lines                                                                   
            //
            // Get all node coordinates and max node id
            int currNodeId;
            int maxNodeId = 0;
            double[] coor;
            double[] allCoor;
            nodeIdCoor = new Dictionary<IntPtr, double[]>();
            HashSet<int> allSurfaceIds = sourceSurfaceIds.Union(sideSurfaceIds).ToHashSet();
            foreach (var id in allSurfaceIds)
            {
                Gmsh.Model.Mesh.GetNodes(out nodeIdsArr, out allCoor, 2, id, true, false);
                for (int i = 0; i < nodeIdsArr.Length; i++)
                {
                    coor = new double[3];
                    Array.Copy(allCoor, i * 3, coor, 0, 3);
                    nodeIdCoor[nodeIdsArr[i]] = coor;
                    currNodeId = nodeIdsArr[i].ToInt32();
                    if (currNodeId > maxNodeId) maxNodeId = currNodeId;
                }
            }
            // Get source nodes neighbours map
            nodeIdNeighbourIds = GetNodesNeighbours(sourceSurfaceIds);
            sweepLineNeighbours = nodeIdNeighbourIds;
            //
            int newNodeId = maxNodeId + 1;
            double[] coor1;
            double[] coor2;
            double[] direction;
            double[] avgDirection;
            IntPtr[] sweepLine;
            List<IntPtr[]> sweepLines;
            HashSet<IntPtr> visitedNodeIds = new HashSet<IntPtr>(currentLayerNodeIds);
            //
            addedNodeIds = new HashSet<IntPtr>();
            currentLayerNodeIds = nodeIdsOfBoundarySweepLines;
            // Go through nodes layer by layer from outside to inside
            while (nodeIdSweepLine.Count != nodeIdNeighbourIds.Count)
            {
                nextLayerNodeIds = new HashSet<IntPtr>();
                foreach (var nodeId in currentLayerNodeIds)
                {
                    nextLayerNodeIds.UnionWith(nodeIdNeighbourIds[nodeId].Except(visitedNodeIds).ToHashSet());
                }
                foreach (var nodeId in nextLayerNodeIds)
                {
                    // If sweep line does not exist
                    if (!nodeIdSweepLine.ContainsKey(nodeId))
                    {
                        // Find neighbouring sweep lines
                        sweepLines = new List<IntPtr[]>();
                        foreach (var neighbourNodeId in nodeIdNeighbourIds[nodeId])
                        {
                            if (nodeIdSweepLine.TryGetValue(neighbourNodeId, out sweepLine)) sweepLines.Add(sweepLine);
                        }
                        // Compute the average positions on the new sweep line
                        if (sweepLines.Count > 0)
                        {
                            sweepLine = new IntPtr[numOfLayers];
                            sweepLine[0] = nodeId; // first node
                            //
                            for (int i = 0; i < numOfLayers - 1; i++)
                            {
                                direction = new double[3];
                                avgDirection = new double[3];
                                for (int j = 0; j < sweepLines.Count; j++)
                                {
                                    coor1 = nodeIdCoor[sweepLines[j][i]];
                                    coor2 = nodeIdCoor[sweepLines[j][i + 1]];
                                    direction[0] = coor2[0] - coor1[0];
                                    direction[1] = coor2[1] - coor1[1];
                                    direction[2] = coor2[2] - coor1[2];
                                    //
                                    avgDirection[0] += direction[0];
                                    avgDirection[1] += direction[1];
                                    avgDirection[2] += direction[2];
                                }
                                avgDirection[0] /= sweepLines.Count;
                                avgDirection[1] /= sweepLines.Count;
                                avgDirection[2] /= sweepLines.Count;
                                //
                                coor1 = nodeIdCoor[sweepLine[i]];
                                coor2 = new double[3];
                                coor2[0] = coor1[0] + avgDirection[0];
                                coor2[1] = coor1[1] + avgDirection[1];
                                coor2[2] = coor1[2] + avgDirection[2];
                                //
                                sweepLine[i + 1] = (IntPtr)newNodeId;
                                nodeIdCoor.Add((IntPtr)newNodeId, coor2);
                                addedNodeIds.Add((IntPtr)newNodeId);
                                //
                                newNodeId++;
                            }
                            nodeIdSweepLine.Add(nodeId, sweepLine);
                        }
                    }
                }
                //
                visitedNodeIds.UnionWith(nextLayerNodeIds);
                //
                currentLayerNodeIds = nextLayerNodeIds;
            }
        }
        private void SmoothSweepLines(Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine, Dictionary<IntPtr, double[]> nodeIdCoor,
                                      HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                      Dictionary<IntPtr, HashSet<IntPtr>> sweepLineNeighbours)
        {
            double[] coor1;
            double[] coor2;
            double[] direction;
            IntPtr[] sweepLine;
            
            Dictionary<IntPtr, double[]> nodeIdDirection = new Dictionary<IntPtr, double[]>();  // direction to the node
            foreach (var entry in nodeIdSweepLine)
            {
                sweepLine = entry.Value;
                //
                for (int i = 1; i < sweepLine.Length; i++)
                {
                    coor1 = nodeIdCoor[sweepLine[i - 1]];
                    coor2 = nodeIdCoor[sweepLine[i]];
                    direction = new double[3];
                    direction[0] = coor2[0] - coor1[0];
                    direction[1] = coor2[1] - coor1[1];
                    direction[2] = coor2[2] - coor1[2];
                    nodeIdDirection.Add(sweepLine[i], direction);
                }
            }
            // Laplacian smoothing of directions
            int n = 10;
            double[] avgDirection;
            HashSet<IntPtr> neighbours;
            Dictionary<IntPtr, double[]> smoothNodeIdDirection;
            IntPtr[] internalNodeIds = nodeIdSweepLine.Keys.Except(nodeIdsOfBoundarySweepLines).ToArray();
            //
            for (int i = 0; i < n; i++) // number of smooth loops
            {
                smoothNodeIdDirection = new Dictionary<IntPtr, double[]>();
                foreach (var nodeId in internalNodeIds) // for each sweep line
                {
                    sweepLine = nodeIdSweepLine[nodeId];
                    for (int j = 1; j < sweepLine.Length; j++)  // for each layer
                    {
                        avgDirection = new double[3];
                        neighbours = sweepLineNeighbours[nodeId];
                        //
                        foreach (var neighbourNodeId in neighbours)
                        {
                            direction = nodeIdDirection[nodeIdSweepLine[neighbourNodeId][j]];
                            //direction = nodeIdCoor[nodeIdSweepLine[neighbourNodeId][j]];
                            avgDirection[0] += direction[0];
                            avgDirection[1] += direction[1];
                            avgDirection[2] += direction[2];
                        }
                        avgDirection[0] /= neighbours.Count;
                        avgDirection[1] /= neighbours.Count;
                        avgDirection[2] /= neighbours.Count;
                        //
                        smoothNodeIdDirection[sweepLine[j]] = avgDirection;
                    }
                }
                // Copy boundary directions
                foreach (var nodeId in nodeIdsOfBoundarySweepLines)
                {
                    sweepLine = nodeIdSweepLine[nodeId];
                    for (int j = 1; j < sweepLine.Length; j++)  // for each layer
                    {
                        direction = nodeIdDirection[sweepLine[j]];
                        smoothNodeIdDirection[sweepLine[j]] = direction;
                    }
                }
                //
                nodeIdDirection = smoothNodeIdDirection;
            }
            // Apply smoother directions to coordinates
            foreach (var nodeId in internalNodeIds) // for each sweep line
            {
                sweepLine = nodeIdSweepLine[nodeId];
                for (int j = 1; j < sweepLine.Length; j++)  // for each layer
                {
                    coor1 = nodeIdCoor[sweepLine[j - 1]];
                    direction = nodeIdDirection[sweepLine[j]];
                    coor2 = new double[3];
                    coor2[0] = coor1[0] + direction[0];
                    coor2[1] = coor1[1] + direction[1];
                    coor2[2] = coor1[2] + direction[2];
                    nodeIdCoor[sweepLine[j]] = coor2;
                    //nodeIdCoor[sweepLine[j]] = direction;
                }
            }

        }
        private void ProjectSweepLineEndNodesToFaces(Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine,
                                                     Dictionary<IntPtr, double[]> nodeIdCoor,
                                                     HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                                     HashSet<int> targetSurfaceIds)
        {
            int count;
            int whileCount;
            int numLayers = -1;
            double length = 0;
            double disByDisDer;
            double[] rations;
            double[] t;
            double[][] coor;
            double[] coor1;
            double[] coor2;
            double[] direction = new double[3];
            IntPtr[] sweepLine;
            // Compute the rations of a boundary sweep line
            sweepLine = nodeIdSweepLine[nodeIdsOfBoundarySweepLines.First()];
            numLayers = sweepLine.Length;
            rations = new double[numLayers - 1];
            for (int i = 0; i < numLayers - 1; i++)
            {
                coor1 = nodeIdCoor[sweepLine[i]];
                coor2 = nodeIdCoor[sweepLine[i + 1]];
                direction[0] = coor2[0] - coor1[0];
                direction[1] = coor2[1] - coor1[1];
                direction[2] = coor2[2] - coor1[2];
                //
                rations[i] = Math.Sqrt(Math.Pow(direction[0], 2) + Math.Pow(direction[1], 2) + Math.Pow(direction[2], 2));
                length += rations[i];
            }
            for (int i = 0; i < rations.Length; i++) rations[i] /= length;
            // Use Newtons method to find the surface intersection
            foreach (var entry in nodeIdSweepLine)
            {
                if (nodeIdsOfBoundarySweepLines.Contains(entry.Key)) continue;
                //
                sweepLine = entry.Value;
                coor1 = nodeIdCoor[sweepLine[numLayers - 2]];
                coor2 = nodeIdCoor[sweepLine[numLayers - 1]];
                direction[0] = coor2[0] - coor1[0];
                direction[1] = coor2[1] - coor1[1];
                direction[2] = coor2[2] - coor1[2];
                //
                t = new double[targetSurfaceIds.Count];
                coor = new double[targetSurfaceIds.Count][];
                for (int i = 0; i < t.Length; i++)
                {
                    t[i] = 1;
                    coor[i] = coor2.ToArray();
                }
                //
                whileCount = 0;
                while (whileCount < 10)
                {
                    count = 0;
                    foreach (var targetSurfaceId in targetSurfaceIds)
                    {
                        disByDisDer = DistanceByDistanceDerivative(coor[count], direction, targetSurfaceId);
                        //
                        if (Math.Abs(disByDisDer) < 1E-6)
                        {
                            nodeIdCoor[sweepLine[numLayers - 1]] = coor[count];
                            //
                            ResetSweepLineLengths(sweepLine, nodeIdCoor, rations);
                            //
                            whileCount = 100;
                            break;
                        }
                        else
                        {
                            t[count] -= disByDisDer;
                            coor[count][0] = coor1[0] + t[count] * direction[0];
                            coor[count][1] = coor1[1] + t[count] * direction[1];
                            coor[count][2] = coor1[2] + t[count] * direction[2];
                        }
                        //
                        count++;
                    }
                    //
                    whileCount++;
                }
            }
            

        }
        private void ResetSweepLineLengths(IntPtr[] sweepLine, Dictionary<IntPtr, double[]> nodeIdCoor, double[] rations)
        {
            int numLayers = sweepLine.Length;
            double length = 0;
            double[] lengths = new double[numLayers - 1];
            double[] coor1;
            double[] coor2;
            double[] direction = new double[3];
            //
            for (int i = 0; i < numLayers - 1; i++)
            {
                coor1 = nodeIdCoor[sweepLine[i]];
                coor2 = nodeIdCoor[sweepLine[i + 1]];
                direction[0] = coor2[0] - coor1[0];
                direction[1] = coor2[1] - coor1[1];
                direction[2] = coor2[2] - coor1[2];
                //
                lengths[i] = Math.Sqrt(Math.Pow(direction[0], 2) + Math.Pow(direction[1], 2) + Math.Pow(direction[2], 2));
                length += rations[i];
            }
            //
            double t;
            double newLength = 0;
            length = 0;
            for (int i = 0; i < numLayers - 1; i++)
            {
                length += lengths[i];
                newLength += rations[i] * length;

                if (length > newLength)
                {
                    t = 1 - ((length - newLength) / lengths[i]);
                    //
                    coor1 = nodeIdCoor[sweepLine[i]];
                    coor2 = nodeIdCoor[sweepLine[i + 1]];
                    direction[0] = coor2[0] - coor1[0];
                    direction[1] = coor2[1] - coor1[1];
                    direction[2] = coor2[2] - coor1[2];
                }
            }
        }
        private double DistanceByDistanceDerivative(double[] coor, double[] direction, int surfaceId)
        {
            double epsilon = 1E-6;
            double distance1;
            double distance2;
            //
            int point1Id = Gmsh.Model.OCC.AddPoint(coor[0], coor[1], coor[2]);
            int point2Id = Gmsh.Model.OCC.AddPoint(coor[0] + epsilon * direction[0],
                                                   coor[1] + epsilon * direction[1],
                                                   coor[2] + epsilon * direction[2]);
            //
            Gmsh.Model.OCC.GetDistance(0, point1Id, 2, surfaceId, out distance1);
            Gmsh.Model.OCC.GetDistance(0, point2Id, 2, surfaceId, out distance2);
            //
            double derivative;
            double disByDisDer;
            derivative = (distance2 - distance1) / epsilon;
            disByDisDer = distance1 / derivative;
            //
            return disByDisDer;
        }

        private Dictionary<IntPtr, HashSet<IntPtr>> GetNodesNeighbours(IEnumerable<int> surfaceIds)
        {
            // Get all elements on the surfaces
            int numNodes;
            IntPtr[] nodeIdsArr;
            Dictionary<IntPtr, IntPtr[]> elementIdNodeIds = new Dictionary<IntPtr, IntPtr[]>();
            foreach (var id in surfaceIds)
            {
                Gmsh.Model.Mesh.GetElements(out int[] elementTypes, out IntPtr[][] elementTags, out IntPtr[][] nodeTags, 2, id);
                //
                for (int i = 0; i < elementTags.Length; i++)
                {
                    Gmsh.Model.Mesh.GetElementProperties(elementTypes[i], out _, out _, out numNodes, out _, out _);
                    //
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        nodeIdsArr = new IntPtr[numNodes];
                        Array.Copy(nodeTags[i], j * numNodes, nodeIdsArr, 0, numNodes);
                        elementIdNodeIds.Add(elementTags[i][j], nodeIdsArr);
                    }
                }
            }
            // Get node neighbours map
            int delta;
            int numOfNodes;
            HashSet<IntPtr> neighbourIds;
            Dictionary<IntPtr, HashSet<IntPtr>> nodeIdNeighbourIds = new Dictionary<IntPtr, HashSet<IntPtr>>();
            foreach (var entry in elementIdNodeIds)
            {
                numOfNodes = entry.Value.Length;
                if (numOfNodes == 3) delta = 1;
                else if (numOfNodes == 4) delta = 2;
                else throw new NotSupportedException();
                //
                for (int i = 0; i < numOfNodes; i++)
                {
                    if (!nodeIdNeighbourIds.TryGetValue(entry.Value[i], out neighbourIds))
                    {
                        neighbourIds = new HashSet<IntPtr>();
                        nodeIdNeighbourIds.Add(entry.Value[i], neighbourIds);
                    }
                    neighbourIds.Add(entry.Value[(i + 1) % numOfNodes]);
                    neighbourIds.Add(entry.Value[(i + 1 + delta) % numOfNodes]);
                }
            }
            //
            return nodeIdNeighbourIds;
        }
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
        private void SetTransfiniteSurfaces(bool transfiniteThreeSided, bool transfiniteFourSided, bool transfiniteVolume,
                                            bool recombine)
        {
            int[] volumeIds;
            int[] edgeIds;
            int[][] edgeVertexIds;
            int[] vertexIds;
            HashSet<int> surfaceVertexIds = new HashSet<int>();
            //
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
                            if (surface.Triangular && transfiniteThreeSided) volume.TriSurfIds.Add(volSurfaceId);
                            else if (surface.Quadrangular && transfiniteFourSided) volume.QuadSurfIds.Add(volSurfaceId);
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
            HashSet<int> groupEdgeIds;
            IntPtr[] nodeTagsIntPtr;
            double[] coor;
            // Create edge mesh to get number of nodes for each edge
            Gmsh.Model.Mesh.Generate(1);
            //
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
                if (maxByRefinement > 0) numOfNodes = maxByRefinement;
                else numOfNodes = (int)Math.Round((double)sum / edgeGroup.Nodes.Count, 0, MidpointRounding.AwayFromZero);
                //numOfNodes = (int)Math.Round((min + max) / 2.0, 0, MidpointRounding.AwayFromZero);
                //
                foreach (var edgeNodeFromGroup in edgeGroup.Nodes)
                {
                    Gmsh.Model.Mesh.SetTransfiniteCurve(edgeNodeFromGroup.Value.Id, numOfNodes);
                }
            }
            //
            Gmsh.Model.Mesh.Clear(); // must clear the mesh
            //
            foreach (var entry in surfaceIdSurface)
            {
                surface = entry.Value;
                if (surface.Transfinite)
                {
                    // "Left", "Right", "AlternateLeft" and "AlternateRight"
                    if (surface.Triangular && transfiniteThreeSided)
                        Gmsh.Model.Mesh.SetTransfiniteSurface(surface.Id, "AlternateLeft", surface.VertexIds);
                    else if (surface.Quadrangular && transfiniteFourSided)
                        Gmsh.Model.Mesh.SetTransfiniteSurface(surface.Id, "AlternateLeft");
                    //
                    if (recombine && surface.Recombine) Gmsh.Model.Mesh.SetRecombine(2, surface.Id);
                    Gmsh.Model.Mesh.SetSmoothing(2, surface.Id, 100);
                }
            }
            //
            foreach (var entry in volumeIdVolume)
            {
                if (entry.Value.Transfinite) Gmsh.Model.Mesh.SetTransfiniteVolume(entry.Key);
                //Gmsh.Mesh.SetOutwardOrientation(volumeDimTag.Item2);
            }
        }
        // Other methods
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
                int n = 5;
                int pointCount;
                int count = 0;
                int tag;
                double[] coor1D;
                double[] minBounds;
                double[] maxBounds;
                double[] parametricCoor1D;
                _gmshData.Coor = new double[surfaceDimTags.Length][][];
                foreach (var entry in surfaceDimTags)
                {
                    tag = entry.Item2;
                    //
                    Gmsh.Model.GetParametrizationBounds(2, tag, out minBounds, out maxBounds);
                    //
                    pointCount = 0;
                    parametricCoor1D = new double[n * n * 2];
                    //
                    double du = (maxBounds[0] - minBounds[0]) / (n - 1);
                    double dv = (maxBounds[1] - minBounds[1]) / (n - 1);
                    //
                    for (int u = 0; u < n; u++)
                    {
                        for (int v = 0; v < n; v++)
                        {
                            parametricCoor1D[pointCount++] = minBounds[0] + du * u;
                            parametricCoor1D[pointCount++] = minBounds[1] + dv * v;
                        }
                    }
                    //
                    Gmsh.Model.GetValue(2, tag, parametricCoor1D, out coor1D);
                    //
                    _gmshData.Coor[count] = new double[coor1D.Length / 3][];
                    for (int i = 0; i < _gmshData.Coor[count].Length; i++)
                    {
                        _gmshData.Coor[count][i] = new double[] { coor1D[3 * i], coor1D[3 * i + 1], coor1D[3 * i + 2] };
                    }
                    count += 1;
                }
                _error = null;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
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
                    else if (System.Diagnostics.Debugger.IsAttached) throw new Exception();
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
                    else if (System.Diagnostics.Debugger.IsAttached) throw new Exception();
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
                    else if (System.Diagnostics.Debugger.IsAttached) throw new Exception();
                }
                _gmshData.FaceIdNodes = faceIdNodes;
            }
            // Faces
            //if (_gmshData.SurfaceIds != null)
            //{
            //    int faceId;
            //    List<int> surfaceIdsList = new List<int>();
            //    foreach (var surfaceId in _gmshData.SurfaceIds)
            //    {
            //        if (netgenFaceIdGmshFaceId.TryGetValue(surfaceId, out faceId))
            //            surfaceIdsList.Add(faceId);
            //        else if (System.Diagnostics.Debugger.IsAttached) throw new Exception();
            //    }
            //    _gmshData.SurfaceIds = surfaceIdsList.ToArray();
            //}
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
            double min;
            double d2;
            double x;
            double y;
            double z;
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
                faceIdData = new GmshIdLocation() { Id = faceId, Location = xyz.Coor };
                if (gmshFaceVertexNodeIdsFaceId.TryGetValue(vertexIds, out faceIdDataList)) faceIdDataList.Add(faceIdData);
                else gmshFaceVertexNodeIdsFaceId.Add(vertexIds, new List<GmshIdLocation>() { faceIdData });
            }
            // Renumber face ids
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
                            min = double.MaxValue;
                            foreach (var idLocation in idLocationList)
                            {
                                cog = new Vec3D(idLocation.Location);
                                d2 = (cog - xyz).Len2;
                                if (d2 < min)
                                {
                                    min = d2;
                                    gmshFaceId = idLocation.Id;
                                }
                            }
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
