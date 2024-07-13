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
    public class CreateFileNameEditor : UITypeEditor
    {
        public static string WorkDirectory;
        public static string FileName;
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService =
                (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            //
            if (editorService != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Comma separated values | *.csv";
                    saveFileDialog.InitialDirectory = WorkDirectory;
                    if (value == null || (value is string stringValue && stringValue == "")) value = FileName;
                    //
                    saveFileDialog.FileName = value as string;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        return saveFileDialog.FileName;
                    }
                }
            }
            //
            return value;
        }
    }

    [Serializable]
    public class ViewHistoryResultSetExporter : ViewMultiRegion
    {
        // Variables                                                                                                                
        private HistoryResultSetExporter _historyResultSetExporter;
        private MultiChoiceContainer _historyOutputNamesContainer;
        private string[] _allHistoryOutputNames;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "File name")]
        [DescriptionAttribute("Select the file name for history output export.")]
        [EditorAttribute(typeof(CreateFileNameEditor), typeof(UITypeEditor))]
        [Id(1, 1)]
        public string FileName
        {
            get { return _historyResultSetExporter.FileName; }
            set { _historyResultSetExporter.FileName = value; }
        }
        //
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
                _historyResultSetExporter.HistoryOutputNames = _historyOutputNamesContainer.Names;
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
            get { return _historyResultSetExporter.Delimiter; }
            set { _historyResultSetExporter.Delimiter = value; }
        }


        // Constructors                                                                                                             
        public ViewHistoryResultSetExporter(HistoryResultSetExporter historyResultSetExporter)
        {
            CreateFileNameEditor.WorkDirectory = historyResultSetExporter.WorkingDirectory;
            CreateFileNameEditor.FileName = HistoryResultSetExporter.DefaultFileName;
            _historyResultSetExporter = historyResultSetExporter;
            //
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods
        public HistoryResultSetExporter GetBase()
        {
            return _historyResultSetExporter;
        }
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
                _historyResultSetExporter.HistoryOutputNames = _historyOutputNamesContainer.Names;
            }
        }
    }
}
