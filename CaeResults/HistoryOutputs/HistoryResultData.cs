using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CaeResults
{
    [Serializable]
    public class HistoryResultData : CaeGlobals.NamedClass, ISerializable
    {
        // Variables                                                                                                                
        public string SetName;                  //ISerializable
        public string FieldName;                //ISerializable
        public string ComponentName;            //ISerializable



        // Constructors                                                                                                              
        public HistoryResultData(string setName, string fieldName, string componentName)
            :base()
        {
            _checkName = false;
            _name = setName + "_" + fieldName + "_" + componentName;
            SetName = setName;
            FieldName = fieldName;
            ComponentName = componentName;
        }
        //ISerializable
        public HistoryResultData(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "SetName":
                        SetName = (string)entry.Value; break;
                    case "FieldName":
                        FieldName = (string)entry.Value; break;
                    case "ComponentName":
                        ComponentName = (string)entry.Value; break;
                }
            }
        }


        // Static methods                                                                                                           


        // Methods                                                                                                                  
        
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("SetName", SetName, typeof(string));
            info.AddValue("FieldName", FieldName, typeof(string));
            info.AddValue("ComponentName", ComponentName, typeof(string));
        }
    }
}
