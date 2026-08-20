// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using CaeResults;
using System;
using System.Collections.Generic;
using System.Linq;
using static CaeResults.DerivedHistorySetBuilder;

namespace PrePoMax
{
    public partial class Controller
    {
        private sealed class HistoryEntryData
        {
            public FeResults Result;
            public HistoryResultField Field;
            public HistoryResultComponent Component;
            public HistoryResultEntries Entry;

            public HistoryEntryData(FeResults result, HistoryResultField field,
                                    HistoryResultComponent component, HistoryResultEntries entry)
            {
                Result = result;
                Field = field;
                Component = component;
                Entry = entry;
            }
        }
        public HistoryResultData[] GetResultHistoryComponents()
        {
            FeResults result = CurrentResult;
            return result == null ? new HistoryResultData[0] : result.GetHistoryResultComponents();
        }
        private void GetHistoryComponent(HistoryResultData source, out FeResults result, out HistoryResultSet set,
                                         out HistoryResultField field, out HistoryResultComponent component)
        {
            if (source == null) throw new ArgumentNullException("source");
            result = CurrentResult;
            if (result == null)
                throw new CaeException("There is no current result for the selected history output.");

            set = result.GetHistoryResultSet(source.SetName);
            field = result.GetHistoryResultField(source.SetName, source.FieldName);
            component = result.GetHistoryResultComponent(source.SetName, source.FieldName, source.ComponentName);
            if (set == null || field == null || component == null)
                throw new CaeException("The history output '" + source + "' cannot be found.");
        }
        public HistoryResultData[] GetExtractableHistoryComponents()
        {
            return GetResultHistoryComponents().Where(data => GetHistoryEntryNames(data).Length > 0).ToArray();
        }
        public string[] GetHistoryEntryNames(HistoryResultData source)
        {
            List<HistoryEntryData> entries = GetHistoryEntries(source);
            return entries.Where(item => GetFiniteTimeValues(item.Entry).Count > 0)
                          .Select(item => item.Entry.Name).ToArray();
        }
        public HistoryResultData[] GetMergeHistoryComponents()
        {
            return GetResultHistoryComponents().Where(IsMergeComponent).ToArray();
        }
        public HistoryResultData[] GetCompatibleMergeHistoryComponents(params HistoryResultData[] selectedSources)
        {
            if (selectedSources == null) return new HistoryResultData[0];
            selectedSources = selectedSources.Where(source => source != null).ToArray();
            if (selectedSources.Length == 0) return GetMergeHistoryComponents();

            List<HistoryEntryData> selectedEntries = new List<HistoryEntryData>();
            foreach (HistoryResultData source in selectedSources)
            {
                if (!IsMergeComponent(source)) return new HistoryResultData[0];
                selectedEntries.AddRange(GetHistoryEntries(source));
            }

            return GetMergeHistoryComponents()
                .Where(candidate => !selectedSources.Any(source => source.Name == candidate.Name))
                .Where(candidate =>
                {
                    List<HistoryEntryData> entries = new List<HistoryEntryData>(selectedEntries);
                    entries.AddRange(GetHistoryEntries(candidate));
                    return HaveSameUnit(entries) && HaveCommonReferenceTime(entries);
                }).ToArray();
        }
        public HistoryResultData[] GetXYXHistoryComponents()
        {
            return GetResultHistoryComponents().Where(data =>
            {
                List<HistoryEntryData> entries = GetHistoryEntries(data);
                return entries.Count == 1 && GetFiniteTimeValues(entries[0].Entry).Count > 0;
            }).ToArray();
        }
        public HistoryResultData[] GetCompatibleXYYHistoryComponents(HistoryResultData xSource)
        {
            List<HistoryEntryData> xEntries = GetHistoryEntries(xSource);
            if (xEntries.Count != 1 || GetFiniteTimeValues(xEntries[0].Entry).Count == 0)
                return new HistoryResultData[0];

            return GetResultHistoryComponents().Where(candidate =>
            {
                List<HistoryEntryData> yEntries = GetHistoryEntries(candidate);
                if (yEntries.Count == 0 || !HaveSameUnit(yEntries)) return false;

                List<HistoryEntryData> entries = new List<HistoryEntryData>();
                entries.Add(xEntries[0]);
                entries.AddRange(yEntries);
                return HaveCommonReferenceTime(entries);
            }).ToArray();
        }
        private List<HistoryEntryData> GetHistoryEntries(HistoryResultData source)
        {
            if (source == null) return new List<HistoryEntryData>();

            FeResults result;
            HistoryResultSet set;
            HistoryResultField field;
            HistoryResultComponent component;
            GetHistoryComponent(source, out result, out set, out field, out component);

            List<HistoryEntryData> entries = new List<HistoryEntryData>();
            foreach (HistoryResultEntries entry in component.Entries.Values)
                entries.Add(new HistoryEntryData(result, field, component, entry));
            return entries;
        }
        private bool IsMergeComponent(HistoryResultData source)
        {
            List<HistoryEntryData> entries = GetHistoryEntries(source);
            return entries.Count > 0 && entries.All(item => GetFiniteTimeValues(item.Entry).Count > 0) &&
                   HaveSameUnit(entries);
        }
        private bool HaveSameUnit(List<HistoryEntryData> entries)
        {
            if (entries == null || entries.Count == 0) return false;
            string unit = GetHistoryEntryUnit(entries[0].Result, entries[0].Field,
                                              entries[0].Component, entries[0].Entry);
            return entries.All(item => string.Equals(unit,
                GetHistoryEntryUnit(item.Result, item.Field, item.Component, item.Entry),
                StringComparison.Ordinal));
        }
        private static bool HaveCommonReferenceTime(List<HistoryEntryData> entries)
        {
            if (entries == null || entries.Count == 0) return false;

            List<List<KeyValuePair<double, double>>> valuesByTime =
                entries.Select(item => GetFiniteTimeValues(item.Entry)).ToList();
            HistoryResultEntries reference = entries[0].Entry;
            int count = Math.Min(reference.Time.Count, reference.Values.Count);
            for (int i = 0; i < count; i++)
            {
                double time = reference.Time[i];
                if (!IsFinite(time) || !IsFinite(reference.Values[i])) continue;

                bool complete = true;
                for (int entry = 1; entry < valuesByTime.Count; entry++)
                {
                    double value;
                    if (!TryGetInterpolatedValue(valuesByTime[entry], time, out value))
                    {
                        complete = false;
                        break;
                    }
                }
                if (complete) return true;
            }
            return false;
        }
    }
}
