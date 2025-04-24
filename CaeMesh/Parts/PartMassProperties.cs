using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public struct PartMassProperties
    {
        public double Volume;
        public double Area;
        public double[] CenterOfMass;
        public double[][] InertiaMatrixCG;
        public double[][] InertiaMatrixOrigin;


        public PartMassProperties(bool initialize)
        {
            Volume = 0;
            Area = 0;
            if (initialize)
            {
                CenterOfMass = new double[3];
                InertiaMatrixCG = new double[3][];
                InertiaMatrixOrigin = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    InertiaMatrixCG[i] = new double[3];
                    InertiaMatrixOrigin[i] = new double[3];
                }
            }
            else
            {
                CenterOfMass = null;
                InertiaMatrixCG = null;
                InertiaMatrixOrigin = null;
            }
        }
        //public PartMassProperties(PartMassProperties massProperties)
        //{
        //    PartMassProperties copy = new PartMassProperties(true);
        //    copy.Volume = massProperties.Volume;
        //    copy.Area = massProperties.Area;
        //    if (massProperties.CenterOfMass != null) copy.CenterOfMass = new Vec3D(massProperties.CenterOfMass);
        //    if (massProperties.InertiaMatrixCG != null)
        //    {
        //        copy.InertiaMatrixCG[0] = massProperties.InertiaMatrixCG[0].ToArray();
        //        copy.InertiaMatrixCG[1] = massProperties.InertiaMatrixCG[1].ToArray();
        //        copy.InertiaMatrixCG[2] = massProperties.InertiaMatrixCG[2].ToArray();
        //    }
        //    if (massProperties.InertiaMatrixOrigin != null)
        //    {
        //        copy.InertiaMatrixOrigin[0] = massProperties.InertiaMatrixOrigin[0].ToArray();
        //        copy.InertiaMatrixOrigin[1] = massProperties.InertiaMatrixOrigin[1].ToArray();
        //        copy.InertiaMatrixOrigin[2] = massProperties.InertiaMatrixOrigin[2].ToArray();
        //    }
        //    return copy;
        //}
    }
}
