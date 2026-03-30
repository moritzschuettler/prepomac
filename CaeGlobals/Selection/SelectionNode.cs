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
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace CaeGlobals
{
    [Serializable]
    public abstract class SelectionNode : ISerializable
    {
        // Variables                                                                                                                
        protected vtkSelectOperation _selectOperation;      //ISerializable
        protected int[] _partIds;                           //ISerializable
        [NonSerialized] protected int _hash;


        // Properties                                                                                                               
        public vtkSelectOperation SelectOperation { get { return _selectOperation; } }
        public int[] PartIds { get { return _partIds; } }
        public int Hash { get { return _hash; } set { _hash = value; } }


        // Constructors                                                                                                             
        public SelectionNode(vtkSelectOperation selectOperation)
            : this(selectOperation, null)
        {
        }
        public SelectionNode(vtkSelectOperation selectOperation, int[] partIds)
        {
            _selectOperation = selectOperation;
            _partIds = partIds;
            _hash = -1;
        }
        public SelectionNode(SerializationInfo info, StreamingContext context)
        {
            _hash = -1;                 // initialize
            _partIds = null;            // Compatibility for version v0.9.0
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_selectOpreation":    // compatibility version 2.2.2
                    case "_selectOperation":
                        _selectOperation = (vtkSelectOperation)entry.Value;
                        break;
                    case "_partIds":
                        _partIds = (int[])entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public void SetSelectOperation(vtkSelectOperation selectOperation)
        {
            _selectOperation = selectOperation;
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_selectOperation", _selectOperation, typeof(vtkSelectOperation));
            info.AddValue("_partIds", _partIds, typeof(int[]));
        }
    }
}
