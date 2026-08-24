// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.IO;
using System.Reflection;


namespace vtkControl
{
    internal static class TrackpadTrace
    {
        // Variables
        private const int _maxLines = 200;
        //
        private static int _lines = 0;
        private static string _fileName = null;


        // Methods
        public static void Write(string text)
        {
            if (_lines >= _maxLines) return;
            _lines++;
            //
            System.Diagnostics.Debug.WriteLine("Trackpad: " + text);
            //
            try
            {
                if (_fileName == null)
                {
                    string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    _fileName = Path.Combine(folder, "trackpad.log");
                    File.WriteAllText(_fileName, DateTime.Now + Environment.NewLine);
                }
                File.AppendAllText(_fileName, text + Environment.NewLine);
            }
            catch { }
        }
    }
}
