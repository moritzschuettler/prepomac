using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CaeModel
{
    [Serializable]
    public class DrapingResult
    {
        public double[,,] NodePositions;    // [row, col, 0..2] -> global x, y, z
        public double[] CellShear;          // shear angle per cell (degrees)
        public int GridRows;
        public int GridCols;
    }

    public static class KinDrape
    {
        // Dir1 and Dir2: arm directions [armIdx, 0=row, 1=col]
        // Dir1[i] = step along the arm; Dir2[i] = perpendicular step for cell vertex layout
        private static readonly int[,] Dir1 = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };
        private static readonly int[,] Dir2 = { { 0, 1 }, { -1, 0 }, { 0, -1 }, { 1, 0 } };

        // Local coordinate frame (set once per Run call)
        private static double[] _origin;
        private static double[] _axisU, _axisV, _axisN;

        // Surface triangulation in local (u, v) space
        private static double[][] _projNodes;   // [nodeIdx] -> [u, v, h]
        private static int[][] _triangles;       // [triIdx] -> [i0, i1, i2]
        private static SurfaceGrid _surfaceGrid;

        // Run the kinematic draping simulation.
        // surfaceNodeCoords: flat array [n*3] -> x0,y0,z0, x1,y1,z1, ...
        // elementNodeCounts: number of nodes per element
        // elementNodeIds:    flat connectivity array
        // plyAngleDeg:       initial draping angle (degrees, relative to local U axis)
        // cellSize:          fabric weave pitch (same units as mesh)
        // preShear:          pre-shear angle (degrees), usually 0
        public static DrapingResult Run(
            double[] surfaceNodeCoords,
            int[] elementNodeCounts,
            int[] elementNodeIds,
            double plyAngleDeg,
            double cellSize,
            double preShear = 0.0)
        {
            int nodeCount = surfaceNodeCoords.Length / 3;
            if (nodeCount < 3 || elementNodeCounts.Length == 0)
                return null;

            double d = cellSize;

            // Build local frame from average element normal
            BuildLocalFrame(surfaceNodeCoords, nodeCount, elementNodeCounts, elementNodeIds);

            // Project nodes to local (u, v, h)
            _projNodes = new double[nodeCount][];
            for (int i = 0; i < nodeCount; i++)
            {
                double dx = surfaceNodeCoords[i * 3]     - _origin[0];
                double dy = surfaceNodeCoords[i * 3 + 1] - _origin[1];
                double dz = surfaceNodeCoords[i * 3 + 2] - _origin[2];
                _projNodes[i] = new double[]
                {
                    dx * _axisU[0] + dy * _axisU[1] + dz * _axisU[2],
                    dx * _axisV[0] + dy * _axisV[1] + dz * _axisV[2],
                    dx * _axisN[0] + dy * _axisN[1] + dz * _axisN[2]
                };
            }

            // Build triangle list from element connectivity
            var triList = new List<int[]>();
            int offset = 0;
            for (int e = 0; e < elementNodeCounts.Length; e++)
            {
                int n = elementNodeCounts[e];
                int n0 = elementNodeIds[offset];
                for (int t = 1; t < n - 1; t++)
                    triList.Add(new int[] { n0, elementNodeIds[offset + t], elementNodeIds[offset + t + 1] });
                offset += n;
            }
            _triangles = triList.ToArray();

            // Build acceleration grid for surface lookup
            _surfaceGrid = new SurfaceGrid(_projNodes, _triangles);

            // Bounding box in local 2D
            double uMin = double.MaxValue, uMax = double.MinValue;
            double vMin = double.MaxValue, vMax = double.MinValue;
            for (int i = 0; i < nodeCount; i++)
            {
                if (_projNodes[i][0] < uMin) uMin = _projNodes[i][0];
                if (_projNodes[i][0] > uMax) uMax = _projNodes[i][0];
                if (_projNodes[i][1] < vMin) vMin = _projNodes[i][1];
                if (_projNodes[i][1] > vMax) vMax = _projNodes[i][1];
            }

            // Auto-compute grid parameters
            double margin = 1.5 * d;
            double plyRad = plyAngleDeg * Math.PI / 180.0;
            double cosA = Math.Abs(Math.Cos(plyRad));
            double sinA = Math.Abs(Math.Sin(plyRad));
            double uSpan = uMax - uMin + 2 * margin;
            double vSpan = vMax - vMin + 2 * margin;
            int gridRows = Math.Max((int)Math.Ceiling((cosA * uSpan + sinA * vSpan) / d) + 1, 5);
            int gridCols = Math.Max((int)Math.Ceiling((sinA * uSpan + cosA * vSpan) / d) + 1, 5);
            int orgRow = gridRows / 2;
            int orgCol = gridCols / 2;
            double uCenter = (uMin + uMax) / 2.0;
            double vCenter = (vMin + vMax) / 2.0;
            double[] org = new double[] { uCenter - d / 2.0, vCenter - d / 2.0 };

            Console.WriteLine($"[KinDrape] Surface: {nodeCount} nodes, {elementNodeCounts.Length} elements, {_triangles.Length} triangles");
            Console.WriteLine($"[KinDrape] Grid: {gridRows} x {gridCols} = {(gridRows - 1) * (gridCols - 1)} cells, cellSize={d:G4}");
            var swTotal = Stopwatch.StartNew();
            // Run the draping algorithm in local frame
            double[,,] localNode = RunDraping(d, gridRows, gridCols, org, plyAngleDeg, orgRow, orgCol, preShear);
            swTotal.Stop();
            Console.WriteLine($"[KinDrape] Total draping: {swTotal.ElapsedMilliseconds} ms");

            // Convert local (u, v, h) to global (x, y, z)
            double[,,] globalNode = new double[gridRows, gridCols, 3];
            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridCols; c++)
                {
                    double u = localNode[r, c, 0];
                    if (double.IsNaN(u)) { globalNode[r, c, 0] = double.NaN; continue; }
                    double v = localNode[r, c, 1];
                    double h = localNode[r, c, 2];
                    globalNode[r, c, 0] = _origin[0] + u * _axisU[0] + v * _axisV[0] + h * _axisN[0];
                    globalNode[r, c, 1] = _origin[1] + u * _axisU[1] + v * _axisV[1] + h * _axisN[1];
                    globalNode[r, c, 2] = _origin[2] + u * _axisU[2] + v * _axisV[2] + h * _axisN[2];
                }
            }

            // Compute CellShear
            int nCells = (gridRows - 1) * (gridCols - 1);
            double[] cellShear = new double[nCells];
            for (int i = 0; i < nCells; i++) cellShear[i] = double.NaN;

            for (int r = 0; r < gridRows - 1; r++)
            {
                for (int c = 0; c < gridCols - 1; c++)
                {
                    // Cell number: same as Python column-major: row + col*(gridRows-1)
                    int cellNo = r + c * (gridRows - 1);
                    double[][] vert = GetVert(localNode, r, c, r, c + 1, r + 1, c + 1, r + 1, c);
                    if (vert == null) continue;
                    // Use arm 0 for shear computation (quadrant doesn't matter much for magnitude)
                    double[] shears = ShearFun(vert, 0);
                    double sum = 0; int cnt = 0;
                    foreach (double s in shears) { if (!double.IsNaN(s)) { sum += s; cnt++; } }
                    if (cnt > 0) cellShear[cellNo] = Math.Abs(sum / cnt);
                }
            }

            return new DrapingResult
            {
                NodePositions = globalNode,
                CellShear = cellShear,
                GridRows = gridRows,
                GridCols = gridCols
            };
        }

        private static double[,,] RunDraping(
            double d, int gridRows, int gridCols,
            double[] org, double plyAngleDeg, int orgRow, int orgCol, double preShear)
        {
            double[,,] node = new double[gridRows, gridCols, 3];
            for (int r = 0; r < gridRows; r++)
                for (int c = 0; c < gridCols; c++)
                    node[r, c, 0] = double.NaN;

            var sw = Stopwatch.StartNew();

            // STEP 1: Place origin node and second node (along Ang+90 direction)
            double h0;
            if (!SurfaceF(org[0], org[1], out h0)) h0 = 0;
            node[orgRow, orgCol, 0] = org[0];
            node[orgRow, orgCol, 1] = org[1];
            node[orgRow, orgCol, 2] = h0;

            int[] rows4, cols4;
            GetCellVertexIndices(orgRow, orgCol, 0, out rows4, out cols4);
            MoldCircIntersec(node, rows4, cols4, d, plyAngleDeg + 90.0, 1);
            Console.WriteLine($"[KinDrape]   Step 1: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            // STEP 2: Generator (arm) cells — arms 0+2 in parallel, then arms 1+3 in parallel
            int[,] genStart = {
                { orgRow,     orgCol     },
                { orgRow + 1, orgCol + 1 },
                { orgRow,     orgCol + 1 },
                { orgRow,     orgCol     }
            };
            int[] nGen = {
                gridRows - orgRow - 1,
                gridCols - orgCol - 2,
                orgRow,
                orgCol
            };

            // Stage A: arms 0 and 2 are independent of each other
            Parallel.For(0, 2, stageArm =>
            {
                int arm = stageArm == 0 ? 0 : 2;
                int[] r4, c4;
                double cellAng = plyAngleDeg + arm * 90.0 + preShear * (1.0 + Math.Pow(-1.0, arm)) / 2.0;
                int stepRow = Dir1[arm, 0];
                int stepCol = Dir1[arm, 1];
                for (int j = 0; j < nGen[arm]; j++)
                {
                    int jRow = genStart[arm, 0] + j * stepRow;
                    int jCol = genStart[arm, 1] + j * stepCol;
                    if (jRow < 0 || jRow >= gridRows - 1 || jCol < 0 || jCol >= gridCols - 1) continue;
                    GetCellVertexIndices(jRow, jCol, arm, out r4, out c4);
                    if (!IndicesValid(r4, c4, gridRows, gridCols)) continue;
                    NRSol(node, r4, c4, ref cellAng, d, preShear, arm);
                }
            });

            // Stage B: arms 1 and 3 (both read nodes written by arm 0, independent of each other)
            Parallel.For(0, 2, stageArm =>
            {
                int arm = stageArm == 0 ? 1 : 3;
                int[] r4, c4;
                double cellAng = plyAngleDeg + arm * 90.0 + preShear * (1.0 + Math.Pow(-1.0, arm)) / 2.0;
                int stepRow = Dir1[arm, 0];
                int stepCol = Dir1[arm, 1];
                for (int j = 0; j < nGen[arm]; j++)
                {
                    int jRow = genStart[arm, 0] + j * stepRow;
                    int jCol = genStart[arm, 1] + j * stepCol;
                    if (jRow < 0 || jRow >= gridRows - 1 || jCol < 0 || jCol >= gridCols - 1) continue;
                    GetCellVertexIndices(jRow, jCol, arm, out r4, out c4);
                    if (!IndicesValid(r4, c4, gridRows, gridCols)) continue;
                    NRSol(node, r4, c4, ref cellAng, d, preShear, arm);
                }
            });
            Console.WriteLine($"[KinDrape]   Step 2: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            // STEP 3: Constrained cells — 4 quadrants run in parallel (disjoint write regions)
            int[,] conStart = {
                { orgRow + 1, orgCol + 1 },
                { orgRow,     orgCol + 1 },
                { orgRow,     orgCol     },
                { orgRow + 1, orgCol     }
            };
            int[,] nConArr = new int[4, 2];
            int[] nGenIdx = { 0, 1, 2, 1, 2, 3, 0, 3 };
            int[] nGenSub = { 1, 0, 0, 0, 0, 0, 1, 0 };
            for (int q = 0; q < 4; q++)
            {
                nConArr[q, 0] = nGen[nGenIdx[q * 2]]     - nGenSub[q * 2];
                nConArr[q, 1] = nGen[nGenIdx[q * 2 + 1]] - nGenSub[q * 2 + 1];
            }

            Parallel.For(0, 4, q =>
            {
                int stepJ = Dir1[q, 0] + Dir2[q, 0];
                int stepK = Dir1[q, 1] + Dir2[q, 1];
                int[] r4, c4;
                for (int jj = 0; jj < nConArr[q, 0]; jj++)
                {
                    int jRow = conStart[q, 0] + jj * stepJ;
                    for (int kk = 0; kk < nConArr[q, 1]; kk++)
                    {
                        int jCol = conStart[q, 1] + kk * stepK;
                        if (jRow < 0 || jRow >= gridRows - 1 || jCol < 0 || jCol >= gridCols - 1) continue;
                        GetCellVertexIndices(jRow, jCol, q, out r4, out c4);
                        if (!IndicesValid(r4, c4, gridRows, gridCols)) continue;
                        MoldCircIntersec(node, r4, c4, d, 0.0, 2);
                    }
                }
            });
            Console.WriteLine($"[KinDrape]   Step 3: {sw.ElapsedMilliseconds} ms");

            return node;
        }

        // Build the local coordinate frame from mesh data
        private static void BuildLocalFrame(
            double[] coords, int nodeCount,
            int[] elemNodeCounts, int[] elemNodeIds)
        {
            // Centroid
            double cx = 0, cy = 0, cz = 0;
            for (int i = 0; i < nodeCount; i++)
            {
                cx += coords[i * 3]; cy += coords[i * 3 + 1]; cz += coords[i * 3 + 2];
            }
            cx /= nodeCount; cy /= nodeCount; cz /= nodeCount;
            _origin = new double[] { cx, cy, cz };

            // Average element normal (using first triangle of each element)
            double nx = 0, ny = 0, nz = 0;
            int off = 0;
            for (int e = 0; e < elemNodeCounts.Length; e++)
            {
                int n = elemNodeCounts[e];
                if (n >= 3)
                {
                    int i0 = elemNodeIds[off], i1 = elemNodeIds[off + 1], i2 = elemNodeIds[off + 2];
                    double ax = coords[i1 * 3] - coords[i0 * 3];
                    double ay = coords[i1 * 3 + 1] - coords[i0 * 3 + 1];
                    double az = coords[i1 * 3 + 2] - coords[i0 * 3 + 2];
                    double bx = coords[i2 * 3] - coords[i0 * 3];
                    double by = coords[i2 * 3 + 1] - coords[i0 * 3 + 1];
                    double bz = coords[i2 * 3 + 2] - coords[i0 * 3 + 2];
                    nx += ay * bz - az * by;
                    ny += az * bx - ax * bz;
                    nz += ax * by - ay * bx;
                }
                off += n;
            }
            double nLen = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (nLen < 1e-15) { nz = 1; nLen = 1; }
            _axisN = new double[] { nx / nLen, ny / nLen, nz / nLen };

            // Build U axis (perpendicular to N)
            double[] tryU = Math.Abs(_axisN[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double dot = tryU[0] * _axisN[0] + tryU[1] * _axisN[1] + tryU[2] * _axisN[2];
            double ux = tryU[0] - dot * _axisN[0];
            double uy = tryU[1] - dot * _axisN[1];
            double uz = tryU[2] - dot * _axisN[2];
            double uLen = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            _axisU = new double[] { ux / uLen, uy / uLen, uz / uLen };

            // V = N x U
            _axisV = new double[]
            {
                _axisN[1] * _axisU[2] - _axisN[2] * _axisU[1],
                _axisN[2] * _axisU[0] - _axisN[0] * _axisU[2],
                _axisN[0] * _axisU[1] - _axisN[1] * _axisU[0]
            };
        }

        // Surface height interpolator: local (u, v) -> height h
        private static bool SurfaceF(double u, double v, out double h)
        {
            return _surfaceGrid.Evaluate(u, v, out h);
        }

        // Newton-Raphson solver: find optimal cell angle to minimize shear
        // Corresponds to Python NRSol()
        private static void NRSol(
            double[,,] node, int[] rows, int[] cols,
            ref double cellAng, double d, double preShear, int arm)
        {
            double[][] vert = ExtractVert(node, rows, cols);
            for (int iter = 0; iter < 100; iter++)
            {
                double objCurr;
                Step2Obj(ref vert, cellAng, d, preShear, arm, out objCurr);
                if (Math.Abs(objCurr) < 1e-3 || iter == 99) break;
                double objPert;
                double[][] vertPert = DeepCopyVert(vert);
                Step2Obj(ref vertPert, cellAng + 1e-8, d, preShear, arm, out objPert);
                double grad = (objPert - objCurr) / 1e-8;
                if (Math.Abs(grad) < 1e-15) break;
                cellAng -= 0.5 * objCurr / grad;
            }
            WriteVert(node, rows, cols, vert);
        }

        private static void Step2Obj(
            ref double[][] vert, double cellAng, double d, double preShear, int arm,
            out double obj)
        {
            // Place vertex 3 (based on cellAng from vertex 0)
            int[] rows4 = { 0, 1, 2, 3 };
            int[] cols4 = { 0, 1, 2, 3 };
            PlaceVertFromAngle(ref vert, 3, d, cellAng);
            PlaceVertFromIntersection(ref vert, 2, d);
            double[] shear = ShearFun(vert, arm);
            double sum = 0;
            foreach (double s in shear) sum += s;
            obj = (sum - preShear * 4.0) / 4.0;
        }

        // Place a vertex on the mold surface via circle-surface bisection
        // unknownVertIdx: 1 = along angle Ang from vert[0]
        //                 2 = intersection of two spheres (from vert[1] and vert[3])
        //                 3 = along angle Ang from vert[0] (same as 1 but writes to vert[3])
        private static void MoldCircIntersec(
            double[,,] node, int[] rows, int[] cols,
            double d, double angDeg, int unknownVertIdx)
        {
            double[][] vert = ExtractVert(node, rows, cols);
            if (unknownVertIdx == 1 || unknownVertIdx == 3)
                PlaceVertFromAngle(ref vert, unknownVertIdx, d, angDeg);
            else
                PlaceVertFromIntersection(ref vert, unknownVertIdx, d);
            WriteVert(node, rows, cols, vert);
        }

        // Place a vertex along direction angDeg (degrees) from vert[0], at distance d, on surface
        private static void PlaceVertFromAngle(ref double[][] vert, int targetIdx, double d, double angDeg)
        {
            if (vert[0] == null) return;
            double[] C = vert[0];
            double angRad = angDeg * Math.PI / 180.0;
            // Vec1 is in the local UV plane, Vec2 is the surface normal direction (h axis = [0,0,1] in local frame)
            double[] vec1 = new double[] { -Math.Cos(angRad), -Math.Sin(angRad), 0.0 };
            double[] vec2 = new double[] { 0.0, 0.0, 1.0 };
            vert[targetIdx] = BisectCircleSurface(C, d, vec1, vec2);
        }

        // Place vertex 2 as intersection of two spheres (from vert[1] and vert[3]) with surface
        private static void PlaceVertFromIntersection(ref double[][] vert, int targetIdx, double d)
        {
            if (vert[1] == null || vert[3] == null) return;
            double[] mid = new double[]
            {
                (vert[1][0] + vert[3][0]) / 2.0,
                (vert[1][1] + vert[3][1]) / 2.0,
                (vert[1][2] + vert[3][2]) / 2.0
            };
            double distMidV1 = Math.Sqrt(
                (mid[0] - vert[1][0]) * (mid[0] - vert[1][0]) +
                (mid[1] - vert[1][1]) * (mid[1] - vert[1][1]) +
                (mid[2] - vert[1][2]) * (mid[2] - vert[1][2]));

            double r2 = d * d - distMidV1 * distMidV1;
            if (r2 <= 0) return;
            double R = Math.Sqrt(r2);

            // Vec1 = direction from mid toward vert[0]
            double[] vec1 = vert[0] != null
                ? Normalize(Sub3(vert[0], mid))
                : new double[] { 1, 0, 0 };

            // CircAxis = direction from mid toward vert[1]
            double[] circAxis = Normalize(Sub3(vert[1], mid));

            // Vec2 = vec1 x circAxis (normalized)
            double[] vec2 = Normalize(Cross3(vec1, circAxis));
            if (vec2 == null) return;

            vert[targetIdx] = BisectCircleSurface(mid, R, vec1, vec2);
        }

        // Bisection to find circle-surface intersection
        // Circle: C + R*cos(theta)*vec1 + R*sin(theta)*vec2 intersected with h = F(u, v)
        private static double[] BisectCircleSurface(double[] C, double R, double[] vec1, double[] vec2)
        {
            double tol = 5e-6;
            double[] theta = { 60.0 * Math.PI / 180.0, 300.0 * Math.PI / 180.0 };

            // Evaluate function value at initial bounds
            double f0 = CircFunVal(C, R, vec1, vec2, theta[0]);
            double f1 = CircFunVal(C, R, vec1, vec2, theta[1]);

            // If no sign change, scan for valid bracket
            if (f0 * f1 > 0)
            {
                int nScan = 36;
                bool found = false;
                double bestAbs = Math.Min(Math.Abs(f0), Math.Abs(f1));
                double bestTheta = Math.Abs(f0) <= Math.Abs(f1) ? theta[0] : theta[1];
                for (int s = 0; s < nScan; s++)
                {
                    double ta = s * 2.0 * Math.PI / nScan;
                    double tb = (s + 1) * 2.0 * Math.PI / nScan;
                    double fa = CircFunVal(C, R, vec1, vec2, ta);
                    double fb = CircFunVal(C, R, vec1, vec2, tb);
                    if (Math.Abs(fa) < bestAbs) { bestAbs = Math.Abs(fa); bestTheta = ta; }
                    if (Math.Abs(fb) < bestAbs) { bestAbs = Math.Abs(fb); bestTheta = tb; }
                    if (fa * fb <= 0)
                    {
                        theta[0] = ta; theta[1] = tb;
                        f0 = fa; f1 = fb;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    // No sign-change bracket found; discard — node stays NaN, cell won't be displayed
                    return null;
                }
            }

            // Ensure theta[0] gives positive FunVal (above surface) and theta[1] negative
            if (f0 < 0) { double tmp = theta[0]; theta[0] = theta[1]; theta[1] = tmp; }

            double result_theta = double.NaN;
            for (int iter = 0; iter < 100; iter++)
            {
                double tMid = (theta[0] + theta[1]) / 2.0;
                double fMid = CircFunVal(C, R, vec1, vec2, tMid);
                if (Math.Abs(fMid) < tol)
                {
                    result_theta = tMid;
                    break;
                }
                if (fMid > 0) theta[0] = tMid;
                else theta[1] = tMid;
                result_theta = tMid;
            }

            if (double.IsNaN(result_theta)) return null;

            double[] pt3D = new double[]
            {
                C[0] + R * Math.Cos(result_theta) * vec1[0] + R * Math.Sin(result_theta) * vec2[0],
                C[1] + R * Math.Cos(result_theta) * vec1[1] + R * Math.Sin(result_theta) * vec2[1],
                C[2] + R * Math.Cos(result_theta) * vec1[2] + R * Math.Sin(result_theta) * vec2[2]
            };

            // Snap to surface; if off-mesh, discard the node so the cell won't be displayed
            double h;
            if (!SurfaceF(pt3D[0], pt3D[1], out h))
                return null;
            pt3D[2] = h;

            return pt3D;
        }

        // FunVal = h_circle - F(u_circle, v_circle)
        private static double CircFunVal(double[] C, double R, double[] v1, double[] v2, double theta)
        {
            double u = C[0] + R * Math.Cos(theta) * v1[0] + R * Math.Sin(theta) * v2[0];
            double v = C[1] + R * Math.Cos(theta) * v1[1] + R * Math.Sin(theta) * v2[1];
            double h_circ = C[2] + R * Math.Cos(theta) * v1[2] + R * Math.Sin(theta) * v2[2];
            double h_surf;
            if (!SurfaceF(u, v, out h_surf)) h_surf = h_circ; // neutral if outside mesh
            return h_circ - h_surf;
        }

        // Compute 4 shear angles at cell vertices (degrees), signed by quadrant
        private static double[] ShearFun(double[][] vert, int arm)
        {
            int[] next = { 1, 2, 3, 0 };
            int[] prev = { 3, 0, 1, 2 };
            double[] signs = { -1, 1, -1, 1 };
            double armSign = (arm % 2 == 0) ? 1.0 : -1.0; // (-1)^arm
            double[] shear = new double[4];
            for (int k = 0; k < 4; k++)
            {
                if (vert[k] == null || vert[next[k]] == null || vert[prev[k]] == null)
                {
                    shear[k] = double.NaN;
                    continue;
                }
                double[] u = Sub3(vert[next[k]], vert[k]);
                double[] v = Sub3(vert[prev[k]], vert[k]);
                double[] cr = Cross3(u, v);
                double crossNorm = (cr != null) ? Norm3(cr) : 0.0;
                double dot = Dot3(u, v);
                double shearRad = Math.Atan2(crossNorm, dot) - Math.PI / 2.0;
                shear[k] = 180.0 / Math.PI * shearRad * signs[k] * armSign;
            }
            return shear;
        }

        // Get cell vertex indices (rows4, cols4) for a cell starting at (cellRow, cellCol) for arm No
        private static void GetCellVertexIndices(int cellRow, int cellCol, int arm,
            out int[] rows4, out int[] cols4)
        {
            rows4 = new int[]
            {
                cellRow,
                cellRow + Dir2[arm, 0],
                cellRow + Dir1[arm, 0] + Dir2[arm, 0],
                cellRow + Dir1[arm, 0]
            };
            cols4 = new int[]
            {
                cellCol,
                cellCol + Dir2[arm, 1],
                cellCol + Dir1[arm, 1] + Dir2[arm, 1],
                cellCol + Dir1[arm, 1]
            };
        }

        private static bool IndicesValid(int[] rows, int[] cols, int gridRows, int gridCols)
        {
            for (int k = 0; k < 4; k++)
            {
                if (rows[k] < 0 || rows[k] >= gridRows) return false;
                if (cols[k] < 0 || cols[k] >= gridCols) return false;
            }
            return true;
        }

        private static double[][] ExtractVert(double[,,] node, int[] rows, int[] cols)
        {
            double[][] vert = new double[4][];
            for (int k = 0; k < 4; k++)
            {
                if (double.IsNaN(node[rows[k], cols[k], 0])) continue;
                vert[k] = new double[]
                {
                    node[rows[k], cols[k], 0],
                    node[rows[k], cols[k], 1],
                    node[rows[k], cols[k], 2]
                };
            }
            return vert;
        }

        private static void WriteVert(double[,,] node, int[] rows, int[] cols, double[][] vert)
        {
            for (int k = 0; k < 4; k++)
            {
                if (vert[k] == null) continue;
                node[rows[k], cols[k], 0] = vert[k][0];
                node[rows[k], cols[k], 1] = vert[k][1];
                node[rows[k], cols[k], 2] = vert[k][2];
            }
        }

        private static double[][] GetVert(double[,,] node,
            int r0, int c0, int r1, int c1, int r2, int c2, int r3, int c3)
        {
            if (double.IsNaN(node[r0, c0, 0]) || double.IsNaN(node[r1, c1, 0]) ||
                double.IsNaN(node[r2, c2, 0]) || double.IsNaN(node[r3, c3, 0]))
                return null;
            return new double[][]
            {
                new double[] { node[r0, c0, 0], node[r0, c0, 1], node[r0, c0, 2] },
                new double[] { node[r1, c1, 0], node[r1, c1, 1], node[r1, c1, 2] },
                new double[] { node[r2, c2, 0], node[r2, c2, 1], node[r2, c2, 2] },
                new double[] { node[r3, c3, 0], node[r3, c3, 1], node[r3, c3, 2] }
            };
        }

        private static double[][] DeepCopyVert(double[][] vert)
        {
            double[][] copy = new double[4][];
            for (int k = 0; k < 4; k++)
            {
                if (vert[k] == null) continue;
                copy[k] = new double[] { vert[k][0], vert[k][1], vert[k][2] };
            }
            return copy;
        }

        // Vector math helpers (3D)
        private static double[] Sub3(double[] a, double[] b)
            => new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };

        private static double Dot3(double[] a, double[] b)
            => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

        private static double[] Cross3(double[] a, double[] b)
            => new double[] {
                a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0]
            };

        private static double Norm3(double[] a)
            => Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);

        private static double[] Normalize(double[] a)
        {
            double len = Norm3(a);
            if (len < 1e-15) return null;
            return new double[] { a[0] / len, a[1] / len, a[2] / len };
        }


        // Acceleration grid for fast surface evaluation
        private class SurfaceGrid
        {
            private double[][] _nodes;
            private int[][] _triangles;
            private double _uMin, _uMax, _vMin, _vMax;
            private const int GS = 32;  // grid size (32x32 cells)
            private List<int>[][] _cells;

            public SurfaceGrid(double[][] projNodes, int[][] triangles)
            {
                _nodes = projNodes;
                _triangles = triangles;

                // Find bounds
                _uMin = double.MaxValue; _uMax = double.MinValue;
                _vMin = double.MaxValue; _vMax = double.MinValue;
                foreach (var n in projNodes)
                {
                    if (n[0] < _uMin) _uMin = n[0]; if (n[0] > _uMax) _uMax = n[0];
                    if (n[1] < _vMin) _vMin = n[1]; if (n[1] > _vMax) _vMax = n[1];
                }
                double pad = Math.Max((_uMax - _uMin), (_vMax - _vMin)) * 0.05 + 1e-10;
                _uMin -= pad; _uMax += pad; _vMin -= pad; _vMax += pad;

                // Init grid
                _cells = new List<int>[GS][];
                for (int i = 0; i < GS; i++)
                {
                    _cells[i] = new List<int>[GS];
                    for (int j = 0; j < GS; j++) _cells[i][j] = new List<int>();
                }

                // Insert triangles into grid cells (triangle AABB)
                for (int t = 0; t < triangles.Length; t++)
                {
                    int i0 = triangles[t][0], i1 = triangles[t][1], i2 = triangles[t][2];
                    double tuMin = Math.Min(Math.Min(projNodes[i0][0], projNodes[i1][0]), projNodes[i2][0]);
                    double tuMax = Math.Max(Math.Max(projNodes[i0][0], projNodes[i1][0]), projNodes[i2][0]);
                    double tvMin = Math.Min(Math.Min(projNodes[i0][1], projNodes[i1][1]), projNodes[i2][1]);
                    double tvMax = Math.Max(Math.Max(projNodes[i0][1], projNodes[i1][1]), projNodes[i2][1]);
                    int gcMin = UToCell(tuMin), gcMax = UToCell(tuMax);
                    int grMin = VToCell(tvMin), grMax = VToCell(tvMax);
                    for (int gi = grMin; gi <= grMax; gi++)
                        for (int gj = gcMin; gj <= gcMax; gj++)
                            _cells[gi][gj].Add(t);
                }
            }

            private int UToCell(double u) => Math.Max(0, Math.Min(GS - 1, (int)((u - _uMin) / (_uMax - _uMin) * GS)));
            private int VToCell(double v) => Math.Max(0, Math.Min(GS - 1, (int)((v - _vMin) / (_vMax - _vMin) * GS)));

            // Evaluate surface height at projected (u, v). Returns true if inside mesh.
            public bool Evaluate(double u, double v, out double h)
            {
                h = double.NaN;
                int gi = VToCell(v);
                int gj = UToCell(u);

                // Check current cell + neighbors (3x3 window for safety)
                double bestDistSq = double.MaxValue;
                double bestH = double.NaN;
                bool found = false;

                int iMin = Math.Max(0, gi - 1), iMax = Math.Min(GS - 1, gi + 1);
                int jMin = Math.Max(0, gj - 1), jMax = Math.Min(GS - 1, gj + 1);
                for (int ii = iMin; ii <= iMax; ii++)
                {
                    for (int jj = jMin; jj <= jMax; jj++)
                    {
                        foreach (int t in _cells[ii][jj])
                        {
                            int i0 = _triangles[t][0], i1 = _triangles[t][1], i2 = _triangles[t][2];
                            double au = _nodes[i0][0], av = _nodes[i0][1], ah = _nodes[i0][2];
                            double bu = _nodes[i1][0], bv = _nodes[i1][1], bh = _nodes[i1][2];
                            double cu = _nodes[i2][0], cv = _nodes[i2][1], ch = _nodes[i2][2];

                            double v0u = cu - au, v0v = cv - av;
                            double v1u = bu - au, v1v = bv - av;
                            double v2u = u - au,  v2v = v - av;

                            double dot00 = v0u * v0u + v0v * v0v;
                            double dot01 = v0u * v1u + v0v * v1v;
                            double dot02 = v0u * v2u + v0v * v2v;
                            double dot11 = v1u * v1u + v1v * v1v;
                            double dot12 = v1u * v2u + v1v * v2v;
                            double denom = dot00 * dot11 - dot01 * dot01;
                            if (Math.Abs(denom) < 1e-20) continue;

                            double inv = 1.0 / denom;
                            double s = (dot11 * dot02 - dot01 * dot12) * inv;
                            double r = (dot00 * dot12 - dot01 * dot02) * inv;

                            if (s >= -0.01 && r >= -0.01 && s + r <= 1.01)
                            {
                                h = ah + r * (bh - ah) + s * (ch - ah);
                                return true;
                            }

                            // Track nearest for fallback
                            double cs = Math.Max(0, Math.Min(1, s));
                            double cr2 = Math.Max(0, Math.Min(1 - cs, r));
                            double nu = au + cr2 * (bu - au) + cs * (cu - au);
                            double nv = av + cr2 * (bv - av) + cs * (cv - av);
                            double distSq = (nu - u) * (nu - u) + (nv - v) * (nv - v);
                            if (distSq < bestDistSq)
                            {
                                bestDistSq = distSq;
                                bestH = ah + cr2 * (bh - ah) + cs * (ch - ah);
                                found = true;
                            }
                        }
                    }
                }

                if (found) { h = bestH; }
                return false;
            }
        }
    }
}
