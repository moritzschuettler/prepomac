using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.IO;

namespace CaeResults
{
    [Serializable]
    public class HistoryResultSetExporter
    {
        // Variables                                                                                                                
        public static readonly string DefaultFileName = "HistoryOutput.csv";
        public static readonly string[] DefaultDelimiters = new string[] {",", ";", ":"};
        private string _fileName;
        private string _workingDirectory;
        private string[] _historyOutputNames;
        private string _delimiter;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set
            {
                _workingDirectory = value;
                _fileName = Path.Combine(_workingDirectory, DefaultFileName);
            }
        }
        public string[] HistoryOutputNames { get { return _historyOutputNames; } set { _historyOutputNames = value; } }
        public string Delimiter { get { return _delimiter; } set { _delimiter = value; } }


        // Constructors                                                                                                             
        public HistoryResultSetExporter(string fileName)
        {
            _fileName = fileName;
            _workingDirectory = null;
            _historyOutputNames = null;
            _delimiter = DefaultDelimiters[0];
        }


        // Methods                                                                                                                  
        public void Export(HistoryResultSet[] historyResultSets, FeResults results)
        {
            StringBuilder sb = new StringBuilder();
            HistoryResultData historyData;
            //string[] columnNames;
            double[] time;
            double[][] values;

            string[] columnNames;
            object[][] rowBasedData;
            //
            foreach (HistoryResultSet historyResultSet in historyResultSets)
            {
                foreach (var fieldEntry in historyResultSet.Fields)
                {
                    foreach (var componentEntry in fieldEntry.Value.Components)
                    {
                        historyData = new HistoryResultData(historyResultSet.Name, fieldEntry.Key, componentEntry.Key);
                        results.GetHistoryOutputData(historyData, out columnNames, out rowBasedData);


                        // Title
                        sb.AppendLine("History output" + _delimiter +
                                      historyResultSet.Name + "." + fieldEntry.Key + "." + componentEntry.Key);
                        // Column names
                        //columnNames = componentEntry.Value.Entries.Keys.ToArray();
                        //sb.Append("Time/Frequency");
                        for (int i = 0; i < columnNames.Length; i++)
                        {
                            if (i == 0) sb.Append(columnNames[i].Replace("\r\n", " / "));    // time/frequency
                            else
                            {
                                sb.Append(_delimiter);
                                sb.Append(columnNames[i].Replace("\n", " ").Replace("\r", " "));
                            }
                        }
                        sb.AppendLine();
                        // Values
                        values = componentEntry.Value.GetAllValues();
                        time = componentEntry.Value.Entries.First().Value.Time.ToArray();
                        for (int i = 0; i < rowBasedData.Length; i++)
                        {
                            //sb.Append(time[i]);
                            for (int j = 0; j < rowBasedData[i].Length; j++)
                            {
                                if (j != 0) sb.Append(_delimiter);
                                sb.Append(rowBasedData[i][j]);
                            }
                            sb.AppendLine();
                        }
                    }
                }
                sb.AppendLine("End");
            }
            //
            File.WriteAllText(_fileName, sb.ToString());
        }

    }
}
