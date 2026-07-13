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
using System.Text;
using CaeGlobals;
using CaeModel;


namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalCoupling : CalculixKeyword
    {
        // Variables                                                                                                                
        private Coupling _coupling;
        private Dictionary<string, CalReferencePoint> _referencePoints;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalCoupling(Coupling coupling, Dictionary<string, CalReferencePoint> referencePoints)
        {
            _coupling = coupling;
            _referencePoints = referencePoints;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            if (_coupling.RegionType != RegionTypeEnum.SurfaceName) throw new NotSupportedException();
            //
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Coupling, Ref node={0}, Surface={1}, Constraint name={2}",
                            _referencePoints[_coupling.ReferencePointName].ReferenceNodeId,
                            _coupling.RegionName, _coupling.Name);
            sb.AppendLine();
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
