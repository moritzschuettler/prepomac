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
        private int _numValues;
        private double _deltaX;
        private double _deltaY;
        private double _deltaZ;
        private CloudPoint[] _cloudPoints;
        private BoundingBox _sourceBox;
        private BoundingBox[] _regionBoxes;     // Tag of each bounding box contains dictionary<triangleId, boundingBox>


        // Variables                                                                                                                
        public int NumValues { get { return _numValues; } }
        public CloudPoint[] CloudPoints { get { return _cloudPoints; } }


        // Constructor                                                                                                              
        public CloudInterpolator(CloudPoint[] cloudPoints)
        {
            _cloudPoints = cloudPoints;
            //
            _sourceBox = ComputeAllPointsBoundingBox(_cloudPoints);
            _sourceBox.InflateIfThinn(1E-6);
            //
            double l = _sourceBox.GetDiagonal() / 100;
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
            _regionBoxes = AssignPointsToRegions(_cloudPoints, _sourceBox, _nx, _ny, _nz);
        }
        public void InterpolateAt(double[] point, CloudInterpolatorEnum interpolator, double radius,
                                  out double[] distance, out double[] values)
        {
            if (interpolator == CloudInterpolatorEnum.ClosestPoint)
            {
                InterpolateByClosestPoint(point, out distance, out values);
            }
            else if (interpolator == CloudInterpolatorEnum.Gauss)
            {
                InterpolateByGauss(point, radius, out distance, out values);
            }
            else if (interpolator == CloudInterpolatorEnum.Shepard)
            {
                InterpolateByShepard(point, radius, out distance, out values);
            }
            else throw new NotSupportedException();
            //
            values = values.ToArray(); // copy
        }
        public void InterpolateByClosestPoint(double[] point, out double[] distance, out double[] values)
        {
            Dictionary<int, BoundingBox> regions = GetClosestRegions(point, -1);
            //
            double absX;
            double absY;
            double absZ;
            double d;
            double minD = double.MaxValue;
            CloudPoint bestPoint = new CloudPoint();
            foreach (var regionEntry in regions)
            {
                if (regionEntry.Value.IsMaxOutsideDistance2SmallerThan(point, minD * minD))
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
            values = bestPoint.Values; // copy values since multiply with magnitude
        }
        public void InterpolateByGauss(double[] point, double radius, out double[] distance, out double[] values)
        {
            Dictionary<int, BoundingBox> regions = GetClosestRegions(point, radius);
            //
            double absX;
            double absY;
            double absZ;
            double d;
            double s = 1;
            double w;
            double radius2 = radius * radius;
            Dictionary<CloudPoint, double> cloudPointWeight = new Dictionary<CloudPoint, double>();
            foreach (var regionEntry in regions)
            {
                if (regionEntry.Value.IsMaxOutsideDistance2SmallerThan(point, radius2))
                {
                    foreach (var cloudPoint in (HashSet<CloudPoint>)regionEntry.Value.Tag)
                    {
                        absX = Math.Abs(cloudPoint.Coor[0] - point[0]);
                        if (absX < radius)
                        {
                            absY = Math.Abs(cloudPoint.Coor[1] - point[1]);
                            if (absY < radius)
                            {
                                absZ = Math.Abs(cloudPoint.Coor[2] - point[2]);
                                if (absZ < radius)
                                {
                                    d = Math.Sqrt(absX * absX + absY * absY + absZ * absZ);
                                    //
                                    w = Math.Exp(Math.Pow(-s * d / radius, 2));
                                    cloudPointWeight.Add(cloudPoint, w);
                                }
                            }
                        }
                    }
                }
            }
            //
            if (cloudPointWeight.Count > 0)
            {
                double wSum = 0;
                foreach (var entry in cloudPointWeight) wSum += entry.Value;
                //
                values = new double[_numValues];
                //
                CloudPoint cp;
                foreach (var entry in cloudPointWeight)
                {
                    cp = entry.Key;
                    for (int i = 0; i < _numValues; i++)
                    {
                        values[i] += (entry.Value / wSum) * cp.Values[i];
                    }
                }
                //
                distance = new double[] { 0, 0, 0 };
            }
            else
            {
                values = new double[_numValues];
                distance = new double[] { radius, radius, radius };
            }
            //
            
        }
        public void InterpolateByShepard(double[] point, double radius, out double[] distance, out double[] values)
        {
            Dictionary<int, BoundingBox> regions = GetClosestRegions(point, radius);
            //
            double absX;
            double absY;
            double absZ;
            double d;
            double w;
            double radius2 = radius * radius;
            Dictionary<CloudPoint, double> cloudPointWeight = new Dictionary<CloudPoint, double>();
            foreach (var regionEntry in regions)
            {
                if (regionEntry.Value.IsMaxOutsideDistance2SmallerThan(point, radius2))
                {
                    foreach (var cloudPoint in (HashSet<CloudPoint>)regionEntry.Value.Tag)
                    {
                        absX = Math.Abs(cloudPoint.Coor[0] - point[0]);
                        if (absX < radius)
                        {
                            absY = Math.Abs(cloudPoint.Coor[1] - point[1]);
                            if (absY < radius)
                            {
                                absZ = Math.Abs(cloudPoint.Coor[2] - point[2]);
                                if (absZ < radius)
                                {
                                    d = Math.Sqrt(absX * absX + absY * absY + absZ * absZ);
                                    //
                                    w = 1 / Math.Pow(d, 2);
                                    cloudPointWeight.Add(cloudPoint, w);
                                }
                            }
                        }
                    }
                }
            }
            //
            if (cloudPointWeight.Count > 0)
            {
                double wSum = 0;
                foreach (var entry in cloudPointWeight) wSum += entry.Value;
                //
                values = new double[_numValues];
                //
                CloudPoint cp;
                foreach (var entry in cloudPointWeight)
                {
                    cp = entry.Key;
                    for (int i = 0; i < _numValues; i++)
                    {
                        values[i] += (entry.Value / wSum) * cp.Values[i];
                    }
                }
                //
                distance = new double[] { 0, 0, 0 };
            }
            else
            {
                values = new double[_numValues];
                distance = new double[] { radius, radius, radius };
            }
            //

        }
        private Dictionary<int, BoundingBox> GetClosestRegions(double[] point, double radius)
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
            int layer;
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
            layer = 0;
            num = bb == null ? 0 : ((HashSet<CloudPoint>)bb.Tag).Count;
            // Add next layer of regions - at least one
            while (num == 0 || layer < 1)
            {
                layer++;
                if (radius <= 0)
                {
                    mini = i - layer;
                    maxi = i + layer;
                    minj = j - layer;
                    maxj = j + layer;
                    mink = k - layer;
                    maxk = k + layer;
                }
                else
                {
                    mini = (int)Math.Floor((point[0] - radius - _sourceBox.MinX) / _deltaX);
                    maxi = (int)Math.Ceiling((point[0] + radius - _sourceBox.MinX) / _deltaX);
                    minj = (int)Math.Floor((point[1] - radius - _sourceBox.MinY) / _deltaY);
                    maxj = (int)Math.Ceiling((point[1] + radius - _sourceBox.MinY) / _deltaY);
                    mink = (int)Math.Floor((point[2] - radius - _sourceBox.MinZ) / _deltaZ);
                    maxk = (int)Math.Ceiling((point[2] + radius - _sourceBox.MinZ) / _deltaZ);
                    num = 1;
                }
                //
                if (mini < 0) mini = 0;
                if (mini >= _nx) mini = _nx - 1;
                if (maxi < 0) maxi = 0;
                if (maxi >= _nx) maxi = _nx - 1;
                //
                if (minj < 0) minj = 0;
                if (minj >= _ny) minj = _ny - 1;
                if (maxj < 0) maxj = 0;
                if (maxj >= _ny) maxj = _ny - 1;
                //
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
            return regions;
        }
        //
        private BoundingBox ComputeAllPointsBoundingBox(CloudPoint[] cloudPoints)
        {
            _numValues = -1;
            BoundingBox bb = new BoundingBox();
            bb.IncludeFirstCoor(cloudPoints[0].Coor);
            for (int i = 1; i < cloudPoints.Length; i++)
            {
                bb.IncludeCoorFast(cloudPoints[i].Coor);
                if (_numValues == -1) _numValues = cloudPoints[i].Values.Length;
                if (_numValues != cloudPoints[i].Values.Length)
                    throw new CaeException("The interpolator does not contain the same number of values at each cloud point.");
            }
            return bb;
        }
        private BoundingBox[] AssignPointsToRegions(CloudPoint[] cloudPoints, BoundingBox sourceBox, int nx, int ny, int nz)
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
                if (pointI < 0) pointI = 0;
                else if (pointI >= _nx) pointI = _nx - 1;
                if (pointJ < 0) pointJ = 0;
                else if (pointJ >= _ny) pointJ = _ny - 1;
                if (pointK < 0) pointK = 0;
                else if (pointK >= _nz) pointK = _nz - 1;
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
