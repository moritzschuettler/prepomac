// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;

namespace vtkControl
{
    class vtkMaxCoorSysSymbol : vtkMaxCoorSysGeometry
    {
        // Variables                                                                                                                
        private double[] _origin;
        private double[][] _axisDirections;
        private string[] _labels;
        //
        protected vtkTextMapper[] _labelMappers;
        protected vtkActor2D[] _labelActors;
        // The coordinate an actor owns never changes, so it is resolved once. Asking for it again
        // means another lookup in the ActiViz wrapper table, which is keyed by the native address
        protected vtkCoordinate[] _labelCoordinates;


        // Properties                                                                                                               
        public string[] Labels { get { return _labels; } }


        // Constructors                                                                                                             
        public vtkMaxCoorSysSymbol(double[] origin, double[][] axisDirections, string[] labels, double shaftLength)
        {
            _origin = new double[] { origin[0], origin[1], origin[2] };
            _axisDirections = new double[NumOfAxes][];
            for (int i = 0; i < NumOfAxes; i++)
            {
                _axisDirections[i] = new double[] { axisDirections[i][0], axisDirections[i][1], axisDirections[i][2] };
            }
            _labels = new string[NumOfAxes];
            for (int i = 0; i < NumOfAxes; i++) _labels[i] = labels[i];
            //
            SetShaftLength(shaftLength);
            InitializeLabels();
        }


        // Private methods                                                                                                          
        private void InitializeLabels()
        {
            _labelMappers = new vtkTextMapper[NumOfAxes];
            _labelActors = new vtkActor2D[NumOfAxes];
            _labelCoordinates = new vtkCoordinate[NumOfAxes];
            //
            for (int i = 0; i < NumOfAxes; i++)
            {
                // Mapper
                _labelMappers[i] = vtkTextMapper.New();
                _labelMappers[i].SetInput(_labels[i]);
                // Actor
                _labelActors[i] = vtkActor2D.New();
                _labelActors[i].SetMapper(_labelMappers[i]);
                // The label positions are computed in pixels
                _labelCoordinates[i] = _labelActors[i].GetPositionCoordinate();
                _labelCoordinates[i].SetCoordinateSystemToDisplay();
                _labelCoordinates[i].SetValue(0, 0);
                _labelActors[i].UseBoundsOff();
            }
        }
        private void InsertCone(vtkPoints points, vtkCellArray polygons, int endId)
        {
            double[] direction = GetNormalizedEndDirection(endId);
            double[] normal = new double[] { -direction[1], direction[0] };
            double[] apex = _endPositions[endId];
            double[] baseCenter = GetConeBaseCenter(endId);
            // The projection of the base circle is an ellipse that is foreshortened along the axis
            double semiAxis = _coneRadius * Math.Abs(_endDepths[endId]);
            //
            long firstPointId = points.GetNumberOfPoints();
            polygons.InsertNextCell(NumOfCircleSegments);
            for (int i = 0; i < NumOfCircleSegments; i++)
            {
                double angle = 2 * Math.PI * i / NumOfCircleSegments;
                double alongAxis = semiAxis * Math.Cos(angle);
                double alongNormal = _coneRadius * Math.Sin(angle);
                points.InsertNextPoint(baseCenter[0] + alongAxis * direction[0] + alongNormal * normal[0],
                                       baseCenter[1] + alongAxis * direction[1] + alongNormal * normal[1], 0);
                polygons.InsertCellPoint(firstPointId + i);
            }
            //
            firstPointId = points.GetNumberOfPoints();
            points.InsertNextPoint(apex[0], apex[1], 0);
            points.InsertNextPoint(baseCenter[0] + _coneRadius * normal[0], baseCenter[1] + _coneRadius * normal[1], 0);
            points.InsertNextPoint(baseCenter[0] - _coneRadius * normal[0], baseCenter[1] - _coneRadius * normal[1], 0);
            polygons.InsertNextCell(3);
            for (int i = 0; i < 3; i++) polygons.InsertCellPoint(firstPointId + i);
        }
        private void InsertDisk(vtkPoints points, vtkCellArray polygons, double[] diskCenter, double radius)
        {
            long firstPointId = points.GetNumberOfPoints();
            //
            polygons.InsertNextCell(NumOfCircleSegments);
            for (int i = 0; i < NumOfCircleSegments; i++)
            {
                double angle = 2 * Math.PI * i / NumOfCircleSegments;
                points.InsertNextPoint(diskCenter[0] + radius * Math.Cos(angle),
                                       diskCenter[1] + radius * Math.Sin(angle), 0);
                polygons.InsertCellPoint(firstPointId + i);
            }
        }
        private static void InsertColor(vtkUnsignedCharArray colors, double[] color, double opacity)
        {
            colors.InsertNextTuple4(Math.Round(255 * color[0]), Math.Round(255 * color[1]),
                                    Math.Round(255 * color[2]), Math.Round(255 * opacity));
        }


        // Public methods                                                                                                           
        public void UpdateProjection(double[] center, double centerDepth, double[] right, double[] up,
                                     double[] viewPlaneNormal)
        {
            Update(center, centerDepth, _axisDirections, right, up, viewPlaneNormal);
        }
        public void PositionLabels(int[][] labelSizes)
        {
            for (int axisId = 0; axisId < NumOfAxes; axisId++)
            {
                double[] labelPosition = GetLabelPosition(axisId, labelSizes[axisId]);
                _labelCoordinates[axisId].SetValue(labelPosition[0], labelPosition[1]);
            }
        }
        public void AppendOrigin(vtkPoints points, vtkCellArray polygons, vtkUnsignedCharArray colors)
        {
            InsertDisk(points, polygons, _center, _originRadius);
            InsertColor(colors, GetOriginColor(), 1);
        }
        public void AppendShaft(vtkPoints points, vtkCellArray lines, vtkUnsignedCharArray colors, int endId)
        {
            long firstPointId = points.GetNumberOfPoints();
            points.InsertNextPoint(_center[0], _center[1], 0);
            points.InsertNextPoint(_endPositions[endId][0], _endPositions[endId][1], 0);
            //
            lines.InsertNextCell(2);
            lines.InsertCellPoint(firstPointId);
            lines.InsertCellPoint(firstPointId + 1);
            //
            InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
        }
        public void AppendEnd(vtkPoints points, vtkCellArray polygons, vtkUnsignedCharArray colors, int endId)
        {
            if (IsPositiveEnd(endId))
            {
                InsertCone(points, polygons, endId);
                InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
                InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
            }
            else
            {
                InsertDisk(points, polygons, _endPositions[endId], _negativeEndRadius);
                InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
            }
        }
        public void AddLabelsToRenderer(vtkRenderer renderer)
        {
            if (renderer == null) return;
            //
            for (int i = 0; i < _labelActors.Length; i++)
            {
                if (_labelActors[i] != null) renderer.AddActor(_labelActors[i]);
            }
        }
        public void RemoveLabelsFromRenderer(vtkRenderer renderer)
        {
            if (renderer == null) return;
            //
            for (int i = 0; i < _labelActors.Length; i++)
            {
                if (_labelActors[i] != null) renderer.RemoveActor(_labelActors[i]);
            }
        }


        // Public getters                                                                                                           
        public double[] GetOrigin()
        {
            return _origin;
        }
        public vtkActor2D[] GetLabelActors()
        {
            return _labelActors;
        }
        public vtkTextMapper GetLabelMapper(int axisId)
        {
            return _labelMappers[axisId];
        }


        // Public setters                                                                                                           
        public void SetTextProperty(vtkTextProperty textProperty)
        {
            for (int i = 0; i < _labelMappers.Length; i++)
            {
                _labelMappers[i].SetTextProperty(textProperty);
            }
        }


        // Static methods
        public static void GetCameraBasis(vtkCamera camera, out double[] right, out double[] up,
                                          out double[] viewPlaneNormal)
        {
            viewPlaneNormal = new double[] { 0, 0, 1 };
            double[] viewUp = new double[] { 0, 1, 0 };
            if (camera != null)
            {
                viewPlaneNormal = camera.GetViewPlaneNormal();  // points from the focal point towards the camera
                viewUp = camera.GetViewUp();
            }
            GetCameraBasis(viewPlaneNormal, viewUp, out right, out up);
        }
        public static int[] GetTextSize(vtkTextMapper textMapper, vtkRenderer renderer)
        {
            int[] size = new int[2];

            if (renderer != null)
            {
                IntPtr sizeIntPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(2 * 4);
                try
                {
                    textMapper.GetSize(renderer, sizeIntPtr);
                    System.Runtime.InteropServices.Marshal.Copy(sizeIntPtr, size, 0, 2);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(sizeIntPtr);
                }
            }
            else
            {
                size[0] = size[1] = 50;
            }

            return size;
        }
    }
}
