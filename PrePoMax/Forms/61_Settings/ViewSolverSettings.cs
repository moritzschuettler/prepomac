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
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;
using CaeGlobals;
using CaeJob;
using CaeMesh;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewSolverSettings : IViewSettings, IReset
    {
        // Variables
        private SolverSettings _solverSettings;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties
        [CategoryAttribute("General")]
        [OrderedDisplayName(0, 10, "Work directory")]
        [DescriptionAttribute("Select the common work directory for all FEM solvers.")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(UITypeEditor))]
        [Id(1, 1)]
        public string WorkDirectory
        {
            get { return _solverSettings.WorkDirectoryForSettingsOnly; }
            set
            {
                if (value != _solverSettings.WorkDirectoryForSettingsOnly)
                {
                    if (!Directory.Exists(value) &&
                        MessageBoxes.ShowWarningQuestionOKCancel("The selected work directory does not exist.") ==
                        DialogResult.Cancel)
                        return;
                    _solverSettings.WorkDirectoryForSettingsOnly = value;
                }
            }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(1, 10, "Use .pmx folder as work directory")]
        [DescriptionAttribute("Select yes to use the .pmx file folder as the work directory.")]
        [Id(2, 1)]
        public bool UsePmxFolderAsWorkDirectory
        {
            get { return _solverSettings.UsePmxFolderAsWorkDirectory; }
            set { _solverSettings.UsePmxFolderAsWorkDirectory = value; }
        }
        //
        [CategoryAttribute("General")]
        [OrderedDisplayName(2, 10, "Environment variables")]
        [DescriptionAttribute("Set the additional environment variables needed for FEM solver to run.")]
        [Editor(typeof(Forms.EnvVarsUIEditor), typeof(UITypeEditor))]
        [Id(3, 1)]
        public List<EnvironmentVariable> CalculixEnvironmentVariables
        {
            get { return _solverSettings.EnvironmentVariables; }
            set { _solverSettings.EnvironmentVariables = value; }
        }
        //
        [CategoryAttribute("Parallelization")]
        [OrderedDisplayName(0, 10, "Number of processors")]
        [DescriptionAttribute("Set the number of processors used by the selected FEM solver.")]
        [Id(1, 2)]
        public int NumCPUs
        {
            get { return _solverSettings.NumCPUs; }
            set { _solverSettings.NumCPUs = value; }
        }
        //
        [CategoryAttribute("CalculiX")]
        [OrderedDisplayName(0, 10, "Executable")]
        [DescriptionAttribute("Select the CalculiX executable file (ccx.exe).")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
        [Id(1, 3)]
        public string CalculixExe
        {
            get { return _solverSettings.CalculixExe; }
            set { _solverSettings.CalculixExe = value; }
        }
        //
        [CategoryAttribute("CalculiX")]
        [OrderedDisplayName(1, 10, "Default matrix solver")]
        [DescriptionAttribute("Select the default matrix solver type.")]
        [Id(2, 3)]
        public SolverTypeEnum CalculixDefaultSolverType
        {
            get { return _solverSettings.CalculixDefaultSolverType; }
            set { _solverSettings.CalculixDefaultSolverType = value; }
        }
        //
        [CategoryAttribute("CalculiX")]
        [OrderedDisplayName(3, 10, "Convert pyramid elements to")]
        [DescriptionAttribute("Currently unsupported pyramid elements can be converted to collapsed wedges or hexahedrons.")]
        [Id(4, 3)]
        public ConvertPyramidsToEnum ConvertPyramidsTo
        {
            get { return _solverSettings.ConvertPyramidsTo; }
            set { _solverSettings.ConvertPyramidsTo = value; }
        }
        //
        [CategoryAttribute("CalculiX")]
        [OrderedDisplayName(4, 10, "Binary results file")]
        [DescriptionAttribute("Select yes to write the results file (.frd) in binary. It is about a third of the size " +
                              "and opens faster. A step with contact energy (CELS) or PCON output stays ASCII.")]
        [Id(5, 3)]
        public bool BinaryResultsFile
        {
            get { return _solverSettings.BinaryResultsFile; }
            set { _solverSettings.BinaryResultsFile = value; }
        }
        //
        [CategoryAttribute("Abaqus")]
        [OrderedDisplayName(0, 10, "Executable")]
        [DescriptionAttribute("Select the Abaqus executable file.")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
        [Id(1, 4)]
        public string AbaqusExe
        {
            get { return _solverSettings.AbaqusExe; }
            set { _solverSettings.AbaqusExe = value; }
        }
        //
        [CategoryAttribute("FEMaster")]
        [OrderedDisplayName(0, 10, "Executable")]
        [DescriptionAttribute("Select the FEMaster executable file (FEMaster.exe).")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
        [Id(1, 5)]
        public string FEMasterExe
        {
            get { return _solverSettings.FEMasterExe; }
            set { _solverSettings.FEMasterExe = value; }
        }
        //
        [CategoryAttribute("Code_Aster")]
        [OrderedDisplayName(0, 10, "Docker program")]
        [DescriptionAttribute("The docker program that runs the Code_Aster image.")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
        [Id(1, 7)]
        public string CodeAsterDocker
        {
            get { return _solverSettings.CodeAsterDocker; }
            set { _solverSettings.CodeAsterDocker = value; }
        }
        [CategoryAttribute("Code_Aster")]
        [OrderedDisplayName(1, 10, "Docker context")]
        [DescriptionAttribute("The Docker context the image runs in, empty for the current one. On Apple Silicon a " +
                              "colima profile with Rosetta: colima start aster --vm-type vz --vz-rosetta.")]
        [Id(2, 7)]
        public string CodeAsterContext
        {
            get { return _solverSettings.CodeAsterContext; }
            set { _solverSettings.CodeAsterContext = value; }
        }
        [CategoryAttribute("Code_Aster")]
        [OrderedDisplayName(2, 10, "Image")]
        [DescriptionAttribute("The Docker image with Code_Aster (built by macOS/Native/CodeAster/build.sh).")]
        [Id(3, 7)]
        public string CodeAsterImage
        {
            get { return _solverSettings.CodeAsterImage; }
            set { _solverSettings.CodeAsterImage = value; }
        }


        // Constructors
        public ViewSolverSettings(SolverSettings solverSettings)
        {
            _solverSettings = solverSettings;
            _dctd = ProviderInstaller.Install(this);
            _dctd.RenameBooleanPropertyToYesNo(nameof(UsePmxFolderAsWorkDirectory));
            _dctd.RenameBooleanPropertyToYesNo(nameof(BinaryResultsFile));
        }


        // Methods
        public ISettings GetBase()
        {
            return _solverSettings;
        }
        public void Reset()
        {
            _solverSettings.Reset();
        }
    }
}
