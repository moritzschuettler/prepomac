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

namespace CaeMesh
{
    [Serializable]
    public class ElementTypeCount
    {
        // Variables
        private static readonly Type[] _orderedElementTypes = new Type[]
        {
            typeof(LinearBeamElement), typeof(ParabolicBeamElement),
            typeof(LinearTriangleElement), typeof(ParabolicTriangleElement),
            typeof(LinearQuadrilateralElement), typeof(ParabolicQuadrilateralElement),
            typeof(LinearTetraElement), typeof(ParabolicTetraElement),
            typeof(LinearPyramidElement), typeof(ParabolicPyramidElement),
            typeof(LinearWedgeElement), typeof(ParabolicWedgeElement),
            typeof(LinearHexaElement), typeof(ParabolicHexaElement),
            typeof(MassElement), typeof(LinearSpringElement), typeof(LinearGapElement)
        };
        private static readonly string[] _orderedShapeNames = new string[]
        {
            "Linear beam", "Parabolic beam",
            "Linear triangle", "Parabolic triangle",
            "Linear quadrilateral", "Parabolic quadrilateral",
            "Linear tetrahedron", "Parabolic tetrahedron",
            "Linear pyramid", "Parabolic pyramid",
            "Linear wedge", "Parabolic wedge",
            "Linear hexahedron", "Parabolic hexahedron",
            "Mass", "Spring", "Gap"
        };
        private static readonly int _numOfElementTypesWithKeyword = 14;
        //
        private Type _elementType;
        private string _keyword;
        private int _count;


        // Properties
        public Type ElementType { get { return _elementType; } }
        public string ShapeName { get { return GetShapeName(_elementType); } }
        public string Keyword { get { return _keyword; } }
        public int Count { get { return _count; } }


        // Constructors
        public ElementTypeCount(Type elementType, string keyword, int count)
        {
            _elementType = elementType;
            _keyword = keyword;
            _count = count;
        }


        // Methods
        public static string GetShapeName(Type elementType)
        {
            int index = Array.IndexOf(_orderedElementTypes, elementType);
            if (index >= 0) return _orderedShapeNames[index];
            else return elementType.Name;
        }
        private static int GetShapeOrder(Type elementType)
        {
            int index = Array.IndexOf(_orderedElementTypes, elementType);
            if (index >= 0) return index;
            else return _orderedElementTypes.Length;
        }
        public static ElementTypeCount[] CountElementTypes(FeMesh mesh, IEnumerable<BasePart> parts)
        {
            Dictionary<Type, Dictionary<string, int>> typeKeywordCount = new Dictionary<Type, Dictionary<string, int>>();
            Dictionary<string, int> keywordCount;
            FeElement element;
            MeshPart meshPart;
            Type elementType;
            string keyword;
            int count;
            //
            foreach (BasePart part in parts)
            {
                meshPart = part as MeshPart;
                foreach (int elementId in part.Labels)
                {
                    if (!mesh.Elements.TryGetValue(elementId, out element)) continue;
                    //
                    elementType = element.GetType();
                    if (meshPart != null && GetShapeOrder(elementType) < _numOfElementTypesWithKeyword)
                        keyword = meshPart.GetElementType(element);
                    else keyword = "";
                    //
                    if (!typeKeywordCount.TryGetValue(elementType, out keywordCount))
                    {
                        keywordCount = new Dictionary<string, int>();
                        typeKeywordCount.Add(elementType, keywordCount);
                    }
                    keywordCount.TryGetValue(keyword, out count);
                    keywordCount[keyword] = count + 1;
                }
            }
            //
            List<ElementTypeCount> elementTypeCounts = new List<ElementTypeCount>();
            foreach (var typeEntry in typeKeywordCount.OrderBy(entry => GetShapeOrder(entry.Key))
                                                      .ThenBy(entry => entry.Key.Name, StringComparer.Ordinal))
            {
                foreach (var keywordEntry in typeEntry.Value.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                    elementTypeCounts.Add(new ElementTypeCount(typeEntry.Key, keywordEntry.Key, keywordEntry.Value));
            }
            return elementTypeCounts.ToArray();
        }
        public static int GetCount(ElementTypeCount[] elementTypeCounts, Type elementType)
        {
            int count = 0;
            if (elementTypeCounts != null)
            {
                foreach (ElementTypeCount elementTypeCount in elementTypeCounts)
                {
                    if (elementTypeCount.ElementType == elementType) count += elementTypeCount.Count;
                }
            }
            return count;
        }
    }
}
