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
using CaeGlobals;

namespace PrePoMax
{
    // What a picture of the viewport is made of. The toolbar button takes these defaults and asks
    // nothing, so the picture it writes and the one the dialog writes come out of one renderer call.
    [Serializable]
    public class CaptureViewportProperties
    {
        // Variables                                                                                                                
        private const string Extension = ".png";
        private bool _saveToFile;
        private string _folder;
        private string _fileName;
        private bool _autoIncrement;
        private bool _transparentBackground;
        private int _magnification;
        private bool _copyToClipboard;


        // Properties                                                                                                             
        public bool SaveToFile { get { return _saveToFile; } set { _saveToFile = value; } }
        public string Folder { get { return _folder; } set { _folder = value == null ? "" : value.Trim(); } }
        // The extension is not the user's to choose, so one typed into the name is taken off again
        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value == null ? "" : value.Trim();
                if (_fileName.Length > Extension.Length &&
                    _fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                    _fileName = _fileName.Substring(0, _fileName.Length - Extension.Length);
            }
        }
        public bool AutoIncrement { get { return _autoIncrement; } set { _autoIncrement = value; } }
        public bool TransparentBackground
        {
            get { return _transparentBackground; }
            set { _transparentBackground = value; }
        }
        // The renderer draws a magnified picture in tiles, so the count is kept to what it will draw
        public int Magnification
        {
            get { return _magnification; }
            set
            {
                _magnification = value;
                if (_magnification < 1) _magnification = 1;
                else if (_magnification > vtkControl.vtkControl.MaxImageMagnification)
                    _magnification = vtkControl.vtkControl.MaxImageMagnification;
            }
        }
        public bool CopyToClipboard { get { return _copyToClipboard; } set { _copyToClipboard = value; } }


        // Constructors                                                                                                             
        public CaptureViewportProperties(string folder, string fileName)
        {
            _saveToFile = false;
            Folder = folder;
            FileName = fileName;
            _autoIncrement = true;
            _transparentBackground = true;
            _magnification = 1;
            _copyToClipboard = true;
        }


        // Methods                                                                                                                  
        // The file the next capture writes. A numbered name takes the first number the folder does not
        // hold yet, so a run of captures replaces none of the pictures taken before it
        public string GetFileName()
        {
            if (_folder.Length == 0) throw new CaeException("The folder of the picture is missing.");
            if (!Directory.Exists(_folder)) throw new CaeException("The folder '" + _folder + "' does not exist.");
            if (_fileName.Length == 0) throw new CaeException("The name of the picture is missing.");
            if (_fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new CaeException("The name '" + _fileName + "' is not a valid file name.");
            //
            if (!_autoIncrement) return Path.Combine(_folder, _fileName + Extension);
            //
            for (int i = 1; i <= 9999; i++)
            {
                string fileName = Path.Combine(_folder, _fileName + "_" + i.ToString("000") + Extension);
                if (!File.Exists(fileName)) return fileName;
            }
            throw new CaeException("The folder " + _folder + " already holds a picture under every number of the " +
                                   "name '" + _fileName + "'.");
        }
    }
}
