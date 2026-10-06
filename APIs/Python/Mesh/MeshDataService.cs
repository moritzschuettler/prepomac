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
using CaeResults;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    internal class MeshDataService
    {
        // Variables
        private readonly ResultResolver _resolver;


        // Constructors
        public MeshDataService(ResultResolver resolver)
        {
            _resolver = resolver;
        }


        // Methods
        // The commands that change result parts act on the current result, so a part of any other one is refused
        // rather than read from one result and written to another
        public void EnsureCurrentResult(string resultName)
        {
            if (!_resolver.IsCurrentResult(resultName))
                throw new PythonAPIException("The result '" + resultName + "' is not the current one, and only the " +
                                             "parts of the current result can be changed.");
        }
        public NodeDataAPI GetNodeData(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            Dictionary<int, FeNode> nodes = result.UndeformedNodes;
            int count = nodes.Count;
            int[] ids = new int[count];
            double[,] coordinates = new double[count, 3];
            int i = 0;
            foreach (var entry in nodes)
            {
                ids[i] = entry.Key;
                if (entry.Value.Coor != null)
                {
                    for (int j = 0; j < 3 && j < entry.Value.Coor.Length; j++) coordinates[i, j] = entry.Value.Coor[j];
                }
                i++;
            }
            return new NodeDataAPI(ids, coordinates);
        }
        public object GetNodeCoordinates(string resultName, int nodeId)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            Dictionary<int, FeNode> nodes = result.UndeformedNodes;
            FeNode node;
            if (!nodes.TryGetValue(nodeId, out node))
                throw new PythonAPIException("The node ID " + nodeId + " does not exist in result '" + resultName + "'.");
            double[] coordinates = new double[3];
            if (node.Coor != null)
            {
                for (int i = 0; i < coordinates.Length && i < node.Coor.Length; i++) coordinates[i] = node.Coor[i];
            }
            return PythonListConverter.ToPythonList(coordinates);
        }
        public ElementDataAPI GetElementData(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            int count = result.Mesh.Elements.Count;
            int[] ids = new int[count];
            int[] offsets = new int[count + 1];
            List<int> connectivity = new List<int>();
            int i = 0;
            foreach (var entry in result.Mesh.Elements)
            {
                int[] nodeIds = entry.Value.NodeIds;
                ids[i] = entry.Key;
                offsets[i] = connectivity.Count;
                if (nodeIds != null) connectivity.AddRange(nodeIds);
                i++;
            }
            offsets[count] = connectivity.Count;
            return new ElementDataAPI(ids, offsets, connectivity.ToArray());
        }
        public int[] GetElementIds(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            return result.Mesh.Elements.Keys.ToArray();
        }
        public ElementAPI GetElement(string resultName, int elementId)
        {
            GetElementInfo(resultName, elementId);
            return new ElementAPI(resultName, elementId, this);
        }
        public ElementInfo GetElementInfo(string resultName, int elementId)
        {
            FeElement element = ResolveElement(resultName, elementId);
            return new ElementInfo(element.Id,
                                         element.GetType().Name,
                                         element.Dimension,
                                         element.NodeIds != null ? element.NodeIds.ToArray() : new int[0]);
        }
        public string[] GetPartNames(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            return result.Mesh.Parts.Keys.ToArray();
        }
        public PartAPI GetPart(string resultName, string partName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            if (!result.Mesh.Parts.ContainsKey(partName))
                throw new PythonAPIException("The part '" + partName + "' does not exist in result '" + resultName + "'.");
            return new PartAPI(resultName, partName, this);
        }
        public PartData GetPartData(string resultName, string partName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            BasePart part;
            if (!result.Mesh.Parts.TryGetValue(partName, out part))
                throw new PythonAPIException("The part '" + partName + "' does not exist in result '" + resultName + "'.");
            return new PartData(part.NodeLabels != null ? part.NodeLabels.ToArray() : new int[0],
                                      part.Labels != null ? part.Labels.ToArray() : new int[0]);
        }
        public BasePart GetPartObject(string resultName, string partName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            BasePart part;
            if (!result.Mesh.Parts.TryGetValue(partName, out part))
                throw new PythonAPIException("The part '" + partName + "' does not exist in result '" + resultName + "'.");
            return part;
        }
        public FeMesh GetMeshObject(string resultName)
        {
            return ResolveResultWithMesh(resultName).Mesh;
        }
        public string[] GetNodeSetNames(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            return result.Mesh.NodeSets.Where(entry => !entry.Value.Internal)
                                       .Select(entry => entry.Key).ToArray();
        }
        public ResultNodeSetAPI GetNodeSet(string resultName, string nodeSetName)
        {
            ValidateUserRegion(resultName, RegionTypeEnum.NodeSetName, nodeSetName);
            return new ResultNodeSetAPI(resultName, nodeSetName, this);
        }
        public int[] GetNodeSetIds(string resultName, string nodeSetName)
        {
            FeNodeSet nodeSet = ResolveUserNodeSet(resultName, nodeSetName);
            return nodeSet.Labels != null ? nodeSet.Labels.ToArray() : new int[0];
        }
        public string[] GetElementSetNames(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            return result.Mesh.ElementSets.Where(entry => !entry.Value.Internal)
                                          .Select(entry => entry.Key).ToArray();
        }
        public ResultElementSetAPI GetElementSet(string resultName, string elementSetName)
        {
            ValidateUserRegion(resultName, RegionTypeEnum.ElementSetName, elementSetName);
            return new ResultElementSetAPI(resultName, elementSetName, this);
        }
        public int[] GetElementSetIds(string resultName, string elementSetName)
        {
            FeElementSet elementSet = ResolveUserElementSet(resultName, elementSetName);
            return elementSet.Labels != null ? elementSet.Labels.ToArray() : new int[0];
        }
        public string[] GetSurfaceNames(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            return result.Mesh.Surfaces.Where(entry => !entry.Value.Internal)
                                       .Select(entry => entry.Key).ToArray();
        }
        public ResultSurfaceAPI GetSurface(string resultName, string surfaceName)
        {
            ValidateUserRegion(resultName, RegionTypeEnum.SurfaceName, surfaceName);
            return new ResultSurfaceAPI(resultName, surfaceName, this);
        }
        public int[] GetSurfaceFaceIds(string resultName, string surfaceName)
        {
            FeSurface surface = ResolveUserSurface(resultName, surfaceName);
            return surface.FaceIds != null ? surface.FaceIds.ToArray() : new int[0];
        }
        public int[] GetSurfaceNodeIds(string resultName, string surfaceName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            FeSurface surface = ResolveUserSurface(resultName, surfaceName);
            if (surface.NodeSetName == null || !result.Mesh.NodeSets.ContainsKey(surface.NodeSetName))
                return new int[0];
            int[] labels = result.Mesh.NodeSets[surface.NodeSetName].Labels;
            return labels != null ? labels.ToArray() : new int[0];
        }
        public double GetSurfaceArea(string resultName, string surfaceName)
        {
            return ResolveUserSurface(resultName, surfaceName).Area;
        }
        public void ValidateUserRegion(string resultName, RegionTypeEnum regionType, string regionName)
        {
            if (regionType == RegionTypeEnum.NodeSetName) ResolveUserNodeSet(resultName, regionName);
            else if (regionType == RegionTypeEnum.ElementSetName) ResolveUserElementSet(resultName, regionName);
            else if (regionType == RegionTypeEnum.SurfaceName) ResolveUserSurface(resultName, regionName);
            else throw new PythonAPIException("Unsupported result mesh region type '" + regionType + "'.");
        }
        public FeResults ResolveResultWithMesh(string resultName)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            if (result.Mesh == null)
                throw new PythonAPIException("The result '" + resultName + "' does not contain a mesh.");
            return result;
        }
        private FeElement ResolveElement(string resultName, int elementId)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            FeElement element;
            if (!result.Mesh.Elements.TryGetValue(elementId, out element))
                throw new PythonAPIException("The element ID " + elementId + " does not exist in result '" + resultName + "'.");
            return element;
        }
        private FeNodeSet ResolveUserNodeSet(string resultName, string nodeSetName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            FeNodeSet nodeSet;
            if (!result.Mesh.NodeSets.TryGetValue(nodeSetName, out nodeSet) || nodeSet.Internal)
                throw new PythonAPIException("The user-visible node set '" + nodeSetName +
                                             "' does not exist in result '" + resultName + "'.");
            return nodeSet;
        }
        private FeElementSet ResolveUserElementSet(string resultName, string elementSetName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            FeElementSet elementSet;
            if (!result.Mesh.ElementSets.TryGetValue(elementSetName, out elementSet) || elementSet.Internal)
                throw new PythonAPIException("The user-visible element set '" + elementSetName +
                                             "' does not exist in result '" + resultName + "'.");
            return elementSet;
        }
        private FeSurface ResolveUserSurface(string resultName, string surfaceName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            FeSurface surface;
            if (!result.Mesh.Surfaces.TryGetValue(surfaceName, out surface) || surface.Internal)
                throw new PythonAPIException("The user-visible surface '" + surfaceName +
                                             "' does not exist in result '" + resultName + "'.");
            return surface;
        }
    }
}
