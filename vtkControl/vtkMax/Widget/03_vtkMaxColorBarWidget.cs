// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using vtkControl.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;


namespace vtkControl
{
    class vtkMaxColorBarWidget : vtkMaxTextWidget
    {
        // Variables
        private Color[] _colors;
        private string[] _labels;


        // Properties
        public Color[] Colors {get {return _colors;} }
        public string[] Labels { get { return _labels; } }


        // Constructors
        public vtkMaxColorBarWidget()
        {
            _backgroundVisibility = false;
            _borderVisibility = false;
            //
            SetColorsAndLabels(null, null);
        }


        // Public setters
        public void ClearColorsAndLabels()
        {
            SetColorsAndLabels(null, null);
        }
        public void SetColorsAndLabels(Color[] colors, string[] labels)
        {
            if (colors != null) _colors = colors.ToArray();
            else _colors = new Color[0];
            if (labels != null) _labels = labels.ToArray();
            else _labels = new string[0];
            // Replace _ and - between letters for the space char
            for (int i = 0; i < _labels.Length; i++)
                _labels[i] = Regex.Replace(_labels[i], @"(?<=\p{L})[-_](?=\p{L})", " ");
            //
            _nativeMirror?.SetColorsAndLabels(_colors, _labels);
        }
        public void AddColorsAndLabels(Color[] colors, string[] labels)
        {
            if (colors != null && labels != null)
            {
                List<Color> allColors = new List<Color>(_colors);
                List<string> allLabels = new List<string>(_labels);
                //
                allColors.AddRange(colors);
                allLabels.AddRange(labels);
                //
                SetColorsAndLabels(allColors.ToArray(), allLabels.ToArray());
            }
        }
        public bool TryGetTitleRectangle(out Rectangle rectangle)
        {
            rectangle = Rectangle.Empty;
            return _nativeMirror != null && _nativeMirror.GetColorBarTitleRectangle(out rectangle);
        }
        protected override void CopyStateToNativeMirror(VtkNativeWidget nativeMirror)
        {
            nativeMirror.SetColorsAndLabels(_colors, _labels);
            base.CopyStateToNativeMirror(nativeMirror);
        }
    }
}
