// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using Kitware.VTK;


namespace vtkControl
{
    public static class SectionViewHatch
    {
        // Variables
        private const int MaxNumberOfLines = 2000;


        // Methods
        public static vtkPolyData GetHatch(vtkDataSet sectionFaces, vtkPlane plane, double spacing, int hatchIndex)
        {
            if (sectionFaces == null || sectionFaces.GetNumberOfCells() == 0 || spacing <= 0) return null;
            // Neighbouring sections alternate the direction of the lines and every second pair also their spacing,
            // as the sections of adjoining parts on a technical drawing
            double angle = (hatchIndex % 2 == 0 ? 45 : 135) * Math.PI / 180;
            spacing *= 1 + 0.5 * ((hatchIndex / 2) % 3);
            //
            double[] firstAxis;
            double[] secondAxis;
            SectionViewCap.GetPlaneAxes(plane, out firstAxis, out secondAxis);
            double[] lineNormal = new double[3];
            for (int i = 0; i < 3; i++) lineNormal[i] = -Math.Sin(angle) * firstAxis[i] + Math.Cos(angle) * secondAxis[i];
            //
            double[] origin = plane.GetOrigin();
            double[] point;
            double distance;
            double minDistance = double.MaxValue;
            double maxDistance = -double.MaxValue;
            for (int i = 0; i < sectionFaces.GetNumberOfPoints(); i++)
            {
                point = sectionFaces.GetPoint(i);
                distance = (point[0] - origin[0]) * lineNormal[0] + (point[1] - origin[1]) * lineNormal[1] +
                           (point[2] - origin[2]) * lineNormal[2];
                if (distance < minDistance) minDistance = distance;
                if (distance > maxDistance) maxDistance = distance;
            }
            // The lines are counted from the plane origin, so the pattern stays in place when the section grows
            int firstLine = (int)Math.Ceiling(minDistance / spacing);
            int lastLine = (int)Math.Floor(maxDistance / spacing);
            int numOfLines = lastLine - firstLine + 1;
            if (numOfLines <= 0 || numOfLines > MaxNumberOfLines) return null;
            //
            vtkPlane linePlane = vtkPlane.New();
            linePlane.SetOrigin(origin[0], origin[1], origin[2]);
            linePlane.SetNormal(lineNormal[0], lineNormal[1], lineNormal[2]);
            //
            vtkCutter cutter = vtkCutter.New();
            cutter.SetCutFunction(linePlane);
            cutter.SetInput(sectionFaces);
            cutter.GenerateCutScalarsOff();
            cutter.SetNumberOfContours(numOfLines);
            for (int i = 0; i < numOfLines; i++) cutter.SetValue(i, (firstLine + i) * spacing);
            cutter.Update();
            //
            vtkPolyData hatch = vtkPolyData.New();
            hatch.SetPoints(cutter.GetOutput().GetPoints());
            hatch.SetLines(cutter.GetOutput().GetLines());
            return hatch;
        }
    }
}
