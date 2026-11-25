// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace vtkControl
{
    internal static class Globals
    {
        // Variables                                                                                                                
        public static char NameSeparator = ':';
        public static string ScalarArrayName = "scalars";
        public static string QuadraticArrayName = "quadratic";
        public static string SectionViewSuffix = "sectionView";
        public static string SectionViewNodeIdsName = "sectionViewNodeIds";
        public static string SectionViewElementIdsName = "sectionViewElementIds";
        public static string TransformationSuffix = "transformed";
        //
        public static string MinAnnotationName = "Min_Annotation";      // this is a copy from AnnotationContainer
        public static string MaxAnnotationName = "Max_Annotation";      // this is a copy from AnnotationContainer
        //
        public static Color CurrentMouseHighlightColor = Color.FromArgb(255, 175, 0);
    }
}
