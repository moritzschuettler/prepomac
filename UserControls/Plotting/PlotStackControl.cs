// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UserControls
{
    public class PlotStackControl : Panel
    {
        public const int MaximumPlotCount = 4;
        private const int SplitterThickness = 6;
        private const int PreferredMinimumPlotHeight = 80;

        private readonly List<PlotRowControl> _plots;
        private readonly List<Panel> _splitters;
        private readonly List<double> _heightFractions;
        private int _activePlotIndex;
        private int _dragSplitterIndex;
        private int _dragStartY;
        private int _dragUpperHeight;
        private int _dragLowerHeight;
        private double _dragPairFraction;

        public int PlotCount { get { return _plots.Count; } }
        public int ActivePlotIndex { get { return _activePlotIndex; } }

        public event EventHandler<PlotEventArgs> ActivePlotChanged;
        public event EventHandler<PlotEventArgs> DataRequested;
        public event EventHandler<PlotEventArgs> RemoveRequested;

        public PlotStackControl()
        {
            _plots = new List<PlotRowControl>();
            _splitters = new List<Panel>();
            _heightFractions = new List<double>();
            _activePlotIndex = -1;
            _dragSplitterIndex = -1;
            DoubleBuffered = true;
            AutoScroll = false;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
        }

        public void SetPlots(IList<PlotViewData> data)
        {
            int count = data == null ? 0 : data.Count;
            if (count > MaximumPlotCount)
                throw new ArgumentOutOfRangeException("data", "A plot panel can contain at most four plots.");

            int previousCount = _plots.Count;
            int previousActivePlotIndex = _activePlotIndex;
            bool countChanged = count != previousCount;
            if (countChanged) RecreateRows(count);

            for (int i = 0; i < count; i++)
            {
                _plots[i].PlotIndex = i;
                _plots[i].SetData(data[i]);
            }

            if (count == 0) _activePlotIndex = -1;
            else if (count > previousCount) _activePlotIndex = count - 1;
            else if (countChanged) _activePlotIndex = Math.Min(Math.Max(0, previousActivePlotIndex), count - 1);
            else if (_activePlotIndex < 0 || _activePlotIndex >= count) _activePlotIndex = 0;
            UpdateActiveRows();
            PerformLayout();
        }

        public Rectangle GetPlotBounds(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= _plots.Count)
                throw new ArgumentOutOfRangeException("plotIndex");
            return _plots[plotIndex].Bounds;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_plots.Count == 0) return;

            int availableHeight = Math.Max(0, ClientSize.Height - _splitters.Count * SplitterThickness);
            NormalizeFractions();
            int y = 0;
            int usedHeight = 0;
            double cumulativeFraction = 0;
            for (int i = 0; i < _plots.Count; i++)
            {
                int height;
                if (i == _plots.Count - 1) height = availableHeight - usedHeight;
                else
                {
                    cumulativeFraction += _heightFractions[i];
                    height = (int)Math.Round(availableHeight * cumulativeFraction) - usedHeight;
                }
                height = Math.Max(0, height);
                _plots[i].SetBounds(0, y, ClientSize.Width, height);
                y += height;
                usedHeight += height;
                if (i < _splitters.Count)
                {
                    _splitters[i].SetBounds(0, y, ClientSize.Width, SplitterThickness);
                    y += SplitterThickness;
                }
            }
        }

        private void RecreateRows(int count)
        {
            SuspendLayout();
            Controls.Clear();
            for (int i = 0; i < _plots.Count; i++) _plots[i].Dispose();
            for (int i = 0; i < _splitters.Count; i++) _splitters[i].Dispose();
            _plots.Clear();
            _splitters.Clear();
            _heightFractions.Clear();

            for (int i = 0; i < count; i++)
            {
                PlotRowControl plot = new PlotRowControl();
                plot.PlotIndex = i;
                plot.Activated += Plot_Activated;
                plot.DataRequested += Plot_DataRequested;
                plot.RemoveRequested += Plot_RemoveRequested;
                _plots.Add(plot);
                _heightFractions.Add(1.0 / count);
                Controls.Add(plot);

                if (i < count - 1)
                {
                    Panel splitter = new Panel();
                    splitter.Tag = i;
                    splitter.Cursor = Cursors.HSplit;
                    splitter.BackColor = SystemColors.ControlDark;
                    splitter.MouseDown += Splitter_MouseDown;
                    splitter.MouseMove += Splitter_MouseMove;
                    splitter.MouseUp += Splitter_MouseUp;
                    _splitters.Add(splitter);
                    Controls.Add(splitter);
                }
            }

            _activePlotIndex = count == 0 ? -1 : 0;
            ResumeLayout(true);
        }

        private void NormalizeFractions()
        {
            if (_heightFractions.Count == 0) return;
            double sum = 0;
            for (int i = 0; i < _heightFractions.Count; i++) sum += _heightFractions[i];
            if (sum <= 0)
            {
                double equal = 1.0 / _heightFractions.Count;
                for (int i = 0; i < _heightFractions.Count; i++) _heightFractions[i] = equal;
            }
            else
            {
                for (int i = 0; i < _heightFractions.Count; i++) _heightFractions[i] /= sum;
            }
        }

        private void Plot_Activated(object sender, PlotEventArgs e)
        {
            _activePlotIndex = e.PlotIndex;
            UpdateActiveRows();
            EventHandler<PlotEventArgs> handler = ActivePlotChanged;
            if (handler != null) handler(this, e);
        }

        private void Plot_DataRequested(object sender, PlotEventArgs e)
        {
            EventHandler<PlotEventArgs> handler = DataRequested;
            if (handler != null) handler(this, e);
        }

        private void Plot_RemoveRequested(object sender, PlotEventArgs e)
        {
            EventHandler<PlotEventArgs> handler = RemoveRequested;
            if (handler != null) handler(this, e);
        }

        private void UpdateActiveRows()
        {
            for (int i = 0; i < _plots.Count; i++) _plots[i].SetActive(i == _activePlotIndex);
        }

        private void Splitter_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Panel splitter = (Panel)sender;
            _dragSplitterIndex = (int)splitter.Tag;
            _dragStartY = PointToClient(splitter.PointToScreen(e.Location)).Y;
            _dragUpperHeight = _plots[_dragSplitterIndex].Height;
            _dragLowerHeight = _plots[_dragSplitterIndex + 1].Height;
            _dragPairFraction = _heightFractions[_dragSplitterIndex] + _heightFractions[_dragSplitterIndex + 1];
            splitter.Capture = true;
        }

        private void Splitter_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragSplitterIndex < 0) return;
            Panel splitter = (Panel)sender;
            int currentY = PointToClient(splitter.PointToScreen(e.Location)).Y;
            int pairHeight = _dragUpperHeight + _dragLowerHeight;
            int minimumHeight = Math.Min(PreferredMinimumPlotHeight, pairHeight / 2);
            int newUpperHeight = _dragUpperHeight + currentY - _dragStartY;
            newUpperHeight = Math.Max(minimumHeight, Math.Min(pairHeight - minimumHeight, newUpperHeight));
            _heightFractions[_dragSplitterIndex] = _dragPairFraction * newUpperHeight / pairHeight;
            _heightFractions[_dragSplitterIndex + 1] = _dragPairFraction * (pairHeight - newUpperHeight) / pairHeight;
            PerformLayout();
        }

        private void Splitter_MouseUp(object sender, MouseEventArgs e)
        {
            if (_dragSplitterIndex < 0) return;
            ((Panel)sender).Capture = false;
            _dragSplitterIndex = -1;
        }
    }
}
