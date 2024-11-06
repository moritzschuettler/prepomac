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
        private vtkActor2D _captionActor;
        private vtkActor _tmpActor; // solves a bug in vtk when no text is shown if there are no other actors in the renderer
        private double[] _position;
        private double[] _offsetVector;


        // Properties                                                                                                               
        public vtkActor2D CaptionActor { get { return _captionActor; } set { _captionActor = value; } }
        public vtkActor TmpActor { get { return _tmpActor; } set { _tmpActor = value; } }
        public double[] Position { get { return _position; } set { _position = value; } }
        public double[] OffsetVector { get { return _offsetVector; } set { _offsetVector = value; } }
        public override bool VtkMaxActorVisible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                if (_captionActor != null) _captionActor.SetVisibility(_visible ? 1 : 0);
            }
        }


        // Constructors                                                                                                             
        public vtkMaxCaptionActor(string name, Color color, vtkActor2D captionActor, vtkActor tmpActor)
            : base()
        {
            _name = name;
            _captionActor = captionActor;
            _tmpActor = tmpActor;
            //
            _actorRepresentation = vtkMaxActorRepresentation.Unknown;
            _backfaceCulling = true;
            _color = color;
            _backfaceColor = Color.Black;
            _colorTable = null;
            _colorContours = false;
            _sectionViewPossible = false;
            _drawOnGeometry = false;
        }
    }
}
