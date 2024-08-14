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
