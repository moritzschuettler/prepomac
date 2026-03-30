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

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class AbqSurface : CalSurface
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqSurface(CalSurface calSurface)
            :base(calSurface)
        {
        }


        // Methods                                                                                                                  
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            if (_surface.Type == FeSurfaceType.Element)
            {
                string faceKey = "";
                foreach (var elementSetEntry in _surface.ElementFaces)
                {
                    if (_twoD)
                    {
                        if (elementSetEntry.Key == FeFaceName.S1) faceKey = "N";
                        else if (elementSetEntry.Key == FeFaceName.S2) faceKey = "P";
                        else if (elementSetEntry.Key == FeFaceName.S3) faceKey = "S1";
                        else if (elementSetEntry.Key == FeFaceName.S4) faceKey = "S2";
                        else if (elementSetEntry.Key == FeFaceName.S5) faceKey = "S3";
                        else if (elementSetEntry.Key == FeFaceName.S6) faceKey = "S4";
                    }
                    else if (_surface.SurfaceFaceTypes == FeSurfaceFaceTypes.ShellFaces ||
                             _surface.SurfaceFaceTypes == FeSurfaceFaceTypes.ShellEdgeFaces)
                    {
                        if (elementSetEntry.Key == FeFaceName.S1) faceKey = "SPOS";
                        else if (elementSetEntry.Key == FeFaceName.S2) faceKey = "SNEG";
                        else if (elementSetEntry.Key == FeFaceName.S3) faceKey = "E1";
                        else if (elementSetEntry.Key == FeFaceName.S4) faceKey = "E2";
                        else if (elementSetEntry.Key == FeFaceName.S5) faceKey = "E3";
                        else if (elementSetEntry.Key == FeFaceName.S6) faceKey = "E4";
                    }
                    else
                    {
                        faceKey = elementSetEntry.Key.ToString();
                    }
                    sb.AppendFormat("{0}, {1}", elementSetEntry.Value, faceKey).AppendLine();
                }
            }
            else if (_surface.Type == FeSurfaceType.Node)
            {
                sb.AppendFormat("{0}", _surface.NodeSetName).AppendLine();
            }
            else throw new NotImplementedException();
            return sb.ToString();
        }
    }
}
