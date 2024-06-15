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
        public vtkMaxCaptionActor(vtkMaxActorData data, vtkActor2D caption)
            : base()
        {
            _name = data.Name;
            _caption = caption;
            //
            _actorRepresentation = data.ActorRepresentation;
            _backfaceCulling = data.BackfaceCulling;
            _color = data.Color;
            _backfaceColor = data.BackfaceColor;
            _colorTable = data.ColorTable;
            _ambient = data.Ambient;
            _diffuse = data.Diffuse;
            _colorContours = data.ColorContours;
            _sectionViewPossible = data.SectionViewPossible;
            _drawOnGeometry = data.DrawOnGeometry;
            _useSecondaryHighlightColor = data.UseSecondaryHighlightColor;
            //
            UpdateColor();
        }
    }
}
