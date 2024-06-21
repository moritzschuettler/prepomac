using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;

namespace CaeResults
{
    [Serializable]
    public class HistoryResultComponent : NamedClass, ISerializable
    {
        // Variables                                                                                                                
        protected string _unit;                                                     //ISerializable
        protected OrderedDictionary<string, HistoryResultEntries> _entries;         //ISerializable


        // Properties                                                                                                               
        public string Unit { get { return _unit; } set { _unit = value; } }
        public OrderedDictionary<string, HistoryResultEntries> Entries { get { return _entries; } set { _entries = value; } }


        // Constructor                                                                                                              
        public HistoryResultComponent(string name)
            : base()
        {
            _checkName = false;
            _name = name;
            _unit = null;
            _entries = new OrderedDictionary<string, HistoryResultEntries>("Entries");
        }
        //ISerializable
        public HistoryResultComponent(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_unit":
                        _unit = (string)entry.Value; break;
                    case "_entries":
                        // Compatibility v2.1.0
                        if (entry.Value is Dictionary<string, HistoryResultEntries> oldEntries)
                        {
                            oldEntries.OnDeserialization(null);
                            _entries = new OrderedDictionary<string, HistoryResultEntries>("Entries", oldEntries);
                        }
                        else _entries = (OrderedDictionary<string, HistoryResultEntries>)entry.Value;
                        break;
                }
            }
        }


        // Static methods                                                                                                           


        // Methods                                                                                                                  

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_unit", _unit, typeof(string));
            info.AddValue("_entries", _entries, typeof(OrderedDictionary<string, HistoryResultEntries>));
        }

    }
}
