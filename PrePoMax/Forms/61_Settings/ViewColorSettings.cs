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
using DynamicTypeDescriptor;
using System.Drawing;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewColorSettings : IViewSettings, IReset
    {
        // Variables                                                                                                                
        private ColorSettings _colorSettings;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Face orientation")]
        [OrderedDisplayName(0, 10, "Front face color")]
        [DescriptionAttribute("Select the front shell face color.")]
        [Id(1, 1)]
        public Color FrontFaceColor
        {
            get { return _colorSettings.FrontFaceColor; }
            set { _colorSettings.FrontFaceColor = value; }
        }
        //
        [CategoryAttribute("Face orientation")]
        [OrderedDisplayName(1, 10, "Back face color")]
        [DescriptionAttribute("Select the back shell face color.")]
        [Id(2, 1)]
        public Color BackFaceColor
        {
            get { return _colorSettings.BackFaceColor; }
            set { _colorSettings.BackFaceColor = value; }
        }
        //
        [CategoryAttribute("Mesh types")]
        [OrderedDisplayName(0, 10, "Transfinite color")]
        [DescriptionAttribute("Select the color of the parts and shell faces that can be meshed by the transfinite " +
                              "algorithm.")]
        [Id(1, 2)]
        public Color TransfiniteMeshColor
        {
            get { return _colorSettings.TransfiniteMeshColor; }
            set { _colorSettings.TransfiniteMeshColor = value; }
        }
        //
        [CategoryAttribute("Mesh types")]
        [OrderedDisplayName(1, 10, "Sweep color")]
        [DescriptionAttribute("Select the color of the parts that can be swept.")]
        [Id(2, 2)]
        public Color SweepMeshColor
        {
            get { return _colorSettings.SweepMeshColor; }
            set { _colorSettings.SweepMeshColor = value; }
        }
        //
        [CategoryAttribute("Mesh types")]
        [OrderedDisplayName(2, 10, "Revolve color")]
        [DescriptionAttribute("Select the color of the parts that are swept by turning them about an axis they touch.")]
        [Id(3, 2)]
        public Color RevolveMeshColor
        {
            get { return _colorSettings.RevolveMeshColor; }
            set { _colorSettings.RevolveMeshColor = value; }
        }
        //
        [CategoryAttribute("Mesh types")]
        [OrderedDisplayName(3, 10, "Tetrahedral color")]
        [DescriptionAttribute("Select the color of the solid parts that can only be meshed by tetrahedra.")]
        [Id(4, 2)]
        public Color TetrahedralMeshColor
        {
            get { return _colorSettings.TetrahedralMeshColor; }
            set { _colorSettings.TetrahedralMeshColor = value; }
        }
        //
        [CategoryAttribute("Mesh types")]
        [OrderedDisplayName(4, 10, "Free color")]
        [DescriptionAttribute("Select the color of the shell faces that are meshed without a structured grid.")]
        [Id(5, 2)]
        public Color FreeMeshColor
        {
            get { return _colorSettings.FreeMeshColor; }
            set { _colorSettings.FreeMeshColor = value; }
        }
        //
        [CategoryAttribute("Mesh types")]
        [OrderedDisplayName(5, 10, "Other color")]
        [DescriptionAttribute("Select the color of the parts meshed in any other way, such as the stl based parts.")]
        [Id(6, 2)]
        public Color OtherMeshColor
        {
            get { return _colorSettings.OtherMeshColor; }
            set { _colorSettings.OtherMeshColor = value; }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(0, 20, "Color palette")]
        [DescriptionAttribute("Select the color palette to fill in the ten colors. " +
                              "Each color can then be changed individually, which switches the palette to Custom.")]
        [Id(1, 3)]
        public ColorPaletteEnum ColorPalette
        {
            get { return _colorSettings.ColorPalette; }
            set
            {
                _colorSettings.ColorPalette = value;
                _colorSettings.ApplyColorPalette();
                UpdateVisibilities();
            }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(1, 20, "Saturation")]
        [DescriptionAttribute("Set the saturation of the palette colors in percent. A value of 100 keeps the " +
                              "original palette colors, lower values wash them out and brighten them and higher " +
                              "values make them more vivid. Values from 10 to 200 are allowed.")]
        [Id(2, 3)]
        public double Saturation
        {
            get { return _colorSettings.Saturation; }
            set
            {
                _colorSettings.Saturation = value;
                _colorSettings.ApplyColorPalette();
            }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(2, 20, "Color 1")]
        [DescriptionAttribute("Select the first color.")]
        [Id(3, 3)]
        public Color Color1
        {
            get { return _colorSettings.ColorTable[0]; }
            set { SetTableColor(0, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(3, 20, "Color 2")]
        [DescriptionAttribute("Select the second color.")]
        [Id(4, 3)]
        public Color Color2
        {
            get { return _colorSettings.ColorTable[1]; }
            set { SetTableColor(1, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(4, 20, "Color 3")]
        [DescriptionAttribute("Select the third color.")]
        [Id(5, 3)]
        public Color Color3
        {
            get { return _colorSettings.ColorTable[2]; }
            set { SetTableColor(2, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(5, 20, "Color 4")]
        [DescriptionAttribute("Select the fourth color.")]
        [Id(6, 3)]
        public Color Color4
        {
            get { return _colorSettings.ColorTable[3]; }
            set { SetTableColor(3, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(6, 20, "Color 5")]
        [DescriptionAttribute("Select the fifth color.")]
        [Id(7, 3)]
        public Color Color5
        {
            get { return _colorSettings.ColorTable[4]; }
            set { SetTableColor(4, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(7, 20, "Color 6")]
        [DescriptionAttribute("Select the sixth color.")]
        [Id(8, 3)]
        public Color Color6
        {
            get { return _colorSettings.ColorTable[5]; }
            set { SetTableColor(5, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(8, 20, "Color 7")]
        [DescriptionAttribute("Select the seventh color.")]
        [Id(9, 3)]
        public Color Color7
        {
            get { return _colorSettings.ColorTable[6]; }
            set { SetTableColor(6, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(9, 20, "Color 8")]
        [DescriptionAttribute("Select the eight color.")]
        [Id(10, 3)]
        public Color Color8
        {
            get { return _colorSettings.ColorTable[7]; }
            set { SetTableColor(7, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(10, 20, "Color 9")]
        [DescriptionAttribute("Select the ninth color.")]
        [Id(11, 3)]
        public Color Color9
        {
            get { return _colorSettings.ColorTable[8]; }
            set { SetTableColor(8, value); }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(11, 20, "Color 10")]
        [DescriptionAttribute("Select the tenth color.")]
        [Id(12, 3)]
        public Color Color10
        {
            get { return _colorSettings.ColorTable[9]; }
            set { SetTableColor(9, value); }
        }


        // Constructors                                                                                                             
        public ViewColorSettings(ColorSettings colorSettings)
        {
            _colorSettings = colorSettings;
            _dctd = ProviderInstaller.Install(this);
            //
            UpdateVisibilities();
        }


        // Methods                                                                                                                  
        public ISettings GetBase()
        {
            return _colorSettings;
        }

        public void Reset()
        {
            _colorSettings.Reset();
            //
            UpdateVisibilities();
        }
        public void UpdateVisibilities()
        {
            bool visible = _colorSettings.ColorPalette != ColorPaletteEnum.Custom;
            _dctd.GetProperty(nameof(Saturation)).SetIsBrowsable(visible);
        }
        private void SetTableColor(int colorId, Color color)
        {
            _colorSettings.SetTableColor(colorId, color);
            //
            UpdateVisibilities();
        }
    }

}
