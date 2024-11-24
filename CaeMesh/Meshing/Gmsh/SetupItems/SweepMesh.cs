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
        private int _numberOfLayerSmoothSteps;                          // ISerializable
        private int _numberOfGlobalSmoothSteps;                         // ISerializable
        private double[] _direction;                                    // ISerializable
        private double[] _sweepCenter;                                  // ISerializable
        private int[] _sideSurfaceIds;                                  // ISerializable
        private int[][][] _layerGroupEdgeIds;                           // ISerializable


        // Properties                                                                                                               
        public int NumberOfLayerSmoothSteps
        {
            get { return _numberOfLayerSmoothSteps; }
            set
            {
                _numberOfLayerSmoothSteps = value;
                if (_numberOfLayerSmoothSteps < 0) _numberOfLayerSmoothSteps = 0;
            }
        }
        public int NumberOfGlobalSmoothSteps
        {
            get { return _numberOfGlobalSmoothSteps; }
            set
            {
                _numberOfGlobalSmoothSteps = value;
                if (_numberOfGlobalSmoothSteps < 0) _numberOfGlobalSmoothSteps = 0;
            }
        }
        public double[] Direction { get { return _direction; } set { _direction = value; } }
        public double[] SweepCenter { get { return _sweepCenter; } set { _sweepCenter = value; } }
        public int[] SideSurfaceIds { get { return _sideSurfaceIds; } set { _sideSurfaceIds = value; } }
        public int[][][] LayerGroupEdgeIds { get { return _layerGroupEdgeIds; } set { _layerGroupEdgeIds = value; } }


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
                    case "_numberOfLayerSmoothSteps":
                        _numberOfLayerSmoothSteps = (int)entry.Value; break;
                    case "_numberOfGlobalSmoothSteps":
                        _numberOfGlobalSmoothSteps = (int)entry.Value; break;
                    case "_direction":
                        _direction = (double[])entry.Value; break;
                    case "_sweepCenter":
                        _sweepCenter = (double[])entry.Value; break;
                    case "_sideSurfaceIds":
                        _sideSurfaceIds = (int[])entry.Value; break;
                    case "_layerGroupEdgeIds":
                        _layerGroupEdgeIds = (int[][][])entry.Value; break;
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
            _numberOfLayerSmoothSteps = 20;
            _numberOfGlobalSmoothSteps = 5;
            _direction = null;
            _sweepCenter = null;
            _sideSurfaceIds = null;
            _layerGroupEdgeIds = null;
        }
        public void CopyFrom(SweepMesh sweepMesh)
        {
            base.CopyFrom(sweepMesh);
            //
            _numberOfLayerSmoothSteps = sweepMesh._numberOfLayerSmoothSteps;
            _numberOfGlobalSmoothSteps = sweepMesh._numberOfGlobalSmoothSteps;
            if (_direction != null) _direction = sweepMesh._direction.ToArray();
            if (_sweepCenter != null) _sweepCenter = sweepMesh._sweepCenter.ToArray();
            if (_sideSurfaceIds != null) sweepMesh._sideSurfaceIds.ToArray();
            if (_layerGroupEdgeIds != null)
            {
                sweepMesh._layerGroupEdgeIds = new int[_layerGroupEdgeIds.Length][][];
                for (int i = 0; i < _layerGroupEdgeIds.Length; i++)
                {
                    sweepMesh._layerGroupEdgeIds[i] = new int[_layerGroupEdgeIds[i].Length][];
                    for (int j = 0; j < _layerGroupEdgeIds[i].Length; j++)
                    {
                        _layerGroupEdgeIds[i][j] = _layerGroupEdgeIds[i][j].ToArray();
                    }
                }
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_numberOfLayerSmoothSteps", _numberOfLayerSmoothSteps, typeof(int));
            info.AddValue("_numberOfGlobalSmoothSteps", _numberOfGlobalSmoothSteps, typeof(int));
            info.AddValue("_direction", _direction, typeof(double[]));
            info.AddValue("_sweepCenter", _sweepCenter, typeof(double[]));
            info.AddValue("_sideSurfaceIds", _sideSurfaceIds, typeof(int[]));
            info.AddValue("_layerGroupEdgeIds", _layerGroupEdgeIds, typeof(int[][][]));
        }
    }
}
