using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using System.Drawing;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalInitialAngularVelocity : CalculixKeyword
    {
        // Variables                                                                                                                
        private InitialAngularVelocity _initialVelocity;
        private InitialTranslationalVelocity[] _initialTranslationalVelocities;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalInitialAngularVelocity(FeModel model, InitialAngularVelocity initialVelocity,
                                         Dictionary<string, int[]> referencePointsNodeIds)
        {
            _initialVelocity = initialVelocity;
            _initialTranslationalVelocities = model.GetTranslationalVelocities(initialVelocity, referencePointsNodeIds);
            //

        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _initialVelocity.Name);
            sb.AppendLine("*Initial conditions, Type=Velocity");
            return sb.ToString();
        }
        public override string GetDataString()
        {
            int nodeId;
            int deltaDOF;
            StringBuilder sb = new StringBuilder();
            //
            if (_initialTranslationalVelocities != null)
            {
                foreach (var velocity in _initialTranslationalVelocities)
                {
                    nodeId = velocity.NodeId;
                    //
                    if (nodeId < 0) { nodeId = -nodeId; deltaDOF = 3; }
                    else deltaDOF = 0;
                    //

                    //deltaDOF = 3;

                    if (velocity.V1.Value != 0)
                        sb.AppendFormat("{0}, {1}, {2}{3}", nodeId, 1 + deltaDOF, velocity.V1.Value.ToCalculiX16String(),
                            Environment.NewLine);
                    if (velocity.V2.Value != 0)
                        sb.AppendFormat("{0}, {1}, {2}{3}", nodeId, 2 + deltaDOF, velocity.V2.Value.ToCalculiX16String(),
                            Environment.NewLine);
                    if (velocity.V3.Value != 0)
                        sb.AppendFormat("{0}, {1}, {2}{3}", nodeId, 3 + deltaDOF, velocity.V3.Value.ToCalculiX16String(),
                            Environment.NewLine);
                }
            }
            //
            return sb.ToString();
        }
    }
}
