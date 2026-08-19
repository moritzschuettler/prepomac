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
    public class ViewResultHistoryOutputFromTable : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private ResultHistoryOutputFromTable _historyOutput;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Number of Y columns")]
        [DescriptionAttribute("Number of Y-value columns in the table.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(2, 1)]
        public int NumberOfYColumns
        {
            get { return _historyOutput.NumberOfYColumns; }
            set { _historyOutput.NumberOfYColumns = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "User defined X-axis unit")]
        [DescriptionAttribute("User defined unit for the x column.")]
        [Id(3, 1)]
        public string XAxisUnit { get { return _historyOutput.XAxisUnit; } set { _historyOutput.XAxisUnit = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "User defined Y-axis unit")]
        [DescriptionAttribute("User defined unit shared by all Y columns.")]
        [Id(4, 1)]
        public string YAxisUnit { get { return _historyOutput.YAxisUnit; } set { _historyOutput.YAxisUnit = value; } }
        //
        [Browsable(false)]
        public double[][] DataPoints
        {
            get { return _historyOutput.DataPoints; }
            set { _historyOutput.DataPoints = value; }
        }


        // Constructors                                                                                                              
        public ViewResultHistoryOutputFromTable(ResultHistoryOutputFromTable historyOutput)
            : base(historyOutput)
        {
            _historyOutput = historyOutput;
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs =
                new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.None, nameof(SelectionHidden));
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override ResultHistoryOutput GetBase()
        {
            return _historyOutput;
        }
        internal string GetYColumnName(int column)
        {
            return _historyOutput.GetYColumnName(column);
        }
        internal string[] GetColumnUnits()
        {
            return Enumerable.Repeat(_historyOutput.YAxisUnit ?? "", _historyOutput.NumberOfYColumns).ToArray();
        }
        private static string[] SplitList(string value, bool removeEmpty)
        {
            if (value == null) return new string[0];
            IEnumerable<string> values = value.Split(new char[] { ';' }).Select(item => item.Trim());
            if (removeEmpty) values = values.Where(item => item.Length > 0);
            return values.ToArray();
        }
        public void PopulateDropDownLists()
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs =
                new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.None, new string[] { "None" });
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            DynamicCustomTypeDescriptor.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
            DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
        }
    }
}
