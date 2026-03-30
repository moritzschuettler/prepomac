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
using CaeModel;
using CaeMesh;
using CaeGlobals;
using System.Runtime.InteropServices;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class AbqImportedPressureLoad : CalImportedPressureLoad
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqImportedPressureLoad(CalImportedPressureLoad calImportedPressureLoad)
             : base(calImportedPressureLoad)
        {
            OpType = OpTypeEnum.New;
        }


        // Methods                                                                                                                  
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            //
            if (_dLoads != null)
            {
                string faceKey = "";
                FeFaceName faceName;
                double magnitude;
                //
                foreach (var dLoad in _dLoads)
                {
                    faceName = dLoad.ElementFaceName;
                    if (_load.TwoD)
                    {
                        if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2) throw new NotSupportedException();
                        else if (faceName == FeFaceName.S3) faceKey = "P1";
                        else if (faceName == FeFaceName.S4) faceKey = "P2";
                        else if (faceName == FeFaceName.S5) faceKey = "P3";
                        else if (faceName == FeFaceName.S6) faceKey = "P4";
                    }
                    else if (_surfaceFaceType == FeSurfaceFaceTypes.ShellFaces ||
                             _surfaceFaceType == FeSurfaceFaceTypes.ShellEdgeFaces)
                    {
                        faceKey = "P";
                    }
                    else
                    {
                        faceKey = "P" + faceName.ToString()[1];
                    }
                    //
                    magnitude = ratio * dLoad.Magnitude.Value;
                    if (_surfaceFaceType == FeSurfaceFaceTypes.ShellFaces && faceName == FeFaceName.S2) magnitude *= -1;
                    //
                    sb.AppendFormat("{0}, {1}, {2}", dLoad.ElementId, faceKey, magnitude.ToCalculiX16String()).AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
