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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kitware.VTK;

namespace vtkControl
{
    enum vtkMaxCoorSysTarget
    {
        None,
        Origin,
        XPositive,
        XNegative,
        YPositive,
        YNegative,
        ZPositive,
        ZNegative
    }

    class vtkMaxCoorSysWidget : vtkMaxBorderWidget
    {
        // Variables                                                                                                                
        private const int NumOfAxes = 3;
        private const int NumOfEnds = 2 * NumOfAxes;
        private const int NumOfCircleSegments = 24;
        //
        private double _shaftLength;
        private double _negativeShaftLength;
        private double _negativeOpacity;
        private double _coneHeight;
        private double _coneRadius;
        private double _negativeEndRadius;
        private double _originRadius;
        private double _positiveHitRadius;
        private double _negativeHitRadius;
        private double _originHitRadius;
        private double _labelGap;
        private double _highlightPadding;
        private double _dimFactor;
        private double[] _dimColor;
        private double[][] _axisColors;
        private double[][] _endDirections;
        private double[][] _endPositions;
        private double[] _endDepths;
        private int[][] _labelSizes;
        private uint _cameraModifiedTime;
        private bool _negativeAxesVisible;
        private bool _geometryGenerated;
        private bool _hovered;
        private vtkMaxCoorSysTarget _pressedTarget;
        //
        protected vtkTextMapper[] _labelMappers;
        protected vtkActor2D[] _labelActors;
        //
        protected vtkPolyDataMapper2D _shaftMapper;
        protected vtkActor2D _shaftActor;
        //
        protected vtkPolyDataMapper2D _endMapper;
        protected vtkActor2D _endActor;
        //
        protected vtkMaxWidgetHighlight _targetHighlight;


        // Events                                                                                                                   
        public event Action<vtkMaxCoorSysTarget> TargetClicked;
        public event Action<bool> HoverChanged;


        // Constructors                                                                                                             
        public vtkMaxCoorSysWidget()
        {
            _shaftLength = 45;
            _negativeShaftLength = 2 * _shaftLength / 3;
            _negativeOpacity = 0.5;
            _coneHeight = 14;
            _coneRadius = 3.5;
            _originRadius = 3.5;
            _negativeEndRadius = _originRadius;
            _positiveHitRadius = 11;
            _negativeHitRadius = 8;
            _originHitRadius = 7;
            _labelGap = 6;
            _highlightPadding = 5;
            _dimFactor = 0.35;
            _dimColor = new double[] { 0.35, 0.35, 0.35 };
            _axisColors = new double[][] { new double[] { 0.706, 0.016, 0.150 },
                                           new double[] { 0.130, 0.806, 0.150 },
                                           new double[] { 0.230, 0.299, 0.754 } };
            //
            _endDirections = new double[NumOfEnds][];
            _endPositions = new double[NumOfEnds][];
            _endDepths = new double[NumOfEnds];
            for (int i = 0; i < NumOfEnds; i++)
            {
                _endDirections[i] = new double[2];
                _endPositions[i] = new double[2];
            }
            _labelSizes = new int[NumOfAxes][];
            _cameraModifiedTime = 0;
            _negativeAxesVisible = false;
            _geometryGenerated = false;
            _hovered = false;
            _pressedTarget = vtkMaxCoorSysTarget.None;
            //
            double side = 2 * (_shaftLength + _coneRadius + 2);
            _size[0] = side;
            _size[1] = side;
            //
            _targetHighlight = new vtkMaxWidgetHighlight();
            //
            InitializeLabels();
            InitializeShafts();
            InitializeEnds();
        }


        // Private methods                                                                                                          
        private void InitializeLabels()
        {
            string[] labels = new string[] { "X", "Y", "Z" };
            vtkTextProperty textProperty = vtkTextProperty.New();
            //
            _labelMappers = new vtkTextMapper[NumOfAxes];
            _labelActors = new vtkActor2D[NumOfAxes];
            //
            for (int i = 0; i < NumOfAxes; i++)
            {
                // Mapper
                _labelMappers[i] = vtkTextMapper.New();
                _labelMappers[i].SetTextProperty(textProperty);
                _labelMappers[i].SetInput(labels[i]);
                // Actor
                _labelActors[i] = vtkActor2D.New();
                _labelActors[i].SetMapper(_labelMappers[i]);
                // Set relative text position
                _labelActors[i].GetPositionCoordinate().SetCoordinateSystemToDisplay();  // set offsets in pixels
                _labelActors[i].GetPositionCoordinate().SetReferenceCoordinate(_positionCoordinate);
                _labelActors[i].GetPositionCoordinate().SetValue(0, 0);
            }
        }
        private void InitializeShafts()
        {
            vtkPolyData shaftPolyData = vtkPolyData.New();
            shaftPolyData.SetPoints(vtkPoints.New());
            shaftPolyData.SetLines(vtkCellArray.New());
            //
            _shaftMapper = vtkPolyDataMapper2D.New();
            _shaftMapper.SetInput(shaftPolyData);
            _shaftMapper.SetScalarModeToUseCellData();
            _shaftMapper.SetColorModeToDefault();
            _shaftMapper.ScalarVisibilityOn();
            //
            _shaftActor = vtkActor2D.New();
            _shaftActor.SetMapper(_shaftMapper);
            _shaftActor.GetProperty().SetLineWidth(2f);
            //
            _shaftActor.GetPositionCoordinate().SetCoordinateSystemToDisplay();  // set offsets in pixels
            _shaftActor.GetPositionCoordinate().SetReferenceCoordinate(_positionCoordinate);
        }
        private void InitializeEnds()
        {
            vtkPolyData endPolyData = vtkPolyData.New();
            endPolyData.SetPoints(vtkPoints.New());
            endPolyData.SetPolys(vtkCellArray.New());
            //
            _endMapper = vtkPolyDataMapper2D.New();
            _endMapper.SetInput(endPolyData);
            _endMapper.SetScalarModeToUseCellData();
            _endMapper.SetColorModeToDefault();
            _endMapper.ScalarVisibilityOn();
            //
            _endActor = vtkActor2D.New();
            _endActor.SetMapper(_endMapper);
            //
            _endActor.GetPositionCoordinate().SetCoordinateSystemToDisplay();  // set offsets in pixels
            _endActor.GetPositionCoordinate().SetReferenceCoordinate(_positionCoordinate);
        }
        //
        private void GenerateGeometry(bool force)
        {
            if (_renderer == null) return;
            // If this function is called when the renderer is not yet loaded, return
            int[] size = _renderer.GetSize();
            if (size[0] == 0 && size[1] == 0) return;
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            uint cameraModifiedTime = camera == null ? 0 : camera.GetMTime();
            if (!force && _geometryGenerated && cameraModifiedTime == _cameraModifiedTime) return;
            _cameraModifiedTime = cameraModifiedTime;
            //
            ComputeEndProjections();    // must be before all the rest
            GenerateShafts();
            GenerateEnds();
            PositionLabels();
            //
            _geometryGenerated = true;
        }
        private void ComputeEndProjections()
        {
            double[] viewPlaneNormal = new double[] { 0, 0, 1 };
            double[] viewUp = new double[] { 0, 1, 0 };
            vtkCamera camera = _renderer.GetActiveCamera();
            if (camera != null)
            {
                viewPlaneNormal = camera.GetViewPlaneNormal();  // points from the focal point towards the camera
                viewUp = camera.GetViewUp();
            }
            double[] right = NormalizedCrossProduct(viewUp, viewPlaneNormal, new double[] { 1, 0, 0 });
            double[] up = NormalizedCrossProduct(viewPlaneNormal, right, new double[] { 0, 1, 0 });
            double[] center = GetLocalCenter();
            //
            for (int axisId = 0; axisId < NumOfAxes; axisId++)
            {
                for (int signId = 0; signId < 2; signId++)
                {
                    int endId = GetEndId(axisId, signId);
                    double sign = signId == 0 ? 1 : -1;
                    double shaftLength = IsPositiveEnd(endId) ? _shaftLength : _negativeShaftLength;
                    //
                    _endDirections[endId][0] = sign * right[axisId];
                    _endDirections[endId][1] = sign * up[axisId];
                    _endDepths[endId] = sign * viewPlaneNormal[axisId];
                    //
                    _endPositions[endId][0] = center[0] + shaftLength * _endDirections[endId][0];
                    _endPositions[endId][1] = center[1] + shaftLength * _endDirections[endId][1];
                }
            }
        }
        private void GenerateShafts()
        {
            double[] center = GetLocalCenter();
            //
            vtkPoints points = vtkPoints.New();
            vtkCellArray lines = vtkCellArray.New();
            vtkUnsignedCharArray colors = vtkUnsignedCharArray.New();
            colors.SetNumberOfComponents(4);
            //
            points.InsertNextPoint(center[0], center[1], 0);
            for (int endId = 0; endId < NumOfEnds; endId++)
            {
                points.InsertNextPoint(_endPositions[endId][0], _endPositions[endId][1], 0);
            }
            // The ends closer to the viewer must be drawn over the ends behind them
            foreach (int endId in GetEndIdsSortedByDepth())
            {
                if (!IsEndVisible(endId)) continue;
                //
                lines.InsertNextCell(2);
                lines.InsertCellPoint(0);
                lines.InsertCellPoint(endId + 1);
                //
                InsertColor(colors, GetEndColor(endId), GetEndOpacity(endId));
            }
            //
            vtkPolyData shaftPolyData = _shaftMapper.GetInput();
            shaftPolyData.SetPoints(points);
            shaftPolyData.SetLines(lines);
            shaftPolyData.GetCellData().SetScalars(colors);
            shaftPolyData.Modified();
        }
        private void GenerateEnds()
        {
            vtkPoints points = vtkPoints.New();
            vtkCellArray polygons = vtkCellArray.New();
            vtkUnsignedCharArray colors = vtkUnsignedCharArray.New();
            colors.SetNumberOfComponents(4);
            //
            InsertDisk(points, polygons, GetLocalCenter(), _originRadius);
            InsertColor(colors, _dimColor, 1);
            // The ends closer to the viewer must be drawn over the ends behind them
            foreach (int endId in GetEndIdsSortedByDepth())
            {
                if (!IsEndVisible(endId)) continue;
                //
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
            //
            vtkPolyData endPolyData = _endMapper.GetInput();
            endPolyData.SetPoints(points);
            endPolyData.SetPolys(polygons);
            endPolyData.GetCellData().SetScalars(colors);
            endPolyData.Modified();
        }
        private void PositionLabels()
        {
            for (int axisId = 0; axisId < NumOfAxes; axisId++)
            {
                int endId = GetEndId(axisId, 0);
                int[] labelSize = GetLabelSize(axisId);
                double[] direction = GetNormalizedEndDirection(endId);
                double distance = _labelGap + Math.Max(labelSize[0], labelSize[1]) / 2.0;
                //
                double[] labelCenter = new double[] { _endPositions[endId][0] + distance * direction[0],
                                                      _endPositions[endId][1] + distance * direction[1] };
                //
                _labelActors[axisId].GetPositionCoordinate().SetValue(labelCenter[0] - labelSize[0] / 2.0,
                                                                      labelCenter[1] - labelSize[1] / 2.0);
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
        private void InsertColor(vtkUnsignedCharArray colors, double[] color, double opacity)
        {
            colors.InsertNextTuple4(Math.Round(255 * color[0]), Math.Round(255 * color[1]),
                                    Math.Round(255 * color[2]), Math.Round(255 * opacity));
        }
        //
        private int[] GetLabelSize(int axisId)
        {
            if (_labelSizes[axisId] == null)
                _labelSizes[axisId] = vtkMaxWidgetTools.GetTextSize(_labelMappers[axisId], _renderer);
            //
            return _labelSizes[axisId];
        }
        private int GetMaxLabelSize()
        {
            int maxLabelSize = 0;
            for (int axisId = 0; axisId < NumOfAxes; axisId++)
            {
                int[] labelSize = GetLabelSize(axisId);
                maxLabelSize = Math.Max(maxLabelSize, Math.Max(labelSize[0], labelSize[1]));
            }
            return maxLabelSize;
        }
        private double[] GetLocalCenter()
        {
            return new double[] { _size[0] / 2, _size[1] / 2 };
        }
        private double[] GetDisplayPosition()
        {
            if (!_visibility || _renderer == null || _position == null) return null;
            //
            double[] position = _position.ToArray();
            if (_widgetPosition == vtkMaxWidgetPosition.FromTopLeft)
                position[1] = _renderer.GetSize()[1] - _size[1] - position[1];
            //
            return new double[] { Math.Floor(position[0]), Math.Floor(position[1]) };
        }
        private double[] GetNormalizedEndDirection(int endId)
        {
            double[] direction = _endDirections[endId];
            double length = Math.Sqrt(direction[0] * direction[0] + direction[1] * direction[1]);
            // The axis points towards or away from the viewer and has no projected direction
            if (length < 0.05) return new double[] { 0.7071, -0.7071 };
            //
            return new double[] { direction[0] / length, direction[1] / length };
        }
        private double[] GetConeBaseCenter(int endId)
        {
            return new double[] { _endPositions[endId][0] - _coneHeight * _endDirections[endId][0],
                                  _endPositions[endId][1] - _coneHeight * _endDirections[endId][1] };
        }
        private double[] GetEndTargetPosition(int endId)
        {
            if (!IsPositiveEnd(endId)) return _endPositions[endId];
            // The cone is the only clickable part of a positive axis
            double[] baseCenter = GetConeBaseCenter(endId);
            //
            return new double[] { (_endPositions[endId][0] + baseCenter[0]) / 2,
                                  (_endPositions[endId][1] + baseCenter[1]) / 2 };
        }
        private double GetEndHitRadius(int endId)
        {
            return IsPositiveEnd(endId) ? _positiveHitRadius : _negativeHitRadius;
        }
        private double GetEndOpacity(int endId)
        {
            return IsPositiveEnd(endId) ? 1 : _negativeOpacity;
        }
        private double[] GetEndColor(int endId)
        {
            double[] color = _axisColors[endId / 2];
            double factor = _dimFactor * (1 - _endDepths[endId]);
            //
            return new double[] { color[0] + (_dimColor[0] - color[0]) * factor,
                                  color[1] + (_dimColor[1] - color[1]) * factor,
                                  color[2] + (_dimColor[2] - color[2]) * factor };
        }
        private int[] GetEndIdsSortedByDepth()
        {
            int[] endIds = new int[NumOfEnds];
            for (int i = 0; i < NumOfEnds; i++) endIds[i] = i;
            //
            return endIds.OrderBy(endId => _endDepths[endId]).ToArray();
        }
        private double[] GetTargetLocalPosition(vtkMaxCoorSysTarget target)
        {
            if (target == vtkMaxCoorSysTarget.Origin) return GetLocalCenter();
            //
            return GetEndTargetPosition(GetEndId(target));
        }
        private double GetTargetHitRadius(vtkMaxCoorSysTarget target)
        {
            if (target == vtkMaxCoorSysTarget.Origin) return _originHitRadius;
            //
            return GetEndHitRadius(GetEndId(target));
        }
        //
        private static double[] NormalizedCrossProduct(double[] first, double[] second, double[] onFailure)
        {
            double[] product = new double[] { first[1] * second[2] - first[2] * second[1],
                                              first[2] * second[0] - first[0] * second[2],
                                              first[0] * second[1] - first[1] * second[0] };
            double length = Math.Sqrt(product[0] * product[0] + product[1] * product[1] + product[2] * product[2]);
            if (length < 1E-12) return onFailure;
            //
            return new double[] { product[0] / length, product[1] / length, product[2] / length };
        }
        private bool IsEndVisible(int endId)
        {
            return IsPositiveEnd(endId) || _negativeAxesVisible;
        }
        private bool SetNegativeAxesVisible(bool visible)
        {
            if (_negativeAxesVisible == visible) return false;
            //
            _negativeAxesVisible = visible;
            GenerateGeometry(true);
            //
            return true;
        }
        private static bool IsPositiveEnd(int endId)
        {
            return endId % 2 == 0;
        }
        private static int GetEndId(int axisId, int signId)
        {
            return 2 * axisId + signId;
        }
        private static int GetEndId(vtkMaxCoorSysTarget target)
        {
            switch (target)
            {
                case vtkMaxCoorSysTarget.XPositive: return 0;
                case vtkMaxCoorSysTarget.XNegative: return 1;
                case vtkMaxCoorSysTarget.YPositive: return 2;
                case vtkMaxCoorSysTarget.YNegative: return 3;
                case vtkMaxCoorSysTarget.ZPositive: return 4;
                case vtkMaxCoorSysTarget.ZNegative: return 5;
                default: throw new NotSupportedException();
            }
        }
        private static vtkMaxCoorSysTarget GetTarget(int endId)
        {
            switch (endId)
            {
                case 0: return vtkMaxCoorSysTarget.XPositive;
                case 1: return vtkMaxCoorSysTarget.XNegative;
                case 2: return vtkMaxCoorSysTarget.YPositive;
                case 3: return vtkMaxCoorSysTarget.YNegative;
                case 4: return vtkMaxCoorSysTarget.ZPositive;
                case 5: return vtkMaxCoorSysTarget.ZNegative;
                default: throw new NotSupportedException();
            }
        }
        private void SetHovered(bool hovered)
        {
            if (_hovered == hovered) return;
            //
            _hovered = hovered;
            HoverChanged?.Invoke(_hovered);
        }


        // Public methods                                                                                                           
        public vtkMaxCoorSysTarget GetTargetAtPosition(int x, int y)
        {
            if (!_visibility || !_geometryGenerated) return vtkMaxCoorSysTarget.None;
            //
            double[] position = GetDisplayPosition();
            if (position == null) return vtkMaxCoorSysTarget.None;
            // The end closest to the viewer wins when the projections overlap
            int closestEndId = -1;
            for (int endId = 0; endId < NumOfEnds; endId++)
            {
                if (!IsEndVisible(endId)) continue;
                //
                double[] endTargetPosition = GetEndTargetPosition(endId);
                double hitRadius = GetEndHitRadius(endId);
                double dx = position[0] + endTargetPosition[0] - x;
                double dy = position[1] + endTargetPosition[1] - y;
                if (dx * dx + dy * dy > hitRadius * hitRadius) continue;
                //
                if (closestEndId < 0 || _endDepths[endId] > _endDepths[closestEndId]) closestEndId = endId;
            }
            if (closestEndId >= 0) return GetTarget(closestEndId);
            //
            double[] center = GetLocalCenter();
            double dxOrigin = position[0] + center[0] - x;
            double dyOrigin = position[1] + center[1] - y;
            if (dxOrigin * dxOrigin + dyOrigin * dyOrigin <= _originHitRadius * _originHitRadius)
                return vtkMaxCoorSysTarget.Origin;
            //
            return vtkMaxCoorSysTarget.None;
        }
        public override void OnRenderWindowModified()
        {
            if (_visibility)
            {
                base.OnRenderWindowModified();
                OnSizeChanged();
            }
        }
        public override void CameraModified()
        {
            if (_visibility)
            {
                base.CameraModified();
                GenerateGeometry(false);
            }
        }
        public override void MouseRotate(double azimuthAngle, double ElevationAngle)
        {
            if (_visibility)
            {
                base.MouseRotate(azimuthAngle, ElevationAngle);
                GenerateGeometry(false);
            }
        }
        public void UpdateGeometry()
        {
            if (_visibility) GenerateGeometry(false);
        }
        public override void OnSizeChanged()
        {
            GenerateGeometry(true);
            OnMovedOrSizeChanged();
        }
        public override bool LeftButtonPress(MouseEventArgs e)
        {
            _pressedTarget = GetTargetAtPosition(e.Location.X, e.Location.Y);
            //
            return _pressedTarget != vtkMaxCoorSysTarget.None;
        }
        public override bool LeftButtonRelease(int x, int y)
        {
            if (_pressedTarget == vtkMaxCoorSysTarget.None) return false;
            //
            vtkMaxCoorSysTarget pressedTarget = _pressedTarget;
            _pressedTarget = vtkMaxCoorSysTarget.None;
            if (GetTargetAtPosition(x, y) == pressedTarget) TargetClicked?.Invoke(pressedTarget);
            //
            return true;
        }
        public override bool RightButtonPress(int x, int y)
        {
            return false;
        }
        public override bool MouseMove(int x, int y)
        {
            return false;
        }
        public override bool UpdateMouseProximity(int x, int y)
        {
            return SetNegativeAxesVisible(GetDisplayRectangle().Contains(x, y));
        }
        public override bool UpdateMouseHover(int x, int y, bool mouseOver, bool highlightEntireWidget = false)
        {
            Rectangle widgetRectangle = mouseOver ? GetDisplayRectangle() : Rectangle.Empty;
            Rectangle targetRectangle = mouseOver && _localHighlightEnabled
                ? GetHighlightRectangle(x, y)
                : Rectangle.Empty;
            //
            bool highlightChanged = _highlight.SetRectangle(widgetRectangle, vtkMaxWidgetHighlight.WidgetFillOpacity);
            if (_targetHighlight.SetRectangle(targetRectangle, vtkMaxWidgetHighlight.WidgetFillOpacity))
                highlightChanged = true;
            //
            SetHovered(targetRectangle != Rectangle.Empty);
            //
            return highlightChanged;
        }
        public override bool ClearMouseProximity()
        {
            return SetNegativeAxesVisible(false);
        }
        public override bool ClearMouseHover()
        {
            SetHovered(false);
            //
            bool targetHighlightChanged = _targetHighlight.Clear();
            //
            return base.ClearMouseHover() || targetHighlightChanged;
        }
        public override void VisibilityOn()
        {
            if (_visibility == false)
            {
                base.VisibilityOn();
                //
                if (_highlightRenderer != null) _targetHighlight.AddToRenderer(_highlightRenderer);
                if (_shaftActor != null) _renderer.AddActor(_shaftActor);
                if (_endActor != null) _renderer.AddActor(_endActor);
                //
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    if (_labelActors[i] != null) _renderer.AddActor(_labelActors[i]);
                }
            }
        }
        public override void VisibilityOff()
        {
            if (_visibility == true)
            {
                base.VisibilityOff();
                //
                if (_highlightRenderer != null) _targetHighlight.RemoveFromRenderer(_highlightRenderer);
                if (_shaftActor != null) _renderer.RemoveActor(_shaftActor);
                if (_endActor != null) _renderer.RemoveActor(_endActor);
                //
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    if (_labelActors[i] != null) _renderer.RemoveActor(_labelActors[i]);
                }
                //
                SetNegativeAxesVisible(false);
                SetHovered(false);
            }
        }
        public override void BringHighlightToFront()
        {
            if (!_visibility || _highlightRenderer == null) return;
            //
            base.BringHighlightToFront();
            _targetHighlight.BringToFront(_highlightRenderer);
        }
        public override void BringToFront()
        {
            if (!_visibility || _renderer == null) return;
            //
            base.BringToFront();
            if (_shaftActor != null)
            {
                _renderer.RemoveActor(_shaftActor);
                _renderer.AddActor(_shaftActor);
            }
            if (_endActor != null)
            {
                _renderer.RemoveActor(_endActor);
                _renderer.AddActor(_endActor);
            }
            for (int i = 0; i < _labelActors.Length; i++)
            {
                if (_labelActors[i] != null)
                {
                    _renderer.RemoveActor(_labelActors[i]);
                    _renderer.AddActor(_labelActors[i]);
                }
            }
        }


        // Protected methods                                                                                                        
        protected override Rectangle GetDisplayRectangle()
        {
            Rectangle rectangle = base.GetDisplayRectangle();
            if (rectangle == Rectangle.Empty) return rectangle;
            // A square that contains the axis labels in any view orientation
            double[] position = GetDisplayPosition();
            double[] center = GetLocalCenter();
            double halfSide = _shaftLength + _labelGap + GetMaxLabelSize() + _highlightPadding;
            double centerX = position[0] + center[0];
            double centerY = position[1] + center[1];
            //
            return Rectangle.FromLTRB((int)Math.Floor(centerX - halfSide), (int)Math.Floor(centerY - halfSide),
                                      (int)Math.Ceiling(centerX + halfSide), (int)Math.Ceiling(centerY + halfSide));
        }
        protected override Rectangle GetHighlightRectangle(int x, int y)
        {
            vtkMaxCoorSysTarget target = GetTargetAtPosition(x, y);
            double[] position = GetDisplayPosition();
            if (target == vtkMaxCoorSysTarget.None || position == null) return Rectangle.Empty;
            //
            double[] localPosition = GetTargetLocalPosition(target);
            double radius = GetTargetHitRadius(target);
            double centerX = position[0] + localPosition[0];
            double centerY = position[1] + localPosition[1];
            //
            return Rectangle.FromLTRB((int)Math.Floor(centerX - radius), (int)Math.Floor(centerY - radius),
                                      (int)Math.Ceiling(centerX + radius), (int)Math.Ceiling(centerY + radius));
        }


        // Public setters                                                                                                           
        public override void SetInteractor(vtkRenderer renderer, vtkRenderWindowInteractor renderWindowInteractor)
        {
            base.SetInteractor(renderer, renderWindowInteractor);
            if (renderer == null || renderWindowInteractor == null) return;
            //
            if (_visibility)
            {
                if (_highlightRenderer != null) _targetHighlight.AddToRenderer(_highlightRenderer);
                _renderer.AddActor(_shaftActor);
                _renderer.AddActor(_endActor);
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    _renderer.AddActor(_labelActors[i]);
                }
            }
            //
            OnSizeChanged();
        }
        public override void RemoveInteractor()
        {
            if (_highlightRenderer != null) _targetHighlight.RemoveFromRenderer(_highlightRenderer);
            if (_renderer != null)
            {
                _renderer.RemoveActor(_shaftActor);
                _renderer.RemoveActor(_endActor);
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    _renderer.RemoveActor(_labelActors[i]);
                }
            }
            //
            base.RemoveInteractor();
        }
        public void SetTextProperty(vtkTextProperty textProperty)
        {
            for (int i = 0; i < _labelMappers.Length; i++)
            {
                _labelMappers[i].SetTextProperty(textProperty);
                _labelSizes[i] = null;
            }
            //
            GenerateGeometry(true);
        }
    }
}
