using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using System.Runtime.Serialization;
using System.Drawing;

namespace CaeModel
{
    [Serializable]
    public abstract class InitialCondition : NamedClass, IMultiRegion, IContainsEquations, ISerializable
    {
        // Variables                                                                                                                
        private string _regionName;             //ISerializable
        private RegionTypeEnum _regionType;     //ISerializable
        private int[] _creationIds;             //ISerializable
        private Selection _creationData;        //ISerializable
        protected bool _twoD;                   //ISerializable
        protected Color _color;                 //ISerializable


        // Properties                                                                                                               
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public bool TwoD { get { return _twoD; } }
        public Color Color
        {
            get
            {
                // Compatibility for version v2.2.3
                if (_color == Color.Empty) _color = Color.Orange;
                //
                return _color;
            }
            set { _color = value; }
        }


        // Constructors                                                                                                             
        public InitialCondition(string name, string regionName, RegionTypeEnum regionType, bool twoD)
            : base(name)
        {
            _regionName = regionName;
            _regionType = regionType;
            _creationIds = null;
            _creationData = null;
            _twoD = twoD;
            _color = Color.Orange;
        }
        public InitialCondition(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionName":
                        _regionName = (string)entry.Value; break;
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_creationIds":
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                        _creationData = (Selection)entry.Value; break;
                    case "_twoD":
                        _twoD = (bool)entry.Value; break;
                    case "_color":
                        _color = (Color)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  

        // IContainsEquations
        public virtual void CheckEquations()
        {
        }
        public virtual bool TryCheckEquations()
        {
            try
            {
                CheckEquations();
                return true;
            }
            catch (Exception ex) { return false; }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_regionName", _regionName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_creationIds", _creationIds, typeof(int[]));
            info.AddValue("_creationData", _creationData, typeof(Selection));
            info.AddValue("_twoD", _twoD, typeof(bool));
            info.AddValue("_color", _color, typeof(Color));
        }
    }
}
