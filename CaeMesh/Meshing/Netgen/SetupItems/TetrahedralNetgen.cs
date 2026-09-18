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
    /// <summary>
    /// Meshes the selected solid parts with Netgen tetrahedra. The mesher is driven by the meshing
    /// parameters alone, so the item carries no settings of its own and exists to name the mesher: it is
    /// what a part is given when the structured mesh its geometry supports is not wanted.
    /// </summary>
    [Serializable]
    public class TetrahedralNetgen : MeshSetupItem, ISerializable
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        public override MesherTypeEnum MesherType { get { return MesherTypeEnum.Netgen; } }


        // Constructors                                                                                                             
        public TetrahedralNetgen(string name)
            : base(name)
        {
            Reset();
        }
        public TetrahedralNetgen(TetrahedralNetgen tetrahedralNetgen)
            : base("tmpName")
        {
            CopyFrom(tetrahedralNetgen);
        }
        public TetrahedralNetgen(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            base.Reset();
        }
        public void CopyFrom(TetrahedralNetgen tetrahedralNetgen)
        {
            base.CopyFrom(tetrahedralNetgen);
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
