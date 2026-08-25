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

namespace PrePoMax
{
    public partial class FrmViewResultHistoryOutput : Form
    {
        // Variables                                                                                                                
        private Controller _controller;


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public FrmViewResultHistoryOutput(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            dgvHistory.EnableCutMenu = false;
            dgvHistory.EnablePasteMenu = false;
        }


        // Event handlers                                                                                                           
        private void FrmHistoryOutput_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                dgvHistory.HidePlot();
                Hide();
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            dgvHistory.HidePlot();
            Hide();
        }


        // Methods                                                                                                                  
        public void SetData(string[] columnNames, object[][] rowBasedData)
        {
            DataGridViewColumnHeadersHeightSizeMode columnHeadersHeightSizeMode = dgvHistory.ColumnHeadersHeightSizeMode;
            dgvHistory.SuspendLayout();
            try
            {
                dgvHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgvHistory.Rows.Clear();
                dgvHistory.Columns.Clear();
                //
                int maxNumCol = _controller.Settings.Post.MaxHistoryEntriesToShow;
                if (maxNumCol < 1)
                    throw new NotSupportedException("The maximum number of history output entries to show is too small." +
                                                    " Use Tools -> Settings to increase it.");
                //
                if (columnNames == null) return;
                //
                if (columnNames.Length > maxNumCol + 1)
                {
                    CaeGlobals.MessageBoxes.ShowWarning("Only first " + maxNumCol + " columns of " +
                                                        (columnNames.Length - 1) + " will be displayed." +
                                                        Environment.NewLine +
                                                        " Use Settings → Post-processing to increase this value.");
                }
                //
                int columnCount = Math.Min(columnNames.Length, maxNumCol + 1);
                DataGridViewColumn[] columns = new DataGridViewColumn[columnCount];
                for (int i = 0; i < columnCount; i++)
                {
                    DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                    column.HeaderText = columnNames[i];
                    column.FillWeight = 1;
                    column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
                    column.SortMode = DataGridViewColumnSortMode.Automatic;
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    columns[i] = column;
                }
                dgvHistory.Columns.AddRange(columns);
                //
                if (rowBasedData.Length > 0)
                {
                    DataGridViewRow[] rows = new DataGridViewRow[rowBasedData.Length];
                    for (int i = 0; i < rowBasedData.Length; i++)
                    {
                        DataGridViewRow row = new DataGridViewRow();
                        row.CreateCells(dgvHistory, rowBasedData[i]);
                        rows[i] = row;
                    }
                    dgvHistory.Rows.AddRange(rows);
                }
                dgvHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
            finally
            {
                dgvHistory.ColumnHeadersHeightSizeMode = columnHeadersHeightSizeMode;
                dgvHistory.ResumeLayout(false);
            }
        }
    }
}
