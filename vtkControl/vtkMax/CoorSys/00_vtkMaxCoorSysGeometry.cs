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
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;

namespace vtkControl
{
    class vtkMaxCoorSysGeometry
    {
        // Variables                                                                                                                
        public const int NumOfAxes = 3;
        public const int NumOfEnds = 2 * NumOfAxes;
        public const double DefaultShaftLength = 45;
        // All the proportions are normalized on the widget, whose shaft length is the default one and whose
        // text property is created with the default font size. The default symbol size of a model coordinate
        // system maps onto the same shaft length, so that both are drawn identically at the default settings
        public const double DefaultSymbolSize = 50;
        public const double DefaultLabelFontSize = 16;
        //
        private const int NumOfCircleSegments = 24;
        private const double NegativeShaftLengthRatio = 2.0 / 3.0;
        private const double ConeHeightRatio = 14.0 / DefaultShaftLength;
        private const double ConeRadiusRatio = 3.5 / DefaultShaftLength;
        private const double ConeTargetRatio = 0.75;
        private const double OriginRadiusRatio = 3.5 / DefaultShaftLength;
        private const double LabelGapRatio = 6.0 / DefaultShaftLength;
        private const double LabelFontSizeRatio = DefaultLabelFontSize / DefaultShaftLength;
        private const double NegativeOpacity = 0.5;
        private const double DimFactor = 0.35;
        //
        private static readonly double[] DimColor = new double[] { 0.35, 0.35, 0.35 };
        private static readonly double[][] AxisColors =
            new double[][] { new double[] { 1, 0, 0 },
                             new double[] { 0, 0.9, 0 },
                             new double[] { 0, 0, 1 } };
        private static readonly double[][] GlobalAxisDirections =
            new double[][] { new double[] { 1, 0, 0 },
                             new double[] { 0, 1, 0 },
                             new double[] { 0, 0, 1 } };
        //
        protected double _shaftLength;
        protected double _negativeShaftLength;
        protected double _coneHeight;
        protected double _coneRadius;
        protected double _originRadius;
        protected double _negativeEndRadius;
        protected double _labelGap;
        protected bool _negativeAxesVisible;
        protected double[] _overrideColor;
        //
        protected double[] _center;
        protected double _centerDepth;
        protected double[][] _endDirections;
        protected double[][] _endPositions;
        protected double[] _endDepths;


        // Properties                                                                                                               
        public double ShaftLength { get { return _shaftLength; } }
        public double ConeRadius { get { return _coneRadius; } }
        public double LabelGap { get { return _labelGap; } }
        public double CenterDepth { get { return _centerDepth; } }
        public bool NegativeAxesVisible { get { return _negativeAxesVisible; } }
        public static double[][] GlobalAxes { get { return GlobalAxisDirections; } }


        // Constructors                                                                                                             
        public vtkMaxCoorSysGeometry()
        {
            _center = new double[2];
            _centerDepth = 0;
            _negativeAxesVisible = false;
            //
            _endDirections = new double[NumOfEnds][];
            _endPositions = new double[NumOfEnds][];
            _endDepths = new double[NumOfEnds];
            for (int i = 0; i < NumOfEnds; i++)
            {
                _endDirections[i] = new double[2];
                _endPositions[i] = new double[2];
            }
            //
            SetShaftLength(DefaultShaftLength);
        }


        // Private methods                                                                                                          
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
        public void Update(double[] center, double centerDepth, double[][] axisDirections,
                           double[] right, double[] up, double[] viewPlaneNormal)
        {
            _center[0] = center[0];
            _center[1] = center[1];
            _centerDepth = centerDepth;
            //
            for (int axisId = 0; axisId < NumOfAxes; axisId++)
            {
                double[] axisDirection = axisDirections[axisId];
                double alongRight = DotProduct(axisDirection, right);
                double alongUp = DotProduct(axisDirection, up);
                double alongNormal = DotProduct(axisDirection, viewPlaneNormal);
                //
                for (int signId = 0; signId < 2; signId++)
                {
                    int endId = GetEndId(axisId, signId);
                    double sign = signId == 0 ? 1 : -1;
                    double shaftLength = IsPositiveEnd(endId) ? _shaftLength : _negativeShaftLength;
                    //
                    _endDirections[endId][0] = sign * alongRight;
                    _endDirections[endId][1] = sign * alongUp;
                    _endDepths[endId] = sign * alongNormal;
                    //
                    _endPositions[endId][0] = _center[0] + shaftLength * _endDirections[endId][0];
                    _endPositions[endId][1] = _center[1] + shaftLength * _endDirections[endId][1];
                }
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
        public bool IsEndVisible(int endId)
        {
            return IsPositiveEnd(endId) || _negativeAxesVisible;
        }


        // Public getters                                                                                                           
        public double[] GetCenter()
        {
            return new double[] { _center[0], _center[1] };
        }
        public double[] GetEndPosition(int endId)
        {
            return _endPositions[endId];
        }
        public double GetEndDepth(int endId)
        {
            return _endDepths[endId];
        }
        public double GetEndProjectedLength(int endId)
        {
            double dx = _endPositions[endId][0] - _center[0];
            double dy = _endPositions[endId][1] - _center[1];
            //
            return Math.Sqrt(dx * dx + dy * dy);
        }
        public double GetEndOpacity(int endId)
        {
            return IsPositiveEnd(endId) ? 1 : NegativeOpacity;
        }
        public double[] GetEndColor(int endId)
        {
            double[] color = _overrideColor == null ? AxisColors[endId / 2] : _overrideColor;
            // Only the ends that point away from the viewer are dimmed, so that the axis colors stay vivid
            double depth = _endDepths[endId];
            double factor = depth < 0 ? -DimFactor * depth : 0;
            //
            return new double[] { color[0] + (DimColor[0] - color[0]) * factor,
                                  color[1] + (DimColor[1] - color[1]) * factor,
                                  color[2] + (DimColor[2] - color[2]) * factor };
        }
        public double[] GetOriginColor()
        {
            return _overrideColor == null ? DimColor : _overrideColor;
        }
        public int[] GetEndIdsSortedByDepth()
        {
            int[] endIds = new int[NumOfEnds];
            for (int i = 0; i < NumOfEnds; i++) endIds[i] = i;
            //
            return endIds.OrderBy(endId => _endDepths[endId]).ToArray();
        }
        public double[] GetNormalizedEndDirection(int endId)
        {
            double[] direction = _endDirections[endId];
            double length = Math.Sqrt(direction[0] * direction[0] + direction[1] * direction[1]);
            // The axis points towards or away from the viewer and has no projected direction
            if (length < 0.05) return new double[] { 0.7071, -0.7071 };
            //
            return new double[] { direction[0] / length, direction[1] / length };
        }
        public double[] GetConeBaseCenter(int endId)
        {
            return new double[] { _endPositions[endId][0] - _coneHeight * _endDirections[endId][0],
                                  _endPositions[endId][1] - _coneHeight * _endDirections[endId][1] };
        }
        public double[] GetEndTargetPosition(int endId)
        {
            if (!IsPositiveEnd(endId)) return _endPositions[endId];
            // The cone is the only clickable part of a positive axis and it sits close to its tip
            // so that the shaft behind it stays free for the vertical axis target
            double[] baseCenter = GetConeBaseCenter(endId);
            //
            return new double[] { baseCenter[0] + ConeTargetRatio * (_endPositions[endId][0] - baseCenter[0]),
                                  baseCenter[1] + ConeTargetRatio * (_endPositions[endId][1] - baseCenter[1]) };
        }
        public double[] GetLabelPosition(int axisId, int[] labelSize)
        {
            int endId = GetEndId(axisId, 0);
            double[] direction = GetNormalizedEndDirection(endId);
            double distance = _labelGap + Math.Max(labelSize[0], labelSize[1]) / 2.0;
            //
            return new double[] { _endPositions[endId][0] + distance * direction[0] - labelSize[0] / 2.0,
                                  _endPositions[endId][1] + distance * direction[1] - labelSize[1] / 2.0 };
        }


        // Public setters                                                                                                           
        public void SetShaftLength(double shaftLength)
        {
            _shaftLength = shaftLength;
            _negativeShaftLength = NegativeShaftLengthRatio * shaftLength;
            _coneHeight = ConeHeightRatio * shaftLength;
            _coneRadius = ConeRadiusRatio * shaftLength;
            _originRadius = OriginRadiusRatio * shaftLength;
            _negativeEndRadius = _originRadius;
            _labelGap = LabelGapRatio * shaftLength;
        }
        public void SetOverrideColor(double[] color)
        {
            // A null color restores the per axis colors
            _overrideColor = color == null ? null : new double[] { color[0], color[1], color[2] };
        }
        public bool SetNegativeAxesVisible(bool visible)
        {
            if (_negativeAxesVisible == visible) return false;
            //
            _negativeAxesVisible = visible;
            //
            return true;
        }


        // Static methods                                                                                                           
        public static bool IsPositiveEnd(int endId)
        {
            return endId % 2 == 0;
        }
        public static int GetEndId(int axisId, int signId)
        {
            return 2 * axisId + signId;
        }
        public static double GetShaftLength(double symbolSize)
        {
            return DefaultShaftLength * symbolSize / DefaultSymbolSize;
        }
        public static int GetLabelFontSize(double shaftLength)
        {
            int fontSize = (int)Math.Round(LabelFontSizeRatio * shaftLength, MidpointRounding.AwayFromZero);
            //
            return fontSize < 1 ? 1 : fontSize;
        }
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
            right = NormalizedCrossProduct(viewUp, viewPlaneNormal, new double[] { 1, 0, 0 });
            up = NormalizedCrossProduct(viewPlaneNormal, right, new double[] { 0, 1, 0 });
        }
        public static double DotProduct(double[] first, double[] second)
        {
            return first[0] * second[0] + first[1] * second[1] + first[2] * second[2];
        }
        public static double[] NormalizedCrossProduct(double[] first, double[] second, double[] onFailure)
        {
            double[] product = new double[] { first[1] * second[2] - first[2] * second[1],
                                              first[2] * second[0] - first[0] * second[2],
                                              first[0] * second[1] - first[1] * second[0] };
            double length = Math.Sqrt(product[0] * product[0] + product[1] * product[1] + product[2] * product[2]);
            if (length < 1E-12) return onFailure;
            //
            return new double[] { product[0] / length, product[1] / length, product[2] / length };
        }
    }
}
