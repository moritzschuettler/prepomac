// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using Kitware.VTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vtkControl
{
    public class vtkMaxLookupTable
    {
        // Variables                                                                                                                
        private double _minValue;
        private double _maxValue;
        private Color[] _colors;


        // Properties
        public Color[] Colors { get { return _colors; } set { _colors = value; } }


        // Constructors                                                                                                             
        public vtkMaxLookupTable()
        {
            _minValue = 0.0;
            _maxValue = 0.0;
            _colors = new Color[0];
        }
        public vtkMaxLookupTable(vtkLookupTable lookupTable)
        {
            _minValue = lookupTable.GetTableRange()[0];
            _maxValue = lookupTable.GetTableRange()[1];
            
            int numberOfColors = (int)lookupTable.GetNumberOfColors();
            _colors = new Color[numberOfColors];
            for (int i = 0; i < numberOfColors; i++)
            {
                double[] rgba = lookupTable.GetTableValue(i);
                _colors[i] = Color.FromArgb(
                    (int)(rgba[3] * 255),
                    (int)(rgba[0] * 255),
                    (int)(rgba[1] * 255),
                    (int)(rgba[2] * 255)
                );
            }
        }

        
        // Methods                                                                                                                  
        public double GetNormalizedValue(double value)
        {
            if (value <= _minValue) return 0.0;
            if (value >= _maxValue) return 1.0;
            double normalizedValue = (value - _minValue) / (_maxValue - _minValue);
            return normalizedValue;
        }
        public int GetColorIndex(double value)
        {
            if (value <= _minValue) return 0;
            if (value >= _maxValue) return _colors.Length - 1;
            double normalizedValue = (value - _minValue) / (_maxValue - _minValue);
            int colorIndex = (int)(normalizedValue * _colors.Length);
            return colorIndex;
        }
        public void WriteAsTexture(int width, int height, string filePath)
        {
            using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                //
                Pen pen;
                float delta = (float)height / _colors.Length;
                for (int i = 0; i < _colors.Length; i++)
                {
                    pen = new Pen(_colors[i], width * 2);
                    g.DrawLine(pen, width / 2, height - i * delta, width / 2, height - (i + 1) * delta);
                }
                //
                bmp.Save(filePath, ImageFormat.Png);
            }
        }
    }
}
