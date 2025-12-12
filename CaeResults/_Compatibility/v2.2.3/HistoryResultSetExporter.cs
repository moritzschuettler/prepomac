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
    }
}
