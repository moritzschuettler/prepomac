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
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// A single visualization face of a part, prepared for the exploded view interface detection.
    ///
    /// The area weighted normal is accumulated from the corner nodes of the face cells only, since the
    /// mid-side nodes of the parabolic elements do not necessarily lie on the underlying geometry. The
    /// normal coherence is the ratio between the length of the accumulated area vector and the summed
    /// cell areas: it is close to one for a planar face and close to zero for a closed cylindrical face,
    /// which is how a mating plane is told apart from a bore.
    /// </summary>
    public class ExplodedViewFace
    {
        // Variables                                                                                                                
        private BasePart _part;
        private int _faceId;
        private double _area;
        private double _normalCoherence;
        private Vec3D _normal;
        private BoundingBox _toleranceBoundingBox;
        private BoundingBox[] _cellToleranceBoundingBoxes;


        // Properties                                                                                                               
        public BasePart Part { get { return _part; } }
        public int FaceId { get { return _faceId; } }
        public double Area { get { return _area; } }
        public double NormalCoherence { get { return _normalCoherence; } }
        public Vec3D Normal { get { return _normal; } }
        /// <summary>
        /// The bounding box of the face, inflated by one half of the contact tolerance
        /// </summary>
        public BoundingBox ToleranceBoundingBox { get { return _toleranceBoundingBox; } }
        /// <summary>
        /// The bounding boxes of the face cells, each inflated by one half of the contact tolerance
        /// </summary>
        public BoundingBox[] CellToleranceBoundingBoxes { get { return _cellToleranceBoundingBoxes; } }
        public int CellCount { get { return _cellToleranceBoundingBoxes.Length; } }


        // Constructors                                                                                                             
        public ExplodedViewFace(FeMesh mesh, BasePart part, int faceId, double tolerance)
        {
            _part = part;
            _faceId = faceId;
            //
            VisualizationData visualization = part.Visualization;
            int[] faceCellIds = visualization.CellIdsByFace[faceId];
            Dictionary<int, FeNode> nodes = mesh.Nodes;
            double halfTolerance = 0.5 * tolerance;
            //
            _cellToleranceBoundingBoxes = new BoundingBox[faceCellIds.Length];
            _toleranceBoundingBox = new BoundingBox();
            //
            Vec3D areaVector = new Vec3D();
            Vec3D cellAreaVector = new Vec3D();
            double areaSum = 0;
            //
            for (int i = 0; i < faceCellIds.Length; i++)
            {
                int[] cell = visualization.Cells[faceCellIds[i]];
                int cornerCount = GetCornerCount(cell);
                //
                GetCellAreaVector(cell, cornerCount, nodes, ref cellAreaVector);
                //
                areaVector += cellAreaVector;
                areaSum += cellAreaVector.Len;
                //
                BoundingBox cellBoundingBox = new BoundingBox();
                cellBoundingBox.IncludeFirstCoor(nodes[cell[0]].Coor);
                for (int j = 1; j < cornerCount; j++) cellBoundingBox.IncludeCoorFast(nodes[cell[j]].Coor);
                cellBoundingBox.Inflate(halfTolerance);
                //
                _cellToleranceBoundingBoxes[i] = cellBoundingBox;
                _toleranceBoundingBox.IncludeBox(cellBoundingBox);
            }
            //
            _normalCoherence = areaSum > 0 ? areaVector.Len / areaSum : 0;
            //
            _normal = areaVector;
            if (_normal.Len > 0) _normal.Normalize();
            //
            if (visualization.FaceAreas != null && faceId < visualization.FaceAreas.Length)
                _area = visualization.FaceAreas[faceId];
            else _area = areaSum;
        }


        // Methods                                                                                                                  
        private static int GetCornerCount(int[] cell)
        {
            if (cell.Length == 3 || cell.Length == 6) return 3;
            else if (cell.Length == 4 || cell.Length == 8) return 4;
            else throw new NotSupportedException();
        }
        private static void GetCellAreaVector(int[] cell, int cornerCount, Dictionary<int, FeNode> nodes,
                                              ref Vec3D areaVector)
        {
            areaVector.X = areaVector.Y = areaVector.Z = 0;
            //
            FeNode current;
            FeNode next;
            for (int i = 0; i < cornerCount; i++)
            {
                current = nodes[cell[i]];
                next = nodes[cell[(i + 1) % cornerCount]];
                //
                areaVector.X += current.Y * next.Z - current.Z * next.Y;
                areaVector.Y += current.Z * next.X - current.X * next.Z;
                areaVector.Z += current.X * next.Y - current.Y * next.X;
            }
            //
            areaVector.X *= 0.5;
            areaVector.Y *= 0.5;
            areaVector.Z *= 0.5;
        }
    }
}
