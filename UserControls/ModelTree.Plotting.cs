// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeResults;
using System;
using System.Windows.Forms;

namespace UserControls
{
    public partial class ModelTree
    {
        private ToolStripSeparator _tsmiSpacePlot;
        private ToolStripMenuItem _tsmiAddToPlot;
        private ToolStripMenuItem _tsmiAddToNewPlot;
        private ToolStripMenuItem _tsmiCombineHistoryOutputs;

        public event Action<HistoryResultData> AddHistoryOutputToPlotEvent;
        public event Action<HistoryResultData> AddHistoryOutputToNewPlotEvent;
        public event Action<HistoryResultData[]> CombineHistoryOutputsEvent;

        public Func<bool> CanAddHistoryOutputToPlot;
        public Func<bool> CanAddHistoryOutputToNewPlot;

        private void InitializePlotContextMenu()
        {
            _tsmiSpacePlot = new ToolStripSeparator();
            _tsmiAddToPlot = new ToolStripMenuItem("Add to Plot");
            _tsmiAddToNewPlot = new ToolStripMenuItem("Add to New Plot");
            _tsmiCombineHistoryOutputs = new ToolStripMenuItem("Combine History Outputs");
            _tsmiCombineHistoryOutputs.ToolTipText = "The first selected history output supplies X; all entries of the second supply Y columns.";
            _tsmiAddToPlot.Click += AddToPlot_Click;
            _tsmiAddToNewPlot.Click += AddToNewPlot_Click;
            _tsmiCombineHistoryOutputs.Click += CombineHistoryOutputs_Click;

            int index = cmsTree.Items.IndexOf(tsmiPreview) + 1;
            cmsTree.Items.Insert(index++, _tsmiSpacePlot);
            cmsTree.Items.Insert(index++, _tsmiAddToPlot);
            cmsTree.Items.Insert(index++, _tsmiAddToNewPlot);
            _tsmiSpacePlot.Visible = false;
            _tsmiAddToPlot.Visible = false;
            _tsmiAddToNewPlot.Visible = false;
            _tsmiCombineHistoryOutputs.Visible = false;
        }

        private void PreparePlotContextMenu(TreeNode[] actionNodes, ref bool oneAboveVisible)
        {
            bool singleVisible = actionNodes.Length == 1 && actionNodes[0].Tag is HistoryResultData;
            bool combineVisible = actionNodes.Length == 2 &&
                                  actionNodes[0].Tag is HistoryResultData &&
                                  actionNodes[1].Tag is HistoryResultData;
            bool plotVisible = singleVisible;
            _tsmiSpacePlot.Visible = plotVisible && oneAboveVisible;
            _tsmiAddToPlot.Visible = singleVisible;
            _tsmiAddToNewPlot.Visible = singleVisible;
            _tsmiCombineHistoryOutputs.Visible = combineVisible;
            _tsmiAddToPlot.Enabled = singleVisible &&
                                     (CanAddHistoryOutputToPlot == null || CanAddHistoryOutputToPlot());
            _tsmiAddToNewPlot.Enabled = singleVisible &&
                                        (CanAddHistoryOutputToNewPlot == null || CanAddHistoryOutputToNewPlot());
            _tsmiCombineHistoryOutputs.Enabled = combineVisible;
            oneAboveVisible |= plotVisible;
        }

        private HistoryResultData[] GetSelectedHistoryResultData()
        {
            TreeNode[] nodes = GetSelectedNodesIncludingGroupItems(GetActiveTree());
            HistoryResultData[] data = new HistoryResultData[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                data[i] = nodes[i].Tag as HistoryResultData;
                if (data[i] == null) return new HistoryResultData[0];
            }
            return data;
        }

        private void AddToPlot_Click(object sender, EventArgs e)
        {
            HistoryResultData[] data = GetSelectedHistoryResultData();
            if (data.Length == 1) AddHistoryOutputToPlotEvent?.Invoke(data[0]);
        }

        private void AddToNewPlot_Click(object sender, EventArgs e)
        {
            HistoryResultData[] data = GetSelectedHistoryResultData();
            if (data.Length == 1) AddHistoryOutputToNewPlotEvent?.Invoke(data[0]);
        }

        private void CombineHistoryOutputs_Click(object sender, EventArgs e)
        {
            HistoryResultData[] data = GetSelectedHistoryResultData();
            if (data.Length == 2) CombineHistoryOutputsEvent?.Invoke(data);
        }
    }
}
