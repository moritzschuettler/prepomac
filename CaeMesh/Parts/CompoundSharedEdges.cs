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
using System.Globalization;
using System.Linq;
using System.Text;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The model edges that the sub parts of a compound part hold twice. Each sub part is tessellated on
    /// its own, so an edge lying on the interface between two sub parts is a model edge of both of them and
    /// is drawn once for each. The edges are drawn with a transparent color, which makes such a doubled
    /// edge darker and thicker than a single one. Each shared edge is therefore drawn by a single sub part:
    /// the first one that is visible, so that hiding a sub part does not take the edge away from the sub
    /// part that is left showing.
    /// The offsets an exploded view gives the sub parts are part of the signature. A compound is
    /// moved as one body, so its sub parts keep the edges they share, but sub parts that were moved
    /// apart share nothing and the edges are read again from where their nodes now are.
    /// </summary>
    public class CompoundSharedEdges
    {
        // Variables                                                                                                                
        private string _signature;
        private Dictionary<string, Dictionary<int, string[]>> _subPartNamesBySharedEdgeId;


        // Properties                                                                                                               
        public string Signature { get { return _signature; } }
        public bool IsEmpty { get { return _subPartNamesBySharedEdgeId.Count == 0; } }


        // Constructors                                                                                                             
        public CompoundSharedEdges(CompoundGeometryPart compoundPart, FeMesh mesh, string signature)
        {
            _signature = signature;
            _subPartNamesBySharedEdgeId = new Dictionary<string, Dictionary<int, string[]>>();
            //
            Build(compoundPart, mesh);
        }


        // Methods                                                                                                                  
        public static string GetSignature(CompoundGeometryPart compoundPart, FeMesh mesh)
        {
            BasePart subPart;
            StringBuilder signature = new StringBuilder();
            //
            foreach (string subPartName in compoundPart.SubPartNames)
            {
                signature.Append(subPartName);
                if (mesh.Parts.TryGetValue(subPartName, out subPart) && subPart.Visualization != null &&
                    subPart.Visualization.EdgeCells != null && subPart.Visualization.EdgeCellIdsByEdge != null)
                {
                    signature.Append("|");
                    signature.Append(subPart.Visualization.EdgeCells.Length);
                    signature.Append("|");
                    signature.Append(subPart.Visualization.EdgeCount);
                    signature.Append("|");
                    signature.Append(subPart.NodeLabels != null ? subPart.NodeLabels.Length : 0);
                    signature.Append("|");
                    signature.Append(GetOffsetKey(subPart.Offset));
                }
                signature.Append(";");
            }
            return signature.ToString();
        }
        public HashSet<int> GetEdgeCellIdsDrawnByAnotherSubPart(string subPartName, FeMesh mesh)
        {
            BasePart subPart;
            Dictionary<int, string[]> subPartNamesByEdgeId;
            //
            if (!_subPartNamesBySharedEdgeId.TryGetValue(subPartName, out subPartNamesByEdgeId)) return null;
            if (!mesh.Parts.TryGetValue(subPartName, out subPart) || subPart.Visualization == null ||
                subPart.Visualization.EdgeCellIdsByEdge == null) return null;
            //
            HashSet<int> edgeCellIds = new HashSet<int>();
            foreach (var entry in subPartNamesByEdgeId)
            {
                if (GetOwnerSubPartName(entry.Value, mesh) != subPartName)
                    edgeCellIds.UnionWith(subPart.Visualization.EdgeCellIdsByEdge[entry.Key]);
            }
            if (edgeCellIds.Count == 0) return null;
            //
            return edgeCellIds;
        }
        private void Build(CompoundGeometryPart compoundPart, FeMesh mesh)
        {
            double tolerance = GetTolerance(compoundPart, mesh);
            if (tolerance <= 0) return;
            //
            string edgeKey;
            BasePart subPart;
            List<string> subPartNames;
            Dictionary<int, string> edgeKeysByEdgeId;
            Dictionary<string, List<string>> subPartNamesByEdgeKey = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<int, string>> edgeKeysBySubPartName =
                new Dictionary<string, Dictionary<int, string>>();
            //
            foreach (string subPartName in compoundPart.SubPartNames)
            {
                if (!mesh.Parts.TryGetValue(subPartName, out subPart)) continue;
                if (subPart.Visualization == null || subPart.Visualization.EdgeCells == null ||
                    subPart.Visualization.EdgeCellIdsByEdge == null) continue;
                if (edgeKeysBySubPartName.ContainsKey(subPartName)) continue;
                //
                edgeKeysByEdgeId = new Dictionary<int, string>();
                edgeKeysBySubPartName.Add(subPartName, edgeKeysByEdgeId);
                //
                for (int edgeId = 0; edgeId < subPart.Visualization.EdgeCount; edgeId++)
                {
                    edgeKey = GetEdgeKey(subPart.Visualization, edgeId, mesh, tolerance);
                    if (edgeKey == null) continue;
                    //
                    edgeKeysByEdgeId.Add(edgeId, edgeKey);
                    //
                    if (subPartNamesByEdgeKey.TryGetValue(edgeKey, out subPartNames)) subPartNames.Add(subPartName);
                    else subPartNamesByEdgeKey.Add(edgeKey, new List<string>() { subPartName });
                }
            }
            //
            Dictionary<int, string[]> sharedEdgeIds;
            foreach (var subPartEntry in edgeKeysBySubPartName)
            {
                sharedEdgeIds = new Dictionary<int, string[]>();
                foreach (var edgeEntry in subPartEntry.Value)
                {
                    subPartNames = subPartNamesByEdgeKey[edgeEntry.Value];
                    if (subPartNames.Count > 1) sharedEdgeIds.Add(edgeEntry.Key, subPartNames.ToArray());
                }
                if (sharedEdgeIds.Count > 0) _subPartNamesBySharedEdgeId.Add(subPartEntry.Key, sharedEdgeIds);
            }
        }
        private static string GetOwnerSubPartName(string[] subPartNames, FeMesh mesh)
        {
            BasePart subPart;
            foreach (string subPartName in subPartNames)
            {
                if (mesh.Parts.TryGetValue(subPartName, out subPart) && subPart.Visible) return subPartName;
            }
            return subPartNames[0];
        }
        private static string GetEdgeKey(VisualizationData visualization, int edgeId, FeMesh mesh, double tolerance)
        {
            FeNode node;
            BoundingBox boundingBox = new BoundingBox();
            HashSet<int> nodeIds = visualization.GetNodeIdsForEdgeId(edgeId);
            //
            if (nodeIds.Count == 0) return null;
            foreach (int nodeId in nodeIds)
            {
                if (!mesh.Nodes.TryGetValue(nodeId, out node)) return null;
                boundingBox.IncludeNode(node);
            }
            //
            List<string> vertexKeys = new List<string>();
            foreach (int nodeId in visualization.GetVertexNodeIdsForEdgeId(edgeId))
            {
                if (!mesh.Nodes.TryGetValue(nodeId, out node)) return null;
                vertexKeys.Add(GetCoorKey(node.Coor, tolerance));
            }
            vertexKeys.Sort();
            //
            StringBuilder edgeKey = new StringBuilder();
            foreach (string vertexKey in vertexKeys) edgeKey.Append(vertexKey);
            edgeKey.Append(GetCoorKey(new double[] { boundingBox.MinX, boundingBox.MinY, boundingBox.MinZ }, tolerance));
            edgeKey.Append(GetCoorKey(new double[] { boundingBox.MaxX, boundingBox.MaxY, boundingBox.MaxZ }, tolerance));
            //
            return edgeKey.ToString();
        }
        private static string GetOffsetKey(double[] offset)
        {
            if (offset == null) return "0";
            //
            StringBuilder offsetKey = new StringBuilder();
            for (int i = 0; i < offset.Length; i++)
            {
                offsetKey.Append(offset[i].ToString(CultureInfo.InvariantCulture));
                offsetKey.Append(",");
            }
            return offsetKey.ToString();
        }
        private static string GetCoorKey(double[] coor, double tolerance)
        {
            StringBuilder coorKey = new StringBuilder();
            for (int i = 0; i < coor.Length; i++)
            {
                coorKey.Append((long)Math.Round(coor[i] / tolerance));
                coorKey.Append(";");
            }
            return coorKey.ToString();
        }
        private static double GetTolerance(CompoundGeometryPart compoundPart, FeMesh mesh)
        {
            BasePart subPart;
            BoundingBox boundingBox = new BoundingBox();
            //
            foreach (string subPartName in compoundPart.SubPartNames)
            {
                if (mesh.Parts.TryGetValue(subPartName, out subPart)) boundingBox.IncludeBox(subPart.BoundingBox);
            }
            double diagonal = boundingBox.GetDiagonal();
            if (double.IsNaN(diagonal) || double.IsInfinity(diagonal) || diagonal <= 0) return 0;
            //
            return diagonal * 1E-7;
        }
    }
}
