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
using System.Runtime.Serialization;

namespace CaeMesh
{
    /// <summary>
    /// The finished symbol of a splitting tool, in the coordinates of the parts themselves. It is made when
    /// the split is applied, while the picks still name the geometry they were made on.
    /// </summary>
    [Serializable]
    public class SplitToolSymbol : ISerializable
    {
        // Variables
        private List<double[][]> _surfaceNodeCoor;          //ISerializable
        private List<int[][]> _surfaceCells;                //ISerializable
        private List<double[][]> _borderNodeCoor;           //ISerializable
        private List<int[][]> _borderCells;                 //ISerializable
        private List<double[]> _points;                     //ISerializable


        // Properties
        public List<double[][]> SurfaceNodeCoor { get { return _surfaceNodeCoor; } }
        public List<int[][]> SurfaceCells { get { return _surfaceCells; } }
        public List<double[][]> BorderNodeCoor { get { return _borderNodeCoor; } }
        public List<int[][]> BorderCells { get { return _borderCells; } }
        public List<double[]> Points { get { return _points; } }
        public bool IsEmpty
        {
            get { return _surfaceNodeCoor.Count == 0 && _borderNodeCoor.Count == 0 && _points.Count == 0; }
        }


        // Constructors
        public SplitToolSymbol()
        {
            _surfaceNodeCoor = new List<double[][]>();
            _surfaceCells = new List<int[][]>();
            _borderNodeCoor = new List<double[][]>();
            _borderCells = new List<int[][]>();
            _points = new List<double[]>();
        }
        public SplitToolSymbol(SerializationInfo info, StreamingContext context)
            : this()
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_surfaceNodeCoor":
                        _surfaceNodeCoor = (List<double[][]>)entry.Value; break;
                    case "_surfaceCells":
                        _surfaceCells = (List<int[][]>)entry.Value; break;
                    case "_borderNodeCoor":
                        _borderNodeCoor = (List<double[][]>)entry.Value; break;
                    case "_borderCells":
                        _borderCells = (List<int[][]>)entry.Value; break;
                    case "_points":
                        _points = (List<double[]>)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        public void AddSurface(double[][] nodeCoor, int[][] cells)
        {
            _surfaceNodeCoor.Add(Copy(nodeCoor));
            _surfaceCells.Add(Copy(cells));
        }
        public void AddBorder(double[][] nodeCoor, int[][] cells)
        {
            _borderNodeCoor.Add(Copy(nodeCoor));
            _borderCells.Add(Copy(cells));
        }
        public void AddPoints(double[][] points)
        {
            foreach (double[] point in points) _points.Add(point.ToArray());
        }
        private static T[][] Copy<T>(T[][] values)
        {
            return values.Select(value => value.ToArray()).ToArray();
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_surfaceNodeCoor", _surfaceNodeCoor, typeof(List<double[][]>));
            info.AddValue("_surfaceCells", _surfaceCells, typeof(List<int[][]>));
            info.AddValue("_borderNodeCoor", _borderNodeCoor, typeof(List<double[][]>));
            info.AddValue("_borderCells", _borderCells, typeof(List<int[][]>));
            info.AddValue("_points", _points, typeof(List<double[]>));
        }
    }
}
