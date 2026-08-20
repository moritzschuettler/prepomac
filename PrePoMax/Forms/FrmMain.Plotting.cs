// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using System;
using UserControls;

namespace PrePoMax
{
    public partial class FrmMain
    {
        private PlotPanel _plotPanel;

        private void tsmiPlotPanel_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized) return;
                //
                tsmiPlotPanel.Checked = !tsmiPlotPanel.Checked;
                if (tsmiPlotPanel.Checked)
                {
                    if (_plotPanel == null)
                    {
                        _plotPanel = new PlotPanel();
                        _plotPanel.CloseButtonPressedEvent += tsmiPlotPanel_Click;
                    }
                    //
                    ShowSidePanel(_plotPanel, 0.5, 100, 1000, false);
                }
                else HideSidePanel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
    }
}
