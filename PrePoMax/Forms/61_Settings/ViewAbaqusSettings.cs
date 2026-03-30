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
using FileInOut.Output.Calculix;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewAbaqusSettings : IViewSettings, IReset
    {
        // Variables                                                                                                                
        private AbaqusSettings _abaqusSettings;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Abaqus")]
        [OrderedDisplayName(0, 10, "Work directory")]
        [DescriptionAttribute("Select the work directory.")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(UITypeEditor))]
        [Id(1, 1)]
        public string WorkDirectory
        {
            get { return _abaqusSettings.WorkDirectoryForSettingsOnly; }
            set
            {
                if (value != _abaqusSettings.WorkDirectoryForSettingsOnly)
                {
                    if (!Directory.Exists(value) &&
                        MessageBoxes.ShowWarningQuestionOKCancel("The selected work directory does not exist.") ==
                        DialogResult.Cancel)
                        return;
                    //
                    _abaqusSettings.WorkDirectoryForSettingsOnly = value;
                }
            }
        }
        //
        [CategoryAttribute("Abaqus")]
        [OrderedDisplayName(1, 10, "Use .pmx folder as work directory")]
        [DescriptionAttribute("Select yes to use .pmx file folder as a work directory.")]
        [Id(2, 1)]
        public bool UsePmxFolderAsWorkDirectory
        {
            get { return _abaqusSettings.UsePmxFolderAsWorkDirectory; }
            set { _abaqusSettings.UsePmxFolderAsWorkDirectory = value; }
        }
        //
        [CategoryAttribute("Abaqus")]
        [OrderedDisplayName(2, 10, "Executable")]
        [DescriptionAttribute("Select the Abaqus executable file.")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
        [Id(3, 1)]
        public string AbaqusExe
        {
            get { return _abaqusSettings.AbaqusExe; }
            set { _abaqusSettings.AbaqusExe = value; }
        }
        //
        [CategoryAttribute("Parallelization")]
        [OrderedDisplayName(0, 10, "Number of processors")]
        [DescriptionAttribute("Set the number of processors for the executable to use.")]
        [Id(1, 2)]
        public int NumCPUs
        {
            get { return _abaqusSettings.NumCPUs; }
            set { _abaqusSettings.NumCPUs = value; }
        }


        // Constructors                                                                                                             
        public ViewAbaqusSettings(AbaqusSettings abaqusSettings)
        {
            _abaqusSettings = abaqusSettings;
            _dctd = ProviderInstaller.Install(this);
            // Now lets display Yes/No instead of True/False
            _dctd.RenameBooleanPropertyToYesNo(nameof(UsePmxFolderAsWorkDirectory));
        }

        // Methods                                                                                                                  
        public ISettings GetBase()
        {
            return _abaqusSettings;
        }
        public void Reset()
        {
            _abaqusSettings.Reset();
        }
    }

}
