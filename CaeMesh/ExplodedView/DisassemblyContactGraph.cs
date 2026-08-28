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
using System.Text;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The contact graph of the exploded view clusters, which is the first stage of the disassembly
    /// direction exploded view. The nodes are the groups of connected parts and the edges are the
    /// detected interfaces, each of which keeps the faces that form it on both sides.
    ///
    /// A face pair is an interface when the cell bounding boxes inflated by one half of the contact
    /// tolerance intersect. When both faces are close to planar their area weighted normals must also be
    /// close to opposite, which removes the pairs that only share a bounding box corner. The normal test
    /// is skipped for the faces of a low normal coherence - the area weighted normal of a full bore is
    /// close to zero and such a face would otherwise never be paired with the shaft inside it.
    /// </summary>
    public class DisassemblyContactGraph
    {
        // Variables                                                                                                                
        public static double DefaultToleranceFactor = 1E-3;
        public static double MinNormalCoherence = 0.7;
        public static double MaxNormalAngleDeg = 30;
        public static int MinGridCellCount = 64;
        public static int MaxCachedGrids = 256;
        //
        private double _tolerance;
        private ExplodedViewCluster[] _clusters;
        private ExplodedViewInterface[] _interfaces;
        private List<ExplodedViewInterface>[] _interfacesByCluster;


        // Properties                                                                                                               
        public ExplodedViewCluster[] Clusters { get { return _clusters; } }
        public ExplodedViewInterface[] Interfaces { get { return _interfaces; } }


        // Constructors                                                                                                             
        private DisassemblyContactGraph(double tolerance, ExplodedViewCluster[] clusters,
                                        ExplodedViewInterface[] interfaces)
        {
            _tolerance = tolerance;
            _clusters = clusters;
            _interfaces = interfaces;
            //
            _interfacesByCluster = new List<ExplodedViewInterface>[_clusters.Length];
            for (int i = 0; i < _interfacesByCluster.Length; i++)
                _interfacesByCluster[i] = new List<ExplodedViewInterface>();
            //
            foreach (ExplodedViewInterface contactInterface in _interfaces)
            {
                _interfacesByCluster[contactInterface.FirstClusterId].Add(contactInterface);
                _interfacesByCluster[contactInterface.SecondClusterId].Add(contactInterface);
            }
        }


        // Static methods                                                                                                           
        public static DisassemblyContactGraph Create(FeMesh mesh, List<List<BasePart>> connectedParts, double tolerance)
        {
            if (mesh == null || connectedParts == null) return null;
            //
            ExplodedViewCluster[] clusters = new ExplodedViewCluster[connectedParts.Count];
            for (int i = 0; i < connectedParts.Count; i++)
                clusters[i] = new ExplodedViewCluster(mesh, connectedParts[i], i, tolerance);
            //
            ExplodedViewInterface[] interfaces = GetAllInterfaces(clusters);
            DisassemblyDirectionResolver.Resolve(mesh, clusters, interfaces);
            //
            return new DisassemblyContactGraph(tolerance, clusters, interfaces);
        }
        public static double GetDefaultTolerance(IEnumerable<BoundingBox> boundingBoxes)
        {
            BoundingBox assemblyBoundingBox = new BoundingBox();
            foreach (BoundingBox boundingBox in boundingBoxes) assemblyBoundingBox.IncludeBox(boundingBox);
            //
            double diagonal = assemblyBoundingBox.GetDiagonal();
            if (diagonal <= 0) return DefaultToleranceFactor;
            else return diagonal * DefaultToleranceFactor;
        }
        private static ExplodedViewInterface[] GetAllInterfaces(ExplodedViewCluster[] clusters)
        {
            Dictionary<ExplodedViewFace, ExplodedViewCellGrid> gridByFace =
                new Dictionary<ExplodedViewFace, ExplodedViewCellGrid>();
            List<ExplodedViewInterface> interfaces = new List<ExplodedViewInterface>();
            //
            for (int i = 0; i < clusters.Length; i++)
            {
                for (int j = i + 1; j < clusters.Length; j++)
                {
                    if (!clusters[i].ToleranceBoundingBox.Intersects(clusters[j].ToleranceBoundingBox)) continue;
                    //
                    ExplodedViewInterface contactInterface = GetInterface(clusters[i], clusters[j], gridByFace);
                    if (contactInterface != null) interfaces.Add(contactInterface);
                }
            }
            //
            return interfaces.ToArray();
        }
        private static ExplodedViewInterface GetInterface(ExplodedViewCluster firstCluster,
                                                          ExplodedViewCluster secondCluster,
                                                          Dictionary<ExplodedViewFace, ExplodedViewCellGrid> gridByFace)
        {
            ExplodedViewInterface contactInterface = null;
            double maxNormalDotProduct = -Math.Cos(MaxNormalAngleDeg * Math.PI / 180);
            //
            foreach (ExplodedViewFace firstFace in firstCluster.Faces)
            {
                foreach (ExplodedViewFace secondFace in secondCluster.Faces)
                {
                    if (!firstFace.ToleranceBoundingBox.Intersects(secondFace.ToleranceBoundingBox)) continue;
                    //
                    if (firstFace.NormalCoherence >= MinNormalCoherence &&
                        secondFace.NormalCoherence >= MinNormalCoherence &&
                        Vec3D.DotProduct(firstFace.Normal, secondFace.Normal) > maxNormalDotProduct) continue;
                    //
                    if (!AreFacesInContact(firstFace, secondFace, gridByFace)) continue;
                    //
                    if (contactInterface == null)
                        contactInterface = new ExplodedViewInterface(firstCluster.Id, secondCluster.Id);
                    contactInterface.AddFacePair(firstFace, secondFace);
                }
            }
            //
            return contactInterface;
        }
        private static bool AreFacesInContact(ExplodedViewFace firstFace, ExplodedViewFace secondFace,
                                              Dictionary<ExplodedViewFace, ExplodedViewCellGrid> gridByFace)
        {
            ExplodedViewFace gridFace;
            ExplodedViewFace queryFace;
            if (firstFace.CellCount >= secondFace.CellCount) { gridFace = firstFace; queryFace = secondFace; }
            else { gridFace = secondFace; queryFace = firstFace; }
            //
            BoundingBox[] gridBoxes = gridFace.CellToleranceBoundingBoxes;
            BoundingBox[] queryBoxes = queryFace.CellToleranceBoundingBoxes;
            //
            if (gridBoxes.Length < MinGridCellCount)
            {
                for (int i = 0; i < queryBoxes.Length; i++)
                {
                    for (int j = 0; j < gridBoxes.Length; j++)
                    {
                        if (queryBoxes[i].Intersects(gridBoxes[j])) return true;
                    }
                }
                return false;
            }
            //
            ExplodedViewCellGrid grid;
            if (!gridByFace.TryGetValue(gridFace, out grid))
            {
                if (gridByFace.Count >= MaxCachedGrids) gridByFace.Clear();
                //
                grid = new ExplodedViewCellGrid(gridBoxes);
                gridByFace.Add(gridFace, grid);
            }
            //
            for (int i = 0; i < queryBoxes.Length; i++)
            {
                if (grid.Intersects(queryBoxes[i])) return true;
            }
            //
            return false;
        }


        // Methods                                                                                                                  
        public List<ExplodedViewInterface> GetInterfaces(int clusterId)
        {
            return _interfacesByCluster[clusterId];
        }
        public ExplodedViewInterface GetInterface(int clusterId, int otherClusterId)
        {
            foreach (ExplodedViewInterface contactInterface in _interfacesByCluster[clusterId])
            {
                if (contactInterface.GetOtherClusterId(clusterId) == otherClusterId) return contactInterface;
            }
            return null;
        }
        public int GetDegree(int clusterId)
        {
            return _interfacesByCluster[clusterId].Count;
        }
        private string GetInterfaceKind(ExplodedViewInterface contactInterface)
        {
            if (contactInterface.IsAxialMount)
                return "axial through " + _clusters[contactInterface.ShaftClusterId].GetName();
            else if (contactInterface.IsAxialDirection) return "axial, no shaft";
            else if (contactInterface.ShaftClusterId >= 0)
                return "planar, along " + _clusters[contactInterface.ShaftClusterId].GetName();
            else return "planar";
        }
        public string GetSummary()
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Disassembly contact graph");
            summary.AppendLine("Tolerance: " + _tolerance);
            summary.AppendLine("Clusters: " + _clusters.Length);
            summary.AppendLine("Interfaces: " + _interfaces.Length);
            //
            foreach (ExplodedViewCluster cluster in _clusters)
            {
                summary.AppendLine("  [" + cluster.Id + "] " + cluster.GetName() +
                                   "  faces: " + cluster.Faces.Length +
                                   "  degree: " + GetDegree(cluster.Id) +
                                   "  volume: " + cluster.Volume.ToString("G4"));
            }
            //
            foreach (ExplodedViewInterface contactInterface in _interfaces)
            {
                string direction;
                if (contactInterface.HasDirection)
                    direction = "  direction: (" + string.Join(", ", contactInterface.Direction.CoorRounded(3)) +
                                ")  " + GetInterfaceKind(contactInterface) +
                                "  confidence: " + contactInterface.DirectionConfidence.ToString("G3");
                else direction = "  direction: undetermined";
                //
                summary.AppendLine("  " + _clusters[contactInterface.FirstClusterId].GetName() + " - " +
                                   _clusters[contactInterface.SecondClusterId].GetName() +
                                   "  faces: " + contactInterface.FirstFaces.Count + "/" +
                                   contactInterface.SecondFaces.Count +
                                   "  area: " + contactInterface.Area.ToString("G4") + direction);
            }
            //
            return summary.ToString();
        }
    }
}
