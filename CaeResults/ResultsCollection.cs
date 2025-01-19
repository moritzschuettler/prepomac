using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeResults
{
    [Serializable]
    public class ResultsCollection
    {
        // Variables                                                                                                                
        private FeResults _currentResult;
        private OrderedDictionary<string, FeResults> _results;
        

        // Properties                                                                                                               
        public int Count { get { return _results.Count; } }
        public FeResults CurrentResult { get { return _currentResult; } }


        //  Constructors                                                                                                            
        public ResultsCollection()
        {
            _results = new OrderedDictionary<string, FeResults>("Results Collection", StringComparer.OrdinalIgnoreCase);
            //
            Clear();
        }


        // Methods
        public static void PrepareForSaving(ResultsCollection resultsCollection)
        {
            foreach (var entry in resultsCollection._results)
            {
                if (entry.Value != null) CaeMesh.FeMesh.PrepareForSaving(entry.Value.Mesh);
            }
        }
        public static void ResetAfterSaving(ResultsCollection resultsCollection)
        {
            foreach (var entry in resultsCollection._results)
            {
                if (entry.Value != null) CaeMesh.FeMesh.ResetAfterSaving(entry.Value.Mesh);
            }
        }
        public static void WriteToBinaryWriter(ResultsCollection allResults, BinaryWriter bw)
        {
            bw.Write(allResults.Count);
            //
            foreach (var entry in allResults._results)
            {
                bw.Write(entry.Key);
                FeResults.WriteToBinaryWriter(entry.Value, bw);
            }
        }
        public static void WriteToFileStream(ResultsCollection allResults, FileStream fileStream,
                                             CompressionLevel compressionLevel)
        {
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
            return failedResultNames.Count == 0;
        }
        public static bool ReadFromFileStream(ResultsCollection allResults, FileStream fileStream, int version)
        {
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
            return failedResultNames.Count == 0;
        }
        //
        public void UpdateResultEquations()
        {
            foreach (var entry in _results) entry.Value.UpdateResultEquations();
        }
        //
        public void Clear()
        {
            _currentResult = null;
            _results.Clear();
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
            return result;
        }
        public bool ContainsResult(string name)
        {
            return _results.ContainsKey(name);
        }
        public bool ContainsComplexResults()
        {
            foreach (var entry in _results)
            {
                if (entry.Value.ContainsComplexResults()) return true;
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
                SetCurrentResults(remainingNames[count]);
            }
        }


    }
}
