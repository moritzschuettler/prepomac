using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{   
    [Serializable]
    public class HistoryResultSetExporter
    {
        // Variables                                                                                                                
        private string _fileName;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        

        // Constructors                                                                                                             
        public HistoryResultSetExporter(string fileName)
        {
            _fileName = fileName;
        }


        // Methods                                                                                                                  


    }
}
