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
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using vtkControl;
using System.IO.Compression;
using System.IO;

namespace CaeResults
{
    [PmxType(5042)]
    public class HistoryResultSet : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected bool _harmonic;                                           //ISerializable
        [PmxKey(1)] protected CaeGlobals.OrderedDictionary<string, HistoryResultField> _fields;    //ISerializable
        [PmxKey(2)] protected string _baseSetName;                                      //ISerializable


        // Properties                                                                                                               
        public bool Harmonic { get { return _harmonic; } set { _harmonic = value; } }
        public CaeGlobals.OrderedDictionary<string, HistoryResultField> Fields { get { return _fields; } set { _fields = value; } }
        public string BaseSetName { get { return _baseSetName; } set { _baseSetName = value; } }


        // Constructor                                                                                                              
        public HistoryResultSet(string name)
            : base()
        {
            _checkName = false;
            _name = name;
            _harmonic = false;
            _fields = new CaeGlobals.OrderedDictionary<string, HistoryResultField>("Fields");
            _baseSetName = null;
        }
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
                            _fields = new CaeGlobals.OrderedDictionary<string, HistoryResultField>("Fields", oldFields);
                        }
                        else _fields = (CaeGlobals.OrderedDictionary<string, HistoryResultField>)entry.Value;
                        break;
                    case "_baseSetName":
                        _baseSetName = (string)entry.Value; break;
                }
            }
        }


        // Static methods                                                                                                           
        public static void WriteToBinaryWriter(HistoryResultSet historyResultSet, BinaryWriter bw)
        {
            if (historyResultSet == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                // Name
                bw.Write(historyResultSet.Name);
                // Harmonic
                bw.Write(historyResultSet.Harmonic);
                // Fields
                if (historyResultSet.Fields == null) bw.Write((int)0);
                else
                {
                    bw.Write((int)1);
                    bw.Write((int)historyResultSet.Fields.Count);
                    foreach (var entry in historyResultSet.Fields)
                        HistoryResultField.WriteToBinaryWriter(entry.Value, bw);
                }
                // BaseSetName
                if (historyResultSet.BaseSetName == null) bw.Write("null");
                else bw.Write(historyResultSet.BaseSetName);
            }
        }
        public static void WriteToFileStream(HistoryResultSet historyResultSet, Stream fileStream,
                                             CompressionLevel compressionLevel,
                                             bool compressBlocks = true)
        {
            if (historyResultSet == null)
            {
                Tools.WriteIntToFileStream(fileStream, 0);
            }
            else
            {
                Tools.WriteIntToFileStream(fileStream, 1);
                // Name
                Tools.WriteStringToFileStream(fileStream, historyResultSet.Name);
                // Harmonic
                Tools.WriteBoolToFileStream(fileStream, historyResultSet.Harmonic);
                // Fields
                if (historyResultSet.Fields == null) Tools.WriteIntToFileStream(fileStream, 0);
                else
                {
                    Tools.WriteIntToFileStream(fileStream, 1);
                    Tools.WriteIntToFileStream(fileStream, historyResultSet.Fields.Count);
                    //
                    foreach (var entry in historyResultSet.Fields)
                        HistoryResultField.WriteToFileStream(entry.Value, fileStream, compressionLevel, compressBlocks);
                }
                // BaseSetName
                Tools.WriteStringToFileStream(fileStream, historyResultSet.BaseSetName);
            }
        }
        public static HistoryResultSet ReadFromBinaryReader(BinaryReader br, int version)
        {
            int numItems;
            HistoryResultField historyResultField;
            HistoryResultSet historyResultSet;
            //
            int exists = br.ReadInt32();
            if (exists == 1)
            {
                // Name
                string name = br.ReadString();
                historyResultSet = new HistoryResultSet(name);
                // Harmonic
                historyResultSet.Harmonic = br.ReadBoolean();
                // Fields
                exists = br.ReadInt32();
                if (exists == 1)
                {
                    numItems = br.ReadInt32();
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultField = HistoryResultField.ReadFromBinaryReader(br, version);
                        historyResultSet.Fields.Add(historyResultField.Name, historyResultField);
                    }
                }
                // BaseSetName
                historyResultSet.BaseSetName = br.ReadString();
                if (historyResultSet.BaseSetName == "null") historyResultSet.BaseSetName = null;
                //
                return historyResultSet;
            }
            return null;
        }
        public static HistoryResultSet ReadFromFileStream(Stream fileStream, int version, bool blocksCompressed = true)
        {
            int numItems;
            HistoryResultField historyResultField;
            HistoryResultSet historyResultSet;
            //
            int exists = Tools.ReadIntFromFileStream(fileStream);
            if (exists == 1)
            {
                // Name
                string name = Tools.ReadStringFromFileStream(fileStream);
                historyResultSet = new HistoryResultSet(name);
                // Harmonic
                historyResultSet.Harmonic = Tools.ReadBoolFromFileStream(fileStream);
                // Fields
                exists = Tools.ReadIntFromFileStream(fileStream);
                if (exists == 1)
                {
                    numItems = Tools.ReadIntFromFileStream(fileStream);
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultField = HistoryResultField.ReadFromFileStream(fileStream, version, blocksCompressed);
                        historyResultSet.Fields.Add(historyResultField.Name, historyResultField);
                    }
                }
                // BaseSetName
                historyResultSet.BaseSetName = Tools.ReadStringFromFileStream(fileStream);
                //
                return historyResultSet;
            }
            return null;
        }


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

    }
}
