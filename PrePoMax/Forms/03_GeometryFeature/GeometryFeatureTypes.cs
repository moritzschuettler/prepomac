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
        /// <summary>
        /// An operation that only means something when the region is a surface, because its tool is
        /// drawn on the surface rather than built in space.
        /// </summary>
        public bool ShellOnly;
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
                DisplayName = "Line by Two Points", ItemType = typeof(SplitByLineTwoPoints),
                ShellOnly = true,
                CreateDefaultView = () => new ViewSplitByLineTwoPoints(new SplitByLineTwoPoints()),
                CreateView = (item) => new ViewSplitByLineTwoPoints(((SplitByLineTwoPoints)item).DeepClone())
            },
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


        // Methods
        /// <summary>
        /// The operations one of the two splitting tools offers. They share every operation that builds
        /// its tool in space; an operation that draws its tool on a surface is only in the surface tool.
        /// </summary>
        private static GeometryFeatureType[] GetTypes(SplitTypeEnum splitType)
        {
            if (splitType == SplitTypeEnum.Shell) return _all;
            //
            return _all.Where(entry => !entry.ShellOnly).ToArray();
        }
        public static int Count(SplitTypeEnum splitType)
        {
            return GetTypes(splitType).Length;
        }
        public static string GetDisplayName(SplitTypeEnum splitType, int index)
        {
            return GetTypes(splitType)[index].DisplayName;
        }
        public static ViewGeometryFeature CreateDefaultView(SplitTypeEnum splitType, int index)
        {
            return GetTypes(splitType)[index].CreateDefaultView();
        }
        public static ViewGeometryFeature CreateView(GeometryFeature geometryFeature)
        {
            foreach (GeometryFeatureType itemType in _all)
            {
                if (itemType.ItemType == geometryFeature.GetType()) return itemType.CreateView(geometryFeature);
            }
            throw new NotSupportedException("GeometryFeatureTypeException");
        }
        public static int GetIndex(SplitTypeEnum splitType,
                                   ViewGeometryFeature viewGeometryFeature)
        {
            GeometryFeatureType[] types = GetTypes(splitType);
            Type itemType = viewGeometryFeature.GetBase().GetType();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].ItemType == itemType) return i;
            }
            throw new NotSupportedException("GeometryFeatureTypeException");
        }
    }
}
