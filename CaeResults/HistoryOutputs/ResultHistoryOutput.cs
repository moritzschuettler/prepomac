using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{   
    [Serializable]
    public abstract class ResultHistoryOutput : NamedClass, IMultiRegion
    {
        // Variables                                                                                                                
        private RegionTypeEnum _regionType;
        private string _regionName;
        private int[] _creationIds;
        private Selection _creationData;
        private HistoryResultSet historyResultSet;
        private HistoryResultFilter _filter1;
        private HistoryResultFilter _filter2;


        // Properties                                                                                                               
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public HistoryResultSet HistoryResultSet { get { return historyResultSet; } set { historyResultSet = value; } }
        public HistoryResultFilter Filter1 { get { return _filter1; } set { _filter1 = value; } }
        public HistoryResultFilter Filter2 { get { return _filter2; } set { _filter2 = value; } }


        // Constructors                                                                                                             
        public ResultHistoryOutput(string name, string regionName, RegionTypeEnum regionType)
            : base(name)
        {
            _regionName = regionName;
            _regionType = regionType;
            _creationIds = null;
            _creationData = null;
            _filter1 = new HistoryResultFilter();
            _filter2 = new HistoryResultFilter();
        }


        // Methods                                                                                                                  
        public abstract string[] GetParentNames();     // for dependency check


    }
}
