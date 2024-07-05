using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;
using CaeResults;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using static System.Windows.Forms.Design.AxImporter;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewResultHistoryOutput : ViewMultiRegion
    {
        // Variables                                                                                                                
        private string _selectionHidden;
        private ResultHistoryOutput _resultHistoryOutput;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the history output.")]
        [Id(1, 1)]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the history output.")]
        [Id(2, 1)]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        [Id(2, 2)]
        public string SelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }
        //                                                              
        [CategoryAttribute("Filter 1")]
        [OrderedDisplayName(0, 10, "Type")]
        [DescriptionAttribute("Select the filter 1 type.")]
        [Id(1, 10)]
        public HistoryResultFilterTypeEnum Type1
        {
            get { return _resultHistoryOutput.Filter1.Type; }
            set
            {
                _resultHistoryOutput.Filter1.Type = value;
                if (_resultHistoryOutput.Filter1.Type == HistoryResultFilterTypeEnum.None)
                {
                    _resultHistoryOutput.Filter2.Type = HistoryResultFilterTypeEnum.None;
                    //_resultHistoryOutput.Filter3.Type = HistoryResultFilterTypeEnum.None;
                }
                SetFilterOption(_resultHistoryOutput.Filter1);
                UpdateFilterVisibility();
            }
        }
        //
        [CategoryAttribute("Filter 1")]
        [OrderedDisplayName(1, 10, "Option")]
        [DescriptionAttribute("Option.")]
        [Id(2, 10)]
        public string Option1
        {
            get { return _resultHistoryOutput.Filter1.Option; } 
            set { _resultHistoryOutput.Filter1.Option = value; }
        }
        //
        [CategoryAttribute("Filter 2")]
        [OrderedDisplayName(2, 10, "Type")]
        [DescriptionAttribute("Select the filter 2 type.")]
        [Id(3, 10)]
        public HistoryResultFilterTypeEnum Type2
        {
            get { return _resultHistoryOutput.Filter2.Type; }
            set
            {
                _resultHistoryOutput.Filter2.Type = value;
                if (_resultHistoryOutput.Filter2.Type == HistoryResultFilterTypeEnum.None)
                {
                    //_resultHistoryOutput.Filter3.Type = HistoryResultFilterTypeEnum.None;
                }
                SetFilterOption(_resultHistoryOutput.Filter2);
                UpdateFilterVisibility();
            }
        }
        //
        [CategoryAttribute("Filter 2")]
        [OrderedDisplayName(3, 10, "Option")]
        [DescriptionAttribute("Option.")]
        [Id(4, 10)]
        public string Option2
        {
            get { return _resultHistoryOutput.Filter2.Option; }
            set { _resultHistoryOutput.Filter2.Option = value; }
        }
        //
        //[CategoryAttribute("Filter 3")]
        //[OrderedDisplayName(4, 10, "Type")]
        //[DescriptionAttribute("Select the filter 3 type.")]
        //[Id(5, 10)]
        //public HistoryResultFilterTypeEnum Type3
        //{
        //    get { return _resultHistoryOutput.Filter3.Type; }
        //    set
        //    {
        //        _resultHistoryOutput.Filter3.Type = value;
        //        SetFilterOption(_resultHistoryOutput.Filter3);
        //        UpdateFilterVisibility();
        //    }
        //}
        ////
        //[CategoryAttribute("Filter 3")]
        //[OrderedDisplayName(5, 10, "Option")]
        //[DescriptionAttribute("Option.")]
        //[Id(6, 10)]
        //public string Option3
        //{
        //    get { return _resultHistoryOutput.Filter3.Option; }
        //    set { _resultHistoryOutput.Filter3.Option = value; }
        //}


        // Constructors                                                                                                             
        public ViewResultHistoryOutput(ResultHistoryOutput resultHistoryOutput)
        {
            _resultHistoryOutput = resultHistoryOutput;
        }

        // Methods
        public abstract ResultHistoryOutput GetBase();
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            // Hide SelectionHidden
            if (base.RegionType == RegionTypeEnum.Selection.ToFriendlyString())
            {
                DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
            }
            UpdateFilterVisibility();
        }
        public void UpdateFilterVisibility()
        {
            bool visible = _resultHistoryOutput.Filter1.Type != HistoryResultFilterTypeEnum.None;
            DynamicCustomTypeDescriptor.GetProperty(nameof(Type2)).SetIsBrowsable(visible);
            visible = _resultHistoryOutput.Filter1.Type != HistoryResultFilterTypeEnum.None &&
                      _resultHistoryOutput.Filter2.Type != HistoryResultFilterTypeEnum.None;
            //DynamicCustomTypeDescriptor.GetProperty(nameof(Type3)).SetIsBrowsable(visible);
            //
            SetFilterVisibility(_resultHistoryOutput.Filter1, nameof(Option1), "");
            SetFilterVisibility(_resultHistoryOutput.Filter2, nameof(Option2), " ");
            //SetFilterVisibility(_resultHistoryOutput.Filter3, nameof(Option3), "  ");
        }
        private void SetFilterVisibility(HistoryResultFilter filter, string optionPropertyName, string nameSuffix)
        {
            if (filter.Type == HistoryResultFilterTypeEnum.None)
            {
                DynamicCustomTypeDescriptor.GetProperty(optionPropertyName).SetIsBrowsable(false);
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Minimum || filter.Type == HistoryResultFilterTypeEnum.Maximum)
            {
                string description = "Select the return data type.";
                DynamicCustomTypeDescriptor.PopulateProperty(optionPropertyName, 
                    new string[] { HistoryResultFilter.Column, HistoryResultFilter.Row });
                DynamicCustomTypeDescriptor.GetProperty(optionPropertyName).SetDisplayName("Return" + nameSuffix);
                DynamicCustomTypeDescriptor.GetProperty(optionPropertyName).SetDescription(description);
                DynamicCustomTypeDescriptor.GetProperty(optionPropertyName).SetIsBrowsable(true);
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Average || filter.Type == HistoryResultFilterTypeEnum.Sum)
            {
                string description = "Select the data type the filter will operate on.";
                DynamicCustomTypeDescriptor.PopulateProperty(optionPropertyName,
                    new string[] { HistoryResultFilter.Columns, HistoryResultFilter.Rows });
                DynamicCustomTypeDescriptor.GetProperty(optionPropertyName).SetDisplayName("Operate on" + nameSuffix);
                DynamicCustomTypeDescriptor.GetProperty(optionPropertyName).SetDescription(description);
                DynamicCustomTypeDescriptor.GetProperty(optionPropertyName).SetIsBrowsable(true);
            }
        }
        private void SetFilterOption(HistoryResultFilter filter)
        {
            if (filter.Type == HistoryResultFilterTypeEnum.None)
                filter.Option = null;
            else if (filter.Type == HistoryResultFilterTypeEnum.Minimum || filter.Type == HistoryResultFilterTypeEnum.Maximum)
                filter.Option = HistoryResultFilter.Column;
            else if (filter.Type == HistoryResultFilterTypeEnum.Average || filter.Type == HistoryResultFilterTypeEnum.Sum)
                filter.Option = HistoryResultFilter.Columns;
        }
    }
}
