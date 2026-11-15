// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicTypeDescriptor;
using CaeGlobals;
using System.Drawing;

namespace CaeMesh
{
    [Serializable]
    [PmxType(2095)]
    public class SplitPartMeshData : IMasterSlaveMultiRegion
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double _offset;
        [PmxKey(1)] private double _maxH;
        [PmxKey(2)] private double _minH;
        [PmxKey(3)] private double _hausdorff;
        [PmxKey(4)] private bool _exact;
        //
        [PmxKey(5)] private RegionTypeEnum _basePartRegionType;
        [PmxKey(6)] private string _basePartRegionName;
        [PmxKey(7)] private RegionTypeEnum _splitterSurfaceRegionType;
        [PmxKey(8)] private string _splitterSurfaceRegionName;
        //
        [PmxKey(9)] private int[] _basePartCreationIds;
        [PmxKey(10)] private Selection _basePartCreationData;
        [PmxKey(11)] private int[] _splitterSurfaceCreationIds;
        [PmxKey(12)] private Selection _splitterSurfaceCreationData;


        // Properties                                                                                                               
        public double Offset { get { return _offset; } set { _offset = value; } }
        public double MaxH
        {
            get { return _maxH; }
            set
            {
                if (value < 0) throw new Exception("The value must be larger or equal to 0.");
                _maxH = value;
                if (value < _minH) _minH = _maxH;
            }
        }
        public double MinH
        {
            get { return _minH; }
            set
            {
                if (value < 0) throw new Exception("The value must be larger or equal to 0.");
                _minH = value;
                if (value > _maxH) _maxH = _minH;
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
        public bool Exact { get { return _exact; } set { _exact = value; } }
        //
        public RegionTypeEnum BasePartRegionType { get { return _basePartRegionType; } set { _basePartRegionType = value; } }
        public string BasePartRegionName { get { return _basePartRegionName; } set { _basePartRegionName = value; } }
        public RegionTypeEnum SplitterSurfaceRegionType
        {
            get { return _splitterSurfaceRegionType; }
            set { _splitterSurfaceRegionType = value; }
        }
        public string SplitterSurfaceRegionName
        {
            get { return _splitterSurfaceRegionName; }
            set { _splitterSurfaceRegionName = value; }
        }
        //
        public int[] BasePartCreationIds { get { return _basePartCreationIds; } set { _basePartCreationIds = value; } }
        public Selection BasePartCreationData { get { return _basePartCreationData; } set { _basePartCreationData = value; } }
        public int[] SplitterSurfaceCreationIds
        {
            get { return _splitterSurfaceCreationIds; }
            set { _splitterSurfaceCreationIds = value; }
        }
        public Selection SplitterSurfaceCreationData
        {
            get { return _splitterSurfaceCreationData; }
            set { _splitterSurfaceCreationData = value; }
        }
        // Compatibility properties
        public RegionTypeEnum MasterRegionType { get { return _basePartRegionType; } set { _basePartRegionType = value; } }
        public string MasterRegionName { get { return _basePartRegionName; } set { _basePartRegionName = value; } }
        public RegionTypeEnum SlaveRegionType
        {
            get { return _splitterSurfaceRegionType; }
            set { _splitterSurfaceRegionType = value; }
        }
        public string SlaveRegionName
        {
            get { return _splitterSurfaceRegionName; }
            set { _splitterSurfaceRegionName = value; }
        }
        //
        public int[] MasterCreationIds { get { return _basePartCreationIds; } set { _basePartCreationIds = value; } }
        public Selection MasterCreationData { get { return _basePartCreationData; } set { _basePartCreationData = value; } }
        public int[] SlaveCreationIds
        {
            get { return _splitterSurfaceCreationIds; }
            set { _splitterSurfaceCreationIds = value; }
        }
        public Selection SlaveCreationData
        {
            get { return _splitterSurfaceCreationData; }
            set { _splitterSurfaceCreationData = value; }
        }


        // Constructors                                                                                                             
        public SplitPartMeshData(RegionTypeEnum basePartRegionType, string basePartRegionName,
                                 RegionTypeEnum splitterRegionType, string splitterRegionName)
        {
            _offset = 0;
            _maxH = 0;
            _minH = 0;
            _hausdorff = 0;
            _exact = false;
            //
            _basePartRegionType = basePartRegionType;
            _basePartRegionName = basePartRegionName;
            _splitterSurfaceRegionType = splitterRegionType;
            _splitterSurfaceRegionName = splitterRegionName;
            //
            _basePartCreationIds = null;
            _basePartCreationData = null;
            _splitterSurfaceCreationIds = null;
            _splitterSurfaceCreationData = null;
        }


        // Methods                                                                                                                  
    }
}
