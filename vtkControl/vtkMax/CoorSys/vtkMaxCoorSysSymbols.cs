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
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;

namespace vtkControl
{
    class vtkMaxCoorSysSymbols
    {
        // Variables                                                                                                                
        private List<vtkMaxCoorSysSymbol> _symbols;
        private Dictionary<string, int[]> _labelSizes;
        private vtkRenderer _renderer;
        private vtkCoordinate _originCoordinate;
        private vtkTextProperty _textProperty;
        private vtkTextProperty _labelTextProperty;
        private double[] _overrideColor;
        private int _labelFontSize;
        private int[] _rendererSize;
        private uint _cameraModifiedTime;
        private bool _geometryGenerated;
        //
        protected vtkPolyDataMapper2D _shaftMapper;
        protected vtkPolyData _shaftPolyData;
        protected vtkPoints _shaftPoints;
        protected vtkCellArray _shaftLines;
        protected vtkUnsignedCharArray _shaftColors;
        protected vtkActor2D _shaftActor;
        //
        protected vtkPolyDataMapper2D _endMapper;
        protected vtkPolyData _endPolyData;
        protected vtkPoints _endPoints;
        protected vtkCellArray _endPolygons;
        protected vtkUnsignedCharArray _endColors;
        protected vtkActor2D _endActor;


        // Constructors                                                                                                             
        public vtkMaxCoorSysSymbols()
        {
            _symbols = new List<vtkMaxCoorSysSymbol>();
            _labelSizes = new Dictionary<string, int[]>();
            _labelFontSize = 0;
            _rendererSize = new int[] { 0, 0 };
            _cameraModifiedTime = 0;
            _geometryGenerated = false;
            // The display position of a symbol origin is computed from its world position
            _originCoordinate = vtkCoordinate.New();
            _originCoordinate.SetCoordinateSystemToWorld();
            //
            InitializeShafts();
            InitializeEnds();
        }


        // Event handlers                                                                                                           
        private void renderer_StartEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            // The camera is not modified through the widgets during the mouse interaction, so the symbols
            // must be re-projected right before the renderer draws them
            GenerateGeometry(false);
        }


        // Private methods                                                                                                          
        private void InitializeShafts()
        {
            _shaftPoints = vtkPoints.New();
            _shaftLines = vtkCellArray.New();
            _shaftColors = vtkUnsignedCharArray.New();
            _shaftColors.SetNumberOfComponents(4);
            //
            _shaftPolyData = vtkPolyData.New();
            _shaftPolyData.SetPoints(_shaftPoints);
            _shaftPolyData.SetLines(_shaftLines);
            _shaftPolyData.GetCellData().SetScalars(_shaftColors);
            //
            _shaftMapper = vtkPolyDataMapper2D.New();
            _shaftMapper.SetInput(_shaftPolyData);
            _shaftMapper.SetScalarModeToUseCellData();
            _shaftMapper.SetColorModeToDefault();
            _shaftMapper.ScalarVisibilityOn();
            //
            _shaftActor = vtkActor2D.New();
            _shaftActor.SetMapper(_shaftMapper);
            _shaftActor.GetProperty().SetLineWidth(2f);
            _shaftActor.UseBoundsOff();
            //
            _shaftActor.GetPositionCoordinate().SetCoordinateSystemToDisplay();
            _shaftActor.GetPositionCoordinate().SetValue(0, 0);
        }
        private void InitializeEnds()
        {
            _endPoints = vtkPoints.New();
            _endPolygons = vtkCellArray.New();
            _endColors = vtkUnsignedCharArray.New();
            _endColors.SetNumberOfComponents(4);
            //
            _endPolyData = vtkPolyData.New();
            _endPolyData.SetPoints(_endPoints);
            _endPolyData.SetPolys(_endPolygons);
            _endPolyData.GetCellData().SetScalars(_endColors);
            //
            _endMapper = vtkPolyDataMapper2D.New();
            _endMapper.SetInput(_endPolyData);
            _endMapper.SetScalarModeToUseCellData();
            _endMapper.SetColorModeToDefault();
            _endMapper.ScalarVisibilityOn();
            //
            _endActor = vtkActor2D.New();
            _endActor.SetMapper(_endMapper);
            _endActor.UseBoundsOff();
            //
            _endActor.GetPositionCoordinate().SetCoordinateSystemToDisplay();
            _endActor.GetPositionCoordinate().SetValue(0, 0);
        }
        //
        private void GenerateGeometry(bool force)
        {
            if (_renderer == null || _symbols.Count == 0) return;
            // If this function is called when the renderer is not yet loaded, return
            int[] size = _renderer.GetSize();
            if (size[0] == 0 && size[1] == 0) return;
            // The projection depends on the camera and on the renderer size
            vtkCamera camera = _renderer.GetActiveCamera();
            uint cameraModifiedTime = camera == null ? 0 : camera.GetMTime();
            if (!force && _geometryGenerated && cameraModifiedTime == _cameraModifiedTime &&
                size[0] == _rendererSize[0] && size[1] == _rendererSize[1]) return;
            _cameraModifiedTime = cameraModifiedTime;
            _rendererSize[0] = size[0];
            _rendererSize[1] = size[1];
            //
            ComputeSymbolProjections(camera);   // must be before all the rest
            GenerateShafts();
            GenerateEnds();
            //
            _geometryGenerated = true;
        }
        private void ComputeSymbolProjections(vtkCamera camera)
        {
            double[] right;
            double[] up;
            double[] viewPlaneNormal;
            vtkMaxCoorSysGeometry.GetCameraBasis(camera, out right, out up, out viewPlaneNormal);
            //
            foreach (vtkMaxCoorSysSymbol symbol in _symbols)
            {
                double[] origin = symbol.GetOrigin();
                _originCoordinate.SetValue(origin[0], origin[1], origin[2]);
                double[] center = _originCoordinate.GetComputedDoubleDisplayValue(_renderer);
                // Only the relative depth of the origins matters when the symbols are sorted
                double centerDepth = vtkMaxCoorSysGeometry.DotProduct(origin, viewPlaneNormal);
                //
                symbol.UpdateProjection(center, centerDepth, right, up, viewPlaneNormal);
                symbol.PositionLabels(GetLabelSizes(symbol));
            }
        }
        private void GenerateShafts()
        {
            _shaftPoints.Reset();
            _shaftLines.Reset();
            _shaftColors.Reset();
            //
            foreach (vtkMaxCoorSysSymbol symbol in GetSymbolsSortedByDepth())
            {
                // The ends closer to the viewer must be drawn over the ends behind them
                foreach (int endId in symbol.GetEndIdsSortedByDepth())
                {
                    if (!symbol.IsEndVisible(endId)) continue;
                    //
                    symbol.AppendShaft(_shaftPoints, _shaftLines, _shaftColors, endId);
                }
            }
            //
            _shaftPoints.Modified();
            _shaftPolyData.Modified();
        }
        private void GenerateEnds()
        {
            _endPoints.Reset();
            _endPolygons.Reset();
            _endColors.Reset();
            //
            foreach (vtkMaxCoorSysSymbol symbol in GetSymbolsSortedByDepth())
            {
                symbol.AppendOrigin(_endPoints, _endPolygons, _endColors);
                // The ends closer to the viewer must be drawn over the ends behind them
                foreach (int endId in symbol.GetEndIdsSortedByDepth())
                {
                    if (!symbol.IsEndVisible(endId)) continue;
                    //
                    symbol.AppendEnd(_endPoints, _endPolygons, _endColors, endId);
                }
            }
            //
            _endPoints.Modified();
            _endPolyData.Modified();
        }
        private vtkMaxCoorSysSymbol[] GetSymbolsSortedByDepth()
        {
            if (_symbols.Count < 2) return _symbols.ToArray();
            // The symbols closer to the viewer must be drawn over the symbols behind them
            return _symbols.OrderBy(symbol => symbol.CenterDepth).ToArray();
        }
        private int[][] GetLabelSizes(vtkMaxCoorSysSymbol symbol)
        {
            string[] labels = symbol.Labels;
            int[][] labelSizes = new int[labels.Length][];
            //
            for (int i = 0; i < labels.Length; i++)
            {
                int[] labelSize;
                if (!_labelSizes.TryGetValue(labels[i], out labelSize))
                {
                    labelSize = vtkMaxWidgetTools.GetTextSize(symbol.GetLabelMapper(i), _renderer);
                    _labelSizes.Add(labels[i], labelSize);
                }
                labelSizes[i] = labelSize;
            }
            //
            return labelSizes;
        }
        private void UpdateLabelTextProperty(double shaftLength)
        {
            int labelFontSize = vtkMaxCoorSysGeometry.GetLabelFontSize(shaftLength);
            if (_labelTextProperty != null && _labelFontSize == labelFontSize) return;
            //
            _labelFontSize = labelFontSize;
            _labelSizes.Clear();
            //
            _labelTextProperty = vtkTextProperty.New();
            if (_textProperty != null) _labelTextProperty.ShallowCopy(_textProperty);
            _labelTextProperty.SetFontSize(_labelFontSize);
            _labelTextProperty.SetLineOffset(-Math.Round(_labelFontSize / 7.0));
            //
            foreach (vtkMaxCoorSysSymbol symbol in _symbols) symbol.SetTextProperty(_labelTextProperty);
        }


        // Public methods                                                                                                           
        public void AddSymbol(double[] origin, double[][] axisDirections, string[] labels, double symbolSize)
        {
            double shaftLength = vtkMaxCoorSysGeometry.GetShaftLength(symbolSize);
            UpdateLabelTextProperty(shaftLength);
            //
            vtkMaxCoorSysSymbol symbol = new vtkMaxCoorSysSymbol(origin, axisDirections, labels, shaftLength);
            symbol.SetTextProperty(_labelTextProperty);
            symbol.SetOverrideColor(_overrideColor);
            _symbols.Add(symbol);
            symbol.AddLabelsToRenderer(_renderer);
            //
            _geometryGenerated = false;
        }
        public void Clear()
        {
            foreach (vtkMaxCoorSysSymbol symbol in _symbols) symbol.RemoveLabelsFromRenderer(_renderer);
            _symbols.Clear();
            // Empty the shared poly data so that the removed symbols stop being drawn
            GenerateShafts();
            GenerateEnds();
            //
            _geometryGenerated = false;
        }


        // Public setters                                                                                                           
        public void SetRenderer(vtkRenderer renderer)
        {
            if (_renderer == renderer) return;
            //
            if (_renderer != null)
            {
                _renderer.StartEvt -= renderer_StartEvt;
                foreach (vtkMaxCoorSysSymbol symbol in _symbols) symbol.RemoveLabelsFromRenderer(_renderer);
                _renderer.RemoveActor(_endActor);
                _renderer.RemoveActor(_shaftActor);
            }
            _renderer = renderer;
            if (_renderer != null)
            {
                _renderer.AddActor(_shaftActor);
                _renderer.AddActor(_endActor);
                foreach (vtkMaxCoorSysSymbol symbol in _symbols) symbol.AddLabelsToRenderer(_renderer);
                _renderer.StartEvt += renderer_StartEvt;
            }
            //
            _geometryGenerated = false;
        }
        public void SetOverrideColor(Color color)
        {
            _overrideColor = new double[] { color.R / 255d, color.G / 255d, color.B / 255d };
            //
            foreach (vtkMaxCoorSysSymbol symbol in _symbols) symbol.SetOverrideColor(_overrideColor);
            //
            _geometryGenerated = false;
        }
        public void SetTextProperty(vtkTextProperty textProperty)
        {
            _textProperty = textProperty;
            _labelTextProperty = null;
            _labelFontSize = 0;
            _labelSizes.Clear();
        }
    }
}
