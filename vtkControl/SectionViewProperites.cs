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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vtkControl
{
    internal class SectionViewData
    {
        // Properties                                                                                                               
        public bool Active;
        public vtkPlane Plane;
        public bool LightenColors;
        public Color SectionColor;


        // Constructors                                                                                                             
        public SectionViewData()
        {
            Reset();
        }
        public SectionViewData(SectionViewData sectionViewData)
        {
            Active = sectionViewData.Active;
            Plane = vtkPlane.New();
            double[] origin = sectionViewData.Plane.GetOrigin();
            double[] normal = sectionViewData.Plane.GetNormal();
            Plane.SetOrigin(origin[0], origin[1], origin[2]);
            Plane.SetNormal(normal[0], normal[1], normal[2]);
            LightenColors = sectionViewData.LightenColors;
            SectionColor = sectionViewData.SectionColor;
        }


        // Methods                                                                                                                  
        public void Reset()
        {
            Active = false;
            Plane = null;
            LightenColors = true;
            SectionColor = Color.Empty;
        }
        public SectionViewData DeepCopy()
        {
            return new SectionViewData(this);
        }
    }
}
