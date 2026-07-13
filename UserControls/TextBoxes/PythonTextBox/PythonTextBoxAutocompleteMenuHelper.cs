// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using FastColoredTextBoxNS;
using System.Collections.Generic;
using System.Drawing;

namespace UserControls
{
    internal static class PythonTextBoxAutocompleteMenuHelper
    {
        private static readonly Color SelectionBackColor = Color.FromArgb(0, 120, 215);
        private static readonly Color SelectionForeColor = Color.White;


        public static AutocompleteMenu Create(FastColoredTextBox textBox)
        {
            AutocompleteMenu menu = new AutocompleteMenu(textBox);
            menu.SearchPattern = @"[\w]";
            // The Python controls explicitly prepare the completion list before calling ShowPrepared.
            // Keep FCTB's timer from opening stale lists, but allow its visible menu to filter as the user types.
            menu.MinFragmentLength = 0;
            menu.AllowTabKey = true;
            menu.AppearInterval = 600000;
            menu.Font = textBox.Font;
            menu.MinimumSize = new Size(220, 0);
            menu.MaximumSize = new Size(500, 180);
            menu.SelectedColor = SelectionBackColor;
            menu.HoveredColor = SelectionBackColor;
            menu.Items.SelectedColor = SelectionBackColor;
            menu.Items.HoveredColor = SelectionBackColor;
            menu.Items.FocussedItemIndexChanged += delegate { menu.Items.Invalidate(); };
            //
            return menu;
        }
        public static bool ShowPrepared(AutocompleteMenu menu, IEnumerable<string> completions)
        {
            List<AutocompleteItem> items = CreateAutocompleteItems(completions);

            if (items.Count == 0)
            {
                menu.Close();
                menu.Items.SetAutocompleteItems(items);
                return false;
            }

            menu.Items.SetAutocompleteItems(items);
            menu.Show(true);
            menu.Items.Invalidate();
            //
            return menu.Visible;
        }
        public static void ClearAutocompleteItems(AutocompleteMenu menu)
        {
            menu.Items.SetAutocompleteItems(new List<AutocompleteItem>());
        }
        private static List<AutocompleteItem> CreateAutocompleteItems(IEnumerable<string> completions)
        {
            List<AutocompleteItem> items = new List<AutocompleteItem>();

            if (completions == null) return items;

            foreach (string completion in completions)
            {
                items.Add(new PythonAutocompleteItem(completion));
            }

            return items;
        }
        private class PythonAutocompleteItem : AutocompleteItem
        {
            public PythonAutocompleteItem(string text)
                : base(text)
            {
            }
            public override Color BackColor
            {
                get
                {
                    if (Parent != null && Parent.Items.FocussedItem == this)
                        return SelectionBackColor;

                    return Color.Transparent;
                }
                set { }
            }
            public override Color ForeColor
            {
                get
                {
                    if (Parent != null && Parent.Items.FocussedItem == this)
                        return SelectionForeColor;

                    return Color.Transparent;
                }
                set { }
            }
        }
    }
}
