// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax
{
    // What a picture of the viewport is made of. The toolbar button decides the same three values on
    // its own, so the picture it takes and the one the dialog takes come out of one renderer call.
    [Serializable]
    public class CaptureViewportProperties
    {
        // Variables                                                                                                                
        private string _fileName;
        private bool _transparentBackground;
        private int _magnification;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public bool TransparentBackground
        {
            get { return _transparentBackground; }
            set { _transparentBackground = value; }
        }
        // The renderer draws a magnified picture in tiles, so the count is kept to what it will draw
        public int Magnification
        {
            get { return _magnification; }
            set
            {
                _magnification = value;
                if (_magnification < 1) _magnification = 1;
                else if (_magnification > vtkControl.vtkControl.MaxImageMagnification)
                    _magnification = vtkControl.vtkControl.MaxImageMagnification;
            }
        }


        // Constructors                                                                                                             
        public CaptureViewportProperties(string fileName)
        {
            _fileName = fileName;
            _transparentBackground = true;
            _magnification = 1;
        }
    }
}
