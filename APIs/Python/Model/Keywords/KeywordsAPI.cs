// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using FileInOut.Output;
using FileInOut.Output.Calculix;
using Python.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    // The CalculiX keyword editor as a scripted surface. A keyword is addressed by the index path it has
    // in the generated keyword tree, which is what the editor tree shows, so a script edits the keyword
    // the user would click. The override signature is derived from the generated keyword and never taken
    // from the caller, because the application uses it to drop an edit whose keyword has since changed.
    public class KeywordsAPI : BaseAPI
    {
        // Variables
        private readonly ModelResolver _modelResolver;


        // Properties
        [PythonAPI("The number of user CalculiX keywords.", Returns = "int")]
        public int count { get { return GetUserKeywords().Count; } }
        [PythonAPI("The number of generated CalculiX keywords modified by the user.", Returns = "int")]
        public int override_count { get { return GetOverrides().Count; } }


        // Constructors
        internal KeywordsAPI(ModelResolver modelResolver)
        {
            _modelResolver = modelResolver;
        }


        // Methods
        [PythonAPI("Returns the generated CalculiX keywords with the index path that addresses each one.",
                   Returns = "list[KeywordEntry]",
                   Example = "for keyword in pmx.model.keywords.get_model_keywords(): print(keyword.indices)")]
        public object get_model_keywords()
        {
            List<KeywordEntryAPI> entries = new List<KeywordEntryAPI>();
            CollectModelKeywords(GetModelKeywords(), new List<int>(), entries);
            //
            return PythonListConverter.ToPythonList(entries.ToArray());
        }
        [PythonAPI("Returns the user CalculiX keywords with the index path each one is inserted at.",
                   Returns = "list[KeywordEntry]")]
        public object get_user_keywords()
        {
            List<KeywordEntryAPI> entries = new List<KeywordEntryAPI>();
            foreach (KeyValuePair<int[], CalculixUserKeyword> entry in GetUserKeywords())
                entries.Add(new KeywordEntryAPI(entry.Key, entry.Value.Data));
            //
            return PythonListConverter.ToPythonList(entries.ToArray());
        }
        [PythonAPI("Returns the user edits of the generated CalculiX keywords.", Returns = "list[KeywordOverride]")]
        public object get_overrides()
        {
            List<CalculixKeywordOverride> overrides = GetOverrides();
            KeywordOverrideAPI[] entries = new KeywordOverrideAPI[overrides.Count];
            for (int i = 0; i < overrides.Count; i++)
                entries[i] = new KeywordOverrideAPI(overrides[i].Indices, overrides[i].Data, overrides[i].Disabled,
                                                    overrides[i].IsSuppressed, overrides[i].OriginalSignature);
            //
            return PythonListConverter.ToPythonList(entries);
        }
        [PythonAPI("Inserts or replaces one user CalculiX keyword at a generated keyword index path.",
                   Arguments = "indices: list[int], data: str", Returns = "None",
                   Example = "pmx.model.keywords.set_user_keyword([3, 1], '*NO ANALYSIS')")]
        public void set_user_keyword(object indices, object data)
        {
            string dataText = PythonValueConverter.ToText(data, "data", true);
            int[] path = ToIndices(indices, "indices");
            if (string.IsNullOrEmpty(dataText)) throw new PythonAPIException("The argument 'data' must not be empty.");
            ValidateUserKeywordPath(path);
            //
            OrderedDictionary<int[], CalculixUserKeyword> keywords = GetUserKeywords();
            RemoveAt(keywords, path);
            keywords.Add(path, new CalculixUserKeyword(dataText));
            //
            SetUserKeywords(keywords);
        }
        [PythonAPI("Removes the user CalculiX keyword at a generated keyword index path.",
                   Arguments = "indices: list[int]", Returns = "None")]
        public void remove_user_keyword(object indices)
        {
            int[] path = ToIndices(indices, "indices");
            OrderedDictionary<int[], CalculixUserKeyword> keywords = GetUserKeywords();
            if (!RemoveAt(keywords, path))
                throw new PythonAPIException("There is no user keyword at the index path [" +
                                             string.Join(", ", path) + "].");
            //
            SetUserKeywords(keywords);
        }
        [PythonAPI("Replaces every user CalculiX keyword by the given index path and text pairs.",
                   Arguments = "keywords: list[tuple[list[int], str]]", Returns = "None",
                   Example = "pmx.model.keywords.replace_user_keywords([([3, 1], '*NO ANALYSIS')])")]
        public void replace_user_keywords(object keywords)
        {
            List<KeyValuePair<int[], string>> pairs = ToKeywordPairs(keywords, "keywords");
            foreach (KeyValuePair<int[], string> pair in pairs) ValidateUserKeywordPath(pair.Key);
            OrderedDictionary<int[], CalculixUserKeyword> replacement =
                new OrderedDictionary<int[], CalculixUserKeyword>("Keywords");
            foreach (KeyValuePair<int[], string> pair in pairs)
            {
                RemoveAt(replacement, pair.Key);
                replacement.Add(pair.Key, new CalculixUserKeyword(pair.Value));
            }
            //
            SetUserKeywords(replacement);
        }
        [PythonAPI("Modifies one generated CalculiX keyword. The text replaces the generated one and the " +
                   "disabled flag writes it as a comment. An omitted value is kept.",
                   Arguments = "indices: list[int], data: str = None, disabled: bool = None", Returns = "None",
                   Example = "pmx.model.keywords.set_override([0, 2], disabled=True)")]
        public void set_override(object indices, object data = null, object disabled = null)
        {
            string dataText = PythonValueConverter.ToText(data, "data", true);
            int[] path = ToIndices(indices, "indices");
            if (dataText == null && disabled == null)
                throw new PythonAPIException("Either the argument 'data' or the argument 'disabled' must be given.");
            if (dataText != null && dataText.Length == 0)
                throw new PythonAPIException("The argument 'data' must not be empty.");
            bool applyDisabled = disabled != null && PythonValueConverter.ToBool(disabled, "disabled");
            string signature = ResolveGeneratedKeywordSignature(path);
            //
            List<CalculixKeywordOverride> overrides = GetOverrides();
            CalculixKeywordOverride existing = FindOverride(overrides, path, signature);
            CalculixKeywordOverride replacement = new CalculixKeywordOverride(path, signature);
            replacement.Data = dataText != null ? dataText : (existing != null ? existing.Data : null);
            replacement.Disabled = disabled != null ? applyDisabled : (existing != null && existing.Disabled);
            overrides.RemoveAll(item => item != null && item.HasSameIndices(path));
            // The editor keeps an edit only while it changes something, so an edit that undoes itself is dropped
            if (replacement.Data != null || replacement.Disabled) overrides.Add(replacement);
            //
            SetOverrides(overrides);
        }
        [PythonAPI("Removes the user edit of the generated CalculiX keyword at an index path.",
                   Arguments = "indices: list[int]", Returns = "None")]
        public void remove_override(object indices)
        {
            int[] path = ToIndices(indices, "indices");
            List<CalculixKeywordOverride> overrides = GetOverrides();
            if (overrides.RemoveAll(item => item != null && item.HasSameIndices(path)) == 0)
                throw new PythonAPIException("There is no keyword override at the index path [" +
                                             string.Join(", ", path) + "].");
            //
            SetOverrides(overrides);
        }
        [PythonAPI("Removes every user edit of the generated CalculiX keywords.", Returns = "None")]
        public void clear_overrides()
        {
            if (GetOverrides().Count == 0)
                throw new PythonAPIException("There are no keyword overrides to clear.");
            //
            SetOverrides(new List<CalculixKeywordOverride>());
        }
        public override string ToString()
        {
            return "Keywords(count=" + count + ", override_count=" + override_count + ")";
        }
        //
        private FeModel GetModelOrNull()
        {
            _modelResolver.EnsureModelAvailable();
            //
            return _modelResolver.ModelGateway.GetModel();
        }
        private OrderedDictionary<int[], CalculixUserKeyword> GetUserKeywords()
        {
            FeModel model = GetModelOrNull();
            if (model == null || model.CalculixUserKeywords == null)
                return new OrderedDictionary<int[], CalculixUserKeyword>("Keywords");
            //
            return model.CalculixUserKeywords.DeepClone();
        }
        private List<CalculixKeywordOverride> GetOverrides()
        {
            FeModel model = GetModelOrNull();
            List<CalculixKeywordOverride> overrides = new List<CalculixKeywordOverride>();
            if (model == null || model.CalculixKeywordOverrides == null) return overrides;
            //
            foreach (CalculixKeywordOverride item in model.CalculixKeywordOverrides)
            {
                if (item == null) continue;
                CalculixKeywordOverride copy = new CalculixKeywordOverride(item.Indices, item.OriginalSignature);
                copy.Data = item.Data;
                copy.Disabled = item.Disabled;
                // The suppression an export left behind is part of the persisted state, and an edit of
                // one override must not reset it on the others
                if (item.IsSuppressed) copy.Suppress();
                overrides.Add(copy);
            }
            //
            return overrides;
        }
        private List<CalculixKeyword> GetModelKeywords()
        {
            _modelResolver.GetModel();
            List<CalculixKeyword> keywords = _modelResolver.ModelGateway.GetCalculixModelKeywords();
            if (keywords == null) throw new PythonAPIException("The generated CalculiX keywords are not available.");
            //
            return keywords;
        }
        private void SetUserKeywords(OrderedDictionary<int[], CalculixUserKeyword> keywords)
        {
            _modelResolver.GetModel();
            _modelResolver.ModelGateway.SetCalculixUserKeywords(keywords);
        }
        private void SetOverrides(List<CalculixKeywordOverride> overrides)
        {
            _modelResolver.GetModel();
            _modelResolver.ModelGateway.SetCalculixKeywordEditorState(GetUserKeywords(), overrides);
        }
        private string ResolveGeneratedKeywordSignature(int[] path)
        {
            List<CalculixKeyword> keywords = GetModelKeywords();
            CalculixKeyword keyword = null;
            for (int i = 0; i < path.Length; i++)
            {
                if (keywords == null || path[i] >= keywords.Count)
                    throw new PythonAPIException("There is no generated keyword at the index path [" +
                                                 string.Join(", ", path) + "].");
                keyword = keywords[path[i]];
                keywords = keyword.Keywords;
            }
            //
            return CalculixFileWriter.GetKeywordSignature(keyword);
        }
        private static void CollectModelKeywords(List<CalculixKeyword> keywords, List<int> parentIndices,
                                                 List<KeywordEntryAPI> entries)
        {
            if (keywords == null) return;
            //
            for (int i = 0; i < keywords.Count; i++)
            {
                List<int> indices = new List<int>(parentIndices);
                indices.Add(i);
                entries.Add(new KeywordEntryAPI(indices.ToArray(),
                                                CalculixFileWriter.GetCompleteKeywordData(keywords[i])));
                CollectModelKeywords(keywords[i].Keywords, indices, entries);
            }
        }
        // A user keyword names an insertion position rather than a keyword, so the last index may be
        // the count of the keywords it is inserted among. A position that does not exist is refused
        // here, because the dialog can only insert at one that does; a position that later disappears
        // is suppressed on export instead, which is the state IsSuppressed reports.
        private void ValidateUserKeywordPath(int[] path)
        {
            List<CalculixKeyword> keywords = GetModelKeywords();
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (keywords == null || path[i] >= keywords.Count) throw NoInsertionPosition(path);
                keywords = keywords[path[i]].Keywords;
            }
            int insertionIndex = path[path.Length - 1];
            if (keywords == null || insertionIndex > keywords.Count) throw NoInsertionPosition(path);
        }
        private static PythonAPIException NoInsertionPosition(int[] path)
        {
            return new PythonAPIException("There is no generated keyword insertion position at the index path [" +
                                          string.Join(", ", path) + "].");
        }
        private static CalculixKeywordOverride FindOverride(List<CalculixKeywordOverride> overrides, int[] path,
                                                            string signature)
        {
            foreach (CalculixKeywordOverride item in overrides)
            {
                if (item != null && item.HasSameIndices(path) && item.OriginalSignature == signature) return item;
            }
            //
            return null;
        }
        private static bool RemoveAt(OrderedDictionary<int[], CalculixUserKeyword> keywords, int[] path)
        {
            foreach (int[] key in keywords.Keys)
            {
                if (SamePath(key, path))
                {
                    keywords.Remove(key);
                    return true;
                }
            }
            //
            return false;
        }
        private static bool SamePath(int[] first, int[] second)
        {
            if (first == null || second == null || first.Length != second.Length) return false;
            //
            for (int i = 0; i < first.Length; i++) if (first[i] != second[i]) return false;
            //
            return true;
        }
        private static int[] ToIndices(object value, string argumentName)
        {
            if (value == null || value is string) throw InvalidIndices(argumentName);
            //
            List<int> indices = new List<int>();
            PyObject pyObject = value as PyObject;
            if (pyObject != null)
            {
                using (Py.GIL())
                {
                    foreach (PyObject item in ToPythonList(pyObject, argumentName))
                    {
                        using (item)
                        {
                            if (PyString.IsStringType(item) || IsPythonBool(item)) throw InvalidIndices(argumentName);
                            try { indices.Add(item.As<int>()); }
                            catch (Exception) { throw InvalidIndices(argumentName); }
                        }
                    }
                }
            }
            else
            {
                IEnumerable enumerable = value as IEnumerable;
                if (enumerable == null) throw InvalidIndices(argumentName);
                foreach (object item in enumerable) indices.Add(PythonValueConverter.ToInt(item, argumentName));
            }
            //
            if (indices.Count == 0)
                throw new PythonAPIException("The argument '" + argumentName +
                                             "' must contain at least one keyword index.");
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] < 0)
                    throw new PythonAPIException("A keyword index in '" + argumentName + "' must not be negative.");
            }
            //
            return indices.ToArray();
        }
        private static List<KeyValuePair<int[], string>> ToKeywordPairs(object value, string argumentName)
        {
            if (value == null || value is string) throw InvalidPairs(argumentName);
            //
            List<KeyValuePair<int[], string>> pairs = new List<KeyValuePair<int[], string>>();
            PyObject pyObject = value as PyObject;
            if (pyObject != null)
            {
                using (Py.GIL())
                {
                    foreach (PyObject item in ToPythonList(pyObject, argumentName))
                    {
                        using (item)
                        {
                            List<PyObject> pair = ToPythonList(item, argumentName);
                            if (pair.Count != 2)
                            {
                                foreach (PyObject entry in pair) entry.Dispose();
                                throw InvalidPairs(argumentName);
                            }
                            using (pair[0])
                            using (pair[1])
                            {
                                if (!PyString.IsStringType(pair[1])) throw InvalidPairs(argumentName);
                                pairs.Add(new KeyValuePair<int[], string>(ToIndices(pair[0], argumentName),
                                                                          pair[1].As<string>()));
                            }
                        }
                    }
                }
            }
            else
            {
                IEnumerable enumerable = value as IEnumerable;
                if (enumerable == null) throw InvalidPairs(argumentName);
                foreach (object item in enumerable)
                {
                    IEnumerable pair = item as IEnumerable;
                    if (pair == null) throw InvalidPairs(argumentName);
                    List<object> entries = new List<object>();
                    foreach (object entry in pair) entries.Add(entry);
                    if (entries.Count != 2 || !(entries[1] is string)) throw InvalidPairs(argumentName);
                    pairs.Add(new KeyValuePair<int[], string>(ToIndices(entries[0], argumentName),
                                                              (string)entries[1]));
                }
            }
            //
            foreach (KeyValuePair<int[], string> pair in pairs)
            {
                if (string.IsNullOrEmpty(pair.Value))
                    throw new PythonAPIException("The text of each item in '" + argumentName +
                                                 "' must not be empty.");
            }
            //
            return pairs;
        }
        private static List<PyObject> ToPythonList(PyObject value, string argumentName)
        {
            if (value.IsNone() || PyString.IsStringType(value)) throw InvalidIndices(argumentName);
            //
            List<PyObject> items = new List<PyObject>();
            using (PyObject builtins = Py.Import("builtins"))
            using (PyObject listFunction = builtins.GetAttr("list"))
            {
                PyObject converted;
                try { converted = listFunction.Invoke(value); }
                catch (Exception) { throw InvalidIndices(argumentName); }
                using (converted)
                {
                    long count = converted.Length();
                    for (int i = 0; i < count; i++) items.Add(converted.GetItem(i));
                }
            }
            //
            return items;
        }
        private static bool IsPythonBool(PyObject value)
        {
            using (PyObject type = value.GetPythonType())
            using (PyObject typeName = type.GetAttr("__name__"))
            {
                return typeName.As<string>() == "bool";
            }
        }
        private static PythonAPIException InvalidIndices(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName + "' must be a list of keyword indices.");
        }
        private static PythonAPIException InvalidPairs(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName +
                                          "' must contain index path and keyword text pairs.");
        }
    }
}
