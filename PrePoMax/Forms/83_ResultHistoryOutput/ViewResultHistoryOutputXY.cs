// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using CaeResults;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultHistoryOutputXY : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private ResultHistoryOutputXY _historyOutput;
        private Dictionary<string, HistoryResultData> _xSourceItems;
        private Dictionary<string, HistoryResultData> _ySourceItems;
        private Dictionary<string, HistoryResultData[]> _ySourceItemsByXSource;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Sources")]
        [OrderedDisplayName(0, 10, "X history component")]
        [DescriptionAttribute("History component containing exactly one entry. Its values become x.")]
        [Id(1, 2)]
        public string XSource
        {
            get { return _historyOutput.XSource?.GetDisplayName(); }
            set
            {
                HistoryResultData source;
                if (_xSourceItems != null && _xSourceItems.TryGetValue(value, out source))
                {
                    _historyOutput.XSource = source;
                    UpdateYSources();
                }
            }
        }
        //
        [CategoryAttribute("Sources")]
        [OrderedDisplayName(1, 10, "Y history component")]
        [DescriptionAttribute("Compatible history component whose entries become y1, y2, and so on.")]
        [Id(2, 2)]
        public string YSource
        {
            get { return _historyOutput.YSource?.GetDisplayName(); }
            set
            {
                HistoryResultData source;
                if (_ySourceItems != null && _ySourceItems.TryGetValue(value, out source))
                    _historyOutput.YSource = source;
            }
        }


        // Constructors                                                                                                              
        public ViewResultHistoryOutputXY(ResultHistoryOutputXY historyOutput)
            : base(historyOutput)
        {
            _historyOutput = historyOutput;
            Dictionary<RegionTypeEnum, string> pairs = new Dictionary<RegionTypeEnum, string>();
            pairs.Add(RegionTypeEnum.None, nameof(SelectionHidden));
            SetBase(_historyOutput, pairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }
        
        
        // Methods                                                                                                                  
        public override ResultHistoryOutput GetBase()
        {
            return _historyOutput;
        }
        public void PopulateDropDownLists(HistoryResultData[] xSources,
                                          Dictionary<string, HistoryResultData[]> ySourcesByXSource)
        {
            _xSourceItems = ViewResultHistoryOutputFromMergedHistories.CreateMap(xSources);
            _ySourceItemsByXSource = ySourcesByXSource ?? new Dictionary<string, HistoryResultData[]>();
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(XSource), _xSourceItems.Keys.ToArray());
            if (!ViewResultHistoryOutputFromMergedHistories.ContainsSource(_xSourceItems, _historyOutput.XSource) &&
                _xSourceItems.Count > 0)
                _historyOutput.XSource = _xSourceItems.First().Value;
            UpdateYSources();
            PopulateBaseDropDownLists();
        }
        private void UpdateYSources()
        {
            HistoryResultData[] sources;
            if (_historyOutput.XSource == null || _ySourceItemsByXSource == null ||
                !_ySourceItemsByXSource.TryGetValue(_historyOutput.XSource.Name, out sources))
                sources = new HistoryResultData[0];

            _ySourceItems = ViewResultHistoryOutputFromMergedHistories.CreateMap(sources);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(YSource), _ySourceItems.Keys.ToArray());
            if (!ViewResultHistoryOutputFromMergedHistories.ContainsSource(_ySourceItems, _historyOutput.YSource))
                _historyOutput.YSource = _ySourceItems.Count > 0 ? _ySourceItems.First().Value : null;
        }
        private void PopulateBaseDropDownLists()
        {
            Dictionary<RegionTypeEnum, string[]> pairs = new Dictionary<RegionTypeEnum, string[]>();
            pairs.Add(RegionTypeEnum.None, new string[] { "None" });
            base.PopulateDropDownLists(pairs);
            DynamicCustomTypeDescriptor.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
            DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
        }
    }
}