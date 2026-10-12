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
using System.Linq;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    internal class CalDistributingWeights
    {
        // Variables                                                                                                                
        static private readonly int[][] _hexaFaces = new int[][]
        {
            new int[] { 4, 3, 2, 1, 11, 10, 9, 12 }, new int[] { 5, 6, 7, 8, 13, 14, 15, 16 },
            new int[] { 1, 2, 6, 5, 9, 18, 13, 17 }, new int[] { 2, 3, 7, 6, 10, 19, 14, 18 },
            new int[] { 3, 4, 8, 7, 11, 20, 15, 19 }, new int[] { 4, 1, 5, 8, 12, 17, 16, 20 }
        };
        static private readonly int[][] _tetraFaces = new int[][]
        {
            new int[] { 1, 3, 2, 7, 6, 5 }, new int[] { 1, 2, 4, 5, 9, 8 },
            new int[] { 2, 3, 4, 6, 10, 9 }, new int[] { 1, 4, 3, 8, 10, 7 }
        };
        static private readonly int[][] _linearWedgeFaces = new int[][]
        {
            new int[] { 1, 3, 2 }, new int[] { 4, 5, 6 }, new int[] { 1, 2, 5, 4 }, new int[] { 2, 3, 6, 5 },
            new int[] { 3, 1, 4, 6 }
        };
        static private readonly int[][] _parabolicWedgeFaces = new int[][]
        {
            new int[] { 1, 3, 2, 9, 8, 7 }, new int[] { 4, 5, 6, 10, 11, 12 }, new int[] { 1, 2, 5, 4, 7, 14, 10, 13 },
            new int[] { 2, 3, 6, 5, 8, 15, 11, 14 }, new int[] { 3, 1, 4, 6, 9, 13, 12, 15 }
        };
        static private readonly double _g2 = 0.577350269189626;
        static private readonly double _g3 = 0.774596669241483;
        static private readonly double[][] _quadOnePoint = new double[][] { new double[] { 0, 0, 4 } };
        static private readonly double[][] _quadFourPoints = new double[][]
        {
            new double[] { -_g2, -_g2, 1 }, new double[] { _g2, -_g2, 1 }, new double[] { -_g2, _g2, 1 },
            new double[] { _g2, _g2, 1 }
        };
        static private readonly double[][] _quadNinePoints = new double[][]
        {
            new double[] { -_g3, -_g3, 0.308641975308642 }, new double[] { 0, -_g3, 0.493827160493827 },
            new double[] { _g3, -_g3, 0.308641975308642 }, new double[] { -_g3, 0, 0.493827160493827 },
            new double[] { 0, 0, 0.790123456790123 }, new double[] { _g3, 0, 0.493827160493827 },
            new double[] { -_g3, _g3, 0.308641975308642 }, new double[] { 0, _g3, 0.493827160493827 },
            new double[] { _g3, _g3, 0.308641975308642 }
        };
        static private readonly double[][] _triangleOnePoint = new double[][]
        {
            new double[] { 0.333333333333333, 0.333333333333333, 0.5 }
        };
        static private readonly double[][] _triangleThreePoints = new double[][]
        {
            new double[] { 0.166666666666667, 0.166666666666667, 0.166666666666666 },
            new double[] { 0.666666666666667, 0.166666666666667, 0.166666666666666 },
            new double[] { 0.166666666666667, 0.666666666666667, 0.166666666666666 }
        };
        //
        private Dictionary<int, double> _nodeAreas;
        private double _area;


        // Constructors                                                                                                             
        public CalDistributingWeights()
        {
            _nodeAreas = new Dictionary<int, double>();
            _area = 0;
        }


        // Methods                                                                                                                  
        public bool AddFace(string elementType, int[] elementNodeIds, int faceNumber, IDictionary<int, FeNode> nodes)
        {
            // The face nodes and the integration points CalculiX uses for the area of a distributing coupling face, whose
            // share of each node is the weight of the node
            int[][] faces;
            int numOfElementNodes;
            bool cornersOnly;
            if (elementType == "C3D8" || elementType == "C3D8I" || elementType == "C3D8R")
            {
                faces = _hexaFaces;
                numOfElementNodes = 8;
                cornersOnly = true;
            }
            else if (elementType == "C3D20" || elementType == "C3D20R")
            {
                faces = _hexaFaces;
                numOfElementNodes = 20;
                cornersOnly = false;
            }
            else if (elementType == "C3D4")
            {
                faces = _tetraFaces;
                numOfElementNodes = 4;
                cornersOnly = true;
            }
            else if (elementType == "C3D10" || elementType == "C3D10T")
            {
                faces = _tetraFaces;
                numOfElementNodes = 10;
                cornersOnly = false;
            }
            else if (elementType == "C3D6")
            {
                faces = _linearWedgeFaces;
                numOfElementNodes = 6;
                cornersOnly = false;
            }
            else if (elementType == "C3D15")
            {
                faces = _parabolicWedgeFaces;
                numOfElementNodes = 15;
                cornersOnly = false;
            }
            else return false;
            //
            if (elementNodeIds.Length != numOfElementNodes || faceNumber < 1 || faceNumber > faces.Length) return false;
            int[] face = faces[faceNumber - 1];
            int numOfFaceNodes = cornersOnly ? face.Length / 2 : face.Length;
            double[][] points;
            if (elementType == "C3D8R" || (elementType == "C3D6" && numOfFaceNodes == 4)) points = _quadOnePoint;
            else if (numOfFaceNodes == 4 || (numOfFaceNodes == 8 && elementType != "C3D20")) points = _quadFourPoints;
            else if (numOfFaceNodes == 8) points = _quadNinePoints;
            else if (numOfFaceNodes == 6) points = _triangleThreePoints;
            else points = _triangleOnePoint;
            //
            int[] faceNodeIds = new int[numOfFaceNodes];
            double[][] coor = new double[numOfFaceNodes][];
            FeNode node;
            for (int i = 0; i < numOfFaceNodes; i++)
            {
                faceNodeIds[i] = elementNodeIds[face[i] - 1];
                if (!nodes.TryGetValue(faceNodeIds[i], out node)) return false;
                coor[i] = node.Coor;
            }
            //
            double[] shapeFunctions = new double[numOfFaceNodes];
            double[] derivativesXi = new double[numOfFaceNodes];
            double[] derivativesEta = new double[numOfFaceNodes];
            double[] tangentXi = new double[3];
            double[] tangentEta = new double[3];
            double area;
            foreach (int nodeId in faceNodeIds)
            {
                if (!_nodeAreas.ContainsKey(nodeId)) _nodeAreas.Add(nodeId, 0);
            }
            foreach (double[] point in points)
            {
                GetShapeFunctions(numOfFaceNodes, point[0], point[1], shapeFunctions, derivativesXi, derivativesEta);
                for (int i = 0; i < 3; i++)
                {
                    tangentXi[i] = 0;
                    tangentEta[i] = 0;
                    for (int j = 0; j < numOfFaceNodes; j++)
                    {
                        tangentXi[i] += coor[j][i] * derivativesXi[j];
                        tangentEta[i] += coor[j][i] * derivativesEta[j];
                    }
                }
                area = point[2] * Math.Sqrt(Math.Pow(tangentXi[1] * tangentEta[2] - tangentXi[2] * tangentEta[1], 2) +
                                            Math.Pow(tangentXi[2] * tangentEta[0] - tangentXi[0] * tangentEta[2], 2) +
                                            Math.Pow(tangentXi[0] * tangentEta[1] - tangentXi[1] * tangentEta[0], 2));
                _area += area;
                for (int i = 0; i < numOfFaceNodes; i++) _nodeAreas[faceNodeIds[i]] += area * shapeFunctions[i];
            }
            return true;
        }
        public void AddFaceArea(int[] faceNodeIds, double[] nodalShares, double area)
        {
            // A face CalculiX has no table for, such as an edge of a 2D element, adds its area by the shares of its nodes
            for (int i = 0; i < faceNodeIds.Length; i++)
            {
                if (_nodeAreas.ContainsKey(faceNodeIds[i])) _nodeAreas[faceNodeIds[i]] += area * nodalShares[i];
                else _nodeAreas.Add(faceNodeIds[i], area * nodalShares[i]);
            }
            _area += area;
        }
        public Dictionary<int, double> GetNodeWeights()
        {
            // The share of each node, in the order of the node ids
            if (_area <= 0) return null;
            //
            Dictionary<int, double> weights = new Dictionary<int, double>();
            foreach (var entry in _nodeAreas.OrderBy(entry => entry.Key)) weights.Add(entry.Key, entry.Value / _area);
            return weights;
        }
        static private void GetShapeFunctions(int numOfNodes, double xi, double eta, double[] n, double[] dXi,
                                              double[] dEta)
        {
            if (numOfNodes == 3)
            {
                n[0] = 1 - xi - eta; n[1] = xi; n[2] = eta;
                dXi[0] = -1; dXi[1] = 1; dXi[2] = 0;
                dEta[0] = -1; dEta[1] = 0; dEta[2] = 1;
            }
            else if (numOfNodes == 6)
            {
                double c = 1 - xi - eta;
                n[0] = 2 * (0.5 - xi - eta) * c; n[1] = xi * (2 * xi - 1); n[2] = eta * (2 * eta - 1);
                n[3] = 4 * xi * c; n[4] = 4 * xi * eta; n[5] = 4 * eta * c;
                dXi[0] = 4 * (xi + eta) - 3; dXi[1] = 4 * xi - 1; dXi[2] = 0;
                dXi[3] = 4 * (c - xi); dXi[4] = 4 * eta; dXi[5] = -4 * eta;
                dEta[0] = 4 * (xi + eta) - 3; dEta[1] = 0; dEta[2] = 4 * eta - 1;
                dEta[3] = -4 * xi; dEta[4] = 4 * xi; dEta[5] = 4 * (c - eta);
            }
            else if (numOfNodes == 4)
            {
                double xip = 1 + xi, xim = 1 - xi, etp = 1 + eta, etm = 1 - eta;
                n[0] = xim * etm / 4; n[1] = xip * etm / 4; n[2] = xip * etp / 4; n[3] = xim * etp / 4;
                dXi[0] = -etm / 4; dXi[1] = etm / 4; dXi[2] = etp / 4; dXi[3] = -etp / 4;
                dEta[0] = -xim / 4; dEta[1] = -xip / 4; dEta[2] = xip / 4; dEta[3] = xim / 4;
            }
            else if (numOfNodes == 8)
            {
                double xip = 1 + xi, xim = 1 - xi, etp = 1 + eta, etm = 1 - eta;
                double xim2 = 1 - xi * xi, etm2 = 1 - eta * eta, xipet = xi + eta, ximet = xi - eta;
                n[0] = xim * etm * (-xipet - 1) / 4; n[1] = xip * etm * (ximet - 1) / 4;
                n[2] = xip * etp * (xipet - 1) / 4; n[3] = xim * etp * (-ximet - 1) / 4;
                n[4] = xim2 * etm / 2; n[5] = xip * etm2 / 2; n[6] = xim2 * etp / 2; n[7] = xim * etm2 / 2;
                dXi[0] = etm * (xi + xipet) / 4; dXi[1] = etm * (xi + ximet) / 4;
                dXi[2] = etp * (xi + xipet) / 4; dXi[3] = etp * (xi + ximet) / 4;
                dXi[4] = -xi * etm; dXi[5] = etm2 / 2; dXi[6] = -xi * etp; dXi[7] = -etm2 / 2;
                dEta[0] = xim * (eta + xipet) / 4; dEta[1] = xip * (eta - ximet) / 4;
                dEta[2] = xip * (eta + xipet) / 4; dEta[3] = xim * (eta - ximet) / 4;
                dEta[4] = -xim2 / 2; dEta[5] = -eta * xip; dEta[6] = xim2 / 2; dEta[7] = -eta * xim;
            }
            else throw new NotSupportedException();
        }
    }
}
