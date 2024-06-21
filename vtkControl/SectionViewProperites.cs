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
        public bool Active;
        public vtkPlane Plane;
        public bool LightenColors;
        public Color SectionColor;


        public SectionViewData()
        {
            Reset();
        }


        public void Reset()
        {
            Active = false;
            Plane = null;
            LightenColors = true;
            SectionColor = Color.Empty;
        }
    }
}
