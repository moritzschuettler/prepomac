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
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;
using CaeResults;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using static System.Windows.Forms.Design.AxImporter;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Windows.Forms;
using CaeMesh;

namespace PrePoMax
{
    [Serializable]
    public class ViewExportHistoryResultSetFileProperties : ViewExportFileProperties
    {
        // Variables                                                                                                                
        private MultiChoiceContainer _historyOutputNamesContainer;
        private string[] _allHistoryOutputNames;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "History outputs")]
        [DescriptionAttribute("Select history outputs to be exported.")]
        [Id(2, 1)]
        public MultiChoiceEnum HistoryOutputNames
        {
            get
            {
                if (_historyOutputNamesContainer == null) return MultiChoiceEnum.Num1;   // at initialization
                else return _historyOutputNamesContainer.MultiChoice;
            }
            set
            {
                _historyOutputNamesContainer.MultiChoice = value;
                (_properties as ExportHistoryResultSetFileProperties).HistoryOutputNames = _historyOutputNamesContainer.Names;
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Delimiter")]
        [DescriptionAttribute("Select the delimiter to use between exported values.")]
        [EditorAttribute(typeof(CreateFileNameEditor), typeof(UITypeEditor))]
        [Id(3, 1)]
        public string Delimiter
        {
            get { return (_properties as ExportHistoryResultSetFileProperties).Delimiter; }
            set { (_properties as ExportHistoryResultSetFileProperties).Delimiter = value; }
        }


        // Constructors                                                                                                             
        public ViewExportHistoryResultSetFileProperties(ExportHistoryResultSetFileProperties properties)
            : base(properties)
        {
            PopulateDropDownLists(properties.HistoryOutputNames);
        }


        // Methods
        public void PopulateDropDownLists(string[] historyOutputNames)
        {
            _allHistoryOutputNames = historyOutputNames;
            UpdateHistoryOutputNames(_allHistoryOutputNames);
            // Delimiters
            _dctd.PopulateProperty(nameof(Delimiter), HistoryResultSetExporter.DefaultDelimiters);
        }
        private void UpdateHistoryOutputNames(string[] selectedHistoryOutputNames = null)
        {
            if (_allHistoryOutputNames != null && _allHistoryOutputNames.Length > 0)
            {
                if (selectedHistoryOutputNames == null) selectedHistoryOutputNames = _allHistoryOutputNames;
                _historyOutputNamesContainer = new MultiChoiceContainer(_allHistoryOutputNames, selectedHistoryOutputNames);
                _dctd.RenameMultiChoiceEnumProperty(nameof(HistoryOutputNames), _historyOutputNamesContainer.EnumData);
                //
                (_properties as ExportHistoryResultSetFileProperties).HistoryOutputNames = _historyOutputNamesContainer.Names;
            }
        }
    }
}
