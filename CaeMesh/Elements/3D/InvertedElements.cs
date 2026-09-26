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

namespace CaeMesh
{
    /// <summary>
    /// Finds the solid elements that are inverted or degenerate. An element is judged by its scaled Jacobian at the
    /// corners: the determinant of the three edge vectors leaving a corner divided by their lengths. It is positive
    /// at every corner of a valid element and zero or negative at a corner of a collapsed or inverted one, and an
    /// element with two corners in one place has an edge of no length. Only the corner nodes are used, so a
    /// parabolic element is judged by the linear element its corners make.
    /// </summary>
    public static class InvertedElements
    {
        // Variables
        private const double _minScaledJacobian = 1E-6;
        private static readonly int[][] _tetraCorners = new int[][]
        {
            new int[] { 0, 1, 2, 3 }, new int[] { 1, 2, 0, 3 }, new int[] { 2, 0, 1, 3 }, new int[] { 3, 2, 1, 0 }
        };
        private static readonly int[][] _pyramidCorners = new int[][]
        {
            new int[] { 0, 1, 3, 4 }, new int[] { 1, 2, 0, 4 }, new int[] { 2, 3, 1, 4 }, new int[] { 3, 0, 2, 4 }
        };
        private static readonly int[][] _wedgeCorners = new int[][]
        {
            new int[] { 0, 1, 2, 3 }, new int[] { 1, 2, 0, 4 }, new int[] { 2, 0, 1, 5 },
            new int[] { 3, 5, 4, 0 }, new int[] { 4, 3, 5, 1 }, new int[] { 5, 4, 3, 2 }
        };
        private static readonly int[][] _hexaCorners = new int[][]
        {
            new int[] { 0, 1, 3, 4 }, new int[] { 1, 2, 0, 5 }, new int[] { 2, 3, 1, 6 }, new int[] { 3, 0, 2, 7 },
            new int[] { 4, 7, 5, 0 }, new int[] { 5, 4, 6, 1 }, new int[] { 6, 5, 7, 2 }, new int[] { 7, 6, 4, 3 }
        };


        // Methods
        /// <summary>
        /// The ids of the inverted or degenerate solid elements of the part, in the order the part holds them.
        /// </summary>
        public static int[] GetElementIds(FeMesh mesh, BasePart part)
        {
            List<int> elementIds = new List<int>();
            if (mesh == null || part == null || part.Labels == null) return elementIds.ToArray();
            //
            FeElement element;
            foreach (int elementId in part.Labels)
            {
                if (mesh.Elements.TryGetValue(elementId, out element) && element is FeElement3D element3D &&
                    IsInverted(element3D, mesh.Nodes))
                    elementIds.Add(elementId);
            }
            return elementIds.ToArray();
        }
        /// <summary>
        /// Whether the solid element is inverted or degenerate.
        /// </summary>
        public static bool IsInverted(FeElement3D element, Dictionary<int, FeNode> nodes)
        {
            int[][] corners = GetCorners(element);
            if (corners == null) return false;
            //
            double[] e1 = new double[3];
            double[] e2 = new double[3];
            double[] e3 = new double[3];
            foreach (int[] corner in corners)
            {
                FeNode node = nodes[element.NodeIds[corner[0]]];
                Subtract(nodes[element.NodeIds[corner[1]]], node, e1);
                Subtract(nodes[element.NodeIds[corner[2]]], node, e2);
                Subtract(nodes[element.NodeIds[corner[3]]], node, e3);
                double lengths = Length(e1) * Length(e2) * Length(e3);
                if (lengths <= 0) return true;
                //
                double determinant = e1[0] * (e2[1] * e3[2] - e2[2] * e3[1]) -
                                     e1[1] * (e2[0] * e3[2] - e2[2] * e3[0]) +
                                     e1[2] * (e2[0] * e3[1] - e2[1] * e3[0]);
                if (determinant / lengths < _minScaledJacobian) return true;
            }
            return false;
        }
        private static int[][] GetCorners(FeElement3D element)
        {
            if (element is LinearTetraElement || element is ParabolicTetraElement) return _tetraCorners;
            if (element is LinearPyramidElement || element is ParabolicPyramidElement) return _pyramidCorners;
            if (element is LinearWedgeElement || element is ParabolicWedgeElement) return _wedgeCorners;
            if (element is LinearHexaElement || element is ParabolicHexaElement) return _hexaCorners;
            return null;
        }
        private static void Subtract(FeNode node, FeNode origin, double[] vector)
        {
            vector[0] = node.X - origin.X;
            vector[1] = node.Y - origin.Y;
            vector[2] = node.Z - origin.Z;
        }
        private static double Length(double[] vector)
        {
            return Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
        }
    }
}
