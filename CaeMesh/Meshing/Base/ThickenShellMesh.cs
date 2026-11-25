// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using Octree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [PmxType(2096)]
    public class ThickenShellMesh : MeshSetupItem
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double _thickness;          //ISerializable
        [PmxKey(1)] private int _numberOfLayers;        //ISerializable
        [PmxKey(2)] private double _offset;             //ISerializable
        [PmxKey(3)] private bool _keepModelEdges;       //ISerializable


        // Properties                                                                                                               
        public double Thickness
        {
            get { return _thickness; }
            set
            {
                if (value < 0) throw new CaeException("Thickness must be larger than 0.");
                _thickness = value;
            }
        }
        public int NumberOfLayers
        {
            get { return _numberOfLayers; }
            set
            {
                if (value < 0) throw new CaeException("Number of layers must be larger than 0.");
                _numberOfLayers = value;
            }
        }
        public double Offset { get { return _offset; } set { _offset = value; } }
        public bool KeepModelEdges { get { return _keepModelEdges; } set { _keepModelEdges = value; } }


        // Constructors                                                                                                             
        public ThickenShellMesh(string name)
            : base(name)
        {
            Reset();
        }
        public ThickenShellMesh(ThickenShellMesh thickenShellMesh)
            : base("tmpName")
        {
            CopyFrom(thickenShellMesh);
        }
        public ThickenShellMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_thickness":
                        _thickness = (double)entry.Value; break;
                    case "_numberOfLayers":
                        _numberOfLayers = (int)entry.Value; break;
                    case "_offset":
                        _offset = (double)entry.Value; break;
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
            _thickness = 1;
            _numberOfLayers = 1;
            _offset = 0;
            _keepModelEdges = true;
        }
        public void CopyFrom(ThickenShellMesh thickenShellMesh)
        {
           base.CopyFrom(thickenShellMesh);
            //
            _thickness = thickenShellMesh.Thickness;
            _numberOfLayers = thickenShellMesh.NumberOfLayers;
            _offset = thickenShellMesh.Offset;
            _keepModelEdges = thickenShellMesh.KeepModelEdges;
        }

    }
}
