// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    public class TextAnnotation : AnnotationBase
    {
        // Variables                                                                                                                
        private string _text;
        protected double[] _anchorPoint;


        // Properties                                                                                                               
        public string Text { get { return _text; } set { _text = value; } }
        public double[] AnchorPoint { get { return _anchorPoint; } set { _anchorPoint = value; } }


        // Constructors                                                                                                             
        public TextAnnotation(string name, string text, double[] anchorPoint)
            : base(name)
        {
            _text = text;
            _anchorPoint = anchorPoint;
        }


        // Methods
        public override void GetAnnotationData(out string text, out double[] coor)
        {
            text = _text.ToString();
            coor = _anchorPoint.ToArray();
            //
            //if (IsTextOverriden) text = OverridenText;
        }
    }
}
