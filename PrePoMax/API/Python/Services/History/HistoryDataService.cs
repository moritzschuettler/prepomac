// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using CaeResults;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.API.Python
{
    internal class HistoryDataService
    {
        // Variables
        private readonly ResultResolver _resolver;


        // Constructors
        public HistoryDataService(ResultResolver resolver)
        {
            _resolver = resolver;
        }


        // Methods
        public string[] GetSetNames(string resultName)
        {
            HistoryResults history = ResolveHistory(resultName);
            return history.Sets.Keys.ToArray();
        }
        public string[] GetFieldNames(string resultName, string setName)
        {
            return ResolveSet(resultName, setName).Fields.Keys.ToArray();
        }
        public string[] GetComponentNames(string resultName, string setName, string fieldName)
        {
            return ResolveField(resultName, setName, fieldName).Components.Keys.ToArray();
        }
        public string[] GetEntryNames(string resultName, string setName, string fieldName, string componentName)
        {
            return ResolveComponent(resultName, setName, fieldName, componentName).Entries.Keys.ToArray();
        }
        public HistoryComponentDataAPI GetComponentData(string resultName, string setName, string fieldName, string componentName)
        {
            HistoryResultComponent component = ResolveComponent(resultName, setName, fieldName, componentName);
            if (component.Entries.Count == 0)
                return new HistoryComponentDataAPI(new double[0], new double[0], component.Unit, new string[0]);
            if (component.Entries.Count == 1)
            {
                HistoryResultEntries entry = component.Entries.First().Value;
                return new HistoryComponentDataAPI(entry.Time.ToArray(), entry.Values.ToArray(),
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
            return new HistoryComponentDataAPI(time.ToArray(), values, component.Unit, entryNames.ToArray());
        }
        public HistoryComponentDataAPI GetEntryData(string resultName, string setName, string fieldName,
                                                          string componentName, string entryName)
        {
            HistoryResultComponent component = ResolveComponent(resultName, setName, fieldName, componentName);
            HistoryResultEntries entry;
            if (!component.Entries.TryGetValue(entryName, out entry))
                throw new PythonAPIException("The history entry '" + entryName + "' does not exist in component '" +
                                             componentName + "'.");
            return new HistoryComponentDataAPI(entry.Time.ToArray(), entry.Values.ToArray(),
                                                     GetUnit(component, entry), new string[] { entry.Name });
        }
        public HistoryResults ResolveHistory(string resultName)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            HistoryResults history = result.GetHistory();
            if (history == null)
                throw new PythonAPIException("The result '" + resultName + "' does not contain history results.");
            return history;
        }
        public HistoryResultSet ResolveSet(string resultName, string setName)
        {
            HistoryResults history = ResolveHistory(resultName);
            HistoryResultSet set;
            if (!history.Sets.TryGetValue(setName, out set))
                throw new PythonAPIException("The history set '" + setName + "' does not exist in result '" + resultName + "'.");
            return set;
        }
        public HistoryResultField ResolveField(string resultName, string setName, string fieldName)
        {
            HistoryResultSet set = ResolveSet(resultName, setName);
            HistoryResultField field;
            if (!set.Fields.TryGetValue(fieldName, out field))
                throw new PythonAPIException("The history field '" + fieldName + "' does not exist in set '" + setName + "'.");
            return field;
        }
        public HistoryResultComponent ResolveComponent(string resultName, string setName, string fieldName, string componentName)
        {
            HistoryResultField field = ResolveField(resultName, setName, fieldName);
            HistoryResultComponent component;
            if (!field.Components.TryGetValue(componentName, out component))
                throw new PythonAPIException("The history component '" + componentName + "' does not exist in field '" +
                                             fieldName + "'.");
            return component;
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

