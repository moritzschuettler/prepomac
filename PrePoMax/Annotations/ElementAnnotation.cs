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
    public class ElementAnnotation : AnnotationBase
    {
        // Variables                                                                                                                
        private int _elementId;


        // Properties                                                                                                               
        public int ElementId { get { return _elementId; } set { _elementId = value; } }


        // Constructors                                                                                                             
        public ElementAnnotation(string name, int elementId)
            : base(name)
        {
            _elementId = elementId;
            _partIds = new int[] { Controller.DisplayedMesh.Elements[_elementId].PartId };
        }


        //
        public override void GetAnnotationData(out string text, out double[] coor)
        {
            // Item name
            string itemName = "Element id:";
            if (Controller.CurrentView == ViewGeometryModelResults.Geometry) itemName = "Facet id:";
            //
            text = string.Format("{0} {1}", itemName, _elementId);
            //
            if (Controller.CurrentView == ViewGeometryModelResults.Model)
            {
                string elementType = Controller.GetElementType(_elementId);
                if (elementType != null)
                {
                    text += string.Format("{0}Element type: {1}", Environment.NewLine, elementType);
                }
            }
            CaeMesh.FeElement element = Controller.GetElement(_elementId);
            if (Controller.CurrentView == ViewGeometryModelResults.Results)
            {
                CaeMesh.FeNode[] scaledNodes =
                    Controller.GetScaledNodes(Controller.GetScale(), element.NodeIds, true);
                Dictionary<int, CaeMesh.FeNode> nodes = new Dictionary<int, CaeMesh.FeNode>();
                foreach (CaeMesh.FeNode node in scaledNodes) nodes.Add(node.Id, node);
                coor = element.GetCG(nodes, out _);
            }
            else coor = element.GetCG(Controller.DisplayedMesh.Nodes, out _);
            //
            if (IsTextOverridden) text = OverriddenText;
        }
    }
}
