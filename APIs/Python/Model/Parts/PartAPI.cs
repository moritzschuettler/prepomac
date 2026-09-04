// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    // The read-only members shared by a geometry part and a model part. A part is changed through its
    // collection, because that is what the command history records.
    public abstract class PartAPI : BaseAPI
    {
        // Properties
        [PythonAPI("The part name.", Returns = "str")]
        public string name { get { return ResolvePart().Name; } }
        [PythonAPI("The part ID.", Returns = "int")]
        public int id { get { return ResolvePart().PartId; } }
        [PythonAPI("The part type: Solid, SolidAsShell, Shell, Wire, Compound or Unknown.", Returns = "str")]
        public string part_type { get { return ResolvePart().PartType.ToString(); } }
        [PythonAPI("The number of nodes in the part.", Returns = "int")]
        public int node_count
        {
            get
            {
                int[] nodeLabels = ResolvePart().NodeLabels;
                return nodeLabels == null ? 0 : nodeLabels.Length;
            }
        }
        [PythonAPI("The number of elements in the part.", Returns = "int")]
        public int element_count
        {
            get
            {
                int[] labels = ResolvePart().Labels;
                return labels == null ? 0 : labels.Length;
            }
        }
        [PythonAPI("The part color as [R, G, B, A] channel values.", Returns = "IntList")]
        public IntListAPI color { get { return PythonColorConverter.ToChannels(ResolvePart().Color); } }
        [PythonAPI("True when the part is visible.", Returns = "bool")]
        public bool visible { get { return ResolvePart().Visible; } }
        [PythonAPI("True when the part is active.", Returns = "bool")]
        public bool active { get { return ResolvePart().Active; } }


        // Methods
        [PythonAPI("Returns the part bounding box as [xMin, xMax, yMin, yMax, zMin, zMax].",
                   Returns = "NumericArray")]
        public NumericArrayAPI get_bounding_box()
        {
            BoundingBox boundingBox = ResolvePart().BoundingBox;
            if (boundingBox == null) return new NumericArrayAPI(new double[0]);
            //
            return new NumericArrayAPI(new double[] { boundingBox.MinX, boundingBox.MaxX, boundingBox.MinY,
                                                      boundingBox.MaxY, boundingBox.MinZ, boundingBox.MaxZ });
        }

        [PythonAPI("Returns the node IDs of the part.", Returns = "IntList")]
        public IntListAPI get_node_ids()
        {
            int[] nodeLabels = ResolvePart().NodeLabels;
            return new IntListAPI(nodeLabels == null ? new int[0] : nodeLabels);
        }

        [PythonAPI("Returns the element IDs of the part.", Returns = "IntList")]
        public IntListAPI get_element_ids()
        {
            int[] labels = ResolvePart().Labels;
            return new IntListAPI(labels == null ? new int[0] : labels);
        }
        //
        protected abstract BasePart ResolvePart();
        //
        public override string ToString()
        {
            BasePart part = ResolvePart();
            return GetType().Name.Replace("API", "") + "(name=" + StringListAPI.ToDisplayString(part.Name) +
                   ", type=" + part.PartType + ", nodes=" + node_count + ", elements=" + element_count + ")";
        }
    }
}
