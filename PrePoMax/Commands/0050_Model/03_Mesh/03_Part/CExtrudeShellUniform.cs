using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CExtrudeShellUniform : PreprocessCommand
    {
        private string _shellPartName;
        private string _newPartName;
        private double _totalThickness;
        private int _numSubdivisions;
        private bool _condenseNodes;
        private double _condenseTolerance;


        public CExtrudeShellUniform(string shellPartName, string newPartName,
                                    double totalThickness, int numSubdivisions,
                                    bool condenseNodes, double condenseTolerance)
            : base("Create solids by extrusion")
        {
            _shellPartName = shellPartName;
            _newPartName = newPartName;
            _totalThickness = totalThickness;
            _numSubdivisions = numSubdivisions;
            _condenseNodes = condenseNodes;
            _condenseTolerance = condenseTolerance;
        }


        public override bool Execute(Controller receiver)
        {
            receiver.ExtrudeShellUniform(_shellPartName, _newPartName,
                                         _totalThickness, _numSubdivisions,
                                         _condenseNodes, _condenseTolerance);
            return true;
        }
    }
}
