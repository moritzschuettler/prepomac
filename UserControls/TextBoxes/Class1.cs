// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

public partial class Form1 : Form
{
    private PropertyGrid propertyGrid;
    private TextBox customTextBox;
    private string targetProperty = "Formula";

    public Form1()
    {
        //InitializeComponent();

        propertyGrid = new PropertyGrid
        {
            Dock = DockStyle.Fill,
            SelectedObject = new FormulaSettings()
        };
        propertyGrid.SelectedGridItemChanged += PropertyGrid_SelectedGridItemChanged;
        Controls.Add(propertyGrid);

        // Hidden TextBox that will overlay the editing cell
        customTextBox = new TextBox
        {
            Visible = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        customTextBox.KeyDown += CustomTextBox_KeyDown;
        Controls.Add(customTextBox);
        customTextBox.BringToFront();
    }

    private void PropertyGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
    {
        HideCustomTextBox();

        if (e.NewSelection?.Label == targetProperty)
        {
            // Delay to allow PropertyGrid to focus its internal editor first
            BeginInvoke((MethodInvoker)(() =>
            {
                ShowCustomTextBoxForSelectedProperty();
            }));
        }
    }

    private void ShowCustomTextBoxForSelectedProperty()
    {
        Rectangle rect = GetPropertyGridItemRectangle(propertyGrid, targetProperty);
        if (rect != Rectangle.Empty)
        {
            customTextBox.Bounds = rect;
            customTextBox.Text = GetPropertyValue(targetProperty);
            customTextBox.Visible = true;
            customTextBox.Focus();
            customTextBox.Select(customTextBox.Text.Length, 0);
        }
    }

    private void HideCustomTextBox()
    {
        if (customTextBox.Visible)
        {
            SetPropertyValue(targetProperty, customTextBox.Text);
            customTextBox.Visible = false;
        }
    }

    private string GetPropertyValue(string propName)
    {
        var prop = propertyGrid.SelectedObject.GetType().GetProperty(propName);
        return prop?.GetValue(propertyGrid.SelectedObject)?.ToString();
    }

    private void SetPropertyValue(string propName, string value)
    {
        var prop = propertyGrid.SelectedObject.GetType().GetProperty(propName);
        prop?.SetValue(propertyGrid.SelectedObject, value);
        propertyGrid.Refresh();
    }

    private void CustomTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
        {
            HideCustomTextBox();
            e.Handled = true;
        }
    }

    private Rectangle GetPropertyGridItemRectangle(PropertyGrid grid, string label)
    {
        // Use reflection to get internal gridView and find bounds
        var gridView = grid.Controls[1]; // usually internal GridView
        foreach (Control c in gridView.Controls)
        {
            if (c is TextBox && c.Visible)
            {
                return c.Bounds;
            }
        }

        return Rectangle.Empty;
    }
}

public class FormulaSettings
{
    [Category("Equation")]
    public string Formula { get; set; } = "sin(x) + log(x)";
}
