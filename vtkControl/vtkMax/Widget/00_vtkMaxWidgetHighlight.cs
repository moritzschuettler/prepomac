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
    public class vtkMaxWidgetHighlight
    {
        public const double WidgetFillOpacity = 0.05;
        public const double WidgetBorderOpacity = 0.5;
        public const float WidgetBorderLineWidth = 2.0f;
        public const double SurfaceFillOpacity = 0.15;
        //
        public static Color BorderColor { get { return SystemColors.Highlight; } }
        public static Color FillColor { get { return Color.LightSkyBlue; } }
        //
        private const int NumOfCorners = 4;
        //
        // Variables
        private vtkPolyDataMapper2D _borderMapper;
        private vtkPolyData _borderPolyData;
        private vtkActor2D _borderActor;
        private vtkPolyDataMapper2D _fillMapper;
        private vtkPolyData _fillPolyData;
        private vtkActor2D _fillActor;
        private vtkPoints _cornerPoints;
        private double[][] _corners;
        private double _fillOpacity;


        // Constructors
        public vtkMaxWidgetHighlight()
        {
            _cornerPoints = vtkPoints.New();
            for (int i = 0; i < NumOfCorners; i++) _cornerPoints.InsertNextPoint(0, 0, 0);
            //
            vtkCellArray lines = vtkCellArray.New();
            for (int i = 0; i < NumOfCorners; i++)
            {
                lines.InsertNextCell(2);
                lines.InsertCellPoint(i);
                lines.InsertCellPoint((i + 1) % NumOfCorners);
            }
            //
            _borderPolyData = vtkPolyData.New();
            _borderPolyData.SetPoints(_cornerPoints);
            _borderPolyData.SetLines(lines);
            //
            _borderMapper = vtkPolyDataMapper2D.New();
            _borderMapper.SetInput(_borderPolyData);
            //
            vtkProperty2D borderProperty = vtkProperty2D.New();
            Color borderColor = BorderColor;
            borderProperty.SetColor(borderColor.R / 255.0, borderColor.G / 255.0, borderColor.B / 255.0);
            borderProperty.SetOpacity(WidgetBorderOpacity);
            borderProperty.SetLineWidth(WidgetBorderLineWidth);
            //
            _borderActor = vtkActor2D.New();
            _borderActor.SetMapper(_borderMapper);
            _borderActor.SetProperty(borderProperty);
            _borderActor.VisibilityOff();
            //
            vtkCellArray polygon = vtkCellArray.New();
            polygon.InsertNextCell(NumOfCorners);
            for (int i = 0; i < NumOfCorners; i++) polygon.InsertCellPoint(i);
            //
            _fillPolyData = vtkPolyData.New();
            _fillPolyData.SetPoints(_cornerPoints);
            _fillPolyData.SetPolys(polygon);
            //
            _fillMapper = vtkPolyDataMapper2D.New();
            _fillMapper.SetInput(_fillPolyData);
            //
            vtkProperty2D fillProperty = vtkProperty2D.New();
            Color fillColor = FillColor;
            fillProperty.SetColor(fillColor.R / 255.0, fillColor.G / 255.0, fillColor.B / 255.0);
            fillProperty.SetOpacity(WidgetFillOpacity);
            //
            _fillActor = vtkActor2D.New();
            _fillActor.SetMapper(_fillMapper);
            _fillActor.SetProperty(fillProperty);
            _fillActor.VisibilityOff();
            //
            _corners = null;
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
            if (rectangle == Rectangle.Empty) return Clear();
            //
            return SetCorners(new double[][] { new double[] { rectangle.Left, rectangle.Top },
                                               new double[] { rectangle.Right, rectangle.Top },
                                               new double[] { rectangle.Right, rectangle.Bottom },
                                               new double[] { rectangle.Left, rectangle.Bottom } }, fillOpacity);
        }
        public bool SetCorners(double[][] corners, double fillOpacity)
        {
            if (corners == null) return Clear();
            if (AreCornersEqual(_corners, corners) && Math.Abs(fillOpacity - _fillOpacity) < 1E-12) return false;
            //
            _corners = corners;
            _fillOpacity = fillOpacity;
            _fillActor.GetProperty().SetOpacity(_fillOpacity);
            //
            for (int i = 0; i < NumOfCorners; i++) _cornerPoints.SetPoint(i, _corners[i][0], _corners[i][1], 0);
            _cornerPoints.Modified();
            _borderPolyData.Modified();
            _fillPolyData.Modified();
            _fillActor.VisibilityOn();
            _borderActor.VisibilityOn();
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
            if (_corners == null) return false;
            //
            _corners = null;
            _borderActor.VisibilityOff();
            _fillActor.VisibilityOff();
            //
            return true;
        }


        // Private methods
        private static bool AreCornersEqual(double[][] first, double[][] second)
        {
            if (first == null || second == null) return first == second;
            //
            for (int i = 0; i < NumOfCorners; i++)
            {
                if (first[i][0] != second[i][0] || first[i][1] != second[i][1]) return false;
            }
            //
            return true;
        }
    }
}
