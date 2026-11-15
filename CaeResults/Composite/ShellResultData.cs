// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CaeGlobals;
using CaeMesh;

namespace CaeResults
{
    // The shell data the model gives to its results: for every result element of a shell, and of every layer of a
    // composite shell, the material axes and the material, and the ply strengths of the materials. The results
    // project does not reference the model, so the controller builds this when the results are opened.
    [Serializable]
    [PmxType(5002)]
    public class ShellResultData
    {
        // Variables
        [PmxKey(0)] private Dictionary<int, ShellResultElement> _elements;
        [PmxKey(1)] private Dictionary<string, PlyStrength> _plyStrengths;
        [PmxKey(2)] [OptionalField]
        private Dictionary<string, PlyStiffness> _plyStiffnesses;
        [NonSerialized] private FeMesh _nodeDataMesh;
        [NonSerialized] private Dictionary<int, ShellResultElement[]> _nodeData;


        // Properties
        public Dictionary<int, ShellResultElement> Elements { get { return _elements; } }
        public Dictionary<string, PlyStrength> PlyStrengths { get { return _plyStrengths; } }
        public Dictionary<string, PlyStiffness> PlyStiffnesses
        {
            get
            {
                if (_plyStiffnesses == null)
                    _plyStiffnesses = new Dictionary<string, PlyStiffness>(StringComparer.OrdinalIgnoreCase);
                return _plyStiffnesses;
            }
        }


        // Constructor
        public ShellResultData()
        {
            _elements = new Dictionary<int, ShellResultElement>();
            _plyStrengths = new Dictionary<string, PlyStrength>(StringComparer.OrdinalIgnoreCase);
        }


        // Methods
        // The result elements around every node of a shell. The layers of a composite shell have their own result nodes,
        // so the elements of a node belong to one layer, but their axes need not agree: the projected global x axis of
        // the solver turns over between neighbouring elements of a cylinder where the normal passes the x axis
        public Dictionary<int, ShellResultElement[]> GetNodeElements(FeMesh mesh)
        {
            if (_nodeData != null && _nodeDataMesh == mesh) return _nodeData;
            //
            Dictionary<int, List<ShellResultElement>> nodeElements = new Dictionary<int, List<ShellResultElement>>();
            FeElement element;
            List<ShellResultElement> elements;
            foreach (var entry in _elements)
            {
                if (!mesh.Elements.TryGetValue(entry.Key, out element)) continue;
                //
                foreach (int nodeId in element.NodeIds)
                {
                    if (!nodeElements.TryGetValue(nodeId, out elements))
                    {
                        elements = new List<ShellResultElement>();
                        nodeElements.Add(nodeId, elements);
                    }
                    elements.Add(entry.Value);
                }
            }
            //
            _nodeData = new Dictionary<int, ShellResultElement[]>();
            foreach (var entry in nodeElements) _nodeData.Add(entry.Key, entry.Value.ToArray());
            _nodeDataMesh = mesh;
            return _nodeData;
        }
    }
}
