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
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.Drawing;

namespace PrePoMax.Forms
{
    [Serializable]
    [PmxType(10310)]
    public class ViewColor
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Color _color;
        [PmxKey(1)] private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [Category("Appearance")]
        [OrderedDisplayName(0, 10, "Color")]
        [Description("Select color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        public Color Color
        {
            get { return _color; }
            set
            {
                _color = Color.FromArgb(Math.Max((byte)25, value.A), value);
            }
        }


        // Constructors                                                                                                             
        public ViewColor(Color color)
        {
            _color = color;
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public Color GetColor()
        {
            return _color;
        }
    }
}
