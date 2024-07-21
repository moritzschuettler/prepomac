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
        public void Export(FeResults results)
        {
            HistoryResultSet[] historyResultSets = new HistoryResultSet[_historyOutputNames.Length];
            for (int i = 0; i < _historyOutputNames.Length; i++)
            {
                historyResultSets[i] = results.GetHistoryResultSet(_historyOutputNames[i]);
            }
            //
            StringBuilder sb = new StringBuilder();
            HistoryResultData historyData;
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
                        results.GetHistoryOutputData(historyData, out columnNames, out rowBasedData, true);
                        // Title
                        sb.AppendLine("History output component" + _delimiter +
                                      historyResultSet.Name + "." + fieldEntry.Key + "." + componentEntry.Key);
                        // Column names
                        for (int i = 0; i < columnNames.Length; i++)
                        {
                            if (i == 0) sb.Append(columnNames[i]);
                            else
                            {
                                sb.Append(_delimiter);
                                sb.Append(columnNames[i]);
                            }
                        }
                        sb.AppendLine();
                        // Data
                        for (int i = 0; i < rowBasedData.Length; i++)
                        {
                            for (int j = 0; j < rowBasedData[i].Length; j++)
                            {
                                if (j != 0) sb.Append(_delimiter);
                                sb.Append(rowBasedData[i][j]);
                            }
                            sb.AppendLine();
                        }
                        sb.AppendLine("End component");
                    }
                }
            }
            //
            File.WriteAllText(_fileName, sb.ToString());
        }

    }
}
