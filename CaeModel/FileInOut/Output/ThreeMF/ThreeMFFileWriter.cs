// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using Lib3MF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using vtkControl;

namespace CaeResults
{
    public static class ThreeMFFileWriter
    {
        public static void Write(string fileName, vtkMaxActorData[] vtkMaxActorDatas, vtkMaxLookupTable lookupTable,
                                 double radiusRatio, double[] closestUpView)
        {
            string path = Path.GetDirectoryName(fileName);
            //
            CModel model = Lib3MFWrapper.CreateModel();
            sTransform transform = Lib3MFWrapper.GetIdentityTransform();
            if (closestUpView == null) { }
            else if (closestUpView[0] != 0) transform = GetRotationY(closestUpView[0] * 90);
            else if (closestUpView[1] != 0) transform = GetRotationX(closestUpView[1] * -90);
            else if (closestUpView[2] == -1) transform = GetRotationX(180);
            //
            int count = 0;
            double sumLength = 0;
            PartExchangeData partExchangeData;
            CaeMesh.BoundingBox boundingBox = new CaeMesh.BoundingBox();
            for (int i = 0; i < vtkMaxActorDatas.Length; i++)
            {
                partExchangeData = vtkMaxActorDatas[i].Geometry;
                if (partExchangeData != null) boundingBox.IncludeCoors(partExchangeData.Nodes.Coor);
                //
                if (vtkMaxActorDatas[i].ElementEdges != null) partExchangeData = vtkMaxActorDatas[i].ElementEdges;
                else partExchangeData = vtkMaxActorDatas[i].ModelEdges;
                //
                if (partExchangeData != null && partExchangeData.Nodes != null && partExchangeData.Nodes.Coor != null &&
                    partExchangeData.Nodes.Coor.Length > 0 && partExchangeData.Cells != null &&
                    partExchangeData.Cells.CellNodeIds != null && partExchangeData.Cells.CellNodeIds.Length > 0)
                {
                    int nid1;
                    int nid2;
                    double[] coor1;
                    double[] coor2;
                    double length;
                    for (int j = 0; j < partExchangeData.Cells.CellNodeIds.Length; j++)
                    {
                        nid1 = partExchangeData.Cells.CellNodeIds[j][0];
                        nid2 = partExchangeData.Cells.CellNodeIds[j][1];
                        coor1 = partExchangeData.Nodes.Coor[nid1];
                        coor2 = partExchangeData.Nodes.Coor[nid2];
                        length = Math.Sqrt(
                            (coor2[0] - coor1[0]) * (coor2[0] - coor1[0]) +
                            (coor2[1] - coor1[1]) * (coor2[1] - coor1[1]) +
                            (coor2[2] - coor1[2]) * (coor2[2] - coor1[2])
                        );
                        sumLength += length;
                        count++;
                    }
                }
            }
            if (count > 0) sumLength /= count;
            //
            //double tubeRadius = boundingBox.GetDiagonal() * sumLength * 0.001;
            double tubeRadius = sumLength * 0.05 * radiusRatio;
            int tubeDivisions = 5;
            //
            CTexture2DGroup textureGroup = CreateTextureGroup(path, model, lookupTable);
            //
            CMeshObject meshObject;
            vtkMaxLookupTable singleColor = new vtkMaxLookupTable();
            for (int i = 0; i < vtkMaxActorDatas.Length; i++)
            {
                partExchangeData = vtkMaxActorDatas[i].Geometry;
                // Geometry
                if (partExchangeData != null && partExchangeData.Nodes != null && partExchangeData.Nodes.Coor != null &&
                    partExchangeData.Nodes.Coor.Length > 0 && partExchangeData.Cells != null &&
                    partExchangeData.Cells.CellNodeIds != null && partExchangeData.Cells.CellNodeIds.Length > 0)
                {
                    if (vtkMaxActorDatas[i].ColorContours)
                    {
                        meshObject = CreateTexturedMesh(path, model, partExchangeData.Nodes.Coor, partExchangeData.Cells.CellNodeIds,
                                                        partExchangeData.Nodes.Values, lookupTable, textureGroup);
                    }
                    else
                    {
                        singleColor.Colors = new Color[] { vtkMaxActorDatas[i].ColorTable[0] };
                        meshObject = CreateColoredMesh(path, model, partExchangeData.Nodes.Coor, partExchangeData.Cells.CellNodeIds,
                                                       partExchangeData.Nodes.Values, singleColor);
                    }
                    //
                    model.AddBuildItem(meshObject, transform);
                }
                // ModelEdges
                partExchangeData = vtkMaxActorDatas[i].ElementEdges;
                if (partExchangeData != null && partExchangeData.Nodes != null && partExchangeData.Nodes.Coor != null &&
                    partExchangeData.Nodes.Coor.Length > 0 && partExchangeData.Cells != null &&
                    partExchangeData.Cells.CellNodeIds != null && partExchangeData.Cells.CellNodeIds.Length > 0)
                {
                    singleColor.Colors = new Color[] { vtkMaxActorDatas[i].ColorTable[1] };
                    //
                    GenerateTubes(partExchangeData.Nodes.Coor, partExchangeData.Cells.CellNodeIds, tubeRadius, tubeDivisions,
                                  out double[][] tubeCoor, out int[][] tubeTriangles);
                    //
                    meshObject = CreateColoredMesh(path, model, tubeCoor, tubeTriangles, null, singleColor);
                    //
                    model.AddBuildItem(meshObject, transform);
                }
                // ModelEdges
                partExchangeData = vtkMaxActorDatas[i].ModelEdges;
                if (partExchangeData != null && partExchangeData.Nodes != null && partExchangeData.Nodes.Coor != null &&
                    partExchangeData.Nodes.Coor.Length > 0 && partExchangeData.Cells != null &&
                    partExchangeData.Cells.CellNodeIds != null && partExchangeData.Cells.CellNodeIds.Length > 0)
                {
                    singleColor.Colors = new Color[] { vtkMaxActorDatas[i].ColorTable[2] };
                    //
                    GenerateTubes(partExchangeData.Nodes.Coor, partExchangeData.Cells.CellNodeIds, tubeRadius, tubeDivisions,
                                  out double[][] tubeCoor, out int[][] tubeTriangles);
                    //
                    meshObject = CreateColoredMesh(path, model, tubeCoor, tubeTriangles, null, singleColor);
                    //
                    model.AddBuildItem(meshObject, transform);
                }
            }
            //
            CWriter writer = model.QueryWriter("3mf");
            writer.WriteToFile(fileName);
        }
        private static CMeshObject CreateTexturedMesh(string path, CModel model, double[][] coor, int[][] cellNodeIds,
                                                      float[] nodalValues, vtkMaxLookupTable lookupTable, CTexture2DGroup textureGroup)
        {
            CMeshObject mesh = model.AddMeshObject();
            mesh.SetName("TexturedObject");
            // Nodes
            sPosition[] sPositions = new sPosition[coor.Length];
            for (int i = 0; i < coor.Length; i++)
            {
                sPositions[i].Coordinates = new float[] { (float)coor[i][0], (float)coor[i][1], (float)coor[i][2] };
            }
            // Triangles
            int[] nodeIds;
            List<sTriangle> triangles = new List<sTriangle>();
            for (int i = 0; i < cellNodeIds.Length; i++)
            {
                nodeIds = cellNodeIds[i];
                if (nodeIds.Length == 3)
                {
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[0], (uint)nodeIds[1], (uint)nodeIds[2] } });
                }
                else if (nodeIds.Length == 8)
                {
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[7], (uint)nodeIds[0], (uint)nodeIds[4] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[7], (uint)nodeIds[4], (uint)nodeIds[6] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[7], (uint)nodeIds[6], (uint)nodeIds[3] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[5], (uint)nodeIds[4], (uint)nodeIds[1] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[5], (uint)nodeIds[6], (uint)nodeIds[4] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[5], (uint)nodeIds[2], (uint)nodeIds[6] } });
                }
                else Debugger.Break();
            }
            mesh.SetGeometry(sPositions, triangles.ToArray());
            // Triangle properties
            uint count = 0;
            float value;
            sTex2Coord texCoor;
            texCoor.U = 0.5;
            sTriangleProperties sTriangleProperty;
            sTriangleProperty.ResourceID = textureGroup.GetResourceID();
            sTriangleProperty.PropertyIDs = new uint[3];
            foreach (sTriangle triangle in triangles)
            {
                value = nodalValues[triangle.Indices[0]];
                texCoor.V = CapNormalizedValue(lookupTable.GetNormalizedValue(value));
                sTriangleProperty.PropertyIDs[0] = textureGroup.AddTex2Coord(texCoor);
                //
                value = nodalValues[triangle.Indices[1]];
                texCoor.V = CapNormalizedValue(lookupTable.GetNormalizedValue(value));
                sTriangleProperty.PropertyIDs[1] = textureGroup.AddTex2Coord(texCoor);
                //
                value = nodalValues[triangle.Indices[2]];
                texCoor.V = CapNormalizedValue(lookupTable.GetNormalizedValue(value));
                sTriangleProperty.PropertyIDs[2] = textureGroup.AddTex2Coord(texCoor);
                //
                mesh.SetTriangleProperties(count++, sTriangleProperty);
            }
            //
            mesh.SetObjectLevelProperty(textureGroup.GetResourceID(), 1);
            //
            return mesh;
        }
        private static double CapNormalizedValue(double value)
        {
            value = Math.Max(value, 1E-3);
            value = Math.Min(value, 1 - 1E-3);
            return value;
        }
        private static CTexture2DGroup CreateTextureGroup(string path, CModel model, vtkMaxLookupTable lookupTable)
        {
            // Textures
            int textureHeight = lookupTable.Colors.Length * 200;
            string textureFileName = Path.Combine(path, "texture.png");
            lookupTable.WriteAsTexture(1, textureHeight, textureFileName);
            string internalTexturePath = "/3D/Textures/texture.png";
            string sRelationshipType_Texture = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dtexture";
            CAttachment attachment = model.AddAttachment(internalTexturePath, sRelationshipType_Texture);
            attachment.ReadFromFile(textureFileName);
            CTexture2D texture2D = model.AddTexture2DFromAttachment(attachment);
            texture2D.SetContentType(eTextureType.PNG);
            texture2D.SetTileStyleUV(eTextureTileStyle.NoTileStyle, eTextureTileStyle.NoTileStyle);
            CTexture2DGroup textureGroup = model.AddTexture2DGroup(texture2D);
            return textureGroup;
        }
        private static CMeshObject CreateColoredMesh(string path, CModel model, double[][] coor, int[][] cellNodeIds,
                                                     float[] nodalValues, vtkMaxLookupTable lookupTable)
        {
            CMeshObject mesh = model.AddMeshObject();
            mesh.SetName("ColoredObject");
            // Nodes
            sPosition[] sPositions = new sPosition[coor.Length];
            for (int i = 0; i < coor.Length; i++)
            {
                sPositions[i].Coordinates = new float[] { (float)coor[i][0], (float)coor[i][1], (float)coor[i][2] };
            }
            // Triangles
            int[] nodeIds;
            List<sTriangle> triangles = new List<sTriangle>();
            for (int i = 0; i < cellNodeIds.Length; i++)
            {
                nodeIds = cellNodeIds[i];
                if (nodeIds.Length == 3)
                {
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[0], (uint)nodeIds[1], (uint)nodeIds[2] } });
                }
                else if (nodeIds.Length == 8)
                {
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[7], (uint)nodeIds[0], (uint)nodeIds[4] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[7], (uint)nodeIds[4], (uint)nodeIds[6] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[7], (uint)nodeIds[6], (uint)nodeIds[3] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[5], (uint)nodeIds[4], (uint)nodeIds[1] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[5], (uint)nodeIds[6], (uint)nodeIds[4] } });
                    triangles.Add(new sTriangle() { Indices = new UInt32[] { (uint)nodeIds[5], (uint)nodeIds[2], (uint)nodeIds[6] } });
                }
                else Debugger.Break();
            }
            mesh.SetGeometry(sPositions, triangles.ToArray());
            // Colors
            uint id;
            Color[] colors = lookupTable.Colors;
            CColorGroup colorGroup = model.AddColorGroup();
            for (int i = 0; i < colors.Length; i++)
            {
                id = colorGroup.AddColor(Lib3MFWrapper.RGBAToColor(colors[i].R, colors[i].G, colors[i].B, 255));
            }
            // Triangle properties
            uint count = 0;
            sTriangleProperties sTriangleProperty;
            sTriangleProperty.ResourceID = colorGroup.GetResourceID();
            sTriangleProperty.PropertyIDs = new uint[3];
            //
            double value1;
            double value2;
            double value3;
            foreach (sTriangle triangle in triangles)
            {
                if (nodalValues != null && nodalValues.Length == sPositions.Length)
                {
                    value1 = nodalValues[triangle.Indices[0]];
                    value2 = nodalValues[triangle.Indices[1]];
                    value3 = nodalValues[triangle.Indices[2]];
                }
                else
                {
                    value1 = 0;
                    value2 = 0;
                    value3 = 0;
                }
                sTriangleProperty.PropertyIDs[0] = 1 + (uint)lookupTable.GetColorIndex(value1);
                sTriangleProperty.PropertyIDs[1] = 1 + (uint)lookupTable.GetColorIndex(value2);
                sTriangleProperty.PropertyIDs[2] = 1 + (uint)lookupTable.GetColorIndex(value3);
                // Gradient-colored Triangles
                mesh.SetTriangleProperties(count++, sTriangleProperty);
            }
            //
            mesh.SetObjectLevelProperty(sTriangleProperty.ResourceID, sTriangleProperty.PropertyIDs[0]);
            //
            return mesh;
        }
        private static void GenerateTubes(double[][] coor, int[][] edges, double radius, int divisions, 
                                          out double[][] coorOut, out int[][] triangles)
        {
            List<double[]> coorList = new List<double[]>();
            List<int[]> cells = new List<int[]>();
            //
            double angleStep = 2.0 * Math.PI / divisions;
            Vector3 globalRef = new Vector3(0, 0, 1);      // fixed reference direction
            //
            foreach (var e in edges)
            {
                int n1 = e[0];
                int n2 = e[1];
                //
                Vector3 p1 = new Vector3((float)coor[n1][0], (float)coor[n1][1], (float)coor[n1][2]);
                Vector3 p2 = new Vector3((float)coor[n2][0], (float)coor[n2][1], (float)coor[n2][2]);
                // Cylinder axis
                Vector3 axis = Vector3.Normalize(p2 - p1);
                // Build 2 perpendicular vectors (for local circle frame)
                // Compute stable perpendicular vector e1
                // project globalRef onto plane perpendicular to d:
                Vector3 v1 = globalRef - Vector3.Dot(globalRef, axis) * axis;
                if (v1.Length() < 1e-8f)
                {
                    // Fallback if the beam is parallel to globalRef
                    Vector3 alt = new Vector3(1, 0, 0);
                    v1 = alt - Vector3.Dot(alt, axis) * axis;
                }
                v1 = Vector3.Normalize(v1);
                // Compute second perpendicular direction e2
                Vector3 v2 = Vector3.Cross(axis, v1);
                //
                int startIndex = coorList.Count;
                // Generate circle vertices at p1 and p2
                for (int i = 0; i < divisions; i++)
                {
                    double a = i * angleStep;
                    float ca = (float)Math.Cos(a);
                    float sa = (float)Math.Sin(a);
                    //
                    Vector3 offset = (float)(radius * ca) * v1 + (float)(radius * sa) * v2;
                    //
                    coorList.Add(Coor(p1 + offset));  // bottom ring
                    coorList.Add(Coor(p2 + offset));  // top ring
                }
                // Generate side faces
                for (int i = 0; i < divisions; i++)
                {
                    int i0 = startIndex + 2 * i;
                    int i1 = startIndex + 2 * ((i + 1) % divisions);
                    //
                    int i0_up = i0 + 1;
                    int i1_up = i1 + 1;
                    // Two triangles per segment
                    cells.Add(new int[] { i0, i1, i0_up });
                    cells.Add(new int[] { i1, i1_up, i0_up });
                }
            }
            triangles = cells.ToArray();
            coorOut = coorList.ToArray();
        }
        private static double[] Coor(Vector3 v)
        {
            return new double[] { v.X, v.Y, v.Z };
        }
        private static sTransform GetRotationX(double angleDeg)
        {
            float angle = (float)(angleDeg * Math.PI / 180);
            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);
            //
            sTransform transform = new sTransform();
            transform.Fields = new float[][]
            {
                new float[] { 1, 0, 0 },
                new float[] { 0, c, -s },
                new float[] { 0, s, c },
                new float[] { 0, 0, 0 }
            };
            return transform;
        }
        private static sTransform GetRotationY(double angleDeg)
        {
            float angle = (float)(angleDeg * Math.PI / 180);
            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);
            //
            sTransform transform = new sTransform();
            transform.Fields = new float[][]
            {
                new float[] { c, 0,  s },
                new float[] { 0, 1,  0 },
                new float[] { -s, 0,  c },
                new float[] { 0, 0, 0 }
            };
            return transform;
        }
    }
}
