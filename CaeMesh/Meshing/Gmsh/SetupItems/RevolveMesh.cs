// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
    public class RevolveMesh : GmshSetupItem, ISerializable
    {
        // Variables                                                                                                                
        private double[] _axisCenter;                                   // ISerializable
        private double[] _axisDirection;                                // ISerializable
        private double _angleDeg;                                       // ISerializable
        private double _middleR;                                        // ISerializable
        private bool _createFull360Model;                               // ISerializable


        // Properties                                                                                                               
        public double[] AxisCenter { get { return _axisCenter; } set { _axisCenter = value; } }
        public double[] AxisDirection { get { return _axisDirection; } set { _axisDirection = value; } }
        public double AngleDeg { get { return _angleDeg; } set { _angleDeg = value; } }
        public double MiddleR { get { return _middleR; } set { _middleR = value; } }
        // The number of equal sectors of size AngleDeg that tile a full revolution, or 0 when AngleDeg does not
        // divide 360 degrees into at least two integer sectors. A full 360 degree model is built by repeating the
        // revolved mesh around the axis this many times.
        public int Full360SegmentCount
        {
            get
            {
                if (_angleDeg <= 1E-3) return 0;
                int n = (int)Math.Round(360.0 / _angleDeg);
                if (n < 2) return 0;
                if (Math.Abs(_angleDeg - 360.0 / n) > 1E-3) return 0;
                return n;
            }
        }
        public bool CanCreateFull360Model { get { return Full360SegmentCount >= 2; } }
        public bool CreateFull360Model
        {
            get { return CanCreateFull360Model && _createFull360Model; }
            set { _createFull360Model = value; }
        }


        // Constructors                                                                                                             
        public RevolveMesh(string name)
            : base(name)
        {
            Reset();
        }
        public RevolveMesh(RevolveMesh revolveMesh)
            : base("tmpName")
        {
            CopyFrom(revolveMesh);
        }
        public RevolveMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_axisCenter":
                        _axisCenter = (double[])entry.Value; break;
                    case "_axisDirection":
                        _axisDirection = (double[])entry.Value; break;
                    case "_angleDeg":
                        _angleDeg = (double)entry.Value; break;
                    case "_middleR":
                        _middleR = (double)entry.Value; break;
                    case "_createFull360Model":
                        _createFull360Model = (bool)entry.Value; break;
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
            _axisCenter = null;
            _axisDirection = null;
            _angleDeg = -1;
            _middleR = -1;
            _createFull360Model = false;
        }
        public void CopyFrom(RevolveMesh revolveMesh)
        {
            base.CopyFrom(revolveMesh);
            //
            if (_axisCenter != null) _axisCenter = revolveMesh._axisCenter.ToArray();
            if (_axisDirection != null) _axisDirection = revolveMesh._axisDirection.ToArray();
            _angleDeg = revolveMesh._angleDeg;
            _middleR = revolveMesh._middleR;
            _createFull360Model = revolveMesh._createFull360Model;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_axisCenter", _axisCenter, typeof(double[]));
            info.AddValue("_axisDirection", _axisDirection, typeof(double[]));
            info.AddValue("_angleDeg", _angleDeg, typeof(double));
            info.AddValue("_middleR", _middleR, typeof(double));
            info.AddValue("_createFull360Model", _createFull360Model, typeof(bool));
        }
    }
}
