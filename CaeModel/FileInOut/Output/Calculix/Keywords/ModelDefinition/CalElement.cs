using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalElement : CalculixKeyword
    {
        // Variables                                                                                                                
        private string _elementType;
        private string _elementSetName;
        private List<FeElement> _elements;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalElement(string elementType, string elementSetName, List<FeElement> elements)
            : this(elementType, elementSetName, elements, ConvertPyramidsToEnum.Wedges)
        {
        }
        public CalElement(string elementType, string elementSetName, List<FeElement> elements,
                          ConvertPyramidsToEnum convertPyramidsTo)
        {
            // Linear pyramids
            if (elementType == "C3D5")
            {
                _elementSetName = elementSetName;
                List<FeElement> collapsedElements = new List<FeElement>();
                //
                if (convertPyramidsTo == ConvertPyramidsToEnum.Wedges)
                {
                    _elementType = "C3D6";
                    //
                    foreach (var element in elements)
                    {
                        if (element is LinearPyramidElement lpe) collapsedElements.Add(lpe.ConvertToWedge());
                    }
                }
                else // Hexahedrons
                {
                    _elementType = "C3D8";
                    //
                    foreach (var element in elements)
                    {
                        if (element is LinearPyramidElement lpe) collapsedElements.Add(lpe.ConvertToHex());
                    }
                }
                _elements = collapsedElements;
            }
            // Parabolic pyramids
            else if (elementType == "C3D13")
            {
                _elementSetName = elementSetName;
                List<FeElement> collapsedElements = new List<FeElement>();
                //
                if (convertPyramidsTo == ConvertPyramidsToEnum.Wedges)
                {
                    _elementType = "C3D15";
                    //
                    foreach (var element in elements)
                    {
                        if (element is ParabolicPyramidElement ppe) collapsedElements.Add(ppe.ConvertToWedge());
                    }
                }
                else // Hexahedrons
                {
                    _elementType = "C3D20";
                    //
                    foreach (var element in elements)
                    {
                        if (element is ParabolicPyramidElement ppe) collapsedElements.Add(ppe.ConvertToHex());
                    }
                }
                _elements = collapsedElements;
            }
            else
            {
                _elementType = elementType;
                _elementSetName = elementSetName;
                _elements = elements;
            }
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string elSet = "";
            if (_elementSetName != null && _elementSetName.Length > 0) elSet = ", Elset=" + _elementSetName;
            //
            return string.Format("*Element, Type={0}{1}{2}", _elementType, elSet, Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            int count;
            // Sort
            List<FeElement> sortedElements = _elements.OrderBy(element => element.Id).ToList();
            //
            foreach (FeElement feElement in sortedElements)
            {
                sb.AppendFormat("{0}", feElement.Id);
                count = 1;
                foreach (int nodeId in feElement.NodeIds)
                {
                    count++;
                    if (count == 17)        // 16 entries per line; 17th entry goes in new line
                    {
                        sb.Append(",");
                        sb.AppendLine();
                        sb.Append(nodeId);
                    }
                    else
                        sb.AppendFormat(", {0}", nodeId);

                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
