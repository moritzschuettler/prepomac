using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    public class PartAnnotation : AnnotationBase
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public PartAnnotation(string name, string partName)
            : base(name)
        {
            _partIds = new int[] { Controller.DisplayedMesh.Parts[partName].PartId };
        }


        // Methods
        public override void GetAnnotationData(out string text, out double[] coor)
        {
            FeMesh mesh = Controller.DisplayedMesh;
            BasePart part = mesh.GetPartFromId(_partIds[0]);
            if (part == null) throw new NotSupportedException();
            //
            double[][] nodeCoor = new double[part.NodeLabels.Length][];
            if (Controller.CurrentView == ViewGeometryModelResults.Geometry ||
                Controller.CurrentView == ViewGeometryModelResults.Model)
            {
                for (int i = 0; i < part.NodeLabels.Length; i++) nodeCoor[i] = mesh.Nodes[part.NodeLabels[i]].Coor;
            }
            else if (Controller.CurrentView == ViewGeometryModelResults.Results)
            {
                FeNode[] nodes = Controller.GetScaledNodes(Controller.GetScale(), part.NodeLabels);
                for (int i = 0; i < nodes.Length; i++) nodeCoor[i] = nodes[i].Coor;
            }
            else throw new NotSupportedException();
            // Coor
            int[] distributedNodeIds = Controller.GetSpatiallyEquallyDistributedCoor(nodeCoor, 1, null);
            coor = nodeCoor[distributedNodeIds[0]];
            //
            bool showPartName = Controller.Settings.Annotations.ShowPartName;
            bool showPartId = Controller.Settings.Annotations.ShowPartId;
            bool showPartType = Controller.Settings.Annotations.ShowPartType;
            bool showPartNumberOfElements = Controller.Settings.Annotations.ShowPartNumberOfElements;
            bool showPartNumberOfNodes = Controller.Settings.Annotations.ShowPartNumberOfNodes;
            bool showPartVolumeArea = Controller.Settings.Annotations.ShowPartVolumeArea;
            if (!showPartId && !showPartType && !showPartNumberOfElements && !showPartNumberOfNodes && !showPartVolumeArea)
                showPartName = true;
            // Item name
            string elementsName = "Number of elements:";
            string nodesName = "Number of nodes:";
            if (Controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                elementsName = "Number of facets:";
                nodesName = "Number of vertices:";
            }
            // Volume/Area
            string areaUnit = Controller.GetAreaUnit();
            string volumeUnit = Controller.GetVolumeUnit();
            string numberFormat = Controller.Settings.Annotations.GetNumberFormat();
            bool volume = part.PartType == PartType.Solid || part.PartType == PartType.SolidAsShell ||
                          part.PartType == PartType.Compound;
            string volumeArea;
            string title;
            if (part is ResultPart)
            {
                if (volume) title = "Undeformed volume";
                else title = "Undeformed area";
            }
            else
            {
                if (volume) title = "Volume";
                else title = "Area";
            }
            volumeArea = string.Format(title + ": {0} {1}", part.MassProperties.Volume.ToString(numberFormat), volumeUnit);
            //
            text = "";
            //
            if (showPartName)
            {
                text += string.Format("Part name: {0}", part.Name);
            }
            if (showPartId)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Part id: {0}", part.PartId);
            }
            if (showPartType)
            {
                string partTypeName;
                if (part.PartType == PartType.SolidAsShell) partTypeName = "Solid as shell";
                else partTypeName = part.PartType.ToString();
                //
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Part type: {0}", partTypeName);
            }
            if (showPartNumberOfElements)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("{0} {1}", elementsName, part.Labels.Length);
            }
            if (showPartNumberOfNodes)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("{0} {1}", nodesName, part.NodeLabels.Length);
            }
            if (showPartVolumeArea)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += volumeArea;
            }
            //
            if (IsTextOverridden) text = OverriddenText;
        }
    }
}
