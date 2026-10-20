using System.ComponentModel;

namespace PrePoMax.Forms
{
    class ViewExtrudeShellUniform
    {
        private string _name;
        private double _totalThickness;
        private int _numSubdivisions;
        private bool _condenseNodes;
        private double _condenseTolerance;


        [Category("1 General")]
        [DisplayName("New Part Name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [Category("1 General")]
        [DisplayName("Total Thickness")]
        [Description("Total extrusion depth in model units.")]
        public double TotalThickness
        {
            get { return _totalThickness; }
            set { _totalThickness = value > 0 ? value : _totalThickness; }
        }

        [Category("1 General")]
        [DisplayName("Number of Subdivisions")]
        [Description("Number of solid element layers along the extrusion direction.")]
        public int NumSubdivisions
        {
            get { return _numSubdivisions; }
            set { _numSubdivisions = value >= 1 ? value : _numSubdivisions; }
        }

        [Category("2 Options")]
        [DisplayName("Condense Nodes")]
        [Description("Merge boundary nodes that coincide (within tolerance) with nodes of other active parts.")]
        public bool CondenseNodes
        {
            get { return _condenseNodes; }
            set { _condenseNodes = value; }
        }

        [Category("2 Options")]
        [DisplayName("Condense Tolerance")]
        [Description("Maximum distance between nodes to be merged (in model units). Only used when Condense Nodes is enabled.")]
        public double CondenseTolerance
        {
            get { return _condenseTolerance; }
            set { _condenseTolerance = value > 0 ? value : _condenseTolerance; }
        }


        public ViewExtrudeShellUniform(string shellPartName)
        {
            _name = shellPartName + "-Solid";
            _totalThickness = 1.0;
            _numSubdivisions = 1;
            _condenseNodes = false;
            _condenseTolerance = 0.01;
        }
    }
}
