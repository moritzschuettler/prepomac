// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
    public class CreateFileNameEditor : UITypeEditor
    {
        public static string FileName;
        public static string Filter;
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
                    if (value == null || (value is string stringValue && stringValue == "")) value = FileName;
                    //
                    saveFileDialog.FileName = Path.GetFileName(value as string);
                    saveFileDialog.InitialDirectory = Path.GetDirectoryName(value as string);
                    saveFileDialog.Filter = Filter;
                    //
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
    public class ViewExportFileProperties
    {
        // Variables                                                                                                                
        protected DynamicCustomTypeDescriptor _dctd = null;
        protected ExportFileProperties _properties;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "File name")]
        [DescriptionAttribute("Select the file name for the exported file.")]
        [EditorAttribute(typeof(CreateFileNameEditor), typeof(UITypeEditor))]
        [Id(1, 1)]
        public string FileName { get { return _properties.FileName; } set { _properties.FileName = value; } }


        // Constructors                                                                                                             
        public ViewExportFileProperties(ExportFileProperties exportFileProperties)
        {
            _properties = exportFileProperties;
            //
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public ExportFileProperties GetBase()
        {
            return _properties;
        }
        public string GetFilter()
        {
            return CreateFileNameEditor.Filter;
        }
    }
}

