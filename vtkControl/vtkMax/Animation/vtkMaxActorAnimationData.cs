// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.InteropServices;
using Kitware.VTK;
using CaeGlobals;

namespace vtkControl
{
    public class vtkMaxActorAnimationData
    {
        // Variables                                                                                                                
        private vtkPoints _points;
        private vtkDataArray _pointNormals;
        private vtkPoints _modelEdgesPoints;
        private vtkFloatArray _values;
        private vtkMaxExtreemeNode _minNode;
        private vtkMaxExtreemeNode _maxNode;
        private vtkPoints _locatorPoints;
        private vtkFloatArray _locatorValues;
        private ResultFieldExchangeData _resultField;
        private ResultFieldExchangeData _locatorResultField;


        // Properties                                                                                                               
        public vtkPoints Points { get { return _points; } set { _points = value; } }
        public vtkDataArray PointNormals { get { return _pointNormals; } set { _pointNormals = value; } }
        public vtkPoints ModelEdgesPoints { get { return _modelEdgesPoints; } set { _modelEdgesPoints = value; } }
        public vtkFloatArray Values { get { return _values; } set { _values = value; } }
        public vtkMaxExtreemeNode MinNode { get { return _minNode; } set { _minNode = value; } }
        public vtkMaxExtreemeNode MaxNode { get { return _maxNode; } set { _maxNode = value; } }
        public vtkPoints LocatorPoints { get { return _locatorPoints; } set { _locatorPoints = value; } }
        public vtkFloatArray LocatorValues { get { return _locatorValues; } set { _locatorValues = value; } }
        public ResultFieldExchangeData ResultField { get { return _resultField; } }
        public ResultFieldExchangeData LocatorResultField { get { return _locatorResultField; } }

        // Constructors                                                                                                             
        public vtkMaxActorAnimationData(double[][] nodes, double[][] modelEdgesNodes, float[] values,
                                        NodesExchangeData extremeNodes, double[][] locatorNodes, float[] locatorValues,
                                        ResultFieldExchangeData resultField,
                                        ResultFieldExchangeData locatorResultField)
        {
            // Nodes
            _points = CreatePoints(nodes);
            // Normals
            _pointNormals = null;
            // Model edges nodes
            _modelEdgesPoints = CreatePoints(modelEdgesNodes);
            // Values
            _values = CreateValues(values);
            // Extreme nodes
            if (extremeNodes == null)
            {
                _minNode = null;
                _maxNode = null;
            }
            else
            {
                _minNode = new vtkMaxExtreemeNode(extremeNodes.Ids[0], extremeNodes.Coor[0], extremeNodes.Values[0]);
                _maxNode = new vtkMaxExtreemeNode(extremeNodes.Ids[1], extremeNodes.Coor[1], extremeNodes.Values[1]);
            }
            // Locator nodes
            _locatorPoints = CreatePoints(locatorNodes);
            // Locator values
            _locatorValues = CreateValues(locatorValues);
            // Complete result components used by transformed actor copies
            _resultField = resultField;
            _locatorResultField = locatorResultField;
        }

        private static vtkPoints CreatePoints(double[][] nodes)
        {
            if (nodes == null) return null;

            float[] coordinates = new float[3 * nodes.Length];
            int count = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                coordinates[count++] = (float)nodes[i][0];
                coordinates[count++] = (float)nodes[i][1];
                coordinates[count++] = (float)nodes[i][2];
            }

            vtkFloatArray vtkCoordinates = vtkFloatArray.New();
            vtkCoordinates.SetNumberOfComponents(3);
            IntPtr pointer = vtkCoordinates.WriteVoidPointer(0, coordinates.Length);
            Marshal.Copy(coordinates, 0, pointer, coordinates.Length);
            vtkCoordinates.DataChanged();

            vtkPoints points = vtkPoints.New();
            points.SetData(vtkCoordinates);
            return points;
        }

        private static vtkFloatArray CreateValues(float[] values)
        {
            if (values == null) return null;

            vtkFloatArray vtkValues = vtkFloatArray.New();
            vtkValues.SetName(Globals.ScalarArrayName);
            vtkValues.SetNumberOfComponents(1);
            IntPtr pointer = vtkValues.WriteVoidPointer(0, values.Length);
            Marshal.Copy(values, 0, pointer, values.Length);
            vtkValues.DataChanged();
            return vtkValues;
        }

        public double GetActualMemorySizeKb()
        {
            double memorySize = 0;
            if (_points != null && _points.GetData() != null)
                memorySize += _points.GetData().GetActualMemorySize();
            if (_pointNormals != null) memorySize += _pointNormals.GetActualMemorySize();
            if (_modelEdgesPoints != null && _modelEdgesPoints.GetData() != null)
                memorySize += _modelEdgesPoints.GetData().GetActualMemorySize();
            if (_values != null) memorySize += _values.GetActualMemorySize();
            if (_locatorPoints != null && _locatorPoints.GetData() != null)
                memorySize += _locatorPoints.GetData().GetActualMemorySize();
            if (_locatorValues != null) memorySize += _locatorValues.GetActualMemorySize();
            return memorySize;
        }
    }

    class vtkMaxAnimationArrowAnchor
    {
        private vtkPointSet _pointSet;
        private long[] _pointIds;
        private double[] _weights;
        private double[] _offset;

        public vtkMaxAnimationArrowAnchor(vtkPointSet pointSet, long[] pointIds, double[] weights, double[] offset)
        {
            _pointSet = pointSet;
            _pointIds = pointIds;
            _weights = weights;
            _offset = offset;
        }

        public double[] GetAnchorPoint()
        {
            double[] anchorPoint = new double[] { _offset[0], _offset[1], _offset[2] };
            for (int i = 0; i < _pointIds.Length; i++)
            {
                double[] point = _pointSet.GetPoint(_pointIds[i]);
                anchorPoint[0] += _weights[i] * point[0];
                anchorPoint[1] += _weights[i] * point[1];
                anchorPoint[2] += _weights[i] * point[2];
            }
            return anchorPoint;
        }
    }
}
