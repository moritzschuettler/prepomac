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
using System.Drawing;
using Kitware.VTK;
using CaeGlobals;

namespace vtkControl
{
    public enum vtkMaxActorRepresentation
    {
        Solid,
        SolidAsShell,
        Shell,
        Wire,
        Unknown
    }
    public class vtkMaxActorData
    {
        // Variables                                                                                                                
        public string Name;
        public string ParentName;
        public string Caption;
        public bool IsAPart;
        public float NodeSize;
        public float LineWidth;
        public Color Color;
        public Color BackfaceColor; // only for annotateWithColor
        public Color[] ColorTable;
        public double Ambient;
        public double Diffuse;
        public vtkRendererLayer Layer;
        public bool CanHaveElementEdges;
        public bool Pickable;
        public bool BackfaceCulling;
        public bool ColorContours;
        public bool Lighting;
        public bool UseGlobalLighting;
        public bool UseEdgeVisibility;
        public bool SmoothShaded;
        public bool DrawOnGeometry;
        // The selection layer paints what it holds with the highlight color. A symbol that is drawn
        // there only to be occluded by the model keeps the color it was given.
        public bool KeepOwnColor;
        public bool UseSecondaryHighlightColor;
        public vtkMaxActorRepresentation ActorRepresentation;
        public bool SectionViewPossible;
        public bool VolumeGridAvailable;
        public bool HasVolumeCells;
        public int[] PartNodeIds;
        public int[] PartElementIds;
        //
        // A mouse over highlight that wants a point drawn on top of what it highlights puts the point
        // here, since Geometry.Nodes carries the highlighted entity itself. Its own color and size say
        // whether the point is only being pointed out or is what the click would take.
        public double[][] ExtraPointCoor;
        public Color ExtraPointColor;
        public float ExtraPointSize;
        public PartExchangeData Geometry;
        public PartExchangeData ModelEdges;
        public PartExchangeData ElementEdges;


        // Constructors                                                                                                             
        public vtkMaxActorData()
        {
            Name = null;
            ParentName = null;
            Caption = null;
            IsAPart = false;
            NodeSize = 1;
            LineWidth = 1;
            Color = Color.Empty;
            BackfaceColor = Color.Empty;    // only for annotateWithColor
            ColorTable = null;
            Ambient = 0.1;
            Diffuse = 0.1;
            Layer = vtkRendererLayer.Base;
            CanHaveElementEdges = false;
            Pickable = false;
            BackfaceCulling = true;
            ColorContours = false;
            Lighting = true;
            UseGlobalLighting = true;
            UseEdgeVisibility = true;
            SmoothShaded = false;
            DrawOnGeometry = false;
            UseSecondaryHighlightColor = false;
            ActorRepresentation = vtkMaxActorRepresentation.Unknown;
            SectionViewPossible = true;
            VolumeGridAvailable = false;
            HasVolumeCells = false;
            PartNodeIds = null;
            PartElementIds = null;
            //
            Geometry = new PartExchangeData();
            ModelEdges = null;
            ElementEdges = null;
        }

        // Methods                                                                                                                  
        public double[][][] GetStlTriangles()
        {
            vtkMaxActor actor = new vtkMaxActor(this);
            return actor.GetStlTriangles();
        }
    }
}
