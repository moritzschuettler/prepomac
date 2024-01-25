using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeGlobals
{
    [Serializable]
    public class CompareVec3D : IEqualityComparer<Vec3D>
    {
        // Variables                                                                                                                
        private double _angleRad;
        private Vec3D _zero;


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public CompareVec3D(double angleDeg)
        {
            _angleRad = angleDeg * Math.PI / 180;
            _zero = new Vec3D();
        }


        // Methods                                                                                                                  
        public bool Equals(Vec3D v1, Vec3D v2)
        {
            double angle = Vec3D.GetAngleAtP2Deg(v1, _zero, v2);
            if (Math.Abs(angle) < _angleRad) return true;
            return false;
        }
        //
        public int GetHashCode(Vec3D v)
        {
            int hash = 23;
            hash = hash * 31 + v.X.GetHashCode();
            hash = hash * 31 + v.Y.GetHashCode();
            hash = hash * 31 + v.Z.GetHashCode();
            return hash;
        }
    }
}
