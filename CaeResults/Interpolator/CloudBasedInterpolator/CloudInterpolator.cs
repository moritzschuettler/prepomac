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
    public class CloudInterpolator
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
        private BoundingBox[] _regionBoxes;     // Tag of each bounding box contains dictionary<triangleId, boundingBox>

        // Constructor                                                                                                              
        public CloudInterpolator(CloudPoint[] cloudPoints)
        {
            _sourceBox = ComputeAllPointsBoundingBox(cloudPoints);
            _sourceBox.InflateIfThinn(1E-6);
            //
            _nx = (int)Math.Ceiling(_sourceBox.GetXSize());
            _ny = (int)Math.Ceiling(_sourceBox.GetYSize());
            _nz = (int)Math.Ceiling(_sourceBox.GetZSize());
            //
            double l = 1;
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
            _regionBoxes = AssignPointsToRegions(cloudPoints, _sourceBox, _nx, _ny, _nz);
        }
        public void InterpolateAt(double[] point, CloudInterpolatorEnum interpolator, out double[] distance, out double[] values)
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
            int index;
            BoundingBox bb;
            Dictionary<int, BoundingBox> regions = new Dictionary<int, BoundingBox>();
            int num;
            int delta;
            double d;
            double minD;
            CloudPoint bestPoint = new CloudPoint();
            //
            i = (int)Math.Floor((point[0] - _sourceBox.MinX) / _deltaX);
            j = (int)Math.Floor((point[1] - _sourceBox.MinY) / _deltaY);
            k = (int)Math.Floor((point[2] - _sourceBox.MinZ) / _deltaZ);
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
            num = bb == null ? 0 : ((HashSet<CloudPoint>)bb.Tag).Count;
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
                                if (bb != null && ((HashSet<CloudPoint>)bb.Tag).Count > 0)
                                {
                                    regions.Add(index, bb);
                                    num += ((HashSet<CloudPoint>)bb.Tag).Count;
                                }
                            }
                        }
                    }
                }
            }
            //
            double absX;
            double absY;
            double absZ;
            minD = double.MaxValue;
            foreach (var regionEntry in regions)
            {
                if (regionEntry.Value.IsMaxOutsideDistance2SmallerThan(point, minD))
                {
                    foreach (var cloudPoint in (HashSet<CloudPoint>)regionEntry.Value.Tag)
                    {
                        absX = Math.Abs(cloudPoint.Coor[0] - point[0]);
                        if (absX < minD)
                        {
                            absY = Math.Abs(cloudPoint.Coor[1] - point[1]);
                            if (absY < minD)
                            {
                                absZ = Math.Abs(cloudPoint.Coor[2] - point[2]);
                                if (absZ < minD)
                                {
                                    d = Math.Sqrt(absX * absX + absY * absY + absZ * absZ);
                                    //
                                    if (d < minD)
                                    {
                                        minD = d;
                                        bestPoint = cloudPoint;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //
            distance = new double[] { bestPoint.Coor[0] - point[0],
                                      bestPoint.Coor[1] - point[1],
                                      bestPoint.Coor[2] - point[2]};
            //
            values = bestPoint.Values;
        }
        //
        private static BoundingBox ComputeAllPointsBoundingBox(CloudPoint[] cloudPoints)
        {
            BoundingBox bb = new BoundingBox();
            bb.IncludeFirstCoor(cloudPoints[0].Coor);
            for (int i = 1; i < cloudPoints.Length; i++) bb.IncludeCoorFast(cloudPoints[i].Coor);
            return bb;
        }
        private static BoundingBox[] AssignPointsToRegions(CloudPoint[] cloudPoints, BoundingBox sourceBox, int nx, int ny, int nz)
        {
            int nxy = nx * ny;
            double deltaX = sourceBox.GetXSize() / nx;
            double deltaY = sourceBox.GetYSize() / ny;
            double deltaZ = sourceBox.GetZSize() / nz;
            //
            BoundingBox bb;
            BoundingBox[] regions = new BoundingBox[nxy * nz];
            //
            int pointI;
            int pointJ;
            int pointK;
            int regionIndex;
            // If cell box max value is on the border of the region division, the cell will be a member of both space regions
            //foreach (var cloudPoint in cloudPoints)
            for (int i = 0; i < cloudPoints.Length; i++)
            {
                pointI = (int)Math.Floor((cloudPoints[i].Coor[0] - sourceBox.MinX) / deltaX);
                pointJ = (int)Math.Floor((cloudPoints[i].Coor[1] - sourceBox.MinY) / deltaY);
                pointK = (int)Math.Floor((cloudPoints[i].Coor[2] - sourceBox.MinZ) / deltaZ);
                //
                regionIndex = pointK * nxy + pointJ * nx + pointI;
                bb = regions[regionIndex];
                if (bb == null)
                {
                    bb = new BoundingBox();
                    bb.MinX = sourceBox.MinX + pointI * deltaX;
                    bb.MaxX = bb.MinX + deltaX;
                    bb.MinY = sourceBox.MinY + pointJ * deltaY;
                    bb.MaxY = bb.MinY + deltaY;
                    bb.MinZ = sourceBox.MinZ + pointK * deltaZ;
                    bb.MaxZ = bb.MinZ + deltaZ;
                    bb.Tag = new HashSet<CloudPoint>();
                    regions[regionIndex] = bb;
                }
                ((HashSet<CloudPoint>)bb.Tag).Add(cloudPoints[i]);
            }
            //
            return regions;
        }
    }
}
