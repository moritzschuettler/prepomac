using CaeGlobals;
using CaeMesh;
using CaeResults;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace PrePoMax
{
    [Serializable]
    public class ExportFileProperties
    {
        // Variables                                                                                                                
        private string _fileName;
        private string _filter;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public string Filter { get { return _filter; } set { _filter = value; } }


        // Constructors                                                                                                             
        public ExportFileProperties(string fileName, string filter)
        {
            _fileName = fileName;
            _filter = filter;
        }


        // Methods                                                                                                                  
        
    }
}

