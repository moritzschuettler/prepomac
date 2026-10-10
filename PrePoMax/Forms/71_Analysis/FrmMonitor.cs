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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeGlobals;
using CaeJob;
using System.Runtime.InteropServices;

namespace PrePoMax.Forms
{
    public partial class FrmMonitor : UserControls.PrePoMaxChildForm
    {
        // Variables                                                                                                                
        private AnalysisJob _job;
        private Controller _controller;
        private string _plottedStatusData;
        private string _plottedConvergenceData;
        private int _plottedEnergyRevision;

        // Properties                                                                                                               


        // Events                                                                                                                   
        public event Action<string> KillJob;
        public event Action<string> Results;


        // Constructors                                                                                                             
        public FrmMonitor(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
        }


        // Event handlers                                                                                                           
        private void FrmMonitor_Shown(object sender, EventArgs e)
        {
            tbOutput.ScrollToEnd();
        }
        private void FrmMonitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            //
            btnClose_Click(null, null);
        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_job.JobStatus == JobStatus.Running)
            {
                if (tabControl1.SelectedTab == tpOutput)
                {
                    tbOutput.ScrollToEnd();
                }
                else if (tabControl1.SelectedTab == tpStatus)
                {
                    tbStatus.ScrollToEnd();
                }
                else if (tabControl1.SelectedTab == tpCovergence)
                {
                    tbConvergence.ScrollToEnd();
                }
            }
        }
        private void btnKill_Click(object sender, EventArgs e)
        {
            try
            {
                if (KillJob != null) KillJob(_job.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnResults_Click(object sender, EventArgs e)
        {
            try
            {
                // Function results hides this form if everything is ok
                if (Results != null) Results(_job.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (_job != null)
            {
                _job.DataOutputEvent -= UpdateOutput;
                _job = null;
            }
            Hide();
        }

        // Methods                                                                                                                  
        public void PrepareForm(string jobName)
        {
            _job = _controller.GetJob(jobName);
            _job.DataOutputEvent += UpdateOutput;
            analysisProgressView.SetTimeUnit(_controller.Model.UnitSystem.TimeUnitAbbreviation);
            analysisProgressView.SetEnergyUnit(_controller.Model.UnitSystem.EnergyUnitAbbreviation);
            //
            //if (_job.FEMSolver == FEMSolverEnum.Calculix) btnResults.Enabled = true;
            //else if (_job.FEMSolver == FEMSolverEnum.Abaqus) btnResults.Enabled = false;
            //else throw new NotSupportedException();
            //
            UpdateProgress();
            _plottedStatusData = null;
            _plottedConvergenceData = null;
            _plottedEnergyRevision = -1;
            analysisProgressView.ClearData();
            //
            tbOutput.Text = _job.GetAllOutputData();
            tbOutput.ScrollToEnd();
            //
            tbStatus.Text = _job.StatusFileData;
            tbStatus.ScrollToEnd();
            //
            tbConvergence.Text = _job.ConvergenceFileData;
            tbConvergence.ScrollToEnd();
            //
            tbDatFile.Text = _job.DatFileData;
            tbDatFile.ScrollToEnd();
            //
            UpdateProgressPlots();
        }

        private void UpdateOutput(AnalysisJob job, string data)
        {
            try
            {
                if (this.tbOutput.InvokeRequired)
                {
                    // This is a worker thread so delegate the task.
                    this.BeginInvoke(new MethodInvoker(() => UpdateOutput(job, data)));
                }
                else
                {
                    if (_job != null)
                    {
                        // It's on the same thread, no need for Invoke
                        tbOutput.AutoScrollAppendText(data);
                        tbDatFile.AutoScrollSetText(_job.DatFileData);
                        tbStatus.AutoScrollSetText(_job.StatusFileData);
                        tbConvergence.AutoScrollSetText(_job.ConvergenceFileData);
                        UpdateProgressPlots();
                    }
                }
            }
            catch
            { }
        }
        private void UpdateProgressPlots()
        {
            string statusData = _job.StatusFileData;
            if (!object.ReferenceEquals(statusData, _plottedStatusData))
            {
                analysisProgressView.SetStatusData(CalculixMonitorDataParser.ParseStatus(statusData));
                _plottedStatusData = statusData;
            }

            string convergenceData = _job.ConvergenceFileData;
            if (!object.ReferenceEquals(convergenceData, _plottedConvergenceData))
            {
                analysisProgressView.SetConvergenceData(CalculixMonitorDataParser.ParseConvergence(convergenceData));
                _plottedConvergenceData = convergenceData;
            }
            //
            CalculixEnergyData energyData = _job.GetEnergyData();
            if (energyData.Revision != _plottedEnergyRevision)
            {
                analysisProgressView.SetEnergyData(energyData);
                _plottedEnergyRevision = energyData.Revision;
            }
        }
        public void UpdateProgress()
        {
            if (_job != null)
            {
                if (_job.JobStatus == JobStatus.Running)
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Marquee;
                    labAnalysisStatus.Text = "      " + _job.JobStatus.ToString();
                    labAnalysisStatus.Image = Properties.Resources.Running;
                }
                else if (_job.JobStatus == JobStatus.OK)
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Blocks;
                    labAnalysisStatus.Text = "      " + "Finished";
                    labAnalysisStatus.Image = Properties.Resources.OK;
                }
                else if (_job.JobStatus == JobStatus.FailedWithResults)
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Blocks;
                    labAnalysisStatus.Text = "      " + "Failed with results";
                    labAnalysisStatus.Image = Properties.Resources.Warning;
                }
                else
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Blocks;
                    labAnalysisStatus.Text = "      " + _job.JobStatus.ToString();
                    labAnalysisStatus.Image = Properties.Resources.NoResult;
                }
            }
        }
    }
}
