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
    internal class AbqRigidBody : CalRigidBody
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqRigidBody(CalRigidBody calRigidBody)
            : base(calRigidBody)
        {
        }
        public AbqRigidBody(RigidBody rigidBody, Dictionary<string, int[]> referencePointsNodeIds, string surfaceNodeSetName)
            : base(rigidBody, referencePointsNodeIds, surfaceNodeSetName)
        {
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            //*RIGID BODY,TIE NSET=rigid1,REF NODE=100
            string nodeSetName;
            if (_rigidBody.RegionType == CaeGlobals.RegionTypeEnum.NodeSetName) nodeSetName = _rigidBody.RegionName;
            else if (_rigidBody.RegionType == CaeGlobals.RegionTypeEnum.SurfaceName) nodeSetName = _surfaceNodeSetName;
            else throw new NotSupportedException();
            //
            return string.Format("*Rigid body, Tie Nset={0}, Ref node={1}{2}", nodeSetName,
                                 _referencePointsNodeIds[_rigidBody.ReferencePointName][0],
                                 Environment.NewLine);
        }
    }
}
