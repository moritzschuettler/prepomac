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
    }
}
