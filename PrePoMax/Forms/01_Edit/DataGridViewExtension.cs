using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    using CaeGlobals;
    using PrePoMax.Settings;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public static class DataGridViewExtension
    {
        // DataGridView Double Buffered
        public static void DoubleBuffered(this DataGridView dgv, bool setting)
        {
            Type dgvType = dgv.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dgv, setting, null);
        }

        // DataGridView Row Reorder
        public static void EnableDragAndDropRows(this DataGridView dataGridView)
        {
            dataGridView.DoubleBuffered(true);
            //
            dataGridView.Paint += new PaintEventHandler(GridRowReorder_Paint);
            dataGridView.MouseDown += new MouseEventHandler(GridRowReorder_MouseDown);
            dataGridView.DragOver += new DragEventHandler(GridRowReorder_DragOver);
            dataGridView.DragDrop += new DragEventHandler(GridRowReorder_DragDrop);
            dataGridView.DragLeave += (s, e) => { rowIndexTo = -1; dataGridView.Refresh(); };
        }
        private static int rowIndexFrom = -1;
        private static int rowIndexTo = -1;
        private static int[] dataGridViewSelectedRows = null;
        private static int prevPosY;
        private static void GridRowReorder_Paint(object sender, PaintEventArgs e)
        {
            if (rowIndexTo == -1) return;
            //
            var dataGridView = sender as DataGridView;
            var rect = dataGridView.GetRowDisplayRectangle(rowIndexTo, false);
            int posY = (rowIndexTo > rowIndexFrom && !dataGridView.Rows[rowIndexTo].IsNewRow)
                       ? rect.Bottom : rect.Top;
            //
            using (var pen = new Pen(Color.FromArgb(150, 33, 186, 71), 7))
                e.Graphics.DrawLine(pen, new Point(rect.Left, posY - 1), new Point(rect.Right, posY - 1));
        }
        private static void GridRowReorder_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) != MouseButtons.Left) { rowIndexTo = -1; return; }
            //
            var dataGridView = sender as DataGridView;
            if (!dataGridView.AllowDrop || dataGridView.SelectedRows.Count == 0) { rowIndexTo = -1; return; }
            //
            var hitTestInfo = dataGridView.HitTest(e.X, e.Y);
            //
            if (hitTestInfo.ColumnIndex == -1)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                    if (row.IsNewRow) row.Selected = false;
                //
                dataGridViewSelectedRows = dataGridView.SelectedRows.Cast<DataGridViewRow>().Select(row => row.Index).ToArray();
                if (dataGridView.SelectedRows.Count == 0) { rowIndexFrom = -1; return; }
                //
                rowIndexFrom = hitTestInfo.RowIndex;
                if (dataGridViewSelectedRows.Contains(rowIndexFrom))
                {
                    dataGridView.EndEdit();
                    Task.Factory.StartNew(() =>
                    {
                        dataGridView.Invoke(new Action(() =>
                        {
                            if (dataGridViewSelectedRows?.Length > 1)
                                foreach (int row in dataGridViewSelectedRows) dataGridView.Rows[row].Selected = true;
                            //
                            if (rowIndexFrom != -1 && dataGridView.Rows.Count >= dataGridViewSelectedRows?.Length)
                                dataGridView.DoDragDrop(0, DragDropEffects.Move);
                        }));
                    });
                }
            }
        }
        private static void GridRowReorder_DragOver(object sender, DragEventArgs e)
        {
            if (rowIndexFrom == -1) return;
            var dataGridView = sender as DataGridView;
            if (!dataGridView.AllowDrop || dataGridView.SelectedRows.Count <= 0) { rowIndexTo = -1; return; }
            //
            e.Effect = DragDropEffects.Move;
            //
            try
            {
                Point clientPoint = dataGridView.PointToClient(new Point(e.X, e.Y));
                rowIndexTo = dataGridView.HitTest(clientPoint.X, clientPoint.Y).RowIndex;
                if (dataGridView.Rows[rowIndexTo].IsNewRow) { rowIndexTo--; }
            }
            catch
            {
                rowIndexTo = -1;
            }
            //
            if (dataGridViewSelectedRows?.Length > 1 && dataGridViewSelectedRows.Contains(rowIndexTo) && 
                dataGridViewSelectedRows.Contains(rowIndexTo - 1))
            {
                rowIndexTo = -1;
            }
            dataGridView.Refresh();
        }
        private static void GridRowReorder_DragDrop(object sender, DragEventArgs e)
        {
            if (rowIndexTo == -1) return;
            var dataGridView = sender as DataGridView;
            if (!dataGridView.AllowDrop || dataGridView.SelectedRows.Count <= 0)
            {
                rowIndexTo = -1;
                dataGridView.Refresh();
                return;
            }
            //
            if (rowIndexTo == rowIndexFrom && dataGridView.SelectedRows.Count == 1)
            {
                rowIndexTo = -1;
                dataGridView.Refresh();
                return;
            }
            //
            if (rowIndexTo == -1)
            {
                dataGridView.Refresh();
                return;
            }
            //
            if (e.Effect == DragDropEffects.Move)
            {
                dataGridView.Invoke(new Action(() =>
                {
                    List<DataGridViewRow> rowsToMove =
                        dataGridView.SelectedRows.Cast<DataGridViewRow>().OrderBy(row => row.Index).ToList();
                    //
                    int rowsAboveDropRow = 0;
                    List<int> sortedRowIndicesToMove = new List<int>();
                    foreach (DataGridViewRow selectedRow in rowsToMove)
                    {
                        sortedRowIndicesToMove.Add(selectedRow.Index);
                        if (selectedRow.Index < rowIndexTo) rowsAboveDropRow++;
                    }
                    //
                    var bindingSource = dataGridView.DataSource as BindingSource;
                    List<ViewCommand> list = (List<ViewCommand>)bindingSource.DataSource;
                    List<ViewCommand> listCopy = list.DeepClone();
                    // Remove
                    int[] reversedIndices = sortedRowIndicesToMove.ToArray().Reverse().ToArray();
                    foreach (var index in reversedIndices) list.RemoveAt(index);
                    // Insert
                    if (rowsAboveDropRow > 0) rowIndexTo -= rowsAboveDropRow - 1;
                    List<int> newRowIndices = new List<int>();
                    foreach (int index in sortedRowIndicesToMove)
                    {
                        list.Insert(rowIndexTo, listCopy[index]);
                        newRowIndices.Add(rowIndexTo);
                        rowIndexTo++;
                    }
                    //
                    dataGridView.ClearSelection();
                    // Select
                    foreach (int index in newRowIndices) dataGridView.Rows[index].Selected = true;
                    
                }));
            }
            //
            rowIndexTo = -1;
            dataGridView.Refresh();
        }
    }
}
