// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public class PeriodicFaceSplit : ISerializable
    {
        // Variables
        private int[] _creationIds;                         //ISerializable
        private Selection _creationData;                    //ISerializable
        private int _numOfFaces;                            //ISerializable


        // Properties
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        /// <summary>
        /// The number of faces every periodic face is split into.
        /// </summary>
        public int NumOfFaces
        {
            get { return _numOfFaces; }
            set
            {
                _numOfFaces = value;
                if (_numOfFaces < 2) _numOfFaces = 2;
            }
        }


        // Constructors
        public PeriodicFaceSplit()
        {
            _creationIds = null;
            _creationData = null;
            _numOfFaces = 2;
        }
        public PeriodicFaceSplit(SerializationInfo info, StreamingContext context)
            : this()
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_creationIds":
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                        _creationData = (Selection)entry.Value; break;
                    case "_numOfFaces":
                        _numOfFaces = (int)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        /// <summary>
        /// Returns the reason the split cannot be applied, or null when it is properly defined.
        /// </summary>
        public string CheckValidity()
        {
            if (_creationIds == null || _creationIds.Length == 0) return "Select at least one part to split.";
            //
            return null;
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_creationIds", _creationIds, typeof(int[]));
            info.AddValue("_creationData", _creationData, typeof(Selection));
            info.AddValue("_numOfFaces", _numOfFaces, typeof(int));
        }
    }
}
