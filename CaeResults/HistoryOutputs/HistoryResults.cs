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
        public static void WriteToBinaryWriter(HistoryResults historyResults, System.IO.BinaryWriter bw)
        {
            if (historyResults == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                // Name
                bw.Write(historyResults.Name);
                // Sets
                if (historyResults.Sets == null) bw.Write((int)0);
                else
                {
                    bw.Write((int)1);
                    bw.Write((int)historyResults.Sets.Count);
                    foreach (var entry in historyResults.Sets)
                        HistoryResultSet.WriteToBinaryWriter(entry.Value, bw);
                }
            }
        }
        public static void WriteToFileStream(HistoryResults historyResults, FileStream fileStream, CompressionLevel compressionLevel)
        {
            if (historyResults == null)
            {
                Tools.WriteIntToFileStream(fileStream, 0);
            }
            else
            {
                Tools.WriteIntToFileStream(fileStream, 1);
                // Name
                Tools.WriteStringToFileStream(fileStream, historyResults.Name);
                // Sets
                if (historyResults.Sets == null) Tools.WriteIntToFileStream(fileStream, 0);
                else
                {
                    Tools.WriteIntToFileStream(fileStream, 1);
                    Tools.WriteIntToFileStream(fileStream, historyResults.Sets.Count);
                    //
                    foreach (var entry in historyResults.Sets)
                        HistoryResultSet.WriteToFileStream(entry.Value, fileStream, compressionLevel);
                }
            }
        }
        public static HistoryResults ReadFromBinaryReader(BinaryReader br, int version)
        {
            int numItems;
            HistoryResultSet historyResultSet;
            HistoryResults historyResults;
            //
            int exists = br.ReadInt32();
            if (exists == 1)
            {
                // Name
                string name = br.ReadString();
                historyResults = new HistoryResults(name);
                // Components
                exists = br.ReadInt32();
                if (exists == 1)
                {
                    numItems = br.ReadInt32();
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultSet = HistoryResultSet.ReadFromBinaryReader(br, version);
                        historyResults.Sets.Add(historyResultSet.Name, historyResultSet);
                    }
                }
                //
                return historyResults;
            }
            return null;
        }
        public static HistoryResults ReadFromFileStream(FileStream fileStream, int version)
        {
            int numItems;
            HistoryResultSet historyResultSet;
            HistoryResults historyResults;
            //
            int exists = Tools.ReadIntFromFileStream(fileStream);
            if (exists == 1)
            {
                // Name
                string name = Tools.ReadStringFromFileStream(fileStream);
                historyResults = new HistoryResults(name);
                // Components
                exists = Tools.ReadIntFromFileStream(fileStream);
                if (exists == 1)
                {
                    numItems = Tools.ReadIntFromFileStream(fileStream);
                    //
                    for (int i = 0; i < numItems; i++)
                    {
                        historyResultSet = HistoryResultSet.ReadFromFileStream(fileStream, version);
                        historyResults.Sets.Add(historyResultSet.Name, historyResultSet);
                    }
                }
                //
                return historyResults;
            }
            return null;
        }


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
