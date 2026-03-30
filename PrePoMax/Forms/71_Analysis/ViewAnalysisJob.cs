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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Management;
using System.ComponentModel;
using CaeJob;
using CaeGlobals;
using System.Drawing.Design;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    

    [Serializable]
    public class ViewAnalysisJob
    {
        // Variables                                                                                                                
        protected AnalysisJob _job;
        protected Dictionary<FEMSolverEnum, string[]> _solverSettings;
        protected DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the analysis.")]
        [Id(1, 1)]
        public string Name { get { return _job.Name; } set { _job.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "FEM solver")]
        [DescriptionAttribute("FEM solver to be used for analysis.")]
        [Id(2, 1)]
        public FEMSolverEnum FEMSolver
        {
            get { return _job.FEMSolver; }
            set
            {
                _job.FEMSolver = value;
                UpdateSolverSettings();
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Executable")]
        [DescriptionAttribute("FEM solver executable file.")]
        [Id(3, 1)]
        public string Executable
        {
            get { return _job.Executable; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "Executable arguments")]
        [DescriptionAttribute("Additional CMD arguments. " + 
                              "Change this value only if you want to run the solver in a different way.")]
        [Id(4, 1)]
        public string Argument
        {
            get { return _job.Argument; }
            set { _job.Argument = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(4, 10, "Work directory")]
        [DescriptionAttribute("Work directory.")]
        [Id(5, 1)]
        public string WorkDirectory { get { return _job.WorkDirectory; } }
        //
        [CategoryAttribute("Advanced settings")]
        [OrderedDisplayName(0, 10, "Compatibility mode")]
        [DescriptionAttribute("Run executable in a simplified mode for improved compatibility.")]
        [Id(1, 2)]
        public bool CompatibilityMode { get { return _job.CompatibilityMode; } set { _job.CompatibilityMode = value; } }


        // Constructor                                                                                                              
        public ViewAnalysisJob(AnalysisJob job)
        {
            _job = job;
            _solverSettings = new Dictionary<FEMSolverEnum, string[]>();
            _dctd = ProviderInstaller.Install(this);
            //
            _dctd.RenameBooleanPropertyToOnOff(nameof(CompatibilityMode));
        }


        // Methods                                                                                                                  
        public AnalysisJob GetBase()
        {
            return _job;
        }
        public void PopulateSolverSettings(FEMSolverEnum[] solvers, string[] executables, string[] workDirectories)
        {
            _solverSettings.Clear();
            //
            if (solvers.Length == workDirectories.Length && workDirectories.Length == executables.Length)
            {
                for (int i = 0; i < solvers.Length; i++)
                {
                    _solverSettings.Add(solvers[i], new string[] { executables[i], workDirectories[i] });
                }
            }
            else throw new NotSupportedException();
            //
            UpdateSolverSettings();
        }
        private void UpdateSolverSettings()
        {
            string[] settings = _solverSettings[_job.FEMSolver];
            _job.Executable = settings[0];
            _job.WorkDirectory = settings[1];
        }
    }
}
