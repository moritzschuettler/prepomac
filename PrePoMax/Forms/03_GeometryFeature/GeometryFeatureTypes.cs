// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.Forms
{
    internal class GeometryFeatureType
    {
        public string DisplayName;
        public Type ItemType;
        public Func<ViewGeometryFeature> CreateDefaultView;
        public Func<GeometryFeature, ViewGeometryFeature> CreateView;
    }

    internal static class GeometryFeatureTypes
    {
        // Variables
        private static readonly GeometryFeatureType[] _all = new GeometryFeatureType[]
        {
            new GeometryFeatureType()
            {
                DisplayName = "Plane by Three Points", ItemType = typeof(SplitByPlaneThreePoints),
                CreateDefaultView = () => new ViewSplitByPlaneThreePoints(new SplitByPlaneThreePoints()),
                CreateView = (item) => new ViewSplitByPlaneThreePoints(((SplitByPlaneThreePoints)item).DeepClone())
            },
            new GeometryFeatureType()
            {
                DisplayName = "Plane by Point and Normal", ItemType = typeof(SplitByPointAndNormal),
                CreateDefaultView = () => new ViewSplitByPointAndNormal(new SplitByPointAndNormal()),
                CreateView = (item) => new ViewSplitByPointAndNormal(((SplitByPointAndNormal)item).DeepClone())
            },
            new GeometryFeatureType()
            {
                DisplayName = "Extend Face", ItemType = typeof(SplitByExtendedFace),
                CreateDefaultView = () => new ViewSplitByExtendedFace(new SplitByExtendedFace()),
                CreateView = (item) => new ViewSplitByExtendedFace(((SplitByExtendedFace)item).DeepClone())
            },
            new GeometryFeatureType()
            {
                DisplayName = "Extrude Edge", ItemType = typeof(SplitByExtrudedEdges),
                CreateDefaultView = () => new ViewSplitByExtrudedEdges(new SplitByExtrudedEdges()),
                CreateView = (item) => new ViewSplitByExtrudedEdges(((SplitByExtrudedEdges)item).DeepClone())
            }
        };


        // Properties
        public static int Count { get { return _all.Length; } }


        // Methods
        public static string GetDisplayName(int index)
        {
            return _all[index].DisplayName;
        }
        public static ViewGeometryFeature CreateDefaultView(int index)
        {
            return _all[index].CreateDefaultView();
        }
        public static ViewGeometryFeature CreateView(GeometryFeature geometryFeature)
        {
            foreach (GeometryFeatureType itemType in _all)
            {
                if (itemType.ItemType == geometryFeature.GetType()) return itemType.CreateView(geometryFeature);
            }
            throw new NotSupportedException("GeometryFeatureTypeException");
        }
        public static int GetIndex(ViewGeometryFeature viewGeometryFeature)
        {
            Type itemType = viewGeometryFeature.GetBase().GetType();
            for (int i = 0; i < _all.Length; i++)
            {
                if (_all[i].ItemType == itemType) return i;
            }
            throw new NotSupportedException("GeometryFeatureTypeException");
        }
    }
}
