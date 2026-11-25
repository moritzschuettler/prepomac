// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeResults
{
    [PmxType(5058)]
    public class ResultsCollection
    {
        // Variables                                                                                                                
        [PmxKey(0)] private FeResults _currentResult;
        [PmxKey(1)] private CaeGlobals.OrderedDictionary<string, FeResults> _results;
        [NonSerialized] private Dictionary<object, Func<FeResults, bool>> _pendingLoads;

        // Properties                                                                                                               
        public int Count { get { return _results.Count; } }
        public FeResults CurrentResult { get { return GetLoaded(_currentResult); } }


        //  Constructors                                                                                                            
        public ResultsCollection()
        {
            _results = new CaeGlobals.OrderedDictionary<string, FeResults>("Results Collection", StringComparer.OrdinalIgnoreCase);
            //
            Clear();
        }


        // Methods
        public static void WriteToFileStream(ResultsCollection allResults, FileStream fileStream,
                                             CompressionLevel compressionLevel)
        {
            allResults.LoadAll();
            // Write the number of results
            Tools.WriteIntToFileStream(fileStream, allResults.Count);
            //
            foreach (var entry in allResults._results)
            {
                // Write the results name
                Tools.WriteStringToFileStream(fileStream, entry.Key);
                // Write the results data
                FeResults.WriteToFileStream(entry.Value, fileStream, compressionLevel);
            }
        }
        public static bool ReadFromBinaryReader(ResultsCollection allResults, BinaryReader br, int version)
        {
            string currentResultName = null;
            if (allResults.CurrentResult != null) currentResultName = allResults.CurrentResult.FileName;   // save
            //
            string resultsName;
            int numResults = br.ReadInt32();
            HashSet<string> addedResultNames = new HashSet<string>();
            //
            if (numResults > 0)
            {
                for (int i = 0; i < numResults; i++)
                {
                    resultsName = br.ReadString();
                    // If something goes wrong the name is written incorrectly
                    if (allResults._results.ContainsKey(resultsName))
                    {
                        allResults.SetCurrentResults(resultsName);
                        if (allResults.CurrentResult != null)
                        {
                            addedResultNames.Add(resultsName);
                            FeResults.ReadFromBinaryReader(allResults.CurrentResult, br, version);
                        }
                    }
                }
            }
            HashSet<string> failedResultNames = allResults._results.Keys.Except(addedResultNames).ToHashSet();
            foreach (var failedResultName in failedResultNames) allResults._results.Remove(failedResultName);
            //
            if (currentResultName != null) allResults.SetCurrentResults(currentResultName);                // load
            //
            return failedResultNames.Count == 0;
        }
        public static bool ReadFromFileStream(ResultsCollection allResults, FileStream fileStream, int version)
        {
            string currentResultName = null;
            if (allResults.CurrentResult != null) currentResultName = allResults.CurrentResult.FileName;   // save
            //
            string resultsName;
            int numResults = Tools.ReadIntFromFileStream(fileStream);
            HashSet<string> addedResultNames = new HashSet<string>();
            //
            if (numResults > 0)
            {
                for (int i = 0; i < numResults; i++)
                {
                    resultsName = Tools.ReadStringFromFileStream(fileStream);
                    // If something goes wrong the name is written incorrectly
                    if (allResults._results.ContainsKey(resultsName))
                    {
                        allResults.SetCurrentResults(resultsName);
                        if (allResults.CurrentResult != null)
                        {
                            addedResultNames.Add(resultsName);
                            FeResults.ReadFromFileStream(allResults.CurrentResult, fileStream, version);
                        }
                    }
                }
            }
            HashSet<string> failedResultNames = allResults._results.Keys.Except(addedResultNames).ToHashSet();
            foreach (var failedResultName in failedResultNames) allResults._results.Remove(failedResultName);
            //
            if (currentResultName != null) allResults.SetCurrentResults(currentResultName);                // load
            //
            return failedResultNames.Count == 0;
        }
        //
        public void UpdateResultEquations()
        {
            LoadAll();
            foreach (var entry in _results) entry.Value.UpdateResultEquations();
        }
        // The field data of a result read from a file is read the first time the result is used, so that opening a
        // file reads only the result it shows
        public void SetPendingLoad(string name, Func<FeResults, bool> load)
        {
            FeResults result;
            if (!_results.TryGetValue(name, out result)) return;
            if (_pendingLoads == null) _pendingLoads = new Dictionary<object, Func<FeResults, bool>>(PmxReferenceComparer.Instance);
            lock (_pendingLoads) _pendingLoads[result] = load;
        }
        public bool IsLoaded(string name)
        {
            FeResults result;
            if (!_results.TryGetValue(name, out result) || _pendingLoads == null) return true;
            lock (_pendingLoads) return !_pendingLoads.ContainsKey(result);
        }
        private FeResults GetLoaded(FeResults result)
        {
            if (result == null || _pendingLoads == null) return result;
            Func<FeResults, bool> load;
            lock (_pendingLoads)
            {
                if (_pendingLoads.TryGetValue(result, out load))
                {
                    _pendingLoads.Remove(result);
                    load(result);
                }
            }
            return result;
        }
        public void LoadAll()
        {
            foreach (var entry in _results) GetLoaded(entry.Value);
        }
        //
        public void Clear()
        {
            _currentResult = null;
            _results.Clear();
            _pendingLoads = null;
        }
        public string[] GetResultNames()
        {
            return _results.Keys.ToArray();
        }
        public string GetCurrentResultName()
        {
            foreach (var entry in _results)
            {
                if (entry.Value == _currentResult) return entry.Key;
            }
            return null;
        }
        public FeResults GetResult(string name)
        {
            FeResults result;
            _results.TryGetValue(name, out result);
            return GetLoaded(result);
        }
        public bool ContainsResult(string name)
        {
            return _results.ContainsKey(name);
        }
        // A result is named by the path it was loaded from, which another work directory changes, so a name that is
        // not loaded finds the one loaded result of the same file name
        public string FindResultName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            //
            foreach (var entry in _results)
            {
                if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase)) return entry.Key;
            }
            //
            string fileName;
            string foundName = null;
            try
            {
                fileName = Path.GetFileName(name);
                foreach (var entry in _results)
                {
                    if (!string.Equals(Path.GetFileName(entry.Key), fileName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (foundName != null) return null;
                    foundName = entry.Key;
                }
            }
            catch (ArgumentException)
            {
                return null;
            }
            return foundName;
        }
        // A result not read yet is not read for it; a saved result holds no complex fields
        public bool ContainsComplexResults()
        {
            foreach (var entry in _results)
            {
                if (IsLoaded(entry.Key) && entry.Value.ContainsComplexResults()) return true;
            }
            return false;
        }
        //
        public void Add(string name, FeResults result)
        {
            if (_results.ContainsKey(name)) _results.Replace(name, name, result);
            else _results.Add(name, result);
            //
            _currentResult = result;
        }
        //
        public void SetCurrentResults(string name)
        {
            _results.TryGetValue(name, out _currentResult);
        }
        public void RemoveCurrentResult()
        {
            if (_results.Count == 0) { }
            else if (_results.Count == 1) Clear();
            else if (_currentResult != null)
            {
                int count = 0;
                string name = null;
                foreach (var entry in _results)
                {
                    if (entry.Value == _currentResult)
                    {
                        name = entry.Key;
                        break;
                    }
                    //
                    count++;
                }
                //
                if (name != null) _results.Remove(name);
                //
                string[] remainingNames = _results.Keys.ToArray();
                if (count >= remainingNames.Length) count = remainingNames.Length - 1;
                _currentResult = null;
                //SetCurrentResults(remainingNames[count]);
            }
        }
    }
}
