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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CCreateMesh : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _partName;
        [OptionalField] private string[] _partNames;
        [OptionalField] private string[] _successfulPartNames;
        [NonSerialized] private TimeSpan _historySerializationTime;


        // Properites                                                                                                               
        public string PartName { get { return PartNames[0]; } }
        public string[] PartNames
        {
            get
            {
                if (_successfulPartNames != null) return _successfulPartNames;
                else if (_partNames != null) return _partNames;
                else return new string[] { _partName };
            }
        }
        public TimeSpan HistorySerializationTime
        {
            get { return _historySerializationTime; }
            set { _historySerializationTime = value; }
        }


        // Constructor                                                                                                              
        public CCreateMesh(string partName)
            : base("Create mesh")
        {
            _partName = partName;
        }
        public CCreateMesh(string[] partNames)
            : base(partNames.Length == 1 ? "Create mesh" : "Create meshes")
        {
            _partNames = partNames.ToArray();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            bool result = receiver.CreateMeshes(PartNames, out string[] successfulPartNames);
            if (_successfulPartNames == null && successfulPartNames.Length > 0)
                _successfulPartNames = successfulPartNames.ToArray();
            //
            return result;
        }
        public override string GetCommandString()
        {
            if (PartNames.Length == 1) return base.GetCommandString() + PartNames[0];
            else return base.GetCommandString() + GetArrayAsString(PartNames);
        }
    }
}
