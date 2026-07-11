// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using CaeMesh;
using CaeResults;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.API.Python
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
        public NodeDataAPI GetNodeData(string resultName)
        {
            FeResults result = ResolveResultWithMesh(resultName);
            int count = result.Mesh.Nodes.Count;
            int[] ids = new int[count];
            double[,] coordinates = new double[count, 3];
            int i = 0;
            foreach (var entry in result.Mesh.Nodes)
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
    }
}
