// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Drawing;

namespace vtkControl
{
    // A caption or the labels of a layer: the world position of its text and the screen offset it is moved by
    public class vtkMaxCaptionActor : vtkMaxActor
    {
        // Variables
        private double[] _position;
        private double[] _offsetVector;


        // Properties
        public double[] Position { get { return _position; } set { _position = value; } }
        public double[] OffsetVector { get { return _offsetVector; } set { _offsetVector = value; } }


        // Constructors
        public vtkMaxCaptionActor(string name, Color color)
            : base()
        {
            _name = name;
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
