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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using System.IO;

namespace PrePoMax.Commands
{
    [Serializable]
    public class CSaveToPmx : SaveCommand, IFileCommand
    {
        // Variables                                                                                                                
        private string _fileName;
        private byte[] _hash;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public byte[] Hash { get { return _hash; } set { _hash = value; } }


        // Constructor                                                                                                              
        public CSaveToPmx(string fileName)
            :base("Save to file")
        {
            _fileName = Tools.GetLocalPath(fileName);
            _hash = null;
        }


        // Methods                                                                                                                  
        private byte[] GetHash()
        {
            byte[] hash = null;
            string globalFileName = Tools.GetGlobalPath(_fileName);
            //
            if (File.Exists(globalFileName))
            {
                using (FileStream fop = File.OpenRead(Tools.GetGlobalPath(_fileName)))
                {
                    hash = System.Security.Cryptography.SHA1.Create().ComputeHash(fop);
                    string hashString = BitConverter.ToString(hash);
                }
            }
            //
            return hash;
        }
        public bool IsFileHashUnchanged()
        {
            byte[] hash = GetHash();
            if (hash == null || _hash == null) return false;
            else return hash.SequenceEqual(_hash);
        }
        public override bool Execute(Controller receiver)
        {
            receiver.SaveToPmx(Tools.GetGlobalPath(_fileName));
            //
            _hash = GetHash();
            //
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
            //return _dateCreated.ToString("MM/dd/yy HH:mm:ss") + "   " + "Model saved to file: " + _fileName;
        }
    }
}
