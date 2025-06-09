using System;
using System.Collections.Generic;
using System.Text;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeModel
{
    [Serializable]
    public abstract class MappedDistribution : Distribution
    {
        // Variables                                                                                                                
        private double _scaleX;
        private double _scaleY;
        private double _scaleZ;
        private double _translateX;
        private double _translateY;
        private double _translateZ;
        protected double[][] _coorValues;


        // Properties                                                                                                               
        public double ScaleX { get { return _scaleX; } set { _scaleX = value; } }
        public double ScaleY { get { return _scaleY; } set { _scaleY = value; } }
        public double ScaleZ { get { return _scaleZ; } set { _scaleZ = value; } }
        public double TranslateX { get { return _translateX; } set { _translateX = value; } }
        public double TranslateY { get { return _translateY; } set { _translateY = value; } }
        public double TranslateZ { get { return _translateZ; } set { _translateZ = value; } }
        public double[][] CoorValues { get { return _coorValues; } set { _coorValues = value; } }


        // Constructors                                                                                                             
        public MappedDistribution(string name)
            : base(name)
        {
            Reset();
        }


        // Methods                                                                                                                  
        private void Reset()
        {
            _scaleX = 1.0;
            _scaleY = 1.0;
            _scaleZ = 1.0;
            _translateX = 0;
            _translateY = 0;
            _translateZ = 0;
        }
    }
}
