using System;


namespace CaeModel
{
    // Meaning of the first table/CSV column
    public enum BladeAeroDistanceTypeEnum
    {
        Absolute = 0,   // distance along the spine measured from the origin, in model units
        Relative = 1    // fraction of the spine length: 0-1, or 0-100 when any value is larger than 1
    }


    [Serializable]
    public class BladeAeroTableRow
    {
        // Properties
        public double DistanceRatio { get; set; }
        public double Force { get; set; }


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
