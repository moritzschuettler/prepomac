using System;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(50041)]
    public class BladeAeroSection
    {
        // Variables
        [PmxKey(0)] private int _index;
        [PmxKey(1)] private double _distance;
        [PmxKey(2)] private double _force;
        [PmxKey(3)] private int[] _nodeIds;
        [PmxKey(4)] private double[] _barycenter;
        [PmxKey(5)] private double[] _forceDir;


        // Properties
        public int Index { get { return _index; } set { _index = value; } }
        public double Distance { get { return _distance; } set { _distance = value; } }
        public double Force { get { return _force; } set { _force = value; } }
        public int[] NodeIds { get { return _nodeIds; } set { _nodeIds = value; } }
        public double[] Barycenter { get { return _barycenter; } set { _barycenter = value; } }
        public double[] ForceDir { get { return _forceDir; } set { _forceDir = value; } }
        //
        public string NodeSetName { get { return string.Format("_BladeAero-{0:D2}-Nodes", _index); } }
        public string RefPointName { get { return string.Format("BladeAero-{0:D2}-RP", _index); } }
        public string ConstraintName { get { return string.Format("BladeAero-{0:D2}-Coupling", _index); } }
        public string LoadName { get { return string.Format("BladeAero-{0:D2}-Load", _index); } }


        // Constructor
        public BladeAeroSection(int index, double distance, double force, int[] nodeIds,
                                double[] barycenter, double[] forceDir)
        {
            _index = index;
            _distance = distance;
            _force = force;
            _nodeIds = nodeIds;
            _barycenter = barycenter;
            _forceDir = forceDir;
        }
    }
}
