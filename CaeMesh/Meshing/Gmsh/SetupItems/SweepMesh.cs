using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeMesh.Meshing;
using DynamicTypeDescriptor;
using GmshCommon;

namespace CaeMesh
{
    [Serializable]
    public class SweepMesh : GmshSetupItem, ISerializable
    {
        // Variables                                                                                                                
        private double[] _direction;                                    // ISerializable
        private double[] _sweepCenter;                                  // ISerializable
        private int[] _sideSurfaceIds;                                  // ISerializable


        // Properties                                                                                                               
        public double[] Direction { get { return _direction; } set { _direction = value; } }
        public double[] SweepCenter { get { return _sweepCenter; } set { _sweepCenter = value; } }
        public int[] SideSurfaceIds { get { return _sideSurfaceIds; } set { _sideSurfaceIds = value; } }


        // Constructors                                                                                                             
        public SweepMesh(string name)
            : base(name)
        {
            Reset();
        }
        public SweepMesh(ExtrudeMesh extrudeMesh)
            : base("tmpName")
        {
            CopyFrom(extrudeMesh);
        }
        public SweepMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_direction":
                        _direction = (double[])entry.Value; break;
                    case "_sweepCenter":
                        _sweepCenter = (double[])entry.Value; break;
                    case "_sideSurfaceIds":
                        _sideSurfaceIds = (int[])entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            base.Reset();
            //
            _direction = null;
            _sweepCenter = null;
        }
        public void CopyFrom(SweepMesh sweepMesh)
        {
            base.CopyFrom(sweepMesh);
            //
            if (_direction != null) _direction = sweepMesh._direction.ToArray();
            if (_sweepCenter != null) _sweepCenter = sweepMesh._sweepCenter.ToArray();
            if (_sideSurfaceIds != null) sweepMesh._sideSurfaceIds.ToArray();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_direction", _direction, typeof(double[]));
            info.AddValue("_sweepCenter", _sweepCenter, typeof(double[]));
            info.AddValue("_sideSurfaceIds", _sideSurfaceIds, typeof(double[]));
        }
    }
}
