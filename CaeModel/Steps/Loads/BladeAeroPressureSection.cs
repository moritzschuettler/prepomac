using System;
using System.Collections.Generic;
using CaeMesh;

namespace CaeModel
{
    [Serializable]
    public class BladeAeroPressureSection
    {
        // Variables
        private int _index;
        private double _distance;
        private double _pressure;
        private double _zoneArea;
        // One int[] per FeFaceName so element sets can be created per face type
        private Dictionary<FeFaceName, int[]> _faceGroups;


        // Properties
        public int Index { get { return _index; } }
        public double Distance { get { return _distance; } }
        public double Pressure { get { return _pressure; } }
        public double ZoneArea { get { return _zoneArea; } }
        public Dictionary<FeFaceName, int[]> FaceGroups { get { return _faceGroups; } }

        // Generated names — internal prefix matches the existing BladeAero convention
        public string ElemSetName(FeFaceName f)
        {
            return string.Format("_BladeAeroP-{0:D2}-{1}-Elems", _index, f);
        }
        public string SurfaceName
        {
            get { return string.Format("_BladeAeroP-{0:D2}-Surf", _index); }
        }
        public string LoadName
        {
            get { return string.Format("BladeAeroP-{0:D2}-Load", _index); }
        }


        // Constructor
        public BladeAeroPressureSection(int index, double distance, double pressure, double zoneArea,
                                        Dictionary<FeFaceName, int[]> faceGroups)
        {
            _index = index;
            _distance = distance;
            _pressure = pressure;
            _zoneArea = zoneArea;
            _faceGroups = faceGroups;
        }
    }
}
