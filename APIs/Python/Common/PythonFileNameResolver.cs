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

namespace APIs.Python
{
    // A relative file name is taken from pmx.work_directory, never from the current directory of the process, which
    // is wherever the application happened to be started. A session without a work directory refuses a relative name
    // rather than guess; a full path is used as it is given.
    internal class PythonFileNameResolver
    {
        // Variables
        private readonly IPythonJobGateway _jobGateway;


        // Constructors
        public PythonFileNameResolver(IPythonJobGateway jobGateway)
        {
            _jobGateway = jobGateway;
        }


        // Methods
        public string GetFullFileName(string fileName, string argumentName)
        {
            if (fileName == null || fileName.Trim().Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            //
            try
            {
                if (Path.IsPathRooted(fileName)) return Path.GetFullPath(fileName);
                //
                string workDirectory = _jobGateway == null ? null : _jobGateway.GetWorkDirectory();
                if (string.IsNullOrWhiteSpace(workDirectory))
                    throw new PythonAPIException("The argument '" + argumentName + "' is the relative name '" +
                                                 fileName + "', which is taken from pmx.work_directory, but there " +
                                                 "is no work directory: pass -w or set it in Settings - Solver, or " +
                                                 "give a full path.");
                return Path.GetFullPath(Path.Combine(workDirectory, fileName));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException ||
                                       ex is PathTooLongException || ex is System.Security.SecurityException)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' is invalid: " + ex.Message);
            }
        }
        public string GetExistingFileName(string fileName, string argumentName)
        {
            string fullFileName = GetFullFileName(fileName, argumentName);
            if (!File.Exists(fullFileName))
                throw new PythonAPIException("The file '" + fullFileName + "' does not exist.");
            //
            return fullFileName;
        }
        // A file is written only into a folder that exists, so a mistyped folder is refused before anything is recorded
        public string GetOutputFileName(string fileName, string argumentName)
        {
            string fullFileName = GetFullFileName(fileName, argumentName);
            string directoryName = Path.GetDirectoryName(fullFileName);
            if (directoryName != null && directoryName.Length > 0 && !Directory.Exists(directoryName))
                throw new PythonAPIException("The directory '" + directoryName + "' does not exist.");
            //
            return fullFileName;
        }
    }
}
