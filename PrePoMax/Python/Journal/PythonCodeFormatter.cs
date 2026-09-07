// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Text;

namespace PrePoMax.Python.Journal
{
    // Breaks a long emitted statement over several lines. A call with a rebuilt selection or a long id
    // list runs to several hundred characters on one line, which reads badly in the script view next to
    // the command history and in an editor. Only the code the emitters build is wrapped; a comment and
    // a recorded script keep the lines they were written with.
    public static class PythonCodeFormatter
    {
        // Variables                                                                                                                
        public const int MaxLineLength = 80;
        private const string Indent = "    ";


        // Methods                                                                                                                  
        public static List<string> Wrap(string statement)
        {
            return Wrap(statement, MaxLineLength);
        }
        public static List<string> Wrap(string statement, int maxLineLength)
        {
            List<string> lines = new List<string>();
            if (statement == null) lines.Add("");
            else Append(lines, "", statement, maxLineLength);
            //
            return lines;
        }
        // The statement is opened at its outermost bracket group and its arguments are packed into as
        // few lines as the width allows, so that a call with many short arguments does not become a
        // column of one word lines
        private static void Append(List<string> lines, string indent, string text, int maxLineLength)
        {
            int openIndex;
            int closeIndex;
            if (indent.Length + text.Length <= maxLineLength ||
                !TryFindLastGroup(text, out openIndex, out closeIndex))
            {
                lines.Add(indent + text);
                return;
            }
            //
            string head = text.Substring(0, openIndex + 1);
            string body = text.Substring(openIndex + 1, closeIndex - openIndex - 1);
            string suffix = text.Substring(closeIndex);
            List<string> parts = SplitTopLevel(body);
            // A single piece that carries no group of its own is as short as it gets
            if (!CanSplit(parts))
            {
                lines.Add(indent + text);
                return;
            }
            //
            string bodyIndent;
            string currentIndent;
            StringBuilder current = new StringBuilder();
            // A list keeps its bracket with the first values and aligns the rest under them, which reads
            // as the one value it is; a call is opened on its own line, because the name in front of it
            // is long enough that aligning under it would leave no room
            if (IsLiteralGroup(text, openIndex) && indent.Length + head.Length + Indent.Length <= maxLineLength)
            {
                bodyIndent = indent + new string(' ', head.Length);
                currentIndent = indent;
                current.Append(head);
            }
            else
            {
                lines.Add(indent + head);
                bodyIndent = indent + Indent;
                currentIndent = bodyIndent;
            }
            //
            for (int i = 0; i < parts.Count; i++)
            {
                string token = parts[i] + (i < parts.Count - 1 ? "," : suffix);
                bool separated = current.Length > 0 && !EndsWithOpenBracket(current);
                //
                if (current.Length > 0 &&
                    currentIndent.Length + current.Length + (separated ? 1 : 0) + token.Length > maxLineLength)
                {
                    lines.Add(currentIndent + current.ToString());
                    current.Length = 0;
                    currentIndent = bodyIndent;
                    separated = false;
                }
                if (current.Length == 0 && currentIndent.Length + token.Length > maxLineLength)
                {
                    Append(lines, currentIndent, token, maxLineLength);
                    currentIndent = bodyIndent;
                    continue;
                }
                if (separated) current.Append(' ');
                current.Append(token);
            }
            if (current.Length > 0) lines.Add(currentIndent + current.ToString());
        }
        private static bool IsLiteralGroup(string text, int openIndex)
        {
            if (text[openIndex] == '(') return false;
            if (openIndex == 0) return true;
            //
            char previous = text[openIndex - 1];
            return !char.IsLetterOrDigit(previous) && previous != '_' &&
                   previous != ')' && previous != ']' && previous != '}';
        }
        private static bool EndsWithOpenBracket(StringBuilder text)
        {
            if (text.Length == 0) return false;
            //
            char last = text[text.Length - 1];
            return last == '(' || last == '[' || last == '{';
        }
        private static bool CanSplit(List<string> parts)
        {
            if (parts.Count == 0) return false;
            if (parts.Count > 1) return true;
            //
            int openIndex;
            int closeIndex;
            return TryFindLastGroup(parts[0], out openIndex, out closeIndex);
        }
        // The last group that closes at the top level, so that a trailing comma or closing bracket left
        // by the caller stays with it
        private static bool TryFindLastGroup(string text, out int openIndex, out int closeIndex)
        {
            openIndex = -1;
            closeIndex = -1;
            //
            Stack<int> open = new Stack<int>();
            char quote = '\0';
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (quote != '\0')
                {
                    if (character == '\\') i++;
                    else if (character == quote) quote = '\0';
                }
                else if (character == '\'' || character == '"') quote = character;
                else if (character == '(' || character == '[' || character == '{') open.Push(i);
                else if (character == ')' || character == ']' || character == '}')
                {
                    if (open.Count == 0) continue;
                    //
                    int start = open.Pop();
                    if (open.Count == 0)
                    {
                        openIndex = start;
                        closeIndex = i;
                    }
                }
            }
            //
            return openIndex >= 0 && closeIndex > openIndex + 1;
        }
        private static List<string> SplitTopLevel(string body)
        {
            List<string> parts = new List<string>();
            int depth = 0;
            int start = 0;
            char quote = '\0';
            for (int i = 0; i < body.Length; i++)
            {
                char character = body[i];
                if (quote != '\0')
                {
                    if (character == '\\') i++;
                    else if (character == quote) quote = '\0';
                }
                else if (character == '\'' || character == '"') quote = character;
                else if (character == '(' || character == '[' || character == '{') depth++;
                else if (character == ')' || character == ']' || character == '}') depth--;
                else if (character == ',' && depth == 0)
                {
                    parts.Add(body.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            parts.Add(body.Substring(start).Trim());
            //
            return parts;
        }
    }
}
