using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using static CaeGlobals.Geometry2;

namespace CaeResults
{
    [Serializable]
    public class HistoryResultEntries : NamedClass, ISerializable
    {
        // Variables                                                                                                                
        private List<double> _time;             //ISerializable
        private List<double> _values;           //ISerializable
        private List<int> _count;               //ISerializable
        private bool _local;                    //ISerializable
        private string _unit;                   //ISerializable


        // Properties                                                                                                               
        public List<double> Time { get { return _time; } set { _time = value; } }
        public List<double> Values { get { return _values; } set { _values = value; } }
        public List<int> Count { get { return _count; } set { _count = value; } }
        public bool Local { get { return _local; } set { _local = value; } }
        public string Unit { get { return _unit; } set { _unit = value; } }


        // Constructor                                                                                                              
        public HistoryResultEntries(string name, bool local)
            : base()
        {
            _checkName = false;
            _name = name;
            _time = new List<double>();
            _values = new List<double>();
            _count = new List<int>();
            _local = local;
            _unit = null;
        }
        public HistoryResultEntries(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_time":
                        _time = (List<double>)entry.Value; break;
                    case "_values":
                        _values = (List<double>)entry.Value; break;
                    case "_count":
                        _count = (List<int>)entry.Value; break;
                    case "_local":
                        _local = (bool)entry.Value; break;
                    case "_unit":
                        _unit = (string)entry.Value; break;
                }
            }
        }


        // Static methods                                                                                                           
        public static void WriteToBinaryWriter(HistoryResultEntries historyResultEntries, BinaryWriter bw)
        {
            if (historyResultEntries == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                // Name
                bw.Write(historyResultEntries.Name);
                // Time
                if (historyResultEntries.Time == null) bw.Write((int)0);
                else
                {
                    bw.Write((int)1);
                    bw.Write((int)historyResultEntries.Time.Count);
                    foreach (var time in historyResultEntries.Time) bw.Write(time);
                }
                // Values
                if (historyResultEntries.Values == null) bw.Write((int)0);
                else
                {
                    bw.Write((int)1);
                    bw.Write((int)historyResultEntries.Values.Count);
                    foreach (var value in historyResultEntries.Values) bw.Write(value);
                }
                // Count
                if (historyResultEntries.Count == null) bw.Write((int)0);
                else
                {
                    bw.Write((int)1);
                    bw.Write((int)historyResultEntries.Count.Count);
                    foreach (var count in historyResultEntries.Count) bw.Write(count);
                }
                // Local
                bw.Write(historyResultEntries.Local);
                // Unit
                if (historyResultEntries.Unit == null) bw.Write("@null#");
                else bw.Write(historyResultEntries.Unit);
            }
        }
        public static void WriteToFileStream(HistoryResultEntries historyResultEntries, FileStream fileStream,
                                             CompressionLevel compressionLevel)
        {
            if (historyResultEntries == null)
            {
                Tools.WriteIntToFileStream(fileStream, 0);
            }
            //
            else
            {
                Tools.WriteIntToFileStream(fileStream, 1);
                //
                using (BinaryWriter bw = new BinaryWriter(new MemoryStream()))
                {
                    WriteToBinaryWriter(historyResultEntries, bw);
                    // Rewind the writer
                    bw.Flush();
                    bw.BaseStream.Position = 0;
                    // Compress the writer
                    byte[] compressedData = Tools.Compress(bw.BaseStream, compressionLevel);
                    // Write the length of the compressed data
                    Tools.WriteIntToFileStream(fileStream, compressedData.Length);
                    // Write the compressed data
                    fileStream.Write(compressedData, 0, compressedData.Length);
                }
            }
            
        }
        public static HistoryResultEntries ReadFromBinaryReader(BinaryReader br, int version)
        {
            int numItems;
            HistoryResultEntries historyResultEntries;
            //
            int exists = br.ReadInt32();
            if (exists == 1)
            {
                // Name
                string name = br.ReadString();
                historyResultEntries = new HistoryResultEntries(name, false);
                // Time
                exists = br.ReadInt32();
                if (exists == 1)
                {
                    numItems = br.ReadInt32();
                    for (int i = 0; i < numItems; i++) historyResultEntries.Time.Add(br.ReadDouble());
                }
                // Values
                exists = br.ReadInt32();
                if (exists == 1)
                {
                    numItems = br.ReadInt32();
                    for (int i = 0; i < numItems; i++) historyResultEntries.Values.Add(br.ReadDouble());
                }
                // Count
                exists = br.ReadInt32();
                if (exists == 1)
                {
                    numItems = br.ReadInt32();
                    for (int i = 0; i < numItems; i++) historyResultEntries.Count.Add(br.ReadInt32());
                }
                // Local
                historyResultEntries.Local = br.ReadBoolean();
                // Unit
                historyResultEntries.Unit = br.ReadString();
                if (historyResultEntries.Unit == "@null#") historyResultEntries.Unit = null;
                //
                return historyResultEntries;
            }
            return null;
        }
        public static HistoryResultEntries ReadFromFileStream(FileStream fileStream, int version)
        {
            int exists = Tools.ReadIntFromFileStream(fileStream);
            //
            if (exists == 1)
            {
                int numOfBytes = Tools.ReadIntFromFileStream(fileStream);
                byte[] compressedData = new byte[numOfBytes];
                fileStream.Read(compressedData, 0, compressedData.Length);
                //
                HistoryResultEntries historyResultEntries;
                using (MemoryStream memoryStream = new MemoryStream(compressedData))
                using (BinaryReader br = new BinaryReader(Tools.Decompress(memoryStream)))
                {
                    historyResultEntries = ReadFromBinaryReader(br, version);
                }
                return historyResultEntries;
            }
            else return null;
        }


        // Methods                                                                                                                  
        public void Add(double time, double value)
        {
            _time.Add(time);
            _values.Add(value);
            _count.Add(1);
        }
        public void Append(HistoryResultEntries historyResultEntry)
        {
            _time.AddRange(historyResultEntry.Time);
            _values.AddRange(historyResultEntry.Values);
        }
        public void ShiftTime(double timeShift)
        {
            List<double> newTime = new List<double>();
            foreach (var time in _time) newTime.Add(time + timeShift);
            _time = newTime;
        }
        public void SumValue(double value)
        {
            _values[_values.Count - 1] += value;
            _count[_count.Count - 1]++;
        }
        public void ComputeAverage()
        {
            for (int i = 0; i < _values.Count; i++)
            {
                if (_count[i] > 1) _values[i] /= _count[i];
            }
        }
        public void KeepOnly(double[][] minMaxTime)
        {
            double[] time = _time.ToArray();
            double[] values = _values.ToArray();
            int[] count = _count.ToArray();
            //
            _time.Clear();
            _values.Clear();
            _count.Clear();
            //
            bool add;
            for (int i = 0; i < time.Length; i++)
            {
                add = false;
                foreach (var pair in minMaxTime)
                {
                    if (pair[0] <= time[i] && time[i] <= pair[1])
                    {
                        add = true;
                        break;
                    }
                }
                //
                if (add)
                {
                    _time.Add(time[i]);
                    _values.Add(values[i]);
                    //
                    if (i >= count.Length) _count.Add(1); // error handling
                    else _count.Add(count[i]);
                }
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_time", _time, typeof(List<double>));
            info.AddValue("_values", _values, typeof(List<double>));
            info.AddValue("_count", _count, typeof(List<int>));
            info.AddValue("_local", _local, typeof(bool));
            info.AddValue("_unit", _unit, typeof(string));
        }
    }
}
