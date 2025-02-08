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
    public class ViewCalculixSettings : IViewSettings, IReset
    {
        // Variables                                                                                                                
        private CalculixSettings _calculixSettings;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Calculix")]
        [OrderedDisplayName(0, 10, "Work directory")]
        [DescriptionAttribute("Select the work directory.")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(UITypeEditor))]
        [Id(1, 1)]
        public string WorkDirectory
        {
            get { return _calculixSettings.WorkDirectoryForSettingsOnly; }
            set
            {
                if (value != _calculixSettings.WorkDirectoryForSettingsOnly)
                {
                    if (!Directory.Exists(value) &&
                        MessageBoxes.ShowWarningQuestionOKCancel("The selected work directory does not exist.") == DialogResult.Cancel)
                        return;
                    else _calculixSettings.WorkDirectoryForSettingsOnly = value;
                }
            }
        }
        //
        [CategoryAttribute("Calculix")]
        [OrderedDisplayName(1, 10, "Use .pmx folder as work directory")]
        [DescriptionAttribute("Select yes to use .pmx file folder as a work directory.")]
        [Id(2, 1)]
        public bool UsePmxFolderAsWorkDirectory
        {
            get { return _calculixSettings.UsePmxFolderAsWorkDirectory; }
            set { _calculixSettings.UsePmxFolderAsWorkDirectory = value; }
        }
        //
        [CategoryAttribute("Calculix")]
        [OrderedDisplayName(2, 10, "Executable")]
        [DescriptionAttribute("Select the calculix executable file (ccx.exe).")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
        [Id(3, 1)]
        public string CalculixExe
        {
            get { return _calculixSettings.CalculixExe; }
            set { _calculixSettings.CalculixExe = value; }
        }
        //
        [CategoryAttribute("Calculix")]
        [OrderedDisplayName(3, 10, "Default solver")]
        [DescriptionAttribute("Select the default matrix solver type.")]
        [Id(4, 1)]
        public CaeModel.SolverTypeEnum DefaultSolverType
        {
            get { return _calculixSettings.DefaultSolverType; }
            set { _calculixSettings.DefaultSolverType = value; }
        }
        //
        [CategoryAttribute("Parallelization")]
        [OrderedDisplayName(0, 10, "Number of processors")]
        [DescriptionAttribute("Set the number of processors for the executable to use (OMP_NUM_THREADS = n).")]
        [Id(1, 2)]
        public int NumCPUs
        {
            get { return _calculixSettings.NumCPUs; }
            set { _calculixSettings.NumCPUs = value; }
        }
        //
        [CategoryAttribute("Parallelization")]
        [OrderedDisplayName(1, 10, "Environment variables")]
        [DescriptionAttribute("Add additional environment variables needed for the executable to run.")]
        [Editor(typeof(Forms.EnvVarsUIEditor), typeof(UITypeEditor))]
        [Id(2, 2)]
        public List<CaeJob.EnvironmentVariable> EnvironmentVariables
        {
            get { return _calculixSettings.EnvironmentVariables; }
            set { _calculixSettings.EnvironmentVariables = value; }
        }
        //
        [CategoryAttribute("Experimental")]
        [OrderedDisplayName(0, 10, "Convert pyramid elements to")]
        [DescriptionAttribute("Currently unsupported pyramid elements can be converted to collapsed wedges or hexahedrons.")]
        [Id(1, 3)]
        public ConvertPyramidsToEnum ConvertPyramidsTo
        {
            get { return _calculixSettings.ConvertPyramidsTo; }
            set { _calculixSettings.ConvertPyramidsTo = value; }
        }


        // Constructors                                                                                                             
        public ViewCalculixSettings(CalculixSettings calculixSettings)
        {
            _calculixSettings = calculixSettings;
            _dctd = ProviderInstaller.Install(this);
            // Now lets display Yes/No instead of True/False
            _dctd.RenameBooleanPropertyToYesNo(nameof(UsePmxFolderAsWorkDirectory));
        }

        // Methods                                                                                                                  
        public ISettings GetBase()
        {
            return _calculixSettings;
        }
        public void Reset()
        {
            _calculixSettings.Reset();
        }
    }

}
