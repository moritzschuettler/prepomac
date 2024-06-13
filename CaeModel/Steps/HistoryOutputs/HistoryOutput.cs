using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public enum TotalsTypeEnum
    {
        [StandardValue("Yes", Description = "Sum of external forces is printed in addition to the individual node values.")]
        Yes,
        [StandardValue("Only", Description = "Only sum of external forces is printed.")]
        Only,
        [StandardValue("No", Description = "Only individual node values are printed (default).")]
        No
    }

    [Serializable]
    public abstract class HistoryOutput : NamedClass, IMultiRegion, ISerializable
    {
        // Variables                                                                                                                
        private RegionTypeEnum _regionType;         //ISerializable
        private string _regionName;                 //ISerializable
        private TotalsTypeEnum _totals;             //ISerializable
        private int[] _creationIds;                 //ISerializable
        private Selection _creationData;            //ISerializable
        private bool _global;                       //ISerializable


        // Properties                                                                                                               
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public TotalsTypeEnum TotalsType { get { return _totals; } set { _totals = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public bool Global { get { return _global; } set { _global = value; } }


        // Constructors                                                                                                             
        public HistoryOutput(string name, string regionName, RegionTypeEnum regionType)
            : base(name)
        {
            _regionName = regionName;
            _regionType = regionType;
            _totals = TotalsTypeEnum.No;
            _creationIds = null;
            _creationData = null;
            _global = true;
        }
        public HistoryOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            // Compatibility v2.1.0
            _global = true;
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionType":
                    case "HistoryOutput+_regionType":       // Compatibility v2.1.0
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_regionName":
                    case "HistoryOutput+_regionName":       // Compatibility v2.1.0
                        _regionName = (string)entry.Value; break;
                    case "_totals":
                    case "HistoryOutput+_totals":           // Compatibility v2.1.0
                        _totals = (TotalsTypeEnum)entry.Value; break;
                    case "_creationIds":
                    case "HistoryOutput+_creationIds":      // Compatibility v2.1.0
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                    case "HistoryOutput+_creationData":     // Compatibility v2.1.0
                        _creationData = (Selection)entry.Value; break;
                    case "_global":
                        _global = (bool)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  


        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_regionName", _regionName, typeof(string));
            info.AddValue("_totals", _totals, typeof(TotalsTypeEnum));
            info.AddValue("_creationIds", _creationIds, typeof(int[]));
            info.AddValue("_creationData", _creationData, typeof(Selection));
            info.AddValue("_global", _global, typeof(bool));
        }
    }
}
