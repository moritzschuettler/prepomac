// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Text;

namespace PrePoMax.Mcp
{
    // A tool script that leaves nothing behind. The Python an agent sends runs in the scope of the
    // Python console, so its variables survive from one call to the next - and a tool script run in
    // that scope would overwrite them: names, value, json. So the script is made the body of one
    // function with a name nobody else uses, the function is called, and it is deleted again whether
    // the script succeeded or not.
    public static class McpPrivateScript
    {
        // Static methods                                                                                                           
        public static string Wrap(string source)
        {
            string name = "_prepomax_tool_" + Guid.NewGuid().ToString("N");
            StringBuilder text = new StringBuilder();
            text.Append("def ").Append(name).Append("():\n");
            foreach (string line in source.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.Length > 0) text.Append("    ").Append(line);
                text.Append("\n");
            }
            text.Append("try:\n");
            text.Append("    ").Append(name).Append("()\n");
            text.Append("finally:\n");
            text.Append("    del ").Append(name).Append("\n");
            return text.ToString();
        }
    }
}
