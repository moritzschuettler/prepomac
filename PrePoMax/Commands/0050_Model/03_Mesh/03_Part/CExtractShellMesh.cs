using System;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [PmxType(50043)]
    class CExtractShellMesh : PreprocessCommand
    {
        [PmxKey(0)] private string _solidPartName;
        [PmxKey(1)] private string _newPartName;
        [PmxKey(2)] private int[] _faceIds;
        [PmxKey(3)] private bool _condenseNodes;
        [PmxKey(4)] private bool _secondOrder;


        public CExtractShellMesh(string solidPartName, string newPartName, int[] faceIds, bool condenseNodes, bool secondOrder)
            : base("Extract shell mesh")
        {
            _solidPartName = solidPartName;
            _newPartName = newPartName;
            _faceIds = faceIds;
            _condenseNodes = condenseNodes;
            _secondOrder = secondOrder;
        }


        public override bool Execute(Controller receiver)
        {
            receiver.ExtractShellMesh(_solidPartName, _newPartName, _faceIds, _condenseNodes, _secondOrder);
            return true;
        }
    }
}
