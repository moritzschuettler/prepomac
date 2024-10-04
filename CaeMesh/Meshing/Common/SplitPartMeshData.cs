using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using DynamicTypeDescriptor;
using CaeGlobals;
using System.Drawing;

namespace PrePoMax
{
    [Serializable]
    public class SplitPartMeshData : IMasterSlaveMultiRegion
    {
        // Variables                                                                                                                
        private double _offset;
        private double _maxH;
        private double _minH;
        private double _hausdorff;
        private bool _exact;
        //
        private RegionTypeEnum _basePartRegionType;
        private string _basePartRegionName;
        private RegionTypeEnum _splitterSurfaceRegionType;
        private string _splitterSurfaceRegionName;
        //
        private int[] _basePartCreationIds;
        private Selection _basePartCreationData;
        private int[] _splitterSurfaceCreationIds;
        private Selection _splitterSurfaceCreationData;


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
