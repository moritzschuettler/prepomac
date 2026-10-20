using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CExtrudeShellFromSections : PreprocessCommand
    {
        private string _shellPartName;
        private string _newPartName;
        private bool _condenseNodes;
        private double _condenseTolerance;


        public CExtrudeShellFromSections(string shellPartName, string newPartName,
                                         bool condenseNodes, double condenseTolerance)
            : base("Extrude shell mesh from sections")
        {
            _shellPartName = shellPartName;
            _newPartName = newPartName;
            _condenseNodes = condenseNodes;
            _condenseTolerance = condenseTolerance;
        }


        public override bool Execute(Controller receiver)
        {
            receiver.ExtrudeShellFromSections(_shellPartName, _newPartName,
                                              _condenseNodes, _condenseTolerance);
            return true;
        }
    }
}
