// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.IO;

namespace CaeGlobals
{
    // A check for the move from one serializer to another: clones with both, keeps the clone of the first and writes every
    // difference of the second to a log file, once for each type and path
    public sealed class DualObjectSerializer : IObjectSerializer
    {
        // Variables                                                                                                                
        public const string EnvironmentVariable = "PREPOMAX_DUAL_SERIALIZER";
        private readonly IObjectSerializer _primary;
        private readonly IObjectSerializer _secondary;
        private readonly string _logFileName;
        private readonly HashSet<string> _reported;
        private readonly object _lock;
        private int _cloneCount;
        private int _differenceCount;


        // Properties                                                                                                               
        public string LogFileName { get { return _logFileName; } }


        // Constructors                                                                                                             
        public DualObjectSerializer(IObjectSerializer primary, IObjectSerializer secondary, string logFileName)
        {
            _primary = primary;
            _secondary = secondary;
            _logFileName = logFileName;
            _reported = new HashSet<string>();
            _lock = new object();
            _cloneCount = 0;
            _differenceCount = 0;
        }


        // Methods                                                                                                                  
        public T Clone<T>(T value)
        {
            T primaryClone = _primary.Clone(value);
            if (value == null) return primaryClone;
            //
            List<string> differences;
            try
            {
                T secondaryClone = _secondary.Clone(value);
                differences = PmxGraphComparer.Compare(primaryClone, secondaryClone);
            }
            catch (Exception ex)
            {
                differences = new List<string>() { "exception: " + ex.GetType().Name + ": " + ex.Message };
            }
            Report(value.GetType(), differences);
            return primaryClone;
        }
        private void Report(Type type, List<string> differences)
        {
            lock (_lock)
            {
                _cloneCount++;
                if (differences.Count == 0) return;
                //
                _differenceCount++;
                string key = type.FullName + " | " + differences[0];
                if (!_reported.Add(key)) return;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_logFileName));
                    using (StreamWriter writer = File.AppendText(_logFileName))
                    {
                        writer.WriteLine("DIFF " + type.FullName + " (clone " + _cloneCount + ", differing clones " +
                                         _differenceCount + ")");
                        foreach (var difference in differences) writer.WriteLine("    " + difference);
                    }
                }
                catch
                {
                }
            }
        }
        public void Serialize(Stream stream, object value)
        {
            _primary.Serialize(stream, value);
        }
        public object Deserialize(Stream stream)
        {
            return _primary.Deserialize(stream);
        }
    }
}
