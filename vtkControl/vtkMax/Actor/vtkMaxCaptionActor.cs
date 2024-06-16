using Kitware.VTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vtkControl
{
    public class vtkMaxCaptionActor : vtkMaxActor
    {
        // Actors
        private vtkActor2D _caption;
        private double[] _position;
        private double[] _offsetVector;


        // Properties                                                                                                               
        public vtkActor2D Caption { get { return _caption; } set { _caption = value; } }
        public double[] Position { get { return _position; } set { _position = value; } }
        public double[] OffsetVector { get { return _offsetVector; } set { _offsetVector = value; } }
        public override bool VtkMaxActorVisible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                if (_caption != null) _caption.SetVisibility(_visible ? 1 : 0);
            }
        }


        // Constructors                                                                                                             
        public vtkMaxCaptionActor(string name, Color color, vtkActor2D vtkCaptionActor)
            : base()
        {
            _name = name;
            _caption = vtkCaptionActor;
            //
            _actorRepresentation = vtkMaxActorRepresentation.Unknown;
            _backfaceCulling = true;
            _color = color;
            _backfaceColor = Color.Black;
            _colorTable = null;
            _colorContours = false;
            _sectionViewPossible = false;
            _drawOnGeometry = false;
            _useSecondaryHighlightColor = false;
        }
    }
}
