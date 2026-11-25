// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace vtkControl
{
    // One animation frame of an actor: the coordinates of its points (null when they do not move), its values and
    // their extreme nodes
    public class vtkMaxActorAnimationData
    {
        // Variables
        private float[] _coordinates;
        private float[] _values;
        private vtkMaxExtreemeNode _minNode;
        private vtkMaxExtreemeNode _maxNode;
        private ResultFieldExchangeData _resultField;


        // Properties
        public float[] Coordinates { get { return _coordinates; } }
        public float[] Values { get { return _values; } }
        public vtkMaxExtreemeNode MinNode { get { return _minNode; } }
        public vtkMaxExtreemeNode MaxNode { get { return _maxNode; } }
        public ResultFieldExchangeData ResultField { get { return _resultField; } }


        // Constructors
        public vtkMaxActorAnimationData(NodesExchangeData nodes, NodesExchangeData extremeNodes)
        {
            _coordinates = GetCoordinates(nodes);
            _values = nodes?.Values;
            if (extremeNodes != null)
            {
                _minNode = new vtkMaxExtreemeNode(extremeNodes.Ids[0], extremeNodes.Coor[0], extremeNodes.Values[0]);
                _maxNode = new vtkMaxExtreemeNode(extremeNodes.Ids[1], extremeNodes.Coor[1], extremeNodes.Values[1]);
            }
            // Complete result components used by transformed actor copies
            _resultField = nodes?.ResultField;
        }


        // Methods
        private static float[] GetCoordinates(NodesExchangeData nodes)
        {
            if (nodes == null) return null;
            if (nodes.FlatCoor != null) return nodes.FlatCoor;
            if (nodes.Coor == null) return null;
            //
            float[] coordinates = new float[3 * nodes.Coor.Length];
            for (int i = 0; i < nodes.Coor.Length; i++)
            {
                coordinates[3 * i] = (float)nodes.Coor[i][0];
                coordinates[3 * i + 1] = (float)nodes.Coor[i][1];
                coordinates[3 * i + 2] = (float)nodes.Coor[i][2];
            }
            return coordinates;
        }
        public double GetMemorySizeKb()
        {
            double bytes = 0;
            if (_coordinates != null) bytes += _coordinates.Length * sizeof(float);
            if (_values != null) bytes += _values.Length * sizeof(float);
            return bytes / 1024;
        }
    }
}
