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
using Kitware.VTK;
using vtkControl.Native;

namespace vtkControl
{
    public static class vtkMaxWidgetTools
    {
        public static int[] GetTextSize(vtkTextMapper textMapper, vtkRenderer renderer)
        {
            int[] size = new int[2];

            if (renderer != null)
            {
                IntPtr sizeIntPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(2 * 4);
                try
                {
                    textMapper.GetSize(renderer, sizeIntPtr);
                    System.Runtime.InteropServices.Marshal.Copy(sizeIntPtr, size, 0, 2);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(sizeIntPtr);
                }
            }
            else
            {
                size[0] = size[1] = 50;
            }

            return size;
        }
        public static VtkNativeTextStyle GetNativeTextStyle(vtkTextProperty textProperty)
        {
            double[] color = textProperty.GetColor();
            VtkNativeTextStyle style = new VtkNativeTextStyle();
            style.ColorR = color[0];
            style.ColorG = color[1];
            style.ColorB = color[2];
            style.LineOffset = textProperty.GetLineOffset();
            style.LineSpacing = textProperty.GetLineSpacing();
            style.FontSize = textProperty.GetFontSize();
            style.FontFamily = textProperty.GetFontFamily();
            style.Bold = textProperty.GetBold();
            style.Italic = textProperty.GetItalic();
            return style;
        }
    }
}
