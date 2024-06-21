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
    public class HistoryResults : NamedClass, ISerializable
    {
        
        // Variables                                                                                                                
        protected OrderedDictionary<string, HistoryResultSet> _sets;           //ISerializable


        // Properties                                                                                                               
        public OrderedDictionary<string, HistoryResultSet> Sets { get { return _sets; } set { _sets = value; } }


        // Constructor                                                                                                              
        /// <summary>
        /// time = HistoryResults.Sets.Fields.Components.Entries.Time
        /// values = HistoryResults.Sets.Fields.Components.Entries.Values
        /// </summary>
        /// <param name="name"></param>
        public HistoryResults(string name)
            : base(name)
        {
            _sets = new OrderedDictionary<string, HistoryResultSet>("Sets");
        }
        //ISerializable
        public HistoryResults(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_sets":
                        // Compatibility v2.1.0
                        if (entry.Value is Dictionary<string, HistoryResultSet> oldSets)
                        {
                            oldSets.OnDeserialization(null);
                            _sets = new OrderedDictionary<string, HistoryResultSet>("Sets", oldSets);
                        }
                        else _sets = (OrderedDictionary<string, HistoryResultSet>)entry.Value;
                        break;
                }
            }
        }


        // Static methods                                                                                                           


        // Methods                                                                                                                  
        public void AppendSets(HistoryResults historyResults)
        {
            HistoryResultSet set;
            foreach (var entry in historyResults.Sets)
            {
                if (_sets.TryGetValue(entry.Key, out set)) set.AppendFields(entry.Value);
                else _sets.Add(entry.Key, entry.Value);
            }
        }

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_sets", _sets, typeof(OrderedDictionary<string, HistoryResultSet>));
        }

    }
}
