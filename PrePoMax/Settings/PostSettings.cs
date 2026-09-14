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
using System.ComponentModel;
using CaeGlobals;
using CaeResults;
using System.IO;
using DynamicTypeDescriptor;
using System.Drawing;

namespace PrePoMax
{

    [Serializable]
    public class PostSettings : ISettings
    {
        // Variables                                                                                                                
        private UndeformedModelTypeEnum _undeformedModelType;
        private Color _undeformedWireModelColor;
        private Color _undeformedSolidModelColor;
        private double _vectorPlotScaleFactor;
        private double _vectorPlotLengthScaleFactor;
        private int _vectorPlotDensity;
        private bool _showMinValueLocation;
        private bool _showMaxValueLocation;
        private int _maxHistoryEntriesToShow;
        private int _maxCurvesPerPlot;


        // Properties                                                                                                               
        public UndeformedModelTypeEnum UndeformedModelType
        {
            get { return _undeformedModelType; }
            set { _undeformedModelType = value; }
        }
        public Color UndeformedModelColor
        {
            get
            {
                // White color is used for times where no undeformed geometry is displayed
                if (_undeformedModelType == UndeformedModelTypeEnum.None) return Color.White;
                else if (_undeformedModelType == UndeformedModelTypeEnum.WireframeBody) return _undeformedWireModelColor;
                else if (_undeformedModelType == UndeformedModelTypeEnum.SolidBody) return _undeformedSolidModelColor;
                else throw new NotSupportedException();
            }
            set
            {
                if (_undeformedModelType == UndeformedModelTypeEnum.None) { }
                else if (_undeformedModelType == UndeformedModelTypeEnum.WireframeBody) _undeformedWireModelColor = value;
                else if (_undeformedModelType == UndeformedModelTypeEnum.SolidBody) _undeformedSolidModelColor = value;
                else throw new NotSupportedException();
            }
        }
        public Color UndeformedWireModelColor
        {
            get { return _undeformedWireModelColor; }
            set { _undeformedWireModelColor = value; }
        }
        public Color UndeformedSolidModelColor
        {
            get { return _undeformedSolidModelColor; }
            set { _undeformedSolidModelColor = value; }
        }
        public double VectorPlotScaleFactor
        {
            get { return _vectorPlotScaleFactor > 0 ? _vectorPlotScaleFactor : 1; }
            set
            {
                _vectorPlotScaleFactor = value;
                if (_vectorPlotScaleFactor <= 0) _vectorPlotScaleFactor = 1;
            }
        }
        public double VectorPlotLengthScaleFactor
        {
            get { return _vectorPlotLengthScaleFactor > 0 ? _vectorPlotLengthScaleFactor : 1; }
            set
            {
                _vectorPlotLengthScaleFactor = value;
                if (_vectorPlotLengthScaleFactor <= 0) _vectorPlotLengthScaleFactor = 1;
            }
        }
        public int VectorPlotDensity
        {
            get { return _vectorPlotDensity; }
            set
            {
                _vectorPlotDensity = value;
                if (_vectorPlotDensity < 0) _vectorPlotDensity = 0;
                else if (_vectorPlotDensity > 100) _vectorPlotDensity = 100;
            }
        }
        public bool ShowMinValueLocation { get { return _showMinValueLocation; } set { _showMinValueLocation = value; } }
        public bool ShowMaxValueLocation { get { return _showMaxValueLocation; } set { _showMaxValueLocation = value; } }
        public int MaxHistoryEntriesToShow
        {
            get { return _maxHistoryEntriesToShow; }
            set
            {
                _maxHistoryEntriesToShow = value;
                if (_maxHistoryEntriesToShow < 1) _maxHistoryEntriesToShow = 1;
            } 
        }
        public int MaxCurvesPerPlot
        {
            get { return _maxCurvesPerPlot; }
            set
            {
                _maxCurvesPerPlot = value;
                if (_maxCurvesPerPlot < 1) _maxCurvesPerPlot = 1;
            }
        }


        // Constructors                                                                                                             
        public PostSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
            VectorPlotScaleFactor = _vectorPlotScaleFactor;
            VectorPlotDensity = _vectorPlotDensity;
            VectorPlotLengthScaleFactor = _vectorPlotLengthScaleFactor;
            MaxCurvesPerPlot = _maxCurvesPerPlot;
        }
        public void Reset()
        {
            _undeformedModelType = UndeformedModelTypeEnum.WireframeBody;
            _undeformedWireModelColor = Color.FromArgb(128, Color.Black);
            _undeformedSolidModelColor = Color.FromArgb(128, Color.Gray);
            _vectorPlotScaleFactor = 1;
            _vectorPlotDensity = 100;
            _vectorPlotLengthScaleFactor = 1;
            //
            _showMinValueLocation = false;
            _showMaxValueLocation = true;
            //
            _maxHistoryEntriesToShow = 1000;
            _maxCurvesPerPlot = 10;
        }
        
    }
}
