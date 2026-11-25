using System;
using System.Collections.Generic;
using CaeGlobals;

namespace CaeMesh
{
    [PmxType(50000)]
    public class FeElementSetFromBox : FeElementSet
    {
        // Variables
        public enum BoxSelectionMode { All, FromBoundingBox }
        //
        [PmxKey(0)] private string _partName;
        [PmxKey(1)] private BoxSelectionMode _mode;
        [PmxKey(2)] private double _centerX;
        [PmxKey(3)] private double _centerY;
        [PmxKey(4)] private double _centerZ;
        [PmxKey(5)] private double _sizeX;
        [PmxKey(6)] private double _sizeY;
        [PmxKey(7)] private double _sizeZ;


        // Properties
        public string PartName { get { return _partName; } set { _partName = value; } }
        public BoxSelectionMode Mode { get { return _mode; } set { _mode = value; } }
        public double CenterX { get { return _centerX; } set { _centerX = value; } }
        public double CenterY { get { return _centerY; } set { _centerY = value; } }
        public double CenterZ { get { return _centerZ; } set { _centerZ = value; } }
        public double SizeX { get { return _sizeX; } set { _sizeX = value; } }
        public double SizeY { get { return _sizeY; } set { _sizeY = value; } }
        public double SizeZ { get { return _sizeZ; } set { _sizeZ = value; } }


        // Constructors
        public FeElementSetFromBox(string name, string partName, BoxSelectionMode mode,
                                   double centerX, double centerY, double centerZ,
                                   double sizeX, double sizeY, double sizeZ)
            : base(name, null)
        {
            _partName = partName;
            _mode = mode;
            _centerX = centerX;
            _centerY = centerY;
            _centerZ = centerZ;
            _sizeX = sizeX;
            _sizeY = sizeY;
            _sizeZ = sizeZ;
        }
        public FeElementSetFromBox(FeElementSetFromBox elementSet)
            : base(elementSet)
        {
            _partName = elementSet._partName;
            _mode = elementSet._mode;
            _centerX = elementSet._centerX;
            _centerY = elementSet._centerY;
            _centerZ = elementSet._centerZ;
            _sizeX = elementSet._sizeX;
            _sizeY = elementSet._sizeY;
            _sizeZ = elementSet._sizeZ;
        }
    }
}
