// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace UserControls
{
    internal class PythonTextBoxScriptAnalysis
    {
        // Variables                                                                                                                
        private readonly HashSet<string> _names;
        private readonly Dictionary<string, string> _aliases;


        // Constructors                                                                                                             
        public PythonTextBoxScriptAnalysis(HashSet<string> names, Dictionary<string, string> aliases)
        {
            _names = names;
            _aliases = aliases;
        }


        // Methods                                                                                                                  
        public List<string> GetMatchingNames(string prefix)
        {
            if (prefix == null) prefix = "";

            List<string> names = new List<string>();

            foreach (string name in _names)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    names.Add(name);
            }

            return names;
        }
        public string ResolveExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return expression;

            HashSet<string> visited = new HashSet<string>();
            string resolved = expression;

            for (int i = 0; i < 20; i++)
            {
                string name = GetLeadingIdentifier(resolved);
                if (string.IsNullOrEmpty(name)) break;
                if (visited.Contains(name)) break;
                if (!_aliases.ContainsKey(name)) break;

                visited.Add(name);
                resolved = _aliases[name] + resolved.Substring(name.Length);
            }

            return resolved;
        }
        private string GetLeadingIdentifier(string expression)
        {
            if (string.IsNullOrEmpty(expression)) return null;
            if (!(char.IsLetter(expression[0]) || expression[0] == '_')) return null;

            int index = 1;
            while (index < expression.Length &&
                   (char.IsLetterOrDigit(expression[index]) || expression[index] == '_'))
            {
                index++;
            }

            return expression.Substring(0, index);
        }
    }

    internal static class PythonTextBoxScriptAnalyzer
    {
        // Methods                                                                                                                  
        public static PythonTextBoxScriptAnalysis Analyze(string script)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.Ordinal);

            if (script == null) script = "";

            string[] lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (string line in lines)
            {
                string code = StripComment(line).Trim();
                if (code.Length == 0) continue;

                AddImportNames(code, names);
                AddDefinitionName(code, names);
                AddForLoopNames(code, names);
                AddAsNames(code, names);
                AddAssignmentNameAndAlias(code, names, aliases);
            }

            return new PythonTextBoxScriptAnalysis(names, aliases);
        }
        private static void AddAssignmentNameAndAlias(string code, HashSet<string> names,
                                                      Dictionary<string, string> aliases)
        {
            Match match = Regex.Match(code, @"^(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<expression>.+)$");
            if (!match.Success) return;

            string name = match.Groups["name"].Value;
            string expression = match.Groups["expression"].Value.Trim();

            names.Add(name);

            if (IsSafeAliasExpression(expression))
                aliases[name] = expression;
            else if (aliases.ContainsKey(name))
                aliases.Remove(name);
        }
        private static void AddDefinitionName(string code, HashSet<string> names)
        {
            Match match = Regex.Match(code, @"^(def|class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b");
            if (match.Success)
                names.Add(match.Groups["name"].Value);
        }
        private static void AddForLoopNames(string code, HashSet<string> names)
        {
            Match match = Regex.Match(code, @"^for\s+(?<names>.+?)\s+in\s+");
            if (!match.Success) return;

            AddSimpleNames(match.Groups["names"].Value, names);
        }
        private static void AddAsNames(string code, HashSet<string> names)
        {
            MatchCollection matches = Regex.Matches(code, @"\bas\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b");

            foreach (Match match in matches)
                names.Add(match.Groups["name"].Value);
        }
        private static void AddImportNames(string code, HashSet<string> names)
        {
            Match match = Regex.Match(code, @"^import\s+(?<items>.+)$");
            if (match.Success)
            {
                foreach (string item in match.Groups["items"].Value.Split(','))
                {
                    string name = GetImportedName(item, true);
                    if (!string.IsNullOrEmpty(name)) names.Add(name);
                }
                return;
            }

            match = Regex.Match(code, @"^from\s+.+\s+import\s+(?<items>.+)$");
            if (match.Success)
            {
                foreach (string item in match.Groups["items"].Value.Split(','))
                {
                    string name = GetImportedName(item, false);
                    if (!string.IsNullOrEmpty(name)) names.Add(name);
                }
            }
        }
        private static string GetImportedName(string item, bool rootName)
        {
            string text = item.Trim();
            if (text.Length == 0 || text == "*") return null;

            Match match = Regex.Match(text, @"^(?<name>[A-Za-z_][A-Za-z0-9_\.]*)(\s+as\s+(?<alias>[A-Za-z_][A-Za-z0-9_]*))?$");
            if (!match.Success) return null;

            if (match.Groups["alias"].Success)
                return match.Groups["alias"].Value;

            string name = match.Groups["name"].Value;
            if (rootName)
            {
                int dotIndex = name.IndexOf('.');
                if (dotIndex >= 0) name = name.Substring(0, dotIndex);
            }

            return name;
        }
        private static void AddSimpleNames(string text, HashSet<string> names)
        {
            foreach (string item in text.Split(','))
            {
                string name = item.Trim();
                if (Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                    names.Add(name);
            }
        }
        private static string StripComment(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";

            StringBuilder sb = new StringBuilder();
            char stringDelimiter = '\0';
            bool escaped = false;

            foreach (char c in line)
            {
                if (stringDelimiter != '\0')
                {
                    sb.Append(c);

                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == stringDelimiter)
                    {
                        stringDelimiter = '\0';
                    }

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    stringDelimiter = c;
                    sb.Append(c);
                    continue;
                }

                if (c == '#')
                    break;

                sb.Append(c);
            }

            return sb.ToString();
        }
        private static bool IsSafeAliasExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;

            char stringDelimiter = '\0';
            bool escaped = false;

            foreach (char c in expression)
            {
                if (stringDelimiter != '\0')
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == stringDelimiter)
                    {
                        stringDelimiter = '\0';
                    }

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    stringDelimiter = c;
                    continue;
                }

                if (char.IsLetterOrDigit(c) || c == '_' || c == '.' ||
                    c == '[' || c == ']' || char.IsWhiteSpace(c))
                {
                    continue;
                }

                return false;
            }

            return stringDelimiter == '\0';
        }
    }
}
