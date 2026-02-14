using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class Ply : NamedClass
    {
        // Variables
        private string _region;
        private string _material;
        private double _thickness;
        private string _csys;
        private double _rotationAngle;


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


        // Constructors
        public Ply(string name)
            : base(name)
        {
            _region = "";
            _material = "";
            _thickness = 1.0;
            _csys = "";
            _rotationAngle = 0.0;
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
