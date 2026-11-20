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
    [PmxType(2093)]
    public abstract class MeshSetupItem : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected int[] _creationIds;               //ISerializable
        [PmxKey(1)] protected Selection _creationData;          //ISerializable


        // Properties                                                                                                               
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public virtual MesherTypeEnum MesherType { get { return MesherTypeEnum.None; } }


        // Constructors                                                                                                             
        public MeshSetupItem(string name)
            : base(name)
        {
            _creationIds = null;
            _creationData = null;
        }
        public MeshSetupItem(MeshSetupItem meshSetupItem)
            : base("tmpName")
        {
            CopyFrom(meshSetupItem);
        }
        public MeshSetupItem(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_creationIds":
                    case "_geometryIds":    // Compatibility for version v1.4.1
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                        _creationData = (Selection)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public virtual void Reset()
        {
            // Do not clear the selection
            //_creationIds = null;
            //_creationData = null;
        }
        public void CopyFrom(MeshSetupItem meshSetupItem)
        {
            base.CopyFrom(meshSetupItem);
            //
            _creationIds = meshSetupItem.CreationIds?.ToArray();
            _creationData = meshSetupItem.CreationData?.DeepCopy(true, false);
        }

    }
}
