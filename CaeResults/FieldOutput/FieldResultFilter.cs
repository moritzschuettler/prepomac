using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeResults
{
    [Serializable]
    public enum FieldResultFilterTypeEnum
    {
        None,
        Median,
        Average,
        Smooth,
    }
    [Serializable]
    public enum FieldResultFilterSourceTypeEnum
    {
        [StandardValue("AllNodes", DisplayName = "All nodes")]
        AllNodes,
        [StandardValue("SurfaceNodes", DisplayName = "Surface nodes")]
        SurfaceNodes,
    }
    //
    [Serializable]
    public class FieldResultFilter
    {
        // Variables                                                                                                                
        protected FieldResultFilterTypeEnum _type;
        protected FieldResultFilterSourceTypeEnum _sourceType;
        protected int _numberOfNeighbouringLayers;
        protected int _numberOfIterations;


        // Properties                                                                                                               
        public FieldResultFilterTypeEnum Type { get { return _type; } set { _type = value; } }
        public FieldResultFilterSourceTypeEnum SourceType { get { return _sourceType; } set { _sourceType = value; } }
        public int NumberOfNeighbouringLayers
        {
            get { return _numberOfNeighbouringLayers; }
            set
            {
                _numberOfNeighbouringLayers = value;
                if (_numberOfNeighbouringLayers < 1) _numberOfNeighbouringLayers = 1;
            }
        }
        public int NumberOfIterations
        {
            get { return _numberOfIterations; }
            set
            {
                _numberOfIterations = value;
                if (_numberOfIterations < 1) _numberOfIterations = 1;
            }
        }


        // Constructors                                                                                                             
        public FieldResultFilter()
        {
            _type = FieldResultFilterTypeEnum.None;
            _sourceType = FieldResultFilterSourceTypeEnum.AllNodes;
            _numberOfNeighbouringLayers = 1;
            _numberOfIterations = 1;
        }
    }
}
