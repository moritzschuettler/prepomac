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
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CRepairGeometry : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private GeometryRepair _geometryRepair;


        // Constructor                                                                                                              
        public CRepairGeometry(string[] partNames, GeometryRepair geometryRepair)
            : base("Repair geometry")
        {
            _partNames = partNames.ToArray();
            _geometryRepair = geometryRepair.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RepairGeometry(_partNames, _geometryRepair.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            GeometryRepair defaults = new GeometryRepair();
            writer.Statement(writer.Call("pmx.model.geometry.parts", "repair", writer.Value(_partNames),
                GetFlag("fix_shape=", _geometryRepair.FixShape, defaults.FixShape),
                GetFlag("remove_small_edges=", _geometryRepair.RemoveSmallEdges, defaults.RemoveSmallEdges),
                GetFlag("remove_small_faces=", _geometryRepair.RemoveSmallFaces, defaults.RemoveSmallFaces),
                GetFlag("sew_faces=", _geometryRepair.SewFaces, defaults.SewFaces),
                GetFlag("fuse_faces=", _geometryRepair.FuseFaces, defaults.FuseFaces),
                GetFlag("join_edges=", _geometryRepair.JoinEdges, defaults.JoinEdges),
                double.IsNaN(_geometryRepair.Tolerance) ? null :
                    "tolerance=" + PythonValueFormatter.Number(_geometryRepair.Tolerance)));
            writer.CountEmittedCommand();
        }
        private static string GetFlag(string argument, bool value, bool defaultValue)
        {
            return value == defaultValue ? null : argument + PythonValueFormatter.Format(value);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + string.Join(", ", _partNames);
        }
    }
}
