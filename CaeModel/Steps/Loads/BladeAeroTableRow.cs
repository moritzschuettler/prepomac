using System;
using CaeGlobals;


namespace CaeModel
{
    // Meaning of the first table/CSV column
    public enum BladeAeroDistanceTypeEnum
    {
        Absolute = 0,   // distance along the spine measured from the origin, in model units
        Relative = 1    // fraction of the spine length: 0-1, or 0-100 when any value is larger than 1
    }


    [PmxType(50042)]
    public class BladeAeroTableRow
    {
        // Properties
        [field: PmxKey(0)] public double DistanceRatio { get; set; }
        [field: PmxKey(1)] public double Force { get; set; }


        // Constructors
        public BladeAeroTableRow()
        {
        }
        public BladeAeroTableRow(double distanceRatio, double force)
        {
            DistanceRatio = distanceRatio;
            Force = force;
        }
    }
}
