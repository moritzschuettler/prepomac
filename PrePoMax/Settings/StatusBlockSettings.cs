// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    [PmxType(10544)]
    public class StatusBlockSettings : ISettings
    {
        // Variables                                                                                                                
        [PmxKey(0)] private bool _visible;
        [PmxKey(1)] private AnnotationBackgroundType _backgroundType;
        [PmxKey(2)] private bool _drawBorder;


        // Properties                                                                                                               
        public bool Visible { get { return _visible; } set { _visible = value; } }
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
        public StatusBlockSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _visible = true;
            _backgroundType = AnnotationBackgroundType.None;
            _drawBorder = true;
        }
    }
}
