// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class FirstRunChecker
{
    public static string PathToFileName(string path)
    {
        // Replace directory separators and invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        string safe = new string(path.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        //
        return safe;
    }
    public static bool IsFirstRun(string startDirectory)
    {
        string MarkerPath = Path.Combine(startDirectory,
                                         "trace.info");
        //
        if (!File.Exists(MarkerPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MarkerPath));
            File.WriteAllText(MarkerPath, DateTime.Now.ToString());
            return true;
        }
        return false;
    }
}
