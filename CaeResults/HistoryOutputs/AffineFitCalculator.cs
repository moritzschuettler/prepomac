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
using CaeGlobals;
using CaeMesh;

namespace CaeResults
{
    public class AffineFitPoint
    {
        public int[] NodeIds;
        public double RefX;
        public double RefY;
        public double Measure;         // area (CellCentroids) or 1 (Nodes)
        public double Perimeter;       // perimeter (CellCentroids) or 0 (Nodes)
        public double Weight;          // taper * weighting scheme, fixed for the whole evaluation
    }

    public class AffineFitResult
    {
        public double F11, F12, F21, F22;
        public double Exx, Eyy, Exy;                   // global-frame strain tensor
        public double ELL, ETT, ELT;                   // strain projected onto loading/transverse axes
        public double E1, E2, PrincipalAngleRad;        // principal strains and angle of E1 direction from X axis
        public double NuLT;
        public double ResidualRms;
        public double ResidualRmsNormalized;            // ResidualRms / (Radius * max(|E1|, |E2|)) - fit quality
        public int PointCount;
        public double ConditionNumberM;
    }

    public static class AffineFitCalculator
    {
        public static List<AffineFitPoint> BuildFitPoints(FeMesh mesh, Dictionary<int, FeNode> undeformedNodes,
                                                           ResultHistoryOutputFromAffineFit rhoaf,
                                                           out int rejectedByAreaCount, out int rejectedOutsideRadiusCount)
        {
            rejectedByAreaCount = 0;
            rejectedOutsideRadiusCount = 0;
            //
            ValidatePlanarModel(undeformedNodes);
            //
            List<AffineFitPoint> candidates = new List<AffineFitPoint>();
            //
            if (rhoaf.PointSource == AffineFitPointSourceEnum.Nodes)
            {
                foreach (var node in undeformedNodes.Values)
                {
                    candidates.Add(new AffineFitPoint()
                    {
                        NodeIds = new int[] { node.Id },
                        RefX = node.X,
                        RefY = node.Y,
                        Measure = 1,
                        Perimeter = 0
                    });
                }
            }
            else
            {
                List<AffineFitPoint> allLoops = new List<AffineFitPoint>();
                foreach (BasePart part in mesh.Parts.Values)
                {
                    VisualizationData vis = part.Visualization;
                    if (vis == null || vis.FaceEdgeIds == null || vis.FaceEdgeIds.Length == 0) continue;
                    //
                    List<int[]> freeEdges = GetFreeEdgeSegmentsFromVisualization(vis);
                    if (freeEdges.Count == 0) continue;
                    //
                    List<int[]> loops = GroupFreeEdgesIntoLoops(freeEdges);
                    //
                    List<AffineFitPoint> partLoops = new List<AffineFitPoint>();
                    foreach (int[] orderedNodeIds in loops)
                    {
                        if (orderedNodeIds.Length < 3) continue;
                        //
                        double[] xs = new double[orderedNodeIds.Length];
                        double[] ys = new double[orderedNodeIds.Length];
                        for (int i = 0; i < orderedNodeIds.Length; i++)
                        {
                            FeNode n = undeformedNodes[orderedNodeIds[i]];
                            xs[i] = n.X;
                            ys[i] = n.Y;
                        }
                        ShoelaceAreaAndCentroid(xs, ys, out double area, out double cx, out double cy, out double perimeter);
                        if (area <= 0) continue;
                        //
                        partLoops.Add(new AffineFitPoint()
                        {
                            NodeIds = orderedNodeIds,
                            RefX = cx,
                            RefY = cy,
                            Measure = area,
                            Perimeter = perimeter
                        });
                    }
                    // The outer boundary of the part is the largest loop - it is not a cell
                    if (partLoops.Count > 0)
                    {
                        AffineFitPoint outerBoundary = partLoops.OrderByDescending(p => p.Measure).First();
                        partLoops.Remove(outerBoundary);
                    }
                    allLoops.AddRange(partLoops);
                }
                // Filter out cropped/degenerate cells by comparing to a reference cell area: the area at the
                // AreaFilterCenterPercentile of the candidate area distribution (50 = median). With a bimodal
                // population of many small cells and a few large but important ones, pushing the percentile up
                // moves the reference towards the large-cell cluster so the small cells fall outside the window.
                if (allLoops.Count > 0)
                {
                    double[] areas = allLoops.Select(p => p.Measure).OrderBy(a => a).ToArray();
                    double relativeMedian = Math.Min(1, Math.Max(0, rhoaf.AreaFilterRelativeMedian / 2));
                    int medianIndex = (int)(relativeMedian * (areas.Length - 1));
                    double referenceArea = areas[medianIndex];
                    double lo = referenceArea * (1 - rhoaf.CellAreaToleranceFraction);
                    double hi = referenceArea * (1 + rhoaf.CellAreaToleranceFraction);
                    //
                    foreach (AffineFitPoint p in allLoops)
                    {
                        if (p.Measure >= lo && p.Measure <= hi) candidates.Add(p);
                        else rejectedByAreaCount++;
                    }
                }
            }
            // Apply the radial window: hard exclusion beyond the radius, cosine taper inside it
            List<AffineFitPoint> accepted = new List<AffineFitPoint>();
            double taperStart = rhoaf.TaperStartFraction * rhoaf.Radius;
            foreach (AffineFitPoint p in candidates)
            {
                double dx = p.RefX - rhoaf.CenterX;
                double dy = p.RefY - rhoaf.CenterY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > rhoaf.Radius) { rejectedOutsideRadiusCount++; continue; }
                //
                double taperWeight = GetTaperWeight(dist, rhoaf.Radius, taperStart);
                //
                double schemeWeight;
                if (rhoaf.PointSource == AffineFitPointSourceEnum.CellCentroids)
                {
                    if (rhoaf.Weighting == AffineFitWeightingEnum.Area) schemeWeight = p.Measure;
                    else if (rhoaf.Weighting == AffineFitWeightingEnum.Perimeter) schemeWeight = p.Perimeter;
                    else schemeWeight = 1;
                }
                else schemeWeight = 1;
                //
                p.Weight = taperWeight * schemeWeight;
                if (p.Weight > 0) accepted.Add(p);
            }
            return accepted;
        }
        private static double GetTaperWeight(double dist, double radius, double taperStart)
        {
            if (dist > radius) return 0;
            if (dist <= taperStart || taperStart >= radius) return 1;
            return 0.5 * (1 + Math.Cos(Math.PI * (dist - taperStart) / (radius - taperStart)));
        }
        // Area of the disc (solid + void) as seen by the taper weight, i.e. integral of GetTaperWeight over the
        // disc - needed to turn a weighted sum of element stresses into a macroscopic (homogenized) stress,
        // which must be divided by the full disc area, not just the solid area covered by elements.
        private static double GetTaperWeightedDiscArea(double radius, double taperStartFraction)
        {
            double taperStart = taperStartFraction * radius;
            if (taperStart >= radius) return Math.PI * radius * radius;
            //
            int ringCount = 400;
            double dr = radius / ringCount;
            double area = 0;
            for (int i = 0; i < ringCount; i++)
            {
                double rMid = (i + 0.5) * dr;
                area += GetTaperWeight(rMid, radius, taperStart) * 2 * Math.PI * rMid * dr;
            }
            return area;
        }
        // Macroscopic (homogenized) stress in the disc: an element-area-weighted, taper-weighted average of the
        // nodal-averaged element stress, divided by the full disc area - independent of point source/weighting,
        // since it is a plain regional average over every 2D element whose centroid falls in the taper window.
        public static void ComputeRegionAverageStress(FeMesh mesh, Dictionary<int, FeNode> undeformedNodes,
                                                       Dictionary<int, int> nodeIdsLookUp, float[] s11Values,
                                                       float[] s22Values, float[] s12Values, double centerX,
                                                       double centerY, double radius, double taperStartFraction,
                                                       out double sxx, out double syy, out double sxy)
        {
            double taperStart = taperStartFraction * radius;
            double sumSxx = 0, sumSyy = 0, sumSxy = 0;
            //
            foreach (BasePart part in mesh.Parts.Values)
            {
                if (part.Labels == null) continue;
                //
                foreach (int elementId in part.Labels)
                {
                    if (!mesh.Elements.TryGetValue(elementId, out FeElement element) || element.Dimension != 2) continue;
                    //
                    int[] elementNodeIds = element.GetVtkNodeIds();
                    int[][] sides = FeMesh.GetVisualizationEdgeCells(elementNodeIds, ElementFaceType.Face);
                    double[] xs = new double[sides.Length];
                    double[] ys = new double[sides.Length];
                    for (int i = 0; i < sides.Length; i++)
                    {
                        FeNode n = undeformedNodes[sides[i][0]];
                        xs[i] = n.X;
                        ys[i] = n.Y;
                    }
                    ShoelaceAreaAndCentroid(xs, ys, out double area, out double cx, out double cy, out _);
                    if (area <= 0) continue;
                    //
                    double dx = cx - centerX;
                    double dy = cy - centerY;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    double weight = GetTaperWeight(dist, radius, taperStart);
                    if (weight <= 0) continue;
                    //
                    double elemS11 = 0, elemS22 = 0, elemS12 = 0;
                    foreach (int nodeId in elementNodeIds)
                    {
                        int resultId = nodeIdsLookUp[nodeId];
                        elemS11 += s11Values[resultId];
                        elemS22 += s22Values[resultId];
                        elemS12 += s12Values[resultId];
                    }
                    elemS11 /= elementNodeIds.Length;
                    elemS22 /= elementNodeIds.Length;
                    elemS12 /= elementNodeIds.Length;
                    //
                    double contribution = weight * area;
                    sumSxx += contribution * elemS11;
                    sumSyy += contribution * elemS22;
                    sumSxy += contribution * elemS12;
                }
            }
            //
            double discArea = GetTaperWeightedDiscArea(radius, taperStartFraction);
            sxx = discArea > 0 ? sumSxx / discArea : double.NaN;
            syy = discArea > 0 ? sumSyy / discArea : double.NaN;
            sxy = discArea > 0 ? sumSxy / discArea : double.NaN;
        }
        //
        // Free edges (edges that bound exactly one face of the part) taken from VisualizationData.GetFreeEdgeIds().
        // An edge id looked up in EdgeCellIdsByEdge gives the (possibly many) mesh-level segments that make up
        // that edge, and each segment in EdgeCells gives its global node ids. The loops are then grouped by
        // walking segment-level node adjacency ourselves instead of via GetFreeEdgeLoops/GetVertexIdEdgeIds,
        // which only connects edges through the reduced vertex node set and returns unconnected single-edge
        // groups when that set is not populated for a part.
        private static List<int[]> GetFreeEdgeSegmentsFromVisualization(VisualizationData vis)
        {
            List<int[]> segments = new List<int[]>();
            foreach (int edgeId in vis.GetFreeEdgeIds())
            {
                foreach (int edgeCellId in vis.EdgeCellIdsByEdge[edgeId])
                {
                    int[] cell = vis.EdgeCells[edgeCellId];
                    segments.Add(new int[] { cell[0], cell[1] });
                }
            }
            return segments;
        }
        // Guards against running the free-edge cell detection on a model that has been thickened/extruded into
        // 3D, where each pore boundary picks up extra free edges from the front/back faces and the thin side
        // walls between them, which breaks the closed-loop walk below.
        private static void ValidatePlanarModel(Dictionary<int, FeNode> undeformedNodes)
        {
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            double minZ = double.MaxValue, maxZ = double.MinValue;
            foreach (FeNode node in undeformedNodes.Values)
            {
                if (node.X < minX) minX = node.X;
                if (node.X > maxX) maxX = node.X;
                if (node.Y < minY) minY = node.Y;
                if (node.Y > maxY) maxY = node.Y;
                if (node.Z < minZ) minZ = node.Z;
                if (node.Z > maxZ) maxZ = node.Z;
            }
            double inPlaneExtent = Math.Max(maxX - minX, maxY - minY);
            double outOfPlaneExtent = maxZ - minZ;
            //
            if (inPlaneExtent > 0 && outOfPlaneExtent > 1e-4 * inPlaneExtent)
                throw new CaeException("The model is not planar: its out-of-plane (Z) extent is " +
                    outOfPlaneExtent.ToString("G4") + ", which is not negligible compared to its in-plane extent " +
                    "of " + inPlaneExtent.ToString("G4") + ". The affine fit assumes a 2D/shell model lying in a " +
                    "single Z plane - a model thickened/extruded into 3D will not be handled correctly.");
        }
        private static List<int[]> GroupFreeEdgesIntoLoops(List<int[]> freeEdges)
        {
            Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();
            foreach (int[] side in freeEdges)
            {
                AddAdjacency(adjacency, side[0], side[1]);
                AddAdjacency(adjacency, side[1], side[0]);
            }
            //
            List<int[]> loops = new List<int[]>();
            HashSet<int> visited = new HashSet<int>();
            foreach (int start in adjacency.Keys)
            {
                if (visited.Contains(start)) continue;
                //
                List<int> ordered = new List<int>();
                int previous = -1;
                int current = start;
                int maxSteps = adjacency.Count + 1;
                bool closed = false;
                do
                {
                    ordered.Add(current);
                    visited.Add(current);
                    List<int> neighbours = adjacency[current];
                    if (neighbours.Count != 2) break;
                    int next = neighbours[0] == previous ? neighbours[1] : neighbours[0];
                    previous = current;
                    current = next;
                    maxSteps--;
                    if (current == start) { closed = true; break; }
                }
                while (maxSteps > 0);
                //
                if (closed && ordered.Count >= 3) loops.Add(ordered.ToArray());
            }
            return loops;
        }
        private static void AddAdjacency(Dictionary<int, List<int>> adjacency, int from, int to)
        {
            if (!adjacency.TryGetValue(from, out List<int> neighbours))
            {
                neighbours = new List<int>();
                adjacency.Add(from, neighbours);
            }
            neighbours.Add(to);
        }
        public static void ShoelaceAreaAndCentroid(double[] xs, double[] ys,
                                                    out double area, out double cx, out double cy, out double perimeter)
        {
            int n = xs.Length;
            double refX = xs[0];
            double refY = ys[0];
            double signedArea2 = 0;
            double cxAccumulator = 0;
            double cyAccumulator = 0;
            double perimeterAccumulator = 0;
            //
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double x0 = xs[i] - refX;
                double y0 = ys[i] - refY;
                double x1 = xs[j] - refX;
                double y1 = ys[j] - refY;
                double cross = x0 * y1 - x1 * y0;
                //
                signedArea2 += cross;
                cxAccumulator += (x0 + x1) * cross;
                cyAccumulator += (y0 + y1) * cross;
                perimeterAccumulator += Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            }
            area = Math.Abs(signedArea2) * 0.5;
            if (Math.Abs(signedArea2) > 1e-30)
            {
                cx = refX + cxAccumulator / (3.0 * signedArea2);
                cy = refY + cyAccumulator / (3.0 * signedArea2);
            }
            else
            {
                cx = refX;
                cy = refY;
            }
            perimeter = perimeterAccumulator;
        }
        //
        public static AffineFitResult Evaluate(List<AffineFitPoint> points, Dictionary<int, int> nodeIdsLookUp,
                                                float[] u1Values, float[] u2Values, Dictionary<int, FeNode> undeformedNodes,
                                                AffineFitStrainMeasureEnum strainMeasure,
                                                double loadingDirectionX, double loadingDirectionY, double radius)
        {
            int n = points.Count;
            double[] refX = new double[n];
            double[] refY = new double[n];
            double[] defX = new double[n];
            double[] defY = new double[n];
            double[] weight = new double[n];
            //
            for (int i = 0; i < n; i++)
            {
                AffineFitPoint p = points[i];
                weight[i] = p.Weight;
                refX[i] = p.RefX;
                refY[i] = p.RefY;
                //
                if (p.NodeIds.Length == 1)
                {
                    int resultId = nodeIdsLookUp[p.NodeIds[0]];
                    defX[i] = p.RefX + u1Values[resultId];
                    defY[i] = p.RefY + u2Values[resultId];
                }
                else
                {
                    double[] dxs = new double[p.NodeIds.Length];
                    double[] dys = new double[p.NodeIds.Length];
                    for (int j = 0; j < p.NodeIds.Length; j++)
                    {
                        FeNode node = undeformedNodes[p.NodeIds[j]];
                        int resultId = nodeIdsLookUp[p.NodeIds[j]];
                        dxs[j] = node.X + u1Values[resultId];
                        dys[j] = node.Y + u2Values[resultId];
                    }
                    ShoelaceAreaAndCentroid(dxs, dys, out _, out double cx, out double cy, out _);
                    defX[i] = cx;
                    defY[i] = cy;
                }
            }
            //
            double wSum = weight.Sum();
            double refXBar = 0, refYBar = 0, defXBar = 0, defYBar = 0;
            for (int i = 0; i < n; i++)
            {
                refXBar += weight[i] * refX[i];
                refYBar += weight[i] * refY[i];
                defXBar += weight[i] * defX[i];
                defYBar += weight[i] * defY[i];
            }
            refXBar /= wSum; refYBar /= wSum; defXBar /= wSum; defYBar /= wSum;
            //
            double mxx = 0, mxy = 0, myy = 0;
            double a11 = 0, a12 = 0, a21 = 0, a22 = 0;
            for (int i = 0; i < n; i++)
            {
                double rx = refX[i] - refXBar;
                double ry = refY[i] - refYBar;
                double dx = defX[i] - defXBar;
                double dy = defY[i] - defYBar;
                double w = weight[i];
                //
                mxx += w * rx * rx;
                mxy += w * rx * ry;
                myy += w * ry * ry;
                //
                a11 += w * dx * rx;
                a12 += w * dx * ry;
                a21 += w * dy * rx;
                a22 += w * dy * ry;
            }
            //
            double detM = mxx * myy - mxy * mxy;
            double minvXX = myy / detM;
            double minvXY = -mxy / detM;
            double minvYY = mxx / detM;
            //
            AffineFitResult result = new AffineFitResult();
            result.PointCount = n;
            //
            double mTrace = mxx + myy;
            double mDisc = Math.Sqrt(Math.Max(0, (mxx - myy) * (mxx - myy) / 4.0 + mxy * mxy));
            double mLambda1 = mTrace / 2.0 + mDisc;
            double mLambda2 = mTrace / 2.0 - mDisc;
            result.ConditionNumberM = mLambda2 > 1e-300 ? mLambda1 / mLambda2 : double.PositiveInfinity;
            //
            result.F11 = a11 * minvXX + a12 * minvXY;
            result.F12 = a11 * minvXY + a12 * minvYY;
            result.F21 = a21 * minvXX + a22 * minvXY;
            result.F22 = a21 * minvXY + a22 * minvYY;
            //
            double bX = defXBar - (result.F11 * refXBar + result.F12 * refYBar);
            double bY = defYBar - (result.F21 * refXBar + result.F22 * refYBar);
            //
            double residualSumSquares = 0;
            for (int i = 0; i < n; i++)
            {
                double predX = bX + result.F11 * refX[i] + result.F12 * refY[i];
                double predY = bY + result.F21 * refX[i] + result.F22 * refY[i];
                double rx = defX[i] - predX;
                double ry = defY[i] - predY;
                residualSumSquares += weight[i] * (rx * rx + ry * ry);
            }
            result.ResidualRms = Math.Sqrt(residualSumSquares / wSum);
            //
            double exx, eyy, exy;
            if (strainMeasure == AffineFitStrainMeasureEnum.Linear)
            {
                exx = result.F11 - 1;
                eyy = result.F22 - 1;
                exy = 0.5 * (result.F12 + result.F21);
            }
            else
            {
                // Right Cauchy-Green tensor C = F^T F, eigen-decomposed to obtain the stretch tensor U
                double cxx = result.F11 * result.F11 + result.F21 * result.F21;
                double cxy = result.F11 * result.F12 + result.F21 * result.F22;
                double cyy = result.F12 * result.F12 + result.F22 * result.F22;
                //
                double cTrace = cxx + cyy;
                double cDisc = Math.Sqrt(Math.Max(0, (cxx - cyy) * (cxx - cyy) / 4.0 + cxy * cxy));
                double cLambda1 = cTrace / 2.0 + cDisc;
                double cLambda2 = cTrace / 2.0 - cDisc;
                //
                double cAngle = 0.5 * Math.Atan2(2 * cxy, cxx - cyy);
                double v1x = Math.Cos(cAngle), v1y = Math.Sin(cAngle);
                double v2x = -v1y, v2y = v1x;
                //
                double stretch1 = Math.Sqrt(Math.Max(0, cLambda1));
                double stretch2 = Math.Sqrt(Math.Max(0, cLambda2));
                //
                double strainEig1, strainEig2;
                if (strainMeasure == AffineFitStrainMeasureEnum.Biot)
                {
                    strainEig1 = stretch1 - 1;
                    strainEig2 = stretch2 - 1;
                }
                else
                {
                    strainEig1 = stretch1 > 1e-300 ? Math.Log(stretch1) : 0;
                    strainEig2 = stretch2 > 1e-300 ? Math.Log(stretch2) : 0;
                }
                //
                exx = strainEig1 * v1x * v1x + strainEig2 * v2x * v2x;
                eyy = strainEig1 * v1y * v1y + strainEig2 * v2y * v2y;
                exy = strainEig1 * v1x * v1y + strainEig2 * v2x * v2y;
            }
            //
            result.Exx = exx;
            result.Eyy = eyy;
            result.Exy = exy;
            //
            double eLNorm = Math.Sqrt(loadingDirectionX * loadingDirectionX + loadingDirectionY * loadingDirectionY);
            double eLx = loadingDirectionX / eLNorm;
            double eLy = loadingDirectionY / eLNorm;
            double eTx = -eLy;
            double eTy = eLx;
            //
            result.ELL = eLx * eLx * exx + 2 * eLx * eLy * exy + eLy * eLy * eyy;
            result.ETT = eTx * eTx * exx + 2 * eTx * eTy * exy + eTy * eTy * eyy;
            result.ELT = eLx * eTx * exx + (eLx * eTy + eLy * eTx) * exy + eLy * eTy * eyy;
            result.NuLT = result.ELL != 0 ? -result.ETT / result.ELL : double.NaN;
            //
            double eTrace = exx + eyy;
            double eDisc = Math.Sqrt(Math.Max(0, (exx - eyy) * (exx - eyy) / 4.0 + exy * exy));
            result.E1 = eTrace / 2.0 + eDisc;
            result.E2 = eTrace / 2.0 - eDisc;
            result.PrincipalAngleRad = 0.5 * Math.Atan2(2 * exy, exx - eyy);
            //
            // Fraction of the disc's displacement field that the affine fit does not explain - the fit is only
            // trustworthy while this stays small (as a guideline, well under 1).
            double strainMagnitude = Math.Max(Math.Abs(result.E1), Math.Abs(result.E2));
            result.ResidualRmsNormalized = strainMagnitude > 1e-300 ? result.ResidualRms / (radius * strainMagnitude)
                                                                     : double.NaN;
            //
            return result;
        }
    }
}
