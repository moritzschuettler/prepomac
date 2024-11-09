using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Collections.Concurrent;
using Priority_Queue;

namespace CaeResults
{
    
    
    public class ResultsInterpolator
    {
        // Variables                                                                                                                
        private int _nx;
        private int _ny;
        private int _nz;
        private int _nxy;
        private double _deltaX;
        private double _deltaY;
        private double _deltaZ;
        private BoundingBox _sourceBox;
        private BoundingBox[] _cellBoxes;
        private BoundingBox[] _regionBoxes;     // Tag of each bounding box contains dictionary<triangleId, boundingBox>
        private Triangle[] _triangles;


        // Constructor                                                                                                              
        public ResultsInterpolator(PartExchangeData source)
        {
            double avgCellBoxSize;
            source = ConvertSourceToTriangularFaces(source);
            _cellBoxes = ComputeCellBoundingBoxes(source, out avgCellBoxSize);
            _triangles = TriangularCellsToTriangles(source);
            //
            _sourceBox = ComputeAllNodesBoundingBox(source);
            _sourceBox.InflateIfThinn(1E-6);
            //
            double l = avgCellBoxSize * 3;
            //
            _nx = (int)Math.Ceiling(_sourceBox.GetXSize() / l);
            _ny = (int)Math.Ceiling(_sourceBox.GetYSize() / l);
            _nz = (int)Math.Ceiling(_sourceBox.GetZSize() / l);
            //
            int currNumBoxes = _nx * _ny * _nz;
            int maxNumBoxes = 10_000_000;
            if (currNumBoxes > maxNumBoxes)
            {
                double factor = Math.Pow((double)maxNumBoxes / currNumBoxes, 0.333333);
                l /= factor;
            }
            //
            _nx = (int)Math.Ceiling(_sourceBox.GetXSize() / l);
            _ny = (int)Math.Ceiling(_sourceBox.GetYSize() / l);
            _nz = (int)Math.Ceiling(_sourceBox.GetZSize() / l);
            //
            _nxy = _nx * _ny;
            _deltaX = _sourceBox.GetXSize() / _nx;
            _deltaY = _sourceBox.GetYSize() / _ny;
            _deltaZ = _sourceBox.GetZSize() / _nz;
            //
            _regionBoxes = SplitCellBoxesToRegions(_cellBoxes, _sourceBox, _nx, _ny, _nz);
        }
        public PartExchangeData ConvertSourceToTriangularFaces(PartExchangeData source)
        {
            PartExchangeData clone = source.DeepClone();
            //
            List<int> ids = new List<int>();
            List<int[]> cellNodeIds = new List<int[]>();
            List<int> cellTypes = new List<int>();
            //
            int[] cell;
            int count = 0;
            for (int i = 0; i < source.Cells.CellNodeIds.Length; i++)
            {
                cell = source.Cells.CellNodeIds[i];
                if (cell.Length == 3)
                {
                    ids.Add(count++);
                    cellNodeIds.Add(cell);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                }
                else if (cell.Length == 4)
                {
                    ids.Add(count++);
                    ids.Add(count++);
                    cellNodeIds.Add(new int[] { cell[0], cell[1], cell[2] });
                    cellNodeIds.Add(new int[] { cell[0], cell[2], cell[3] });
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                }
                else if (cell.Length == 6)
                {
                    ids.Add(count++);
                    ids.Add(count++);
                    ids.Add(count++);
                    ids.Add(count++);
                    cellNodeIds.Add(new int[] { cell[3], cell[5], cell[0] });
                    cellNodeIds.Add(new int[] { cell[3], cell[4], cell[5] });
                    cellNodeIds.Add(new int[] { cell[3], cell[1], cell[4] });
                    cellNodeIds.Add(new int[] { cell[5], cell[4], cell[2] });
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                }
                else if (cell.Length == 8)
                {
                    ids.Add(count++);
                    ids.Add(count++);
                    ids.Add(count++);
                    ids.Add(count++);
                    ids.Add(count++);
                    ids.Add(count++);
                    cellNodeIds.Add(new int[] { cell[7], cell[0], cell[4] });
                    cellNodeIds.Add(new int[] { cell[7], cell[4], cell[6] });
                    cellNodeIds.Add(new int[] { cell[7], cell[6], cell[3] });
                    cellNodeIds.Add(new int[] { cell[5], cell[4], cell[1] });
                    cellNodeIds.Add(new int[] { cell[5], cell[6], cell[4] });
                    cellNodeIds.Add(new int[] { cell[5], cell[2], cell[6] });
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                    cellTypes.Add((int)vtkCellType.VTK_TRIANGLE);
                }
            }
            //
            clone.Cells.Ids = ids.ToArray();
            clone.Cells.CellNodeIds = cellNodeIds.ToArray();
            clone.Cells.Types = cellTypes.ToArray();
            //
            return clone;
        }
        public double GetSignedDistanceAt(double[] point, bool exact)
        {
            int i;
            int j;
            int k;
            int mini;
            int maxi;
            int minj;
            int maxj;
            int mink;
            int maxk;
            bool jBorder;
            bool kBorder;
            int iStep;
            double[] sourceCoor;
            int index;
            BoundingBox regionBox;
            BoundingBox cellBox;
            HashSet<int> regions = new HashSet<int>();
            int num;
            int delta;
            double d;
            double minD;
            Vec3D sourcePoint;
            Vec3D closestPoint;
            Vec3D bestPoint = new Vec3D();
            Triangle triangle;
            Triangle bestTriangle;
            //
            sourceCoor = point;
            sourcePoint = new Vec3D(sourceCoor);
            i = (int)Math.Floor((sourceCoor[0] - _sourceBox.MinX) / _deltaX);
            j = (int)Math.Floor((sourceCoor[1] - _sourceBox.MinY) / _deltaY);
            k = (int)Math.Floor((sourceCoor[2] - _sourceBox.MinZ) / _deltaZ);
            if (i < 0) i = 0;
            else if (i >= _nx) i = _nx - 1;
            if (j < 0) j = 0;
            else if (j >= _ny) j = _ny - 1;
            if (k < 0) k = 0;
            else if (k >= _nz) k = _nz - 1;
            index = k * _nxy + j * _nx + i;
            regionBox = _regionBoxes[index];
            if (regionBox != null) regions.Add(index);
            //
            delta = 0;
            num = regionBox == null ? 0 : ((Dictionary<int, BoundingBox>)regionBox.Tag).Count;
            // Add next layer of regions
            double minD2 = double.MaxValue;
            while (num == 0 || delta < 1)
            {
                delta++;
                mini = i - delta;
                maxi = i + delta;
                minj = j - delta;
                maxj = j + delta;
                mink = k - delta;
                maxk = k + delta;
                if (mini < 0) mini = 0;
                if (maxi >= _nx) maxi = _nx - 1;
                if (minj < 0) minj = 0;
                if (maxj >= _ny) maxj = _ny - 1;
                if (mink < 0) mink = 0;
                if (maxk >= _nz) maxk = _nz - 1;
                //
                for (int kk = mink; kk <= maxk; kk++)
                {
                    kBorder = kk == mink || kk == maxk;
                    for (int jj = minj; jj <= maxj; jj++)
                    {
                        jBorder = jj == minj || jj == maxj;
                        if (!kBorder && !jBorder) iStep = 1;
                        else iStep = maxi - mini;
                        //
                        for (int ii = mini; ii <= maxi; ii+=iStep)
                        {
                            index = kk * _nxy + jj * _nx + ii;
                            regionBox = _regionBoxes[index];
                            //
                            if (regionBox != null && regionBox.Tag is Dictionary<int, BoundingBox> cellIdCellBox &&
                                cellIdCellBox.Count > 0)
                            {
                                if (regions.Contains(index)) continue;
                                //
                                regions.Add(index);
                                num += cellIdCellBox.Count;
                                // In case the cell box is large it will prevent smaller closer cell boxes to be added to the
                                // region collection - so add all region boxes occupied by the cell box
                                foreach (var entry in cellIdCellBox)
                                {
                                    cellBox = entry.Value;
                                    regions.UnionWith((HashSet<int>)cellBox.Tag);
                                }
                                minD2 = Math.Min(minD2, regionBox.MaxOutsideDistance2(point));
                            }
                        }
                    }
                }
            }
            if (exact)
            {
                // Check a spherical region
                minD = Math.Sqrt(minD2);
                mini = (int)Math.Floor(((sourceCoor[0] - minD) - _sourceBox.MinX) / _deltaX);
                maxi = (int)Math.Ceiling(((sourceCoor[0] + minD) - _sourceBox.MinX) / _deltaX);
                minj = (int)Math.Floor(((sourceCoor[1] - minD) - _sourceBox.MinY) / _deltaY);
                maxj = (int)Math.Ceiling(((sourceCoor[1] + minD) - _sourceBox.MinY) / _deltaY);
                mink = (int)Math.Floor(((sourceCoor[2] - minD) - _sourceBox.MinZ) / _deltaZ);
                maxk = (int)Math.Ceiling(((sourceCoor[2] + minD) - _sourceBox.MinZ) / _deltaZ);
                //
                if (mini < 0) mini = 0;
                if (mini >= _nx) mini = _nx - 1;
                if (maxi < 0) maxi = 0;
                if (maxi >= _nx) maxi = _nx - 1;
                if (minj < 0) minj = 0;
                if (minj >= _ny) minj = _ny - 1;
                if (maxj < 0) maxj = 0;
                if (maxj >= _ny) maxj = _ny - 1;
                if (mink < 0) mink = 0;
                if (mink >= _nz) mink = _nz - 1;
                if (maxk < 0) maxk = 0;
                if (maxk >= _nz) maxk = _nz - 1;
                //
                for (int kk = mink; kk <= maxk; kk++)
                {
                    for (int jj = minj; jj <= maxj; jj++)
                    {
                        for (int ii = mini; ii <= maxi; ii++)
                        {
                            index = kk * _nxy + jj * _nx + ii;
                            regionBox = _regionBoxes[index];
                            //
                            if (regionBox != null && regionBox.Tag is Dictionary<int, BoundingBox> cellIdCellBox &&
                                cellIdCellBox.Count > 0)
                            {
                                if (regions.Contains(index)) continue;
                                //
                                regions.Add(index);
                            }
                        }
                    }
                }
            }
            //
            minD = double.MaxValue;
            HashSet<int> visitedTriangles = new HashSet<int>();
            ClosestPointTypeEnum closestPointType;
            Dictionary<Triangle, ClosestPointTypeEnum> bestTriangles = new Dictionary<Triangle, ClosestPointTypeEnum> ();
            List<double> bestDistances = new List<double> ();
            //
            foreach (var regionIndex in regions)
            {
                regionBox = _regionBoxes[regionIndex];
                if (regionBox.IsMaxOutsideDistance2SmallerThan(sourceCoor, minD))
                {
                    foreach (var entry in (Dictionary<int, BoundingBox>)regionBox.Tag)
                    {
                        if (visitedTriangles.Add(entry.Key))    // a single triangle might be in multiple regions
                        {
                            triangle = _triangles[entry.Key];
                            if (entry.Value.IsMaxOutsideDistance2SmallerThan(sourceCoor, minD))
                            {
                                triangle.GetClosestPointTo(sourcePoint, minD, out closestPoint, out closestPointType);
                                //
                                if (closestPoint != null)
                                {
                                    d = (closestPoint - sourcePoint).Len2;
                                    //
                                    if (Math.Abs(d - minD) < 1E-9 * _sourceBox.GetDiagonal())
                                    {
                                        bestTriangles.Add(triangle, closestPointType);
                                        bestDistances.Add(d);
                                    }
                                    else if (d < minD)
                                    {
                                        minD = d;
                                        bestTriangle = triangle;
                                        bestPoint.X = closestPoint.X;
                                        bestPoint.Y = closestPoint.Y;
                                        bestPoint.Z = closestPoint.Z;
                                        //
                                        bestTriangles.Clear();
                                        bestTriangles.Add(triangle, closestPointType);
                                        //
                                        bestDistances.Clear();
                                        bestDistances.Add(minD);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //
            Vec3D distanceVec = bestPoint - sourcePoint;
            double direction = 1;
            foreach (var entry in bestTriangles)
            {
                if (Vec3D.DotProduct(distanceVec, entry.Key.TriNorm) < 0)
                {
                    direction = -1;
                    break;
                }
            }
            //
            return distanceVec.Len * Math.Sign(direction);
        }
        public void InterpolateAt(double[] point, InterpolatorEnum interpolator, out double[] distance, out double value)
        {
            int i;
            int j;
            int k;
            int mini;
            int maxi;
            int minj;
            int maxj;
            int mink;
            int maxk;
            double[] sourceCoor;
            int index;
            BoundingBox bb;
            Dictionary<int, BoundingBox> regions = new Dictionary<int, BoundingBox>();
            int num;
            int delta;
            double d;
            double minD;
            Vec3D sourcePoint;
            Vec3D closestPoint;
            Vec3D bestPoint = new Vec3D();
            Triangle triangle;
            Triangle bestTriangle = null;
            bool closer;
            //
            sourceCoor = point;
            sourcePoint = new Vec3D(sourceCoor);
            i = (int)Math.Floor((sourceCoor[0] - _sourceBox.MinX) / _deltaX);
            j = (int)Math.Floor((sourceCoor[1] - _sourceBox.MinY) / _deltaY);
            k = (int)Math.Floor((sourceCoor[2] - _sourceBox.MinZ) / _deltaZ);
            if (i < 0) i = 0;
            else if (i >= _nx) i = _nx - 1;
            if (j < 0) j = 0;
            else if (j >= _ny) j = _ny - 1;
            if (k < 0) k = 0;
            else if (k >= _nz) k = _nz - 1;
            index = k * _nxy + j * _nx + i;
            bb = _regionBoxes[index];
            if (bb != null) regions.Add(index, bb);
            //
            delta = 0;
            num = bb == null ? 0 : ((Dictionary<int, BoundingBox>)bb.Tag).Count;
            // Add next layer of regions
            while (num == 0 || delta < 1)
            {
                delta++;
                mini = i - delta;
                maxi = i + delta;
                minj = j - delta;
                maxj = j + delta;
                mink = k - delta;
                maxk = k + delta;
                if (mini < 0) mini = 0;
                if (maxi >= _nx) maxi = _nx - 1;
                if (minj < 0) minj = 0;
                if (maxj >= _ny) maxj = _ny - 1;
                if (mink < 0) mink = 0;
                if (maxk >= _nz) maxk = _nz - 1;
                //
                for (int kk = mink; kk <= maxk; kk++)
                {
                    for (int jj = minj; jj <= maxj; jj++)
                    {
                        for (int ii = mini; ii <= maxi; ii++)
                        {
                            index = kk * _nxy + jj * _nx + ii;
                            if (!regions.ContainsKey(index))
                            {
                                bb = _regionBoxes[index];
                                //
                                if (bb != null && ((Dictionary<int, BoundingBox>)bb.Tag).Count > 0)
                                {
                                    regions.Add(index, bb);
                                    num += ((Dictionary<int, BoundingBox>)bb.Tag).Count;
                                }
                            }
                        }
                    }
                }
            }
            //
            minD = double.MaxValue;
            //
            foreach (var regionEntry in regions)
            {
                if (regionEntry.Value.IsMaxOutsideDistance2SmallerThan(sourceCoor, minD))
                {
                    foreach (var entry in (Dictionary<int, BoundingBox>)regionEntry.Value.Tag)
                    {
                        //if (entry.Value == null) continue;  // empty boxes are null
                        //
                        triangle = _triangles[entry.Key];
                        if (entry.Value.IsMaxOutsideDistance2SmallerThan(sourceCoor, minD))
                        {
                            if (interpolator == InterpolatorEnum.ClosestNode)
                                closer = triangle.GetClosestNodeTo(sourcePoint, minD, out closestPoint);
                            else if (interpolator == InterpolatorEnum.ClosestPoint)
                                closer = triangle.GetClosestPointTo(sourcePoint, minD, out closestPoint, out _);
                            else throw new NotSupportedException();
                            //
                            if (closer)
                            {
                                d = (closestPoint - sourcePoint).Len2;
                                //
                                if (d < minD)
                                {
                                    minD = d;
                                    bestTriangle = triangle;
                                    bestPoint.X = closestPoint.X;
                                    bestPoint.Y = closestPoint.Y;
                                    bestPoint.Z = closestPoint.Z;
                                }
                            }
                        }
                    }
                }
            }
            //
            distance = (bestPoint - sourcePoint).Coor;
            value = bestTriangle.InterpolateAt(bestPoint);
        }
        //
        private static BoundingBox ComputeAllNodesBoundingBox(PartExchangeData pData)
        {
            BoundingBox bb = new BoundingBox();
            bb.IncludeFirstCoor(pData.Nodes.Coor[0]);
            for (int i = 0; i < pData.Nodes.Coor.Length; i++) bb.IncludeCoorFast(pData.Nodes.Coor[i]);
            return bb;
        }
        private static BoundingBox[] ComputeCellBoundingBoxes(PartExchangeData pData, out double size)
        {
            size = 0;
            int[] cell;
            BoundingBox bb;
            BoundingBox[] cellBBoxes = new BoundingBox[pData.Cells.Ids.Length];
            //
            for (int i = 0; i < pData.Cells.CellNodeIds.Length; i++)
            {
                cell = pData.Cells.CellNodeIds[i];
                if (cell.Length != 3) throw new NotSupportedException();
                bb = new BoundingBox();
                bb.IncludeFirstCoor(pData.Nodes.Coor[cell[0]]);
                bb.IncludeCoorFast(pData.Nodes.Coor[cell[1]]);
                bb.IncludeCoorFast(pData.Nodes.Coor[cell[2]]);
                bb.Tag = new HashSet<int>();
                //
                //bb.InflateIfThinn(0.01);
                //
                cellBBoxes[i] = bb;
                //
                size += bb.GetDiagonal();
            }
            size /= cellBBoxes.Length;
            //
            return cellBBoxes;
        }
        private static BoundingBox[] SplitCellBoxesToRegions(BoundingBox[] cellBoxes, BoundingBox sourceBox, 
                                                             int nx, int ny, int nz)
        {
            int nxy = nx * ny;
            double deltaX = sourceBox.GetXSize() / nx;
            double deltaY = sourceBox.GetYSize() / ny;
            double deltaZ = sourceBox.GetZSize() / nz;
            //
            BoundingBox bb;
            BoundingBox[] regions = new BoundingBox[nxy * nz];
            //
            int cellId = 0;
            int mini;
            int maxi;
            int minj;
            int maxj;
            int mink;
            int maxk;
            int regionIndex;
            // If cell box max value is on the border of the region division, the cell will be a member of both space regions
            foreach (var cellBox in cellBoxes)
            {
                mini = (int)Math.Floor((cellBox.MinX - sourceBox.MinX) / deltaX);
                maxi = (int)Math.Floor((cellBox.MaxX - sourceBox.MinX) / deltaX);
                if (maxi == nx) maxi--;
                //
                minj = (int)Math.Floor((cellBox.MinY - sourceBox.MinY) / deltaY);
                maxj = (int)Math.Floor((cellBox.MaxY - sourceBox.MinY) / deltaY);
                if (maxj == ny) maxj--;
                //
                mink = (int)Math.Floor((cellBox.MinZ - sourceBox.MinZ) / deltaZ);
                maxk = (int)Math.Floor((cellBox.MaxZ - sourceBox.MinZ) / deltaZ);
                if (maxk == nz) maxk--;
                //
                for (int k = mink; k <= maxk; k++)
                {
                    for (int j = minj; j <= maxj; j++)
                    {
                        for (int i = mini; i <= maxi; i++)
                        {
                            regionIndex = k * nxy + j * nx + i;
                            bb = regions[regionIndex];
                            if (bb == null)
                            {
                                bb = new BoundingBox();
                                bb.MinX = sourceBox.MinX + i * deltaX;
                                bb.MaxX = bb.MinX + deltaX;
                                bb.MinY = sourceBox.MinY + j * deltaY;
                                bb.MaxY = bb.MinY + deltaY;
                                bb.MinZ = sourceBox.MinZ + k * deltaZ;
                                bb.MaxZ = bb.MinZ + deltaZ;
                                bb.Tag = new Dictionary<int, BoundingBox>();
                                regions[regionIndex] = bb;
                            }
                            ((Dictionary<int, BoundingBox>)bb.Tag).Add(cellId, cellBox);
                            ((HashSet<int>)cellBox.Tag).Add(regionIndex);
                        }
                    }
                }
                //
                cellId++;
            }
            //
            return regions;
        }
        private static Triangle[] TriangularCellsToTriangles(PartExchangeData pData)
        {
            Triangle[] triangles = new Triangle[pData.Cells.CellNodeIds.Length];
            //
            Parallel.For(0, triangles.Length, i =>
            //for (int i = 0; i < triangles.Length; i++)
            {
                int[] cell = pData.Cells.CellNodeIds[i];
                if (cell.Length != 3) throw new NotSupportedException();
                triangles[i] = new Triangle(i, pData.Nodes.Coor[cell[0]],
                                               pData.Nodes.Coor[cell[1]],
                                               pData.Nodes.Coor[cell[2]],
                                               pData.Nodes.Values[cell[0]],
                                               pData.Nodes.Values[cell[1]],
                                               pData.Nodes.Values[cell[2]]);
            }
            );
            //
            return triangles;
        }
    }
}
