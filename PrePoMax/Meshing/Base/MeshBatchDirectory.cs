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
using System.Threading;

namespace PrePoMax
{
    internal class MeshBatchDirectory
    {
        // Variables
        private const string _directoryNamePrefix = "MeshBatch_";
        private const string _lockFileName = ".lock";
        private const string _retainedFileName = ".retained";
        private const int _maxDirectoryCount = 10000;
        private const int _deleteAttempts = 5;
        private const int _deleteRetryDelayMs = 200;
        //
        private readonly string _path;
        private FileStream _lockStream;


        // Properties
        public string Path { get { return _path; } }


        // Constructors
        private MeshBatchDirectory(string path, FileStream lockStream)
        {
            _path = path;
            _lockStream = lockStream;
        }


        // Methods
        public static MeshBatchDirectory Create(string workDirectory)
        {
            for (int counter = 1; counter <= _maxDirectoryCount; counter++)
            {
                string path = System.IO.Path.Combine(workDirectory, $"{_directoryNamePrefix}{counter:D4}");
                if (Directory.Exists(path)) continue;
                //
                Directory.CreateDirectory(path);
                try
                {
                    FileStream lockStream = new FileStream(System.IO.Path.Combine(path, _lockFileName),
                                                           FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                    return new MeshBatchDirectory(path, lockStream);
                }
                catch (IOException) { }
            }
            throw new CaeGlobals.CaeException("A meshing folder could not be created in the work directory.");
        }
        public void Retain()
        {
            try
            {
                File.WriteAllText(System.IO.Path.Combine(_path, _retainedFileName), DateTime.Now.ToString());
            }
            catch { }
            ReleaseLock();
        }
        public bool Delete()
        {
            ReleaseLock();
            return TryDeleteDirectory(_path, _deleteAttempts);
        }
        private void ReleaseLock()
        {
            if (_lockStream == null) return;
            _lockStream.Dispose();
            _lockStream = null;
        }
        public static void DeleteStaleDirectories(string workDirectory)
        {
            try
            {
                if (workDirectory == null || !Directory.Exists(workDirectory)) return;
                //
                foreach (string path in Directory.GetDirectories(workDirectory, _directoryNamePrefix + "*"))
                {
                    try
                    {
                        if (File.Exists(System.IO.Path.Combine(path, _retainedFileName))) continue;
                        // A directory in use by a running PrePoMax holds its lock file open
                        using (new FileStream(System.IO.Path.Combine(path, _lockFileName), FileMode.OpenOrCreate,
                                              FileAccess.ReadWrite, FileShare.None)) { }
                        TryDeleteDirectory(path, 1);
                    }
                    catch { }
                }
            }
            catch { }
        }
        private static bool TryDeleteDirectory(string path, int attempts)
        {
            for (int i = 0; i < attempts; i++)
            {
                if (i > 0) Thread.Sleep(_deleteRetryDelayMs);
                try
                {
                    if (!Directory.Exists(path)) return true;
                    //
                    foreach (string fileName in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        FileAttributes attributes = File.GetAttributes(fileName);
                        if ((attributes & FileAttributes.ReadOnly) != 0)
                            File.SetAttributes(fileName, attributes & ~FileAttributes.ReadOnly);
                    }
                    Directory.Delete(path, true);
                    return true;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return !Directory.Exists(path);
        }
    }
}
