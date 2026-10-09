// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    // The node and element operations of the model branch
    internal class MeshEditService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonMeshGateway _meshGateway;


        // Constructors
        public MeshEditService(ModelResolver modelResolver, SelectionService selectionService,
                               IPythonMeshGateway meshGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _meshGateway = meshGateway;
        }


        // Methods
        public int GetNodeCount()
        {
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            return mesh == null || mesh.Nodes == null ? 0 : mesh.Nodes.Count;
        }
        public int GetElementCount()
        {
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            return mesh == null || mesh.Elements == null ? 0 : mesh.Elements.Count;
        }
        public NodeDataAPI GetNodeData()
        {
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            if (mesh == null) throw new PythonAPIException("The model does not contain a mesh.");
            int[] ids = mesh.Nodes.Keys.OrderBy(id => id).ToArray();
            double[,] coordinates = new double[ids.Length, 3];
            for (int i = 0; i < ids.Length; i++)
            {
                double[] point = mesh.Nodes[ids[i]].Coor;
                for (int j = 0; j < 3; j++) coordinates[i, j] = point[j];
            }
            return new NodeDataAPI(ids, coordinates);
        }
        public ElementDataAPI GetElementData()
        {
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            if (mesh == null) throw new PythonAPIException("The model does not contain a mesh.");
            int[] ids = mesh.Elements.Keys.OrderBy(id => id).ToArray();
            int[] offsets = new int[ids.Length + 1];
            List<int> connectivity = new List<int>();
            for (int i = 0; i < ids.Length; i++)
            {
                offsets[i] = connectivity.Count;
                connectivity.AddRange(mesh.Elements[ids[i]].NodeIds);
            }
            offsets[ids.Length] = connectivity.Count;
            return new ElementDataAPI(ids, offsets, connectivity.ToArray());
        }
        public void RenumberNodes(int startId)
        {
            ValidateStartId(startId);
            EnsureCommandsAvailable();
            _meshGateway.RenumberNodes(startId);
        }
        public void RenumberElements(int startId)
        {
            ValidateStartId(startId);
            EnsureCommandsAvailable();
            _meshGateway.RenumberElements(startId);
        }
        public void MergeCoincidentNodes(SelectionAPI selection, object tolerance, string nodesToKeep)
        {
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            //
            MergeCoincidentNodes mergeCoincidentNodes = new MergeCoincidentNodes();
            SelectionCompilation compilation = _selectionService.CompileForView(selection, "femodel", "geometry");
            mergeCoincidentNodes.GeometryIds = compilation.Ids;
            mergeCoincidentNodes.CreationData = compilation.Selection;
            if (tolerance != null)
                mergeCoincidentNodes.Tolerance = PythonValueConverter.ToFiniteDouble(tolerance, "tolerance");
            if (nodesToKeep != null)
                mergeCoincidentNodes.NodesToKeep =
                    PythonValueConverter.ToEnum<NodesToKeepEnum>(nodesToKeep, "nodes_to_keep");
            //
            EnsureCommandsAvailable();
            _meshGateway.MergeCoincidentNodes(mergeCoincidentNodes);
        }
        public void CreateElementSetFromQuality(string name, string metric, object partNames, bool largerThan,
                                                object limit)
        {
            // An element set name also takes the characters FeElementSet adds to the ones every name may contain
            PythonNameComparer.ValidateFormat(name, "name", null, new HashSet<char>() { '@' });
            // An element set shares the name space with the other mesh entities
            foreach (string existingName in _modelResolver.GetAllMeshEntityNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The name '" + name + "' is already used by another model item.");
            }
            // The canonical spelling is what gmsh is asked for
            string metricName = PythonValueConverter.ToEnum<GmshElementQualityMetricEnum>(metric, "metric").ToString();
            double limitValue = PythonValueConverter.ToFiniteDouble(limit, "limit");
            //
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(partNames, "part_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'part_names' must not be empty.");
            //
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            foreach (string partName in names)
            {
                if (mesh == null || !mesh.Parts.ContainsKey(partName))
                    throw new PythonAPIException("The model part '" + partName + "' does not exist.");
            }
            PythonNameComparer.RefuseRepeated(names, "part_names");
            //
            EnsureCommandsAvailable();
            _meshGateway.CreateElementSetFromElementQuality(name, metricName, names, largerThan, limitValue);
        }
        //
        private static void ValidateStartId(int startId)
        {
            if (startId < 1) throw new PythonAPIException("The argument 'start_id' must be larger than zero.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_meshGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
