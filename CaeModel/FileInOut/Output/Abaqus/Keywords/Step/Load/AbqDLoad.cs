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

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class AbqDLoad : CalDLoad
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqDLoad(CalDLoad calDLoad)
            :base(calDLoad)
        {
            OpType = OpTypeEnum.New;
        }


        // Methods                                                                                                                  
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            FeFaceName faceName;
            string faceKey = "";
            double magnitude;
            foreach (var entry in _surface.ElementFaces)
            {
                faceName = entry.Key;
                if (_load.TwoD)
                {
                    if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2) throw new NotSupportedException();
                    else if (faceName == FeFaceName.S3) faceKey = "P1";
                    else if (faceName == FeFaceName.S4) faceKey = "P2";
                    else if (faceName == FeFaceName.S5) faceKey = "P3";
                    else if (faceName == FeFaceName.S6) faceKey = "P4";
                }
                else if (_surface.SurfaceFaceTypes == FeSurfaceFaceTypes.ShellFaces ||
                         _surface.SurfaceFaceTypes == FeSurfaceFaceTypes.ShellEdgeFaces)
                {
                    faceKey = "P";
                }
                else
                {
                    faceKey = "P" + faceName.ToString()[1];
                }
                //
                double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
                //
                magnitude = ratio * _load.Magnitude.Value;
                if (_surface.SurfaceFaceTypes == FeSurfaceFaceTypes.ShellFaces && faceName == FeFaceName.S2) magnitude *= -1;
                //
                sb.AppendFormat("{0}, {1}, {2}", entry.Value, faceKey, magnitude.ToCalculiX16String()).AppendLine();
            }
            return sb.ToString();
        }
    }
}
