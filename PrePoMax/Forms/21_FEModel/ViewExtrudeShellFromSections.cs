using System;
using System.ComponentModel;

namespace PrePoMax.Forms
{
    class ViewExtrudeShellFromSections
    {
        private string _name;
        private string _compositeLayupName;
        private int _numberOfLayers;
        private string _baseMeshPosition;
        private bool _condenseNodes;
        private double _condenseTolerance;


        [Category("1 General")]
        [DisplayName("New Part Name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [Category("2 Info")]
        [DisplayName("Composite Layup")]
        [ReadOnly(true)]
        public string CompositeLayupName
        {
            get { return _compositeLayupName; }
            set { _compositeLayupName = value; }
        }

        [Category("2 Info")]
        [DisplayName("Number of Layers")]
        [ReadOnly(true)]
        public int NumberOfLayers
        {
            get { return _numberOfLayers; }
            set { _numberOfLayers = value; }
        }

        [Category("2 Info")]
        [DisplayName("Base Mesh Position")]
        [Description("Shell offset type read from the composite layup settings.")]
        [ReadOnly(true)]
        public string BaseMeshPosition
        {
            get { return _baseMeshPosition; }
            set { _baseMeshPosition = value; }
        }

        [Category("3 Options")]
        [DisplayName("Condense Nodes")]
        [Description("Merge solid mesh boundary nodes that coincide (within tolerance) with nodes of other active parts.")]
        public bool CondenseNodes
        {
            get { return _condenseNodes; }
            set { _condenseNodes = value; }
        }

        [Category("3 Options")]
        [DisplayName("Condense Tolerance")]
        [Description("Maximum distance between nodes to be merged (in model units). Only used when Condense Nodes is enabled.")]
        public double CondenseTolerance
        {
            get { return _condenseTolerance; }
            set { _condenseTolerance = value > 0 ? value : _condenseTolerance; }
        }


        public ViewExtrudeShellFromSections(string shellPartName, string compositeLayupName,
                                            int numberOfLayers, string baseMeshPosition)
        {
            _name = shellPartName + "-Solid";
            _compositeLayupName = compositeLayupName;
            _numberOfLayers = numberOfLayers;
            _baseMeshPosition = baseMeshPosition;
            _condenseNodes = false;
            _condenseTolerance = 0.01;
        }
    }
}
