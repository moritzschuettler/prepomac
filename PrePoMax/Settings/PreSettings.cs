using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using System.IO;
using DynamicTypeDescriptor;
using System.Drawing;

namespace PrePoMax
{
    [Serializable]
    public class PreSettings : ISettings
    {
        // Variables                                                                                                                
        private GeometrySelectModeEnum _geometrySelectMode;
        private Color _primaryHighlightColor;
        private Color _secondaryHighlightColor;
        private Color _mouseHighlightColor;
        private Color _constraintSymbolColor;
        private Color _initialConditionSymbolColor;
        private Color _boundaryConditionSymbolColor;
        private Color _loadSymbolColor;
        private Color _definedFieldSymbolColor;
        private int _symbolSize;
        private int _nodeSymbolSize;
        private bool _drawSymbolEdges;
        //
        private AnnotationBackgroundType _colorBarBackgroundType;
        private bool _colorBarDrawBorder;


        // Properties                                                                                                               
        public GeometrySelectModeEnum GeometrySelectMode
        {
            get { return _geometrySelectMode; }
            set { _geometrySelectMode = value; }
        }
        public Color PrimaryHighlightColor
        {
            get { return _primaryHighlightColor; }
            set { _primaryHighlightColor = value; }
        }
        public Color SecondaryHighlightColor
        {
            get { return _secondaryHighlightColor; }
            set { _secondaryHighlightColor = value; }
        }
        public Color MouseHighlightColor
        {
            get { return _mouseHighlightColor; }
            set { _mouseHighlightColor = value; }
        }
        public Color ConstraintSymbolColor
        {
            get { return _constraintSymbolColor; }
            set { _constraintSymbolColor = value; }
        }
        public Color InitialConditionSymbolColor
        {
            get { return _initialConditionSymbolColor; }
            set { _initialConditionSymbolColor = value; }
        }
        public Color BoundaryConditionSymbolColor
        {
            get { return _boundaryConditionSymbolColor; }
            set { _boundaryConditionSymbolColor = value; }
        }
        public Color LoadSymbolColor
        {
            get { return _loadSymbolColor; }
            set { _loadSymbolColor = value; }
        }
        public Color DefinedFieldSymbolColor
        {
            get { return _definedFieldSymbolColor; }
            set { _definedFieldSymbolColor = value; }
        }
        public int SymbolSize
        {
            get { return _symbolSize; }
            set 
            {
                _symbolSize = value;
                if (_symbolSize < 1) _symbolSize = 1;
            }
        }
        public int NodeSymbolSize
        {
            get { return _nodeSymbolSize; }
            set
            {
                _nodeSymbolSize = value;
                if (_nodeSymbolSize < 1) _nodeSymbolSize = 1;
            }
        }
        public int HighlightNodeSymbolSize { get { return _nodeSymbolSize + 3; } }
        public bool DrawSymbolEdges
        {
            get { return _drawSymbolEdges; }
            set { _drawSymbolEdges = value; }
        }
        //
        public AnnotationBackgroundType ColorBarBackgroundType
        {
            get { return _colorBarBackgroundType; }
            set
            {
                if (value != _colorBarBackgroundType)
                {
                    _colorBarBackgroundType = value;
                    if (_colorBarBackgroundType == AnnotationBackgroundType.White) _colorBarDrawBorder = true;
                }
            }
        }
        public bool ColorBarDrawBorder { get { return _colorBarDrawBorder; } set { _colorBarDrawBorder = value; } }


        // Constructors                                                                                                             
        public PreSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _primaryHighlightColor = Color.Red;
            _secondaryHighlightColor = Color.Violet;
            _mouseHighlightColor = Color.Orange;
            _geometrySelectMode = GeometrySelectModeEnum.SelectLocation;
            _constraintSymbolColor = Color.Yellow;
            _initialConditionSymbolColor = Color.Orange;
            _boundaryConditionSymbolColor = Color.Lime;
            _loadSymbolColor = Color.RoyalBlue;
            _definedFieldSymbolColor = Color.Tomato;
            _symbolSize = 50;
            _nodeSymbolSize = 5;
            _drawSymbolEdges = true;
            //
            _colorBarBackgroundType = AnnotationBackgroundType.None;
            _colorBarDrawBorder = true;
        }
      
    }
}
