// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaeResults
{
    public static class DerivedHistorySetBuilder
    {
        // Variables                                                                                                                
        private class HistoryEntryData
        {
            public HistoryResultField Field;
            public HistoryResultComponent Component;
            public HistoryResultEntries Entry;
            //
            public HistoryEntryData(HistoryResultField field, HistoryResultComponent component,
                                    HistoryResultEntries entry)
            {
                Field = field;
                Component = component;
                Entry = entry;
            }
        }


        // Methods                                                                                                                  
        public static HistoryResultSet CreateMergedSet(FeResults results,
                                                       ResultHistoryOutputFromMergedHistories resultHistoryOutput)
        {
            HistoryResultData[] sources = resultHistoryOutput.GetSources();
            if (sources.Length < 2)
                throw new CaeException("At least two history components must be selected for merging.");
            for (int i = 1; i < sources.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (sources[i].Name == sources[j].Name)
                        throw new CaeException("Each merged history source must be selected only once.");
                }
            }
            //
            HistoryResultField field;
            HistoryResultComponent component;
            List<HistoryEntryData> entries = new List<HistoryEntryData>();
            List<string> entryNames = new List<string>();
            List<string> entrySourceNames = new List<string>();
            foreach (HistoryResultData source in sources)
            {
                GetHistoryComponent(results, source, out field, out component);
                foreach (KeyValuePair<string, HistoryResultEntries> entry in component.Entries)
                {
                    entries.Add(new HistoryEntryData(field, component, entry.Value));
                    entryNames.Add(entry.Key);
                    entrySourceNames.Add(source.GetDisplayName());
                }
            }
            if (entries.Count == 0) throw new CaeException("The selected history outputs contain no entries.");
            //
            string yUnit = GetHistoryEntryUnit(results, entries[0].Field, entries[0].Component, entries[0].Entry);
            foreach (HistoryEntryData item in entries)
            {
                if (!string.Equals(yUnit, GetHistoryEntryUnit(results, item.Field, item.Component, item.Entry),
                                   StringComparison.Ordinal))
                    throw new CaeException("All merged history entries must use the same unit.");
            }
            //
            List<List<KeyValuePair<double, double>>> valuesByTime =
                entries.Select(item => GetFiniteTimeValues(item.Entry)).ToList();
            HistoryResultEntries reference = entries[0].Entry;
            int count = Math.Min(reference.Time.Count, reference.Values.Count);
            List<double[]> rows = new List<double[]>();
            for (int i = 0; i < count; i++)
            {
                double time = reference.Time[i];
                double value = reference.Values[i];
                if (!IsFinite(time) || !IsFinite(value)) continue;
                double[] row = new double[entries.Count + 1];
                row[0] = time;
                row[1] = value;
                bool complete = true;
                for (int column = 1; column < entries.Count; column++)
                {
                    if (!TryGetInterpolatedValue(valuesByTime[column], time, out row[column + 1]))
                    {
                        complete = false;
                        break;
                    }
                }
                if (complete) rows.Add(row);
            }
            if (rows.Count == 0)
                throw new CaeException("The selected history outputs have no finite points within their common time range.");
            //
            return BuildSet(resultHistoryOutput, yUnit, GetUniqueColumnNames(entryNames, entrySourceNames), rows);
        }
        public static HistoryResultSet CreateExtractedSet(FeResults results,
                                                          ResultHistoryOutputFromExtractedEntry resultHistoryOutput)
        {
            if (string.IsNullOrWhiteSpace(resultHistoryOutput.EntryName))
                throw new CaeException("The history entry to extract is not defined.");
            //
            HistoryResultField field;
            HistoryResultComponent component;
            GetHistoryComponent(results, resultHistoryOutput.Source, out field, out component);
            HistoryResultEntries entry;
            if (!component.Entries.TryGetValue(resultHistoryOutput.EntryName, out entry))
                throw new CaeException("The selected history entry no longer exists.");
            //
            int count = Math.Min(entry.Time.Count, entry.Values.Count);
            List<double[]> rows = new List<double[]>();
            for (int i = 0; i < count; i++)
            {
                if (IsFinite(entry.Time[i]) && IsFinite(entry.Values[i]))
                    rows.Add(new double[] { entry.Time[i], entry.Values[i] });
            }
            if (rows.Count == 0) throw new CaeException("The selected history entry contains no finite data.");
            //
            return BuildSet(resultHistoryOutput, GetHistoryEntryUnit(results, field, component, entry),
                            new string[] { entry.Name }, rows);
        }
        public static HistoryResultSet CreateXYSet(FeResults results, ResultHistoryOutputXY resultHistoryOutput)
        {
            HistoryResultField xField;
            HistoryResultComponent xComponent;
            GetHistoryComponent(results, resultHistoryOutput.XSource, out xField, out xComponent);
            HistoryResultField yField;
            HistoryResultComponent yComponent;
            GetHistoryComponent(results, resultHistoryOutput.YSource, out yField, out yComponent);
            //
            if (xComponent.Entries.Count != 1)
                throw new CaeException("The X history component must contain exactly one entry.");
            if (yComponent.Entries.Count == 0)
                throw new CaeException("The Y history component does not contain any entries.");
            //
            HistoryResultEntries xEntry = xComponent.Entries.First().Value;
            List<KeyValuePair<string, HistoryResultEntries>> yEntries = yComponent.Entries.ToList();
            List<List<KeyValuePair<double, double>>> yValuesByTime = new List<List<KeyValuePair<double, double>>>();
            foreach (KeyValuePair<string, HistoryResultEntries> yEntry in yEntries)
                yValuesByTime.Add(GetFiniteTimeValues(yEntry.Value));
            //
            List<double[]> rows = new List<double[]>();
            int xCount = Math.Min(xEntry.Time.Count, xEntry.Values.Count);
            for (int i = 0; i < xCount; i++)
            {
                double time = xEntry.Time[i];
                double x = xEntry.Values[i];
                if (!IsFinite(time) || !IsFinite(x)) continue;
                //
                double[] row = new double[yEntries.Count + 1];
                row[0] = x;
                bool complete = true;
                for (int column = 0; column < yValuesByTime.Count; column++)
                {
                    double y;
                    if (!TryGetInterpolatedValue(yValuesByTime[column], time, out y))
                    {
                        complete = false;
                        break;
                    }
                    row[column + 1] = y;
                }
                if (complete) rows.Add(row);
            }
            if (rows.Count == 0)
                throw new CaeException("The selected history outputs have no finite points within their common time range.");
            //
            string yUnit = GetHistoryEntryUnit(results, yField, yComponent, yEntries[0].Value);
            foreach (KeyValuePair<string, HistoryResultEntries> yEntry in yEntries)
            {
                if (!string.Equals(yUnit, GetHistoryEntryUnit(results, yField, yComponent, yEntry.Value),
                                   StringComparison.Ordinal))
                    throw new CaeException("All selected Y entries must use the same unit.");
            }
            //
            return BuildSet(resultHistoryOutput, yUnit, yEntries.Select(entry => entry.Key).ToArray(), rows);
        }
        // The rows are X first and one value per Y column - the same layout as the user defined table
        public static HistoryResultSet BuildSet(ResultHistoryOutput resultHistoryOutput, string yAxisUnit,
                                                string[] yColumnNames, IList<double[]> rows)
        {
            HistoryResultField field = new HistoryResultField(ResultHistoryOutputFromTable.FieldName);
            HistoryResultComponent component = new HistoryResultComponent(ResultHistoryOutputFromTable.ComponentName);
            for (int column = 0; column < yColumnNames.Length; column++)
            {
                HistoryResultEntries entries = new HistoryResultEntries(yColumnNames[column], false);
                entries.Unit = yAxisUnit;
                foreach (double[] row in rows) entries.Add(row[0], row[column + 1]);
                component.Entries.Add(entries.Name, entries);
            }
            component.Unit = yAxisUnit;
            if (resultHistoryOutput.Filter1 != null) component.ApplyFilter(resultHistoryOutput.Filter1);
            if (resultHistoryOutput.Filter2 != null) component.ApplyFilter(resultHistoryOutput.Filter2);
            field.Components.Add(component.Name, component);
            //
            HistoryResultSet set = new HistoryResultSet(resultHistoryOutput.Name);
            set.Fields.Add(field.Name, field);
            return set;
        }
        public static string GetSourceUnit(FeResults results, HistoryResultData source)
        {
            if (results == null || source == null) return "";
            HistoryResultField field = results.GetHistoryResultField(source.SetName, source.FieldName);
            HistoryResultComponent component = results.GetHistoryResultComponent(source.SetName, source.FieldName,
                                                                                 source.ComponentName);
            if (field == null || component == null || component.Entries.Count == 0) return "";
            return GetHistoryEntryUnit(results, field, component, component.Entries.First().Value);
        }
        public static string GetHistoryEntryUnit(FeResults results, HistoryResultField field,
                                                 HistoryResultComponent component, HistoryResultEntries entry)
        {
            string unit = entry.Unit;
            if (unit == null) unit = component.Unit;
            if (unit == null) unit = results.GetHistoryUnitAbbreviation(field.Name, component.Name, -1, -1);
            return unit ?? "";
        }
        public static List<KeyValuePair<double, double>> GetFiniteTimeValues(HistoryResultEntries entry)
        {
            Dictionary<double, double> valuesByTime = new Dictionary<double, double>();
            int count = Math.Min(entry.Time.Count, entry.Values.Count);
            for (int i = 0; i < count; i++)
            {
                double time = entry.Time[i];
                double value = entry.Values[i];
                if (IsFinite(time) && IsFinite(value)) valuesByTime[time] = value;
            }
            return valuesByTime.OrderBy(item => item.Key).ToList();
        }
        public static bool TryGetInterpolatedValue(List<KeyValuePair<double, double>> valuesByTime,
                                                   double time, out double value)
        {
            value = 0;
            if (valuesByTime == null || valuesByTime.Count == 0 || !IsFinite(time)) return false;
            //
            int last = valuesByTime.Count - 1;
            if (AreTimesEqual(time, valuesByTime[0].Key))
            {
                value = valuesByTime[0].Value;
                return true;
            }
            if (AreTimesEqual(time, valuesByTime[last].Key))
            {
                value = valuesByTime[last].Value;
                return true;
            }
            if (time < valuesByTime[0].Key || time > valuesByTime[last].Key) return false;
            //
            int lower = 0;
            int upper = last;
            while (upper - lower > 1)
            {
                int middle = lower + (upper - lower) / 2;
                double middleTime = valuesByTime[middle].Key;
                if (AreTimesEqual(time, middleTime))
                {
                    value = valuesByTime[middle].Value;
                    return true;
                }
                if (time < middleTime) upper = middle;
                else lower = middle;
            }
            //
            if (AreTimesEqual(time, valuesByTime[lower].Key))
            {
                value = valuesByTime[lower].Value;
                return true;
            }
            if (AreTimesEqual(time, valuesByTime[upper].Key))
            {
                value = valuesByTime[upper].Value;
                return true;
            }
            //
            double lowerTime = valuesByTime[lower].Key;
            double upperTime = valuesByTime[upper].Key;
            double interpolationFraction = (time - lowerTime) / (upperTime - lowerTime);
            value = valuesByTime[lower].Value + interpolationFraction *
                    (valuesByTime[upper].Value - valuesByTime[lower].Value);
            return IsFinite(value);
        }
        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
        private static void GetHistoryComponent(FeResults results, HistoryResultData source,
                                                out HistoryResultField field, out HistoryResultComponent component)
        {
            if (results == null) throw new CaeException("There is no current result for the selected history output.");
            if (source == null) throw new CaeException("The history component of the history output is not defined.");
            //
            field = results.GetHistoryResultField(source.SetName, source.FieldName);
            component = results.GetHistoryResultComponent(source.SetName, source.FieldName, source.ComponentName);
            if (field == null || component == null)
                throw new CaeException("The history output '" + source.GetDisplayName() + "' cannot be found.");
        }
        private static string[] GetUniqueColumnNames(List<string> entryNames, List<string> sourceNames)
        {
            Dictionary<string, int> entryNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string entryName in entryNames)
            {
                string name = entryName ?? "";
                if (entryNameCounts.ContainsKey(name)) entryNameCounts[name]++;
                else entryNameCounts.Add(name, 1);
            }
            //
            HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] uniqueNames = new string[entryNames.Count];
            for (int i = 0; i < entryNames.Count; i++)
            {
                string entryName = entryNames[i];
                if (string.IsNullOrWhiteSpace(entryName))
                    entryName = ResultHistoryOutputFromTable.YAxisName + (i + 1);
                string name = entryName;
                if (entryNameCounts[entryNames[i] ?? ""] > 1 && sourceNames != null && i < sourceNames.Count &&
                    !string.IsNullOrWhiteSpace(sourceNames[i]))
                    name = sourceNames[i] + "." + entryName;
                //
                string uniqueName = name;
                int number = 2;
                while (!usedNames.Add(uniqueName)) uniqueName = name + "-" + number++;
                uniqueNames[i] = uniqueName;
            }
            return uniqueNames;
        }
        private static bool AreTimesEqual(double first, double second)
        {
            const double absoluteTolerance = 1E-12;
            const double relativeTolerance = 1E-10;
            return Math.Abs(first - second) <= absoluteTolerance + relativeTolerance *
                   Math.Max(Math.Abs(first), Math.Abs(second));
        }
    }
}
