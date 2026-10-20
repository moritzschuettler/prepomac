using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CExtractShellMesh : PreprocessCommand
    {
        private string _solidPartName;
        private string _newPartName;
        private int[] _faceIds;
        private bool _condenseNodes;
        private bool _secondOrder;


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
