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
    public class ViewResultHistoryOutputFromExtractedEntry : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private ResultHistoryOutputFromExtractedEntry _historyOutput;
        private Dictionary<string, HistoryResultData> _sourceItems;
        private Dictionary<string, string[]> _entryNamesBySource;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(0, 10, "History component")]
        [DescriptionAttribute("History component containing the entry to extract.")]
        [Id(1, 2)]
        public string Source
        {
            get { return _historyOutput.Source?.GetDisplayName(); }
            set
            {
                HistoryResultData source;
                if (_sourceItems != null && _sourceItems.TryGetValue(value, out source))
                {
                    _historyOutput.Source = source;
                    UpdateEntries();
                }
            }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(1, 10, "Entry")]
        [DescriptionAttribute("Single entry to extract into a new history output.")]
        [Id(2, 2)]
        public string Entry
        {
            get { return _historyOutput.EntryName; }
            set { _historyOutput.EntryName = value; }
        }


        // Constructors                                                                                                              
        public ViewResultHistoryOutputFromExtractedEntry(ResultHistoryOutputFromExtractedEntry historyOutput)
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
        public void PopulateDropDownLists(HistoryResultData[] sources,
                                          Dictionary<string, string[]> entryNamesBySource)
        {
            _sourceItems = ViewResultHistoryOutputFromMergedHistories.CreateMap(sources);
            _entryNamesBySource = entryNamesBySource ?? new Dictionary<string, string[]>();
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(Source), _sourceItems.Keys.ToArray());
            if (!ViewResultHistoryOutputFromMergedHistories.ContainsSource(_sourceItems, _historyOutput.Source) &&
                _sourceItems.Count > 0)
                _historyOutput.Source = _sourceItems.First().Value;
            UpdateEntries();
            PopulateBaseDropDownLists();
        }
        private void UpdateEntries()
        {
            string[] entries;
            if (_historyOutput.Source == null || _entryNamesBySource == null ||
                !_entryNamesBySource.TryGetValue(_historyOutput.Source.Name, out entries))
                entries = new string[0];

            DynamicCustomTypeDescriptor.PopulateProperty(nameof(Entry), entries);
            if (!entries.Contains(_historyOutput.EntryName))
                _historyOutput.EntryName = entries.Length > 0 ? entries[0] : null;
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
