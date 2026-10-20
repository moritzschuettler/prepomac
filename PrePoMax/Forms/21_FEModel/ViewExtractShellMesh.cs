using System;
using System.ComponentModel;

namespace PrePoMax.Forms
{
    class ViewExtractShellMesh
    {
        private string _name;
        private bool _condenseNodes;
        private bool _secondOrder;
        private int _selectedFaces;


        [Category("1 General")]
        [DisplayName("New Part Name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [Category("2 Options")]
        [DisplayName("Condense Coincident Nodes")]
        [Description("When checked, the shell mesh shares nodes with the solid mesh. When unchecked, new independent nodes are created.")]
        public bool CondenseNodes
        {
            get { return _condenseNodes; }
            set { _condenseNodes = value; }
        }

        [Category("2 Options")]
        [DisplayName("Second Order Elements")]
        [Description("When checked, linear shell elements (e.g. S4) are converted to parabolic (e.g. S8) by adding midside nodes at edge midpoints.")]
        public bool SecondOrder
        {
            get { return _secondOrder; }
            set { _secondOrder = value; }
        }

        [Category("3 Selection")]
        [DisplayName("Selected Faces")]
        [ReadOnly(true)]
        public int SelectedFaces
        {
            get { return _selectedFaces; }
            set { _selectedFaces = value; }
        }


        public ViewExtractShellMesh(string partName)
        {
            _name = partName + "-Shell";
            _condenseNodes = false;
            _secondOrder = false;
            _selectedFaces = 0;
        }
    }
}
