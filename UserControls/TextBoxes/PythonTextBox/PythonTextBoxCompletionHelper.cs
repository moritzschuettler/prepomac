// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace UserControls
{
    internal static class PythonTextBoxCompletionHelper
    {
        // Methods                                                                                                                  
        public static void GetCompletionExpression(string beforeCaret, out string expression, out string prefix,
                                                   out int replaceStartLocal)
        {
            expression = "";
            prefix = "";
            replaceStartLocal = beforeCaret.Length;

            int index = beforeCaret.Length - 1;

            // Find the identifier prefix after the last dot.
            while (index >= 0 && IsPythonIdentifierCharacter(beforeCaret[index]))
                index--;

            prefix = beforeCaret.Substring(index + 1);
            replaceStartLocal = index + 1;

            // There is no object expression before the prefix.
            if (index < 0 || beforeCaret[index] != '.')
                return;

            int dotIndex = index;
            index--;

            int squareBracketDepth = 0;
            int roundBracketDepth = 0;
            char stringDelimiter = '\0';

            while (index >= 0)
            {
                char c = beforeCaret[index];

                // Handle quoted strings inside indexers and method arguments.
                if (stringDelimiter != '\0')
                {
                    if (c == stringDelimiter &&
                        (index == 0 || beforeCaret[index - 1] != '\\'))
                    {
                        stringDelimiter = '\0';
                    }

                    index--;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    stringDelimiter = c;
                    index--;
                    continue;
                }

                if (c == ']')
                {
                    squareBracketDepth++;
                    index--;
                    continue;
                }

                if (c == '[')
                {
                    if (squareBracketDepth == 0)
                        break;

                    squareBracketDepth--;
                    index--;
                    continue;
                }

                if (c == ')')
                {
                    roundBracketDepth++;
                    index--;
                    continue;
                }

                if (c == '(')
                {
                    if (roundBracketDepth == 0)
                        break;

                    roundBracketDepth--;
                    index--;
                    continue;
                }

                // Inside brackets or parentheses, accept all characters.
                if (squareBracketDepth > 0 || roundBracketDepth > 0)
                {
                    index--;
                    continue;
                }

                // Outside brackets, continue through a chained Python expression.
                if (IsPythonIdentifierCharacter(c) || c == '.')
                {
                    index--;
                    continue;
                }

                break;
            }
            //
            expression = beforeCaret.Substring(index + 1, dotIndex - index - 1);
        }
        private static bool IsPythonIdentifierCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
