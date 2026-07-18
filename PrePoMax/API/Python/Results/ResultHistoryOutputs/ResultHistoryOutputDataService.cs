// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.API.Python
{
    internal class ResultHistoryOutputDataService
    {
        // Variables
        private readonly ResultResolver _resolver;


        // Constructors
        public ResultHistoryOutputDataService(ResultResolver resolver)
        {
            _resolver = resolver;
        }


        // Methods
        public string[] GetHistoryOutputNames(string resultName)
        {
            HistoryResults history = ResolveHistory(resultName);
            return history.Sets.Keys.ToArray();
        }
        public string[] GetFieldNames(string resultName, string historyOutputName)
        {
            return ResolveHistoryOutput(resultName, historyOutputName).Fields.Keys.ToArray();
        }
        public string[] GetComponentNames(string resultName, string historyOutputName, string fieldName)
        {
            return ResolveField(resultName, historyOutputName, fieldName).Components.Keys.ToArray();
        }
        public string[] GetEntryNames(string resultName, string historyOutputName, string fieldName, string componentName)
        {
            return ResolveComponent(resultName, historyOutputName, fieldName, componentName).Entries.Keys.ToArray();
        }
        public ResultHistoryOutputComponentDataAPI GetComponentData(string resultName, string historyOutputName, string fieldName,
                                                        string componentName)
        {
            HistoryResultComponent component = ResolveComponent(resultName, historyOutputName, fieldName, componentName);
            if (component.Entries.Count == 0)
                return new ResultHistoryOutputComponentDataAPI(new double[0], new double[0], component.Unit, new string[0]);
            if (component.Entries.Count == 1)
            {
                HistoryResultEntries entry = component.Entries.First().Value;
                return new ResultHistoryOutputComponentDataAPI(entry.Time.ToArray(), entry.Values.ToArray(),
                                                         GetUnit(component, entry), new string[] { entry.Name });
            }
            List<string> entryNames = component.Entries.Keys.ToList();
            List<double> time = component.Entries.First().Value.Time;
            foreach (var entry in component.Entries)
            {
                if (!AreTimesEqual(time, entry.Value.Time))
                    throw new PythonAPIException("The history component '" + componentName +
                                                 "' contains entries with different time arrays. Select an entry explicitly.");
            }
            double[,] values = new double[time.Count, component.Entries.Count];
            int column = 0;
            foreach (var entry in component.Entries)
            {
                for (int row = 0; row < entry.Value.Values.Count; row++) values[row, column] = entry.Value.Values[row];
                column++;
            }
            return new ResultHistoryOutputComponentDataAPI(time.ToArray(), values, component.Unit, entryNames.ToArray());
        }
        public ResultHistoryOutputEntryDataAPI GetEntryData(string resultName, string historyOutputName, string fieldName,
                                                string componentName, string entryName)
        {
            HistoryResultComponent component = ResolveComponent(resultName, historyOutputName, fieldName, componentName);
            HistoryResultEntries entry = ResolveEntry(resultName, historyOutputName, fieldName, componentName, entryName);
            return new ResultHistoryOutputEntryDataAPI(entry.Name, entry.Time.ToArray(), entry.Values.ToArray(),
                                           GetUnit(component, entry));
        }
        public HistoryResults ResolveHistory(string resultName)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            HistoryResults history = result.GetHistory();
            if (history == null)
                throw new PythonAPIException("The result '" + resultName + "' does not contain history results.");
            return history;
        }
        public HistoryResultSet ResolveHistoryOutput(string resultName, string historyOutputName)
        {
            HistoryResults history = ResolveHistory(resultName);
            HistoryResultSet historyOutput;
            if (!history.Sets.TryGetValue(historyOutputName, out historyOutput))
                throw new PythonAPIException("The history output '" + historyOutputName +
                                             "' does not exist in result '" + resultName + "'.");
            return historyOutput;
        }
        public HistoryResultField ResolveField(string resultName, string historyOutputName, string fieldName)
        {
            HistoryResultSet historyOutput = ResolveHistoryOutput(resultName, historyOutputName);
            HistoryResultField field;
            if (!historyOutput.Fields.TryGetValue(fieldName, out field))
                throw new PythonAPIException("The history field '" + fieldName + "' does not exist in history output '" +
                                             historyOutputName + "'.");
            return field;
        }
        public HistoryResultComponent ResolveComponent(string resultName, string historyOutputName, string fieldName,
                                                       string componentName)
        {
            HistoryResultField field = ResolveField(resultName, historyOutputName, fieldName);
            HistoryResultComponent component;
            if (!field.Components.TryGetValue(componentName, out component))
                throw new PythonAPIException("The history component '" + componentName + "' does not exist in field '" +
                                             fieldName + "'.");
            return component;
        }
        public HistoryResultEntries ResolveEntry(string resultName, string historyOutputName, string fieldName,
                                                 string componentName, string entryName)
        {
            HistoryResultComponent component = ResolveComponent(resultName, historyOutputName, fieldName, componentName);
            HistoryResultEntries entry;
            if (!component.Entries.TryGetValue(entryName, out entry))
                throw new PythonAPIException("The history entry '" + entryName + "' does not exist in component '" +
                                             componentName + "'.");
            return entry;
        }
        private static bool AreTimesEqual(List<double> first, List<double> second)
        {
            if (first.Count != second.Count) return false;
            for (int i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i]) return false;
            }
            return true;
        }
        private static string GetUnit(HistoryResultComponent component, HistoryResultEntries entry)
        {
            if (!string.IsNullOrEmpty(entry.Unit)) return entry.Unit;
            return component.Unit;
        }
    }
}

