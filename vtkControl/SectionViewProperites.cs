// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;
using System.Linq;

namespace vtkControl
{
    internal class SectionViewData
    {
        // Properties                                                                                                               
        public bool Active;
        public double[] Point;
        public double[] Normal;
        public bool LightenColors;
        public Color SectionColor;
        public bool ElementWise;
        public bool Hatch;
        public double HatchSpacing;


        // Constructors                                                                                                             
        public SectionViewData()
        {
            Reset();
        }
        public SectionViewData(SectionViewData sectionViewData)
        {
            Active = sectionViewData.Active;
            Point = sectionViewData.Point?.ToArray();
            Normal = sectionViewData.Normal?.ToArray();
            LightenColors = sectionViewData.LightenColors;
            SectionColor = sectionViewData.SectionColor;
            ElementWise = sectionViewData.ElementWise;
            Hatch = sectionViewData.Hatch;
            HatchSpacing = sectionViewData.HatchSpacing;
        }


        // Methods                                                                                                                  
        public void Reset()
        {
            Active = false;
            Point = null;
            Normal = null;
            LightenColors = true;
            SectionColor = Color.Empty;
            ElementWise = false;
            Hatch = false;
            HatchSpacing = 0;
        }
        public SectionViewData DeepCopy()
        {
            return new SectionViewData(this);
        }
    }
}
