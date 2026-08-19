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
    public class ViewResultHistoryOutputFromMergedHistories : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private ResultHistoryOutputFromMergedHistories _historyOutput;
        private Dictionary<string, HistoryResultData> _source1Items;
        private Dictionary<string, HistoryResultData> _source2Items;
        private Dictionary<string, HistoryResultData> _source3Items;
        private Dictionary<string, HistoryResultData> _source4Items;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Sources")]
        [OrderedDisplayName(0, 10, "History component 1")]
        [DescriptionAttribute("First history component. Its first entry defines the output time values.")]
        [Id(1, 2)]
        public string Source1
        {
            get { return _historyOutput.Source1?.GetDisplayName(); }
            set
            {
                HistoryResultData source;
                if (_source1Items != null && _source1Items.TryGetValue(value, out source))
                {
                    _historyOutput.Source1 = source;
                }
            }
        }

        [CategoryAttribute("Sources")]
        [OrderedDisplayName(1, 10, "History component 2")]
        [DescriptionAttribute("Second compatible history component to merge.")]
        [Id(2, 2)]
        public string Source2
        {
            get { return _historyOutput.Source2?.GetDisplayName(); }
            set
            {
                HistoryResultData source;
                if (_source2Items != null && _source2Items.TryGetValue(value, out source))
                    _historyOutput.Source2 = source;
            }
        }
        [CategoryAttribute("Sources")]
        [OrderedDisplayName(2, 10, "History component 3")]
        [DescriptionAttribute("Optional third compatible history component to merge.")]
        [Id(3, 2)]
        public string Source3
        {
            get { return _historyOutput.Source3?.GetDisplayName() ?? ""; }
            set
            {
                HistoryResultData source;
                if (string.IsNullOrEmpty(value))
                    _historyOutput.Source3 = null;
                else if (_source3Items != null && _source3Items.TryGetValue(value, out source))
                    _historyOutput.Source3 = source;
            }
        }

        [CategoryAttribute("Sources")]
        [OrderedDisplayName(3, 10, "History component 4")]
        [DescriptionAttribute("Optional fourth compatible history component to merge.")]
        [Id(4, 2)]
        public string Source4
        {
            get { return _historyOutput.Source4?.GetDisplayName() ?? ""; }
            set
            {
                HistoryResultData source;
                if (string.IsNullOrEmpty(value))
                    _historyOutput.Source4 = null;
                else if (_source4Items != null && _source4Items.TryGetValue(value, out source))
                    _historyOutput.Source4 = source;
            }
        }



        // Constructors                                                                                                              
        public ViewResultHistoryOutputFromMergedHistories(ResultHistoryOutputFromMergedHistories historyOutput)
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
        public void PopulateDropDownLists(HistoryResultData[] source1Items,
                                          HistoryResultData[] source2Items,
                                          HistoryResultData[] source3Items,
                                          HistoryResultData[] source4Items)
        {
            _source1Items = CreateMap(source1Items);
            _source2Items = CreateMap(source2Items);
            _source3Items = CreateOptionalMap(source3Items);
            _source4Items = CreateOptionalMap(source4Items);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(Source1), _source1Items.Keys.ToArray());
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(Source2), _source2Items.Keys.ToArray());
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(Source3), _source3Items.Keys.ToArray());
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(Source4), _source4Items.Keys.ToArray());
            PopulateBaseDropDownLists();
        }
        private static Dictionary<string, HistoryResultData> CreateOptionalMap(
            IEnumerable<HistoryResultData> sources)
        {
            Dictionary<string, HistoryResultData> map = new Dictionary<string, HistoryResultData>();
            map.Add("", null);
            foreach (KeyValuePair<string, HistoryResultData> item in CreateMap(sources))
                map.Add(item.Key, item.Value);
            return map;
        }
        private void PopulateBaseDropDownLists()
        {
            Dictionary<RegionTypeEnum, string[]> pairs = new Dictionary<RegionTypeEnum, string[]>();
            pairs.Add(RegionTypeEnum.None, new string[] { "None" });
            base.PopulateDropDownLists(pairs);
            DynamicCustomTypeDescriptor.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
            DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
        }
        internal static Dictionary<string, HistoryResultData> CreateMap(IEnumerable<HistoryResultData> sources)
        {
            Dictionary<string, HistoryResultData> map = new Dictionary<string, HistoryResultData>();
            if (sources != null)
            {
                foreach (HistoryResultData source in sources)
                {
                    string name = source?.GetDisplayName();
                    if (source != null && !map.ContainsKey(name)) map.Add(name, source);
                }
            }
            return map;
        }
        internal static bool ContainsSource(Dictionary<string, HistoryResultData> map, HistoryResultData source)
        {
            return source != null && map.Values.Any(item => item.Name == source.Name);
        }
    }
}
