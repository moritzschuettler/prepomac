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
        // A mouse over highlight that wants a point marked on top of what it highlights puts the point
        // here, since Geometry.Nodes carries the highlighted entity itself
        public double[][] ExtraPointCoor;
        public float ExtraPointSize;
        public PartExchangeData Geometry;
        public PartExchangeData ModelEdges;
        // A sub part of a compound part draws only the model edges no other sub part draws, while the
        // model edges it owns on its own are the ones it is highlighted by
        public PartExchangeData ModelEdgesToDraw;
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
            ModelEdgesToDraw = null;
            ElementEdges = null;
        }

        // Methods                                                                                                                  
        // The triangles the surface cells are drawn with (AddCellDataToPoly of vtkMaxActor); a cell that is not a
        // surface has no triangles to export
        public double[][][] GetStlTriangles()
        {
            List<double[][]> triangles = new List<double[][]>();
            double[][] coor = Geometry.Nodes.Coor;
            int[][] cells = Geometry.Cells.CellNodeIds ?? new int[0][];
            for (int i = 0; i < cells.Length; i++)
            {
                foreach (int[] triangle in GetTriangles(cells[i], Geometry.Cells.Types[i], coor))
                    triangles.Add(new double[][] { coor[triangle[0]], coor[triangle[1]], coor[triangle[2]] });
            }
            return triangles.ToArray();
        }
        public static int[][] GetTriangles(int[] n, int cellType, double[][] coor)
        {
            switch (cellType)
            {
                case 5:     // VTK_TRIANGLE
                    return new int[][] { new int[] { n[0], n[1], n[2] } };
                case 9:     // VTK_QUAD
                    return new int[][] { new int[] { n[0], n[1], n[2] }, new int[] { n[0], n[2], n[3] } };
                case 22:    // VTK_QUADRATIC_TRIANGLE
                    return new int[][] { new int[] { n[0], n[3], n[5] }, new int[] { n[3], n[4], n[5] },
                                         new int[] { n[3], n[1], n[4] }, new int[] { n[5], n[4], n[2] } };
                case 23:    // VTK_QUADRATIC_QUAD
                    double d1 = Vec3D.GetDirVec(coor[n[4]], coor[n[6]]).Len2;
                    double d2 = Vec3D.GetDirVec(coor[n[5]], coor[n[7]]).Len2;
                    if (d1 < d2)
                        return new int[][] { new int[] { n[7], n[0], n[4] }, new int[] { n[7], n[4], n[6] },
                                             new int[] { n[7], n[6], n[3] }, new int[] { n[5], n[4], n[1] },
                                             new int[] { n[5], n[6], n[4] }, new int[] { n[5], n[2], n[6] } };
                    else
                        return new int[][] { new int[] { n[4], n[7], n[0] }, new int[] { n[4], n[5], n[7] },
                                             new int[] { n[4], n[1], n[5] }, new int[] { n[6], n[5], n[2] },
                                             new int[] { n[6], n[7], n[5] }, new int[] { n[6], n[3], n[7] } };
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
