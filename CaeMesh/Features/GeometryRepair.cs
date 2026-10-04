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
    public class GeometryRepair : ISerializable
    {
        // Variables                                                                                                                
        private int[] _creationIds;                         //ISerializable
        private Selection _creationData;                    //ISerializable
        private bool _fixShape;                             //ISerializable
        private bool _removeSmallEdges;                     //ISerializable
        private bool _removeSmallFaces;                     //ISerializable
        private bool _sewFaces;                             //ISerializable
        private bool _fuseFaces;                            //ISerializable
        private bool _joinEdges;                            //ISerializable
        private double _tolerance;                          //ISerializable


        // Properties                                                                                                               
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public bool FixShape { get { return _fixShape; } set { _fixShape = value; } }
        public bool RemoveSmallEdges { get { return _removeSmallEdges; } set { _removeSmallEdges = value; } }
        public bool RemoveSmallFaces { get { return _removeSmallFaces; } set { _removeSmallFaces = value; } }
        public bool SewFaces { get { return _sewFaces; } set { _sewFaces = value; } }
        public bool FuseFaces { get { return _fuseFaces; } set { _fuseFaces = value; } }
        public bool JoinEdges { get { return _joinEdges; } set { _joinEdges = value; } }
        /// <summary>
        /// The size below which an edge or a face is small and up to which a gap is sewn. NaN takes it from the
        /// size of each part.
        /// </summary>
        public double Tolerance { get { return _tolerance; } set { _tolerance = value; } }
        /// <summary>
        /// The repairs as the modelling kernel numbers them, added up.
        /// </summary>
        public int KernelOptions
        {
            get
            {
                return (_fixShape ? 1 : 0) + (_removeSmallEdges ? 2 : 0) + (_removeSmallFaces ? 4 : 0) +
                       (_sewFaces ? 8 : 0) + (_fuseFaces ? 16 : 0) + (_joinEdges ? 32 : 0);
            }
        }


        // Constructors                                                                                                             
        public GeometryRepair()
        {
            _creationIds = null;
            _creationData = null;
            _fixShape = true;
            _removeSmallEdges = true;
            _removeSmallFaces = true;
            _sewFaces = true;
            _fuseFaces = false;
            _joinEdges = false;
            _tolerance = double.NaN;
        }
        public GeometryRepair(SerializationInfo info, StreamingContext context)
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
                    case "_fixShape":
                        _fixShape = (bool)entry.Value; break;
                    case "_removeSmallEdges":
                        _removeSmallEdges = (bool)entry.Value; break;
                    case "_removeSmallFaces":
                        _removeSmallFaces = (bool)entry.Value; break;
                    case "_sewFaces":
                        _sewFaces = (bool)entry.Value; break;
                    case "_fuseFaces":
                        _fuseFaces = (bool)entry.Value; break;
                    case "_joinEdges":
                        _joinEdges = (bool)entry.Value; break;
                    case "_tolerance":
                        _tolerance = (double)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Returns the reason the repair cannot be applied, or null when it is properly defined.
        /// </summary>
        public string CheckValidity()
        {
            if (_creationIds == null || _creationIds.Length == 0) return "Select at least one part to repair.";
            if (KernelOptions == 0) return "Select at least one repair.";
            if (!double.IsNaN(_tolerance) && _tolerance <= 0) return "The tolerance must be larger than zero.";
            //
            return null;
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_creationIds", _creationIds, typeof(int[]));
            info.AddValue("_creationData", _creationData, typeof(Selection));
            info.AddValue("_fixShape", _fixShape, typeof(bool));
            info.AddValue("_removeSmallEdges", _removeSmallEdges, typeof(bool));
            info.AddValue("_removeSmallFaces", _removeSmallFaces, typeof(bool));
            info.AddValue("_sewFaces", _sewFaces, typeof(bool));
            info.AddValue("_fuseFaces", _fuseFaces, typeof(bool));
            info.AddValue("_joinEdges", _joinEdges, typeof(bool));
            info.AddValue("_tolerance", _tolerance, typeof(double));
        }
    }
}
