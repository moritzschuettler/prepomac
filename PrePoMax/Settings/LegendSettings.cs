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
using System.IO;
using DynamicTypeDescriptor;
using System.Runtime.InteropServices;

namespace PrePoMax
{
    [Serializable]
    public enum AnnotationBackgroundType
    {
        None,
        White
    }


    [Serializable]
    public class LegendSettings : ISettings
    {
        // Variables                                                                                                                
        private vtkControl.vtkMaxColorSpectrum _colorSpectrum;
        private AnnotationBackgroundType _backgroundType;
        private bool _drawBorder;


        // Properties                                                                                                               
        public vtkControl.vtkMaxColorSpectrum ColorSpectrum { get { return _colorSpectrum; } set { _colorSpectrum = value; } }
        public AnnotationBackgroundType BackgroundType
        {
            get { return _backgroundType; }
            set
            {
                if (value != _backgroundType)
                {
                    _backgroundType = value;
                    if (_backgroundType == AnnotationBackgroundType.White) _drawBorder = true;
                }
            }
        }
        public bool DrawBorder { get { return _drawBorder; } set { _drawBorder = value; } }


        // Constructors                                                                                                             
        public LegendSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _colorSpectrum = new vtkControl.vtkMaxColorSpectrum();
            //
            _backgroundType = AnnotationBackgroundType.None;
            _drawBorder = false;
        }
    }
}
