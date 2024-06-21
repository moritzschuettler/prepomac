using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using vtkControl;

namespace CaeResults
{
    [Serializable]
    public class HistoryResultSet : NamedClass, ISerializable
    {
        // Variables                                                                                                                
        protected bool _harmonic;                                           //ISerializable
        protected OrderedDictionary<string, HistoryResultField> _fields;    //ISerializable
        protected string _baseSetName;                                      //ISerializable


        // Properties                                                                                                               
        public bool Harmonic { get { return _harmonic; } set { _harmonic = value; } }
        public OrderedDictionary<string, HistoryResultField> Fields { get { return _fields; } set { _fields = value; } }
        public string BaseSetName { get { return _baseSetName; } set { _baseSetName = value; } }


        // Constructor                                                                                                              
        public HistoryResultSet(string name)
            : base()
        {
            _checkName = false;
            _name = name;
            _harmonic = false;
            _fields = new OrderedDictionary<string, HistoryResultField>("Fields");
            _baseSetName = null;
        }
        //ISerializable
        public HistoryResultSet(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_harmonic":
                        _harmonic = (bool)entry.Value; break;
                    case "_fields":
                        // Compatibility v2.1.0
                        if (entry.Value is Dictionary<string, HistoryResultField> oldFields)
                        {
                            oldFields.OnDeserialization(null);
                            _fields = new OrderedDictionary<string, HistoryResultField>("Fields", oldFields);
                        }
                        else _fields = (OrderedDictionary<string, HistoryResultField>)entry.Value;
                        break;
                    case "_baseSetName":
                        _baseSetName = (string)entry.Value; break;
                }
            }
        }

        // Static methods                                                                                                           


        // Methods                                                                                                                  
        public void AppendFields(HistoryResultSet historyResultSet)
        {
            HistoryResultField field;
            foreach (var entry in historyResultSet.Fields)
            {
                if (_fields.TryGetValue(entry.Key, out field)) field.AppendComponents(entry.Value);
                else _fields.Add(entry.Key, entry.Value);
            }
        }
        public Dictionary<string, string[]> GetFieldNameComponentNames()
        {
            Dictionary<string, string[]> fieldNameComponentNames = new Dictionary<string, string[]>();
            foreach (var entry in _fields)
            {
                fieldNameComponentNames.Add(entry.Key, entry.Value.Components.Keys.ToArray());
            }
            return fieldNameComponentNames;
        }

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_harmonic", _harmonic, typeof(bool));
            info.AddValue("_fields", _fields, typeof(OrderedDictionary<string, HistoryResultField>));
            info.AddValue("_baseSetName", _baseSetName, typeof(string));
        }

    }
}
