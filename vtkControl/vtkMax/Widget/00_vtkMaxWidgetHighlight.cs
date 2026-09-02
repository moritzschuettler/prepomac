// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;
using Kitware.VTK;


namespace vtkControl
{
    class vtkMaxWidgetHighlight
    {
        public const double WidgetFillOpacity = 0.05;
        public const double WidgetBorderOpacity = 0.5;
        //
        // Variables
        private vtkPolyDataMapper2D _borderMapper;
        private vtkActor2D _borderActor;
        private vtkPolyDataMapper2D _fillMapper;
        private vtkActor2D _fillActor;
        private Rectangle _rectangle;
        private double _fillOpacity;


        // Constructors
        public vtkMaxWidgetHighlight()
        {
            vtkPoints points = vtkPoints.New();
            for (int i = 0; i < 4; i++) points.InsertNextPoint(0, 0, 0);
            //
            vtkCellArray lines = vtkCellArray.New();
            for (int i = 0; i < 4; i++)
            {
                lines.InsertNextCell(2);
                lines.InsertCellPoint(i);
                lines.InsertCellPoint((i + 1) % 4);
            }
            //
            vtkPolyData borderPolyData = vtkPolyData.New();
            borderPolyData.SetPoints(points);
            borderPolyData.SetLines(lines);
            //
            _borderMapper = vtkPolyDataMapper2D.New();
            _borderMapper.SetInput(borderPolyData);
            //
            vtkProperty2D borderProperty = vtkProperty2D.New();
            Color borderColor = SystemColors.Highlight;
            borderProperty.SetColor(borderColor.R / 255.0, borderColor.G / 255.0, borderColor.B / 255.0);
            borderProperty.SetOpacity(WidgetBorderOpacity);
            borderProperty.SetLineWidth(2.0f);
            //
            _borderActor = vtkActor2D.New();
            _borderActor.SetMapper(_borderMapper);
            _borderActor.SetProperty(borderProperty);
            _borderActor.VisibilityOff();
            //
            vtkCellArray polygon = vtkCellArray.New();
            polygon.InsertNextCell(4);
            for (int i = 0; i < 4; i++) polygon.InsertCellPoint(i);
            //
            vtkPolyData fillPolyData = vtkPolyData.New();
            fillPolyData.SetPoints(points);
            fillPolyData.SetPolys(polygon);
            //
            _fillMapper = vtkPolyDataMapper2D.New();
            _fillMapper.SetInput(fillPolyData);
            //
            vtkProperty2D fillProperty = vtkProperty2D.New();
            Color fillColor = Color.LightSkyBlue;
            fillProperty.SetColor(fillColor.R / 255.0, fillColor.G / 255.0, fillColor.B / 255.0);
            fillProperty.SetOpacity(WidgetFillOpacity);
            //
            _fillActor = vtkActor2D.New();
            _fillActor.SetMapper(_fillMapper);
            _fillActor.SetProperty(fillProperty);
            _fillActor.VisibilityOff();
            //
            _rectangle = Rectangle.Empty;
            _fillOpacity = WidgetFillOpacity;
        }


        // Public methods
        public void AddToRenderer(vtkRenderer renderer)
        {
            renderer.AddActor(_fillActor);
            renderer.AddActor(_borderActor);
            Clear();
        }
        public void RemoveFromRenderer(vtkRenderer renderer)
        {
            renderer.RemoveActor(_borderActor);
            renderer.RemoveActor(_fillActor);
            Clear();
        }
        public bool SetRectangle(Rectangle rectangle, double fillOpacity)
        {
            if (rectangle == Rectangle.Empty && _rectangle == Rectangle.Empty) return false;
            if (rectangle == _rectangle && Math.Abs(fillOpacity - _fillOpacity) < 1E-12) return false;
            //
            _rectangle = rectangle;
            if (rectangle == Rectangle.Empty)
            {
                _borderActor.VisibilityOff();
                _fillActor.VisibilityOff();
            }
            else
            {
                _fillOpacity = fillOpacity;
                _fillActor.GetProperty().SetOpacity(_fillOpacity);
                //
                vtkPoints points = _borderMapper.GetInput().GetPoints();
                points.SetPoint(0, rectangle.Left, rectangle.Top, 0);
                points.SetPoint(1, rectangle.Right, rectangle.Top, 0);
                points.SetPoint(2, rectangle.Right, rectangle.Bottom, 0);
                points.SetPoint(3, rectangle.Left, rectangle.Bottom, 0);
                points.Modified();
                _borderMapper.GetInput().Modified();
                _fillMapper.GetInput().Modified();
                _fillActor.VisibilityOn();
                _borderActor.VisibilityOn();
            }
            //
            return true;
        }
        public void BringToFront(vtkRenderer renderer)
        {
            renderer.RemoveActor(_fillActor);
            renderer.RemoveActor(_borderActor);
            renderer.AddActor(_fillActor);
            renderer.AddActor(_borderActor);
        }
        public bool Clear()
        {
            if (_rectangle == Rectangle.Empty) return false;
            //
            _rectangle = Rectangle.Empty;
            _borderActor.VisibilityOff();
            _fillActor.VisibilityOff();
            return true;
        }
    }
}
