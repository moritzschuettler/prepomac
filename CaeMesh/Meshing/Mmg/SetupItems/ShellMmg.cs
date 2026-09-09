// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;

namespace CaeMesh
{
    [Serializable]
    public class ShellMmg : MeshSetupItem, ISerializable
    {
        // Variables
        public static readonly double DefaultRelativeHausdorff = 0.01;
        //
        private bool _relativeSize;                 //ISerializable
        private double _relativeHausdorff;          //ISerializable
        private double _hausdorff;                  //ISerializable
        private bool _keepModelEdges;               //ISerializable


        // Properties
        public override MesherTypeEnum MesherType { get { return MesherTypeEnum.Mmg; } }
        public bool RelativeSize { get { return _relativeSize; } set { _relativeSize = value; } }
        public double RelativeHausdorff
        {
            get { return _relativeHausdorff; }
            set
            {
                if (value <= 0) throw new Exception("The value must be larger than 0.");
                _relativeHausdorff = value;
            }
        }
        public double Hausdorff
        {
            get { return _hausdorff; }
            set
            {
                if (value <= 0) throw new Exception("The value must be larger than 0.");
                _hausdorff = value;
            }
        }
        public bool KeepModelEdges { get { return _keepModelEdges; } set { _keepModelEdges = value; } }


        // Constructors
        public ShellMmg(string name)
            : base(name)
        {
            Reset();
        }
        public ShellMmg(ShellMmg shellMmg)
            : base("tmpName")
        {
            CopyFrom(shellMmg);
        }
        public ShellMmg(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Reset();
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_relativeSize":
                        _relativeSize = (bool)entry.Value; break;
                    case "_relativeHausdorff":
                        _relativeHausdorff = (double)entry.Value; break;
                    case "_hausdorff":
                        _hausdorff = (double)entry.Value; break;
                    case "_keepModelEdges":
                        _keepModelEdges = (bool)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        public override void Reset()
        {
            base.Reset();
            _relativeSize = true;
            _relativeHausdorff = DefaultRelativeHausdorff;
            _hausdorff = 1;
            _keepModelEdges = true;
        }
        public void CopyFrom(ShellMmg shellMmg)
        {
            base.CopyFrom(shellMmg);
            _relativeSize = shellMmg.RelativeSize;
            _relativeHausdorff = shellMmg.RelativeHausdorff;
            _hausdorff = shellMmg.Hausdorff;
            _keepModelEdges = shellMmg.KeepModelEdges;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_relativeSize", _relativeSize, typeof(bool));
            info.AddValue("_relativeHausdorff", _relativeHausdorff, typeof(double));
            info.AddValue("_hausdorff", _hausdorff, typeof(double));
            info.AddValue("_keepModelEdges", _keepModelEdges, typeof(bool));
        }
    }
}
