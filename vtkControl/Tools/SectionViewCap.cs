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
using Kitware.VTK;


namespace vtkControl
{
    public static class SectionViewCap
    {
        // Methods
        public static vtkPolyData GetCap(vtkPolyData closedSurface, vtkPlane plane)
        {
            // The section face is the area the closed loops of the cut enclose on the plane
            vtkCutter cutter = vtkCutter.New();
            cutter.SetCutFunction(plane);
            cutter.SetInput(closedSurface);
            cutter.GenerateCutScalarsOff();
            cutter.Update();
            //
            vtkCleanPolyData clean = vtkCleanPolyData.New();
            clean.SetInput(cutter.GetOutput());
            clean.PointMergingOn();
            clean.ConvertLinesToPointsOff();
            clean.ConvertPolysToLinesOff();
            clean.ConvertStripsToPolysOff();
            clean.Update();
            //
            vtkPolyData cut = clean.GetOutput();
            int numOfPoints = (int)cut.GetNumberOfPoints();
            if (numOfPoints < 3) return null;
            //
            double[][] planePoints = GetPlanePoints(cut, plane);
            //
            int[] pointDegrees = new int[numOfPoints];
            HashSet<long> loopEdges = new HashSet<long>();
            vtkCellArray loopLines = vtkCellArray.New();
            vtkIdList edgePointIds;
            vtkCell cell;
            double loopArea = 0;
            int firstId;
            int secondId;
            for (int i = 0; i < cut.GetNumberOfCells(); i++)
            {
                cell = cut.GetCell(i);
                for (int j = 0; j < cell.GetNumberOfPoints() - 1; j++)
                {
                    firstId = (int)cell.GetPointId(j);
                    secondId = (int)cell.GetPointId(j + 1);
                    if (firstId == secondId || !loopEdges.Add(GetEdgeKey(firstId, secondId))) continue;
                    //
                    pointDegrees[firstId]++;
                    pointDegrees[secondId]++;
                    loopArea += 0.5 * (planePoints[firstId][0] * planePoints[secondId][1] -
                                       planePoints[secondId][0] * planePoints[firstId][1]);
                    //
                    edgePointIds = vtkIdList.New();
                    edgePointIds.InsertNextId(firstId);
                    edgePointIds.InsertNextId(secondId);
                    loopLines.InsertNextCell(edgePointIds);
                }
            }
            // A cut that is not a set of closed loops - a plane laying on a face of the surface - cannot be filled
            for (int i = 0; i < numOfPoints; i++) if (pointDegrees[i] != 0 && pointDegrees[i] != 2) return null;
            loopArea = Math.Abs(loopArea);
            if (loopArea <= 0) return null;
            //
            vtkPoints flatPoints = vtkPoints.New();
            flatPoints.SetNumberOfPoints(numOfPoints);
            for (int i = 0; i < numOfPoints; i++) flatPoints.SetPoint(i, planePoints[i][0], planePoints[i][1], 0);
            //
            vtkPolyData flatCut = vtkPolyData.New();
            flatCut.SetPoints(flatPoints);
            //
            vtkPolyData loops = vtkPolyData.New();
            loops.SetPoints(flatPoints);
            loops.SetLines(loopLines);
            // The default tolerance merges points closer than 1e-5 of the bounding box diagonal, which on a tessellated
            // surface merges neighbouring cut points and leaves the loop edges between them unrecovered
            vtkDelaunay2D delaunay = vtkDelaunay2D.New();
            delaunay.SetInput(flatCut);
            delaunay.SetSource(loops);
            delaunay.SetProjectionPlaneMode(0);
            delaunay.SetTolerance(0);
            delaunay.BoundingTriangulationOff();
            delaunay.Update();
            //
            vtkPolyData triangulated = delaunay.GetOutput();
            int numOfTriangles = (int)triangulated.GetNumberOfPolys();
            if (numOfTriangles == 0) return null;
            //
            int[][] triangles = new int[numOfTriangles][];
            for (int i = 0; i < numOfTriangles; i++)
            {
                cell = triangulated.GetCell(i);
                triangles[i] = new int[] { (int)cell.GetPointId(0), (int)cell.GetPointId(1), (int)cell.GetPointId(2) };
            }
            //
            int[] insideTheCut = GetTrianglesInsideTheCut(triangles, loopEdges);
            if (insideTheCut == null) return null;
            //
            vtkCellArray capCells = vtkCellArray.New();
            vtkIdList trianglePointIds;
            double triangleArea;
            double capArea = 0;
            for (int i = 0; i < numOfTriangles; i++)
            {
                if (insideTheCut[i] != 1) continue;
                //
                triangleArea = GetSignedArea(planePoints, triangles[i]);
                capArea += Math.Abs(triangleArea);
                //
                trianglePointIds = vtkIdList.New();
                trianglePointIds.InsertNextId(triangles[i][0]);
                // The section face of the remaining part faces away from it - opposite to the plane normal
                if (triangleArea > 0)
                {
                    trianglePointIds.InsertNextId(triangles[i][2]);
                    trianglePointIds.InsertNextId(triangles[i][1]);
                }
                else
                {
                    trianglePointIds.InsertNextId(triangles[i][1]);
                    trianglePointIds.InsertNextId(triangles[i][2]);
                }
                capCells.InsertNextCell(trianglePointIds);
            }
            // The filled area must be the area the loops enclose
            if (Math.Abs(capArea - loopArea) > 0.001 * loopArea) return null;
            //
            vtkPolyData cap = vtkPolyData.New();
            cap.SetPoints(cut.GetPoints());
            cap.SetPolys(capCells);
            return cap;
        }
        private static double[][] GetPlanePoints(vtkPolyData cut, vtkPlane plane)
        {
            double[] origin = plane.GetOrigin();
            double[] firstAxis;
            double[] secondAxis;
            GetPlaneAxes(plane, out firstAxis, out secondAxis);
            //
            int numOfPoints = (int)cut.GetNumberOfPoints();
            double[][] planePoints = new double[numOfPoints][];
            double[] point;
            double[] distance;
            for (int i = 0; i < numOfPoints; i++)
            {
                point = cut.GetPoint(i);
                distance = new double[] { point[0] - origin[0], point[1] - origin[1], point[2] - origin[2] };
                planePoints[i] = new double[]
                {
                    distance[0] * firstAxis[0] + distance[1] * firstAxis[1] + distance[2] * firstAxis[2],
                    distance[0] * secondAxis[0] + distance[1] * secondAxis[1] + distance[2] * secondAxis[2]
                };
            }
            return planePoints;
        }
        public static void GetPlaneAxes(vtkPlane plane, out double[] firstAxis, out double[] secondAxis)
        {
            double[] normal = plane.GetNormal();
            double length = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            normal = new double[] { normal[0] / length, normal[1] / length, normal[2] / length };
            // The first axis is any direction in the plane; the second one completes a right handed system with the normal
            firstAxis = Math.Abs(normal[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double projection = firstAxis[0] * normal[0] + firstAxis[1] * normal[1] + firstAxis[2] * normal[2];
            firstAxis = new double[] { firstAxis[0] - projection * normal[0], firstAxis[1] - projection * normal[1],
                                       firstAxis[2] - projection * normal[2] };
            length = Math.Sqrt(firstAxis[0] * firstAxis[0] + firstAxis[1] * firstAxis[1] + firstAxis[2] * firstAxis[2]);
            firstAxis = new double[] { firstAxis[0] / length, firstAxis[1] / length, firstAxis[2] / length };
            secondAxis = new double[] { normal[1] * firstAxis[2] - normal[2] * firstAxis[1],
                                        normal[2] * firstAxis[0] - normal[0] * firstAxis[2],
                                        normal[0] * firstAxis[1] - normal[1] * firstAxis[0] };
        }
        private static int[] GetTrianglesInsideTheCut(int[][] triangles, HashSet<long> loopEdges)
        {
            Dictionary<long, List<int>> edgeTriangles = new Dictionary<long, List<int>>();
            List<int> neighbours;
            long edgeKey;
            for (int i = 0; i < triangles.Length; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    edgeKey = GetEdgeKey(triangles[i][j], triangles[i][(j + 1) % 3]);
                    if (!edgeTriangles.TryGetValue(edgeKey, out neighbours))
                    {
                        neighbours = new List<int>();
                        edgeTriangles.Add(edgeKey, neighbours);
                    }
                    neighbours.Add(i);
                }
            }
            // Crossing a loop edge turns the inside of the cut into the outside and the other way around
            int[] insideTheCut = new int[triangles.Length];
            for (int i = 0; i < insideTheCut.Length; i++) insideTheCut[i] = -1;
            //
            Queue<int> queue = new Queue<int>();
            foreach (var entry in edgeTriangles)
            {
                if (entry.Value.Count != 1 || insideTheCut[entry.Value[0]] >= 0) continue;
                insideTheCut[entry.Value[0]] = loopEdges.Contains(entry.Key) ? 1 : 0;
                queue.Enqueue(entry.Value[0]);
            }
            int triangleId;
            int neighbourSide;
            while (queue.Count > 0)
            {
                triangleId = queue.Dequeue();
                for (int j = 0; j < 3; j++)
                {
                    edgeKey = GetEdgeKey(triangles[triangleId][j], triangles[triangleId][(j + 1) % 3]);
                    neighbourSide = loopEdges.Contains(edgeKey) ? 1 - insideTheCut[triangleId] : insideTheCut[triangleId];
                    //
                    neighbours = edgeTriangles[edgeKey];
                    for (int k = 0; k < neighbours.Count; k++)
                    {
                        if (neighbours[k] == triangleId) continue;
                        // A loop edge the triangulation did not recover leaves the sides contradicting each other
                        if (insideTheCut[neighbours[k]] >= 0)
                        {
                            if (insideTheCut[neighbours[k]] != neighbourSide) return null;
                        }
                        else
                        {
                            insideTheCut[neighbours[k]] = neighbourSide;
                            queue.Enqueue(neighbours[k]);
                        }
                    }
                }
            }
            for (int i = 0; i < insideTheCut.Length; i++) if (insideTheCut[i] < 0) return null;
            //
            return insideTheCut;
        }
        private static double GetSignedArea(double[][] planePoints, int[] triangle)
        {
            double[] first = planePoints[triangle[0]];
            double[] second = planePoints[triangle[1]];
            double[] third = planePoints[triangle[2]];
            return 0.5 * ((second[0] - first[0]) * (third[1] - first[1]) - (third[0] - first[0]) * (second[1] - first[1]));
        }
        private static long GetEdgeKey(int firstPointId, int secondPointId)
        {
            return firstPointId < secondPointId ? ((long)firstPointId << 32) + secondPointId
                                                : ((long)secondPointId << 32) + firstPointId;
        }
    }
}
