using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CaeMesh
{
    [Serializable]
    public struct FeNode
    {
        // Variables                                                                                                                
        public int Id;
        public double X;
        public double Y;
        public double Z;


        // Constructors                                                                                                             
        public FeNode(int id, double x, double y, double z)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
        }
        public FeNode(int id, double[] coor)
            : this(id, coor[0], coor[1], coor[2])
        {
        }
        public FeNode(FeNode node)
            : this(node.Id, node.X, node.Y, node.Z)
        {
        }
       

        // Methods                                                                                                                  
        public void SetCoor(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public double[] Coor
        {
            get
            {
                return new double[] { X, Y, Z };
            }
            set
            {
                X = value[0];
                Y = value[1];
                Z = value[2];
            }
        }
        //
        public void Translate(Vec3D translateVector)
        {
            X += translateVector.X;
            Y += translateVector.Y;
            Z += translateVector.Z;
        }
        public void Rotate(Vec3D center, double[][] matrix)
        {
            // Translate to origin
            Translate(-1 * center);
            // Copy
            double x = X;
            double y = Y;
            double z = Z;
            // Rotate
            X = matrix[0][0] * x + matrix[0][1] * y + matrix[0][2] * z;
            Y = matrix[1][0] * x + matrix[1][1] * y + matrix[1][2] * z;
            Z = matrix[2][0] * x + matrix[2][1] * y + matrix[2][2] * z;
            // Translate to rotation center
            Translate(center);
        }
        public void Mirror(Vec3D pointOnPlane, Vec3D planeNormal)
        {
            // Translate to origin
            Translate(-1 * pointOnPlane);
            // Copy
            double x = X;
            double y = Y;
            double z = Z;
            // Mirror
            double d = planeNormal.X * x + planeNormal.Y * y + planeNormal.Z * z;
            X = x - 2 * d * planeNormal.X;
            Y = y - 2 * d * planeNormal.Y;
            Z = z - 2 * d * planeNormal.Z;
            // Translate to mirror plane
            Translate(pointOnPlane);
        }
        public void Scale(Vec3D center, double[] scaleFactors)
        {
            // Translate to origin
            Translate(-1 * center);
            // Scale
            X *= scaleFactors[0];
            Y *= scaleFactors[1];
            Z *= scaleFactors[2];
            // Translate to scale center
            Translate(center);
        }
        public bool IsEqual(FeNode node, double epsilon = 1E-6)
        {
            //int div = 10000;
            // The <= sign solves the problem when a coordinate equals 0
            //if (Id == node.Id && Math.Abs(X - node.X) <= Math.Abs(X / div) &&
            //                     Math.Abs(Y - node.Y) <= Math.Abs(Y / div) &&
            //                     Math.Abs(Z - node.Z) <= Math.Abs(Z / div))
            if (Id == node.Id && Math.Abs(X - node.X) <= epsilon &&
                                 Math.Abs(Y - node.Y) <= epsilon &&
                                 Math.Abs(Z - node.Z) <= epsilon)
                return true;
            else
                return false;
        }
        public FeNode DeepCopy()
        {
            return new FeNode(Id, X, Y, Z);
        }
    }
}
