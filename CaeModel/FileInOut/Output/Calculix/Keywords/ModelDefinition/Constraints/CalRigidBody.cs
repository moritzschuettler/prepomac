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
    internal class CalRigidBody : CalculixKeyword
    {
        // Variables                                                                                                                
        protected RigidBody _rigidBody;
        protected Dictionary<string, CalReferencePoint> _referencePoints;
        protected string _surfaceNodeSetName;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalRigidBody(CalRigidBody calRigidBody)
            :this(calRigidBody._rigidBody, calRigidBody._referencePoints, calRigidBody._surfaceNodeSetName)
        {
        }
        public CalRigidBody(RigidBody rigidBody, Dictionary<string, CalReferencePoint> referencePoints, string surfaceNodeSetName)
        {
            _rigidBody = rigidBody;
            _referencePoints = referencePoints;
            _surfaceNodeSetName = surfaceNodeSetName;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            //*RIGID BODY,NSET=rigid1,REF NODE=100,ROT NODE=101
            string nodeSetName;
            if (_rigidBody.RegionType == CaeGlobals.RegionTypeEnum.NodeSetName) nodeSetName = _rigidBody.RegionName;
            else if (_rigidBody.RegionType == CaeGlobals.RegionTypeEnum.SurfaceName) nodeSetName = _surfaceNodeSetName;
            else throw new NotSupportedException();
            //
            CalReferencePoint referencePoint = _referencePoints[_rigidBody.ReferencePointName];
            if (!referencePoint.HasRotationNode) throw new InvalidOperationException("Rigid body rotation node is missing.");
            return string.Format("*Rigid body, Nset={0}, Ref node={1}, Rot node={2}{3}", nodeSetName,
                                 referencePoint.ReferenceNodeId,
                                 referencePoint.RotationNodeId.Value,
                                 Environment.NewLine);
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
