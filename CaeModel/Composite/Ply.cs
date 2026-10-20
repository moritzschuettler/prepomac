// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public enum PropagationPathType
    {
        Natural,
        Rosette
    }

    [Serializable]
    public class Ply : NamedClass
    {
        // Variables
        private string _region;
        private string _material;
        private double _thickness;
        private string _csys;
        private double _rotationAngle;
        private PropagationPathType _propagationType;
        private double[] _propagationSeedPoint;
        private double? _tapeWidth;
        private double[][] _contourPoints;
        private string _orientationRosette;
        private ProcessType? _processType;


        // Properties
        public string Region
        {
            get { return _region; }
            set { _region = value; }
        }

        public string Material
        {
            get { return _material; }
            set { _material = value; }
        }

        public double Thickness
        {
            get { return _thickness; }
            set
            {
                if (value <= 0) throw new CaeException("Ply thickness must be greater than 0.");
                _thickness = value;
            }
        }

        public string Csys
        {
            get { return _csys; }
            set { _csys = value; }
        }

        public double RotationAngle
        {
            get { return _rotationAngle; }
            set { _rotationAngle = value; }
        }

        // Tape placement (AFP) propagation settings - only meaningful when the layup's ProcessType
        // is TapePlacement; each ply/course can have its own seed point, path strategy and tape width
        public PropagationPathType PropagationType
        {
            get { return _propagationType; }
            set { _propagationType = value; }
        }

        // Null until picked/typed by the user; [x, y, z] world coordinates (projected onto the mesh)
        public double[] PropagationSeedPoint
        {
            get { return _propagationSeedPoint; }
            set { _propagationSeedPoint = value; }
        }

        // Null = use the ply material's Width; set to explicitly override it for this ply
        public double? TapeWidth
        {
            get { return _tapeWidth; }
            set { _tapeWidth = value; }
        }

        // Outer boundary polyline in world coordinates; null unless the region was imported from
        // a geometrical surface (Ply region mapping from geometrical surface)
        public double[][] ContourPoints
        {
            get { return _contourPoints; }
            set { _contourPoints = value; }
        }

        // Name of the rosette that defines the material reference direction of this ply; null or
        // empty means that the ply uses the layup rosette when the layup is in rosette mode, or the
        // plain rotation angle otherwise
        public string OrientationRosette
        {
            get { return _orientationRosette; }
            set { _orientationRosette = value; }
        }

        // Manufacturing process of this single ply; null means that the ply follows the process
        // type of its layup. Resolved through CompositeLayup.GetEffectivePlyProcessType.
        public ProcessType? ProcessType
        {
            get { return _processType; }
            set { _processType = value; }
        }


        // Constructors
        public Ply(string name)
            : base(name)
        {
            _region = "";
            _material = "";
            _thickness = 1.0;
            _csys = "<layup>";
            _rotationAngle = 0.0;
            _propagationType = PropagationPathType.Natural;
            _propagationSeedPoint = null;
            _tapeWidth = null;
            _contourPoints = null;
            _orientationRosette = null;
            _processType = null;
        }

        public Ply(string name, string region, string material, double thickness, string csys, double rotationAngle)
            : base(name)
        {
            _region = region;
            _material = material;
            _thickness = thickness;
            _csys = csys;
            _rotationAngle = rotationAngle;
        }


        // Methods
    }
}
