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
        public static void WriteToBinaryWriter(HistoryResultComponent historyResultComponent, BinaryWriter bw)
        {
            if (historyResultComponent == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                // Name
                bw.Write(historyResultComponent.Name);
                // Unit
                if (historyResultComponent.Unit == null) bw.Write("null");
                else bw.Write(historyResultComponent.Unit);
                // Entries
                if (historyResultComponent.Entries == null) bw.Write((int)0);
                else
                {
                    


                    bw.Write((int)1);
                    bw.Write((int)historyResultComponent.Entries.Count);
                    foreach (var entry in historyResultComponent.Entries)
                        HistoryResultEntries.WriteToBinaryWriter(entry.Value, bw);
                }
            }
        }
        public static void WriteToFileStream(HistoryResultComponent historyResultComponent, FileStream fileStream,
                                             CompressionLevel compressionLevel)
        {
            if (historyResultComponent == null)
            {
                Tools.WriteIntToFileStream(fileStream, 0);
            }
            else
            {
                Tools.WriteIntToFileStream(fileStream, 1);
                // Name
                Tools.WriteStringToFileStream(fileStream, historyResultComponent.Name);
                // Unit
                Tools.WriteStringToFileStream(fileStream, historyResultComponent.Unit);
                // Entries
                if (historyResultComponent.Entries == null)
                {
                    Tools.WriteIntToFileStream(fileStream, 0);
                }
                else
                {
                    // If all time arrays are equal, remove all except the first
                    if (historyResultComponent.Entries.Count > 0)
                    {
                        bool allTimesEqual = true;
                        List<double> time = historyResultComponent.Entries.First().Value.Time;
                        foreach (var entry in historyResultComponent.Entries)
                        {
                            if (!AreTimesEqual(entry.Value.Time, time))
                            {
                                allTimesEqual = false;
                                break;
                            }
                        }
                        if (allTimesEqual)
                        {
                            int count = 0;
                            foreach (var entry in historyResultComponent.Entries)
                            {
                                if (count++ > 0) entry.Value.Time = new List<double>();
                            }
                        }
                    }
                    //
                    Tools.WriteIntToFileStream(fileStream, 1);
                    Tools.WriteIntToFileStream(fileStream, historyResultComponent.Entries.Count);
                    //
                    foreach (var entry in historyResultComponent.Entries)
                        HistoryResultEntries.WriteToFileStream(entry.Value, fileStream, compressionLevel);
                }
            }
        }
        public static HistoryResultComponent ReadFromBinaryReader(BinaryReader br, int version)
        {
            int numItems;
            HistoryResultEntries historyResultEntry;
            HistoryResultComponent historyResultComponent;
            //
            int exists = br.ReadInt32();
            if (exists == 1)
            {
                // Name
                string name = br.ReadString();
                historyResultComponent = new HistoryResultComponent(name);
                // Unit
                historyResultComponent.Unit = br.ReadString();
                if (historyResultComponent.Unit == "null") historyResultComponent.Unit = null;
                // Entries
                exists = br.ReadInt32();
                if (exists == 1)
                {
                    numItems = br.ReadInt32();
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultEntry = HistoryResultEntries.ReadFromBinaryReader(br, version);
                        historyResultComponent.Entries.Add(historyResultEntry.Name, historyResultEntry);
                    }
                }
                //
                return historyResultComponent;
            }
            return null;
        }
        public static HistoryResultComponent ReadFromFileStream(FileStream fileStream, int version)
        {
            int numItems;
            HistoryResultEntries historyResultEntry;
            HistoryResultComponent historyResultComponent;
            //
            int exists = Tools.ReadIntFromFileStream(fileStream);
            if (exists == 1)
            {
                // Name
                string name = Tools.ReadStringFromFileStream(fileStream);
                historyResultComponent = new HistoryResultComponent(name);
                // Unit
                historyResultComponent.Unit = Tools.ReadStringFromFileStream(fileStream);
                // Entries
                exists = Tools.ReadIntFromFileStream(fileStream);
                if (exists == 1)
                {
                    numItems = Tools.ReadIntFromFileStream(fileStream);
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultEntry = HistoryResultEntries.ReadFromFileStream(fileStream, version);
                        historyResultComponent.Entries.Add(historyResultEntry.Name, historyResultEntry);
                    }
                }
                // If all time arrays are equal, they were removed before saving
                if (historyResultComponent.Entries.Count > 0)
                {
                    List<double> time = historyResultComponent.Entries.First().Value.Time;
                    foreach (var entry in historyResultComponent.Entries)
                    {
                        if (entry.Value.Time.Count == 0) entry.Value.Time = new List<double>(time);
                    }
                }
                //
                return historyResultComponent;
            }
            return null;
        }


        // Methods                                                                                                                  
        public double[][] GetAllValues()
        {
            // Collect all component values in a matrix form
            int col;
            int row;
            int numCol = _entries.Count();
            int numRow = _entries.First().Value.Values.Count();
            //
            col = 0;
            double[][] values = new double[numRow][];
            for (int i = 0; i < numRow; i++) values[i] = new double[numCol];
            //
            foreach (var entry in _entries)
            {
                // Get values
                row = 0;
                foreach (double value in entry.Value.Values)
                {
                    values[row++][col] = value;
                }
                col++;
            }
            //
            return values;
        }
        public void ApplyFilter(HistoryResultFilter filter)
        {
            int index;
            string entryName = null;
            double entryTime = -1;
            double currentValue;
            double[] time;
            //
            if (filter.Type == HistoryResultFilterTypeEnum.None) { }
            else if (filter.Type == HistoryResultFilterTypeEnum.Minimum)
            {
                currentValue = double.MaxValue;
                foreach (var entry in _entries)
                {
                    index = 0;
                    time = entry.Value.Time.ToArray();  // for speedup
                    foreach (var value in entry.Value.Values)
                    {
                        if (value < currentValue)
                        {
                            currentValue = value;
                            entryName = entry.Key;
                            entryTime = time[index];
                        }
                        index++;
                    }
                }
                //
                if (filter.Option == HistoryResultFilter.Row)
                {
                    double[][] minMaxTime = new double[][] { new double[] { entryTime, entryTime } };
                    foreach (var entry in _entries)
                    {
                        entry.Value.KeepOnly(minMaxTime);
                    }
                }
                else if (filter.Option == HistoryResultFilter.Column)
                {
                    foreach (var name in _entries.Keys.ToArray())
                    {
                        if (name != entryName) _entries.Remove(name);
                    }
                }
                else throw new NotSupportedException();
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Maximum)
            {
                currentValue = -double.MaxValue;
                foreach (var entry in _entries)
                {
                    index = 0;
                    time = entry.Value.Time.ToArray();  // for speedup
                    foreach (var value in entry.Value.Values)
                    {
                        if (value > currentValue)
                        {
                            currentValue = value;
                            entryName = entry.Key;
                            entryTime = time[index];
                        }
                        index++;
                    }
                }
                //
                if (filter.Option == HistoryResultFilter.Row)
                {
                    double[][] minMaxTime = new double[][] { new double[] { entryTime, entryTime } };
                    foreach (var entry in _entries)
                    {
                        entry.Value.KeepOnly(minMaxTime);
                    }
                }
                else if (filter.Option == HistoryResultFilter.Column)
                {
                    foreach (var name in _entries.Keys.ToArray())
                    {
                        if (name != entryName) _entries.Remove(name);
                    }
                }
                else throw new NotSupportedException();
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Sum)
            {
                double[][] values = GetAllValues();
                double[] sums;
                //
                if (filter.Option == HistoryResultFilter.Rows)
                {
                    List<double> timeList = _entries.First().Value.Time;
                    sums = new double[values.Length];
                    //
                    for (int i = 0; i < values.Length; i++)
                    {
                        sums[i] = 0;
                        for (int j = 0; j < values[i].Length; j++) sums[i] += values[i][j];
                    }
                    _entries.Clear();
                    //
                    HistoryResultEntries historyResultEntries =
                        new HistoryResultEntries(HistoryResultFilterTypeEnum.Sum.ToString(), false);
                    // Set time
                    historyResultEntries.Time = timeList;
                    // Set values
                    for (int i = 0; i < sums.Length; i++) historyResultEntries.Values.Add(sums[i]);
                    // Set unit
                    historyResultEntries.Unit = _unit;
                    //
                    _entries.Add(historyResultEntries.Name, historyResultEntries);
                }
                else if (filter.Option == HistoryResultFilter.Columns)
                {
                    sums = new double[values[0].Length];
                    //
                    for (int j = 0; j < values[0].Length; j++)
                    {
                        sums[j] = 0;
                        for (int i = 0; i < values.Length; i++) sums[j] += values[i][j];
                    }
                    //
                    index = 0;
                    foreach (var entry in _entries)
                    {
                        // Set time
                        entry.Value.Time = new List<double> { 1 };
                        // Set values
                        entry.Value.Values = new List<double> { sums[index] };
                        // Set unit
                        entry.Value.Unit = _unit;
                        //
                        index++;
                    }
                }
                else throw new NotSupportedException();
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Average)
            {
                double[][] values = GetAllValues();
                double[] sums;
                //
                if (filter.Option == HistoryResultFilter.Rows)
                {
                    List<double> timeList = _entries.First().Value.Time;
                    sums = new double[values.Length];
                    //
                    for (int i = 0; i < values.Length; i++)
                    {
                        sums[i] = 0;
                        for (int j = 0; j < values[i].Length; j++) sums[i] += values[i][j];
                    }
                    _entries.Clear();
                    //
                    HistoryResultEntries historyResultEntries =
                        new HistoryResultEntries(HistoryResultFilterTypeEnum.Average.ToString(), false);
                    // Set time
                    historyResultEntries.Time = timeList;
                    // Set values
                    for (int i = 0; i < sums.Length; i++) historyResultEntries.Values.Add(sums[i] / values[0].Length);
                    // Set unit
                    historyResultEntries.Unit = _unit;
                    //
                    _entries.Add(historyResultEntries.Name, historyResultEntries);
                }
                else if (filter.Option == HistoryResultFilter.Columns)
                {
                    sums = new double[values[0].Length];
                    //
                    for (int j = 0; j < values[0].Length; j++)
                    {
                        sums[j] = 0;
                        for (int i = 0; i < values.Length; i++) sums[j] += values[i][j];
                    }
                    //
                    index = 0;
                    foreach (var entry in _entries)
                    {
                        // Set time
                        entry.Value.Time = new List<double> { 1 };
                        // Set values
                        entry.Value.Values = new List<double> { sums[index] / values.Length };
                        // Set unit
                        entry.Value.Unit = _unit;
                        //
                        index++;
                    }
                }
                else throw new NotSupportedException();
            }
            else throw new NotSupportedException();
        }
        private static bool AreTimesEqual(List<double> list1, List<double> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                    return false;
            }

            return true;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_unit", _unit, typeof(string));
            info.AddValue("_entries", _entries, typeof(OrderedDictionary<string, HistoryResultComponent>));
        }

    }
}
