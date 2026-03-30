// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.IO;

namespace CaeResults
{
    [Serializable]
    public class HistoryResultField : NamedClass, ISerializable
    {
        // Variables                                                                                                                
        protected OrderedDictionary<string, HistoryResultComponent> _components;        //ISerializable


        // Properties                                                                                                               
        public OrderedDictionary<string, HistoryResultComponent> Components
        {
            get { return _components; }
            set { _components = value; }
        }
        

        // Constructor                                                                                                              
        public HistoryResultField(string name)
            : base()
        {
            _checkName = false;
            _name = name;
            _components = new OrderedDictionary<string, HistoryResultComponent>("Components");
        }
        public HistoryResultField(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_components":
                        // Compatibility v1.3.5
                        if (entry.Value is Dictionary<string, HistoryResultComponent> oldComponents)
                        {
                            oldComponents.OnDeserialization(null);
                            _components = new OrderedDictionary<string, HistoryResultComponent>("Components", oldComponents);
                        }
                        else _components = (OrderedDictionary<string, HistoryResultComponent>)entry.Value;
                        break;
                }
            }
        }


        // Static methods                                                                                                           
        public static void WriteToBinaryWriter(HistoryResultField historyResultField, BinaryWriter bw)
        {
            if (historyResultField == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                // Name
                bw.Write(historyResultField.Name);
                // Components
                if (historyResultField.Components == null) bw.Write((int)0);
                else
                {
                    bw.Write((int)1);
                    bw.Write((int)historyResultField.Components.Count);
                    foreach (var entry in historyResultField.Components)
                        HistoryResultComponent.WriteToBinaryWriter(entry.Value, bw);
                }
            }
        }
        public static void WriteToFileStream(HistoryResultField historyResultField, FileStream fileStream,
                                             CompressionLevel compressionLevel)
        {
            if (historyResultField == null)
            {
                Tools.WriteIntToFileStream(fileStream, 0);
            }
            else
            {
                Tools.WriteIntToFileStream(fileStream, 1);
                // Name
                Tools.WriteStringToFileStream(fileStream, historyResultField.Name);
                // Components
                if (historyResultField.Components == null) Tools.WriteIntToFileStream(fileStream, 0);
                else
                {
                    Tools.WriteIntToFileStream(fileStream, 1);
                    Tools.WriteIntToFileStream(fileStream, historyResultField.Components.Count);
                    //
                    foreach (var entry in historyResultField.Components)
                        HistoryResultComponent.WriteToFileStream(entry.Value, fileStream, compressionLevel);
                }
            }
        }
        public static HistoryResultField ReadFromBinaryReader(System.IO.BinaryReader br, int version)
        {
            int numItems;
            HistoryResultComponent historyResultComponent;
            HistoryResultField historyResultField;
            //
            int exists = br.ReadInt32();
            if (exists == 1)
            {
                // Name
                string name = br.ReadString();
                historyResultField = new HistoryResultField(name);
                // Components
                exists = br.ReadInt32();
                if (exists == 1)
                {
                    numItems = br.ReadInt32();
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultComponent = HistoryResultComponent.ReadFromBinaryReader(br, version);
                        historyResultField.Components.Add(historyResultComponent.Name, historyResultComponent);
                    }
                }
                //
                return historyResultField;
            }
            return null;
        }
        public static HistoryResultField ReadFromFileStream(FileStream fileStream, int version)
        {
            int numItems;
            HistoryResultComponent historyResultComponent;
            HistoryResultField historyResultField;
            //
            int exists = Tools.ReadIntFromFileStream(fileStream);
            if (exists == 1)
            {
                // Name
                string name = Tools.ReadStringFromFileStream(fileStream);
                historyResultField = new HistoryResultField(name);
                // Components
                exists = Tools.ReadIntFromFileStream(fileStream);
                if (exists == 1)
                {
                    numItems = Tools.ReadIntFromFileStream(fileStream);
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultComponent = HistoryResultComponent.ReadFromFileStream(fileStream, version);
                        historyResultField.Components.Add(historyResultComponent.Name, historyResultComponent);
                    }
                }
                //
                return historyResultField;
            }
            return null;
        }


        // Methods                                                                                                                  
        public void AppendComponents(HistoryResultField historyResultField)
        {
            HistoryResultComponent component;
            foreach (var entry in historyResultField.Components)
            {
                if (_components.TryGetValue(entry.Key, out component)) throw new NotSupportedException();
                else _components.Add(entry.Key, entry.Value);
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_components", _components, typeof(OrderedDictionary<string, HistoryResultComponent>));
        }

    }
}
