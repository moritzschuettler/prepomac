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

namespace PrePoMax
{
    public class CreateFileNameEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            //
            if (editorService != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Comma separated values | *.csv";
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


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "File name")]
        [DescriptionAttribute("Select the file name for history output export.")]
        [EditorAttribute(typeof(CreateFileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Id(1, 1)]
        public string FileName
        {
            get { return _historyResultSetExporter.FileName; }
            set { _historyResultSetExporter.FileName = value; }
        }
        

        // Constructors                                                                                                             
        public ViewHistoryResultSetExporter(HistoryResultSetExporter historyResultSetExporter)
        {
            _historyResultSetExporter = historyResultSetExporter;
        }


        // Methods
        public HistoryResultSetExporter GetBase()
        {
            return _historyResultSetExporter;
        }
    }
}
