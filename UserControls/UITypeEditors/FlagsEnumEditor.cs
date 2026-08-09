// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace UserControls
{
    public class FlagsEnumEditorUIEditor : UITypeEditor
    {
        private FlagsEnumEditorUI _editorUI;


        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override bool IsDropDownResizable
        {
            get { return true; }
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context == null || context.PropertyDescriptor == null || provider == null || value == null)
                return value;

            Type enumType = context.PropertyDescriptor.PropertyType;
            if (!enumType.IsEnum || !enumType.IsDefined(typeof(FlagsAttribute), false)) return value;

            IWindowsFormsEditorService editorService =
                provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (editorService == null) return value;

            if (_editorUI == null) _editorUI = new FlagsEnumEditorUI();
            _editorUI.SetData(context, editorService, value);
            editorService.DropDownControl(_editorUI);

            return _editorUI.GetValue();
        }


        private class FlagsEnumEditorUI : UserControl
        {
            private const int WsExComposited = 0x02000000;

            private Type _enumType;
            private ulong _value;
            private ulong _originalValue;
            private ulong _allValues;
            private bool _zeroValueAllowed;
            private bool _updatingCheckStates;
            private IWindowsFormsEditorService _editorService;
            private BufferedSplitContainer _splitContainer;
            private BufferedCheckedListBox _checkedListBox;
            private Label _displayNameLabel;
            private Label _descriptionLabel;


            public FlagsEnumEditorUI()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
                InitializeComponent();
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams createParams = base.CreateParams;
                    createParams.ExStyle |= WsExComposited;
                    return createParams;
                }
            }

            public void SetData(ITypeDescriptorContext context, IWindowsFormsEditorService editorService, object value)
            {
                _editorService = editorService;
                _enumType = context.PropertyDescriptor.PropertyType;
                Type underlyingType = Enum.GetUnderlyingType(_enumType);
                _value = ToUInt64(value, underlyingType);
                _originalValue = _value;
                _allValues = 0;
                _zeroValueAllowed = Enum.IsDefined(_enumType,
                    Enum.ToObject(_enumType, FromUInt64(0, underlyingType)));

                TypeConverter converter = context.PropertyDescriptor.Converter;
                List<FlagsEnumItem> enumItems = GetEnumItems(context, converter);
                foreach (FlagsEnumItem item in enumItems) _allValues |= item.Value;

                _updatingCheckStates = true;
                _checkedListBox.BeginUpdate();
                try
                {
                    _checkedListBox.Items.Clear();
                    _checkedListBox.Items.Add(new FlagsEnumItem(FlagsEnumItemType.SelectAll, "Select all",
                                                               "Select all available items.", _allValues));
                    _checkedListBox.Items.Add(new FlagsEnumItem(FlagsEnumItemType.SelectNone, "Select none",
                                                               "Clear all selected items.", 0));
                    foreach (FlagsEnumItem item in enumItems) _checkedListBox.Items.Add(item);

                    UpdateCheckStates();
                    SelectCurrentItem();
                    UpdateDescription();
                }
                finally
                {
                    _checkedListBox.EndUpdate();
                    _updatingCheckStates = false;
                }
            }

            public object GetValue()
            {
                if (_value == 0 && !_zeroValueAllowed)
                {
                    _value = _originalValue;
                    if (_value == 0) _value = GetFirstNonZeroValue();
                }
                return Enum.ToObject(_enumType, FromUInt64(_value, Enum.GetUnderlyingType(_enumType)));
            }


            private void InitializeComponent()
            {
                _splitContainer = new BufferedSplitContainer();
                _checkedListBox = new BufferedCheckedListBox();
                _displayNameLabel = new Label();
                _descriptionLabel = new Label();

                SuspendLayout();
                _splitContainer.SuspendLayout();
                _splitContainer.Panel1.SuspendLayout();
                _splitContainer.Panel2.SuspendLayout();

                _splitContainer.BorderStyle = BorderStyle.FixedSingle;
                _splitContainer.Dock = DockStyle.Fill;
                _splitContainer.Orientation = Orientation.Horizontal;
                _splitContainer.SplitterDistance = 114;
                _splitContainer.SplitterWidth = 6;

                _checkedListBox.BackColor = SystemColors.Window;
                _checkedListBox.BorderStyle = BorderStyle.None;
                _checkedListBox.CheckOnClick = true;
                _checkedListBox.Dock = DockStyle.Fill;
                _checkedListBox.ForeColor = SystemColors.WindowText;
                _checkedListBox.IntegralHeight = false;
                _checkedListBox.ItemCheck += CheckedListBox_ItemCheck;
                _checkedListBox.SelectedIndexChanged += CheckedListBox_SelectedIndexChanged;
                _checkedListBox.MouseDoubleClick += CloseDropDown;

                _displayNameLabel.Dock = DockStyle.Top;
                _displayNameLabel.Font = new Font(_displayNameLabel.Font, FontStyle.Bold);
                _displayNameLabel.Height = 18;
                _displayNameLabel.MouseDoubleClick += CloseDropDown;

                _descriptionLabel.Dock = DockStyle.Fill;
                _descriptionLabel.MouseDoubleClick += CloseDropDown;

                _splitContainer.Panel1.Controls.Add(_checkedListBox);
                _splitContainer.Panel2.Controls.Add(_descriptionLabel);
                _splitContainer.Panel2.Controls.Add(_displayNameLabel);
                _splitContainer.MouseDoubleClick += CloseDropDown;
                _splitContainer.Panel1.MouseDoubleClick += CloseDropDown;
                _splitContainer.Panel2.MouseDoubleClick += CloseDropDown;

                AutoScaleDimensions = new SizeF(6F, 13F);
                AutoScaleMode = AutoScaleMode.Font;
                Controls.Add(_splitContainer);
                MinimumSize = new Size(160, 120);
                Padding = new Padding(3);
                Size = new Size(209, 195);

                _splitContainer.Panel2.ResumeLayout(false);
                _splitContainer.Panel1.ResumeLayout(false);
                _splitContainer.ResumeLayout(false);
                ResumeLayout(false);
            }

            private void CheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
            {
                if (_updatingCheckStates || e.Index < 0 || e.Index >= _checkedListBox.Items.Count) return;

                FlagsEnumItem item = (FlagsEnumItem)_checkedListBox.Items[e.Index];
                if (item.ItemType == FlagsEnumItemType.SelectAll)
                {
                    _value = _allValues;
                    e.NewValue = CheckState.Checked;
                }
                else if (item.ItemType == FlagsEnumItemType.SelectNone || item.Value == 0)
                {
                    _value = 0;
                    e.NewValue = CheckState.Checked;
                }
                else if (e.NewValue == CheckState.Checked)
                {
                    _value |= item.Value;
                }
                else
                {
                    _value &= ~item.Value;
                }

                BeginInvoke(new MethodInvoker(UpdateCheckStates));
            }

            private void CheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
            {
                UpdateDescription();
            }

            private void CloseDropDown(object sender, MouseEventArgs e)
            {
                if (_editorService != null) _editorService.CloseDropDown();
            }

            private List<FlagsEnumItem> GetEnumItems(ITypeDescriptorContext context, TypeConverter converter)
            {
                ICollection standardValues = converter.GetStandardValues(context);
                if (standardValues == null) standardValues = Enum.GetValues(_enumType);

                Type underlyingType = Enum.GetUnderlyingType(_enumType);
                List<FlagsEnumItem> enumItems = new List<FlagsEnumItem>();
                foreach (object standardValue in standardValues)
                {
                    object enumValue = standardValue;
                    if (!_enumType.IsInstanceOfType(enumValue))
                    {
                        enumValue = converter.ConvertTo(context, CultureInfo.CurrentCulture,
                                                        standardValue, _enumType);
                    }

                    string displayName = converter.ConvertToString(context, CultureInfo.CurrentCulture,
                                                                    standardValue);
                    string description = GetDescription(standardValue, enumValue);
                    ulong itemValue = ToUInt64(enumValue, underlyingType);
                    enumItems.Add(new FlagsEnumItem(FlagsEnumItemType.EnumValue, displayName,
                                                    description, itemValue));
                }
                return enumItems;
            }

            private string GetDescription(object standardValue, object enumValue)
            {
                PropertyDescriptor descriptionProperty = TypeDescriptor.GetProperties(standardValue)["Description"];
                if (descriptionProperty != null && descriptionProperty.PropertyType == typeof(string))
                {
                    string description = descriptionProperty.GetValue(standardValue) as string;
                    if (!string.IsNullOrEmpty(description)) return description;
                }

                string enumName = Enum.GetName(_enumType, enumValue);
                if (enumName != null)
                {
                    FieldInfo fieldInfo = _enumType.GetField(enumName);
                    DescriptionAttribute descriptionAttribute = fieldInfo == null ? null :
                        fieldInfo.GetCustomAttribute<DescriptionAttribute>();
                    if (descriptionAttribute != null) return descriptionAttribute.Description;
                }
                return string.Empty;
            }

            private void UpdateCheckStates()
            {
                if (_checkedListBox.Items.Count == 0) return;

                _updatingCheckStates = true;
                try
                {
                    for (int i = 0; i < _checkedListBox.Items.Count; i++)
                    {
                        FlagsEnumItem item = (FlagsEnumItem)_checkedListBox.Items[i];
                        bool isChecked;
                        if (item.ItemType == FlagsEnumItemType.SelectAll)
                            isChecked = _allValues != 0 && (_value & _allValues) == _allValues;
                        else if (item.ItemType == FlagsEnumItemType.SelectNone || item.Value == 0)
                            isChecked = _value == 0;
                        else
                            isChecked = (_value & item.Value) == item.Value;

                        _checkedListBox.SetItemChecked(i, isChecked);
                    }
                }
                finally
                {
                    _updatingCheckStates = false;
                }
            }

            private void SelectCurrentItem()
            {
                for (int i = 2; i < _checkedListBox.Items.Count; i++)
                {
                    if (_checkedListBox.GetItemChecked(i))
                    {
                        _checkedListBox.SelectedIndex = i;
                        return;
                    }
                }

                _checkedListBox.SelectedIndex = _value == 0 ? 1 : 0;
            }

            private void UpdateDescription()
            {
                FlagsEnumItem item = _checkedListBox.SelectedItem as FlagsEnumItem;
                _displayNameLabel.Text = item == null ? string.Empty : item.DisplayName;
                _descriptionLabel.Text = item == null ? string.Empty : item.Description;
            }

            private ulong GetFirstNonZeroValue()
            {
                for (int i = 2; i < _checkedListBox.Items.Count; i++)
                {
                    FlagsEnumItem item = (FlagsEnumItem)_checkedListBox.Items[i];
                    if (item.Value != 0) return item.Value;
                }
                return 0;
            }

            private static ulong ToUInt64(object value, Type underlyingType)
            {
                switch (Type.GetTypeCode(underlyingType))
                {
                    case TypeCode.SByte: return unchecked((byte)Convert.ToSByte(value));
                    case TypeCode.Byte: return Convert.ToByte(value);
                    case TypeCode.Int16: return unchecked((ushort)Convert.ToInt16(value));
                    case TypeCode.UInt16: return Convert.ToUInt16(value);
                    case TypeCode.Int32: return unchecked((uint)Convert.ToInt32(value));
                    case TypeCode.UInt32: return Convert.ToUInt32(value);
                    case TypeCode.Int64: return unchecked((ulong)Convert.ToInt64(value));
                    case TypeCode.UInt64: return Convert.ToUInt64(value);
                    default: throw new NotSupportedException("Unsupported enum underlying type.");
                }
            }

            private static object FromUInt64(ulong value, Type underlyingType)
            {
                switch (Type.GetTypeCode(underlyingType))
                {
                    case TypeCode.SByte: return unchecked((sbyte)value);
                    case TypeCode.Byte: return unchecked((byte)value);
                    case TypeCode.Int16: return unchecked((short)value);
                    case TypeCode.UInt16: return unchecked((ushort)value);
                    case TypeCode.Int32: return unchecked((int)value);
                    case TypeCode.UInt32: return unchecked((uint)value);
                    case TypeCode.Int64: return unchecked((long)value);
                    case TypeCode.UInt64: return value;
                    default: throw new NotSupportedException("Unsupported enum underlying type.");
                }
            }


            private class BufferedSplitContainer : SplitContainer
            {
                public BufferedSplitContainer()
                {
                    DoubleBuffered = true;
                    ResizeRedraw = true;
                }
            }

            private class BufferedCheckedListBox : CheckedListBox
            {
                public BufferedCheckedListBox()
                {
                    DoubleBuffered = true;
                    ResizeRedraw = true;
                }
            }
        }


        private enum FlagsEnumItemType
        {
            SelectAll,
            SelectNone,
            EnumValue
        }

        private class FlagsEnumItem
        {
            public FlagsEnumItemType ItemType { get; private set; }
            public string DisplayName { get; private set; }
            public string Description { get; private set; }
            public ulong Value { get; private set; }


            public FlagsEnumItem(FlagsEnumItemType itemType, string displayName, string description, ulong value)
            {
                ItemType = itemType;
                DisplayName = displayName;
                Description = description;
                Value = value;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}
