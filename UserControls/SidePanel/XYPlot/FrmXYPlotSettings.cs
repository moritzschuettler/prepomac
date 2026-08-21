// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Windows.Forms;

namespace UserControls
{
    public class FrmXYPlotSettings : FrmProperties
    {
        // Variables
        private Action _applyAction;


        // Constructors
        public FrmXYPlotSettings()
            : base(1.5)
        {
            _hideOnClose = false;
            _addNew = false;
            _applyAction = null;
            //
            btnOkAddNew.Text = "Apply";
            StartPosition = FormStartPosition.CenterParent;
        }


        // Methods
        protected override void OnPropertyGridPropertyValueChanged()
        {
            base.OnPropertyGridPropertyValueChanged();
            //
            IXYPlotSettings settings = propertyGrid.SelectedObject as IXYPlotSettings;
            if (settings != null)
            {
                settings.UpdateVisibility();
                propertyGrid.Refresh();
            }
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _applyAction?.Invoke();
            //
            if (!onOkAddNew) DialogResult = DialogResult.OK;
        }
        public DialogResult ShowSettings(IWin32Window owner, IXYPlotSettings settings, string title, Action applyAction)
        {
            Text = title;
            _applyAction = applyAction;
            //
            settings.InstallProvider();
            settings.UpdateVisibility();
            //
            propertyGrid.SelectedObject = settings;
            //
            return ShowDialog(owner);
        }
    }
}
