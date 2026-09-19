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
    public abstract class GeometryFeature : ISerializable
    {
        // Variables
        protected RegionTypeEnum _regionType;               //ISerializable
        protected string[] _partNames;                      //ISerializable
        protected int[] _creationIds;                       //ISerializable
        protected Selection _creationData;                  //ISerializable
        protected int[] _splitterIds;                       //ISerializable
        protected Selection _splitterData;                  //ISerializable
        protected double _fuzzyValue;                       //ISerializable


        // Properties
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public string[] PartNames { get { return _partNames; } set { _partNames = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public int[] SplitterIds { get { return _splitterIds; } set { _splitterIds = value; } }
        public Selection SplitterData { get { return _splitterData; } set { _splitterData = value; } }
        public double FuzzyValue
        {
            get { return _fuzzyValue; }
            set
            {
                _fuzzyValue = value;
                if (_fuzzyValue < 0) _fuzzyValue = 0;
            }
        }
        public abstract SplitGeometryToolTypeEnum ToolType { get; }
        public abstract string DisplayName { get; }


        // Constructors
        public GeometryFeature()
        {
            _regionType = RegionTypeEnum.Selection;
            _partNames = null;
            _creationIds = null;
            _creationData = null;
            _splitterIds = null;
            _splitterData = null;
            _fuzzyValue = 0;
        }
        public GeometryFeature(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_partNames":
                        _partNames = (string[])entry.Value; break;
                    case "_creationIds":
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                        _creationData = (Selection)entry.Value; break;
                    case "_splitterIds":
                        _splitterIds = (int[])entry.Value; break;
                    case "_splitterData":
                        _splitterData = (Selection)entry.Value; break;
                    case "_fuzzyValue":
                        _fuzzyValue = (double)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        public void CopyRegionFrom(GeometryFeature geometryFeature)
        {
            _regionType = geometryFeature.RegionType;
            _partNames = geometryFeature.PartNames?.ToArray();
            _creationIds = geometryFeature.CreationIds?.ToArray();
            _creationData = geometryFeature.CreationData?.DeepCopy(true, false);
            _fuzzyValue = geometryFeature.FuzzyValue;
        }
        /// <summary>
        /// Drops the picked geometry and everything that was read from it. The values are not kept,
        /// because the geometry they were read from no longer exists once the split has been applied.
        /// </summary>
        public virtual void ClearSplitterSelection()
        {
            _splitterIds = null;
            _splitterData = null;
        }
        /// <summary>
        /// Returns the reason the item cannot be applied, or null when it is properly defined.
        /// </summary>
        public abstract string CheckValidity();
        // ISerialization
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_partNames", _partNames, typeof(string[]));
            info.AddValue("_creationIds", _creationIds, typeof(int[]));
            info.AddValue("_creationData", _creationData, typeof(Selection));
            info.AddValue("_splitterIds", _splitterIds, typeof(int[]));
            info.AddValue("_splitterData", _splitterData, typeof(Selection));
            info.AddValue("_fuzzyValue", _fuzzyValue, typeof(double));
        }
    }
}
