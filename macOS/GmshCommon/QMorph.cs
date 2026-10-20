// PrePoMax for macOS: the API of the Q-Morph wrapper (GmshCommon/QMorph.h). The QuadMesher library is built only for
// Windows, so every surface is left to gmsh: its triangles are marked for the gmsh recombination.
using System;

namespace GmshCommon
{
    public class QMorphFaceReport
    {
        public int Tag { get; set; }
        public bool QMorph { get; set; }
        public string Reason { get; set; }
        public int Background { get; set; }
        public int Triangles { get; set; }
        public int Quads { get; set; }
        public double P95Condition { get; set; }
        public double MaxCondition { get; set; }
        public double MinAngle { get; set; }
        public double MaxAngle { get; set; }
        public double Seconds { get; set; }
        public bool Periodic { get; set; }
        public bool Singular { get; set; }
    }

    public static class QMorph
    {
        public static int MakeFaceNodeCountsEven(int[] surfaceTags)
        {
            return 0;
        }
        public static void SetBackgroundAlgorithms(int[] surfaceTags)
        {
            if (surfaceTags == null) return;
            foreach (int tag in surfaceTags) Gmsh.Model.Mesh.SetAlgorithm(2, tag, 8);
        }
        public static QMorphFaceReport[] QuadMeshSurfaces(int[] surfaceTags)
        {
            return QuadMeshSurfaces(surfaceTags, true, new int[] { 6, 5 });
        }
        public static QMorphFaceReport[] QuadMeshSurfaces(int[] surfaceTags, bool cleanup, int[] alternatives)
        {
            if (surfaceTags == null) return new QMorphFaceReport[0];
            QMorphFaceReport[] reports = new QMorphFaceReport[surfaceTags.Length];
            for (int i = 0; i < surfaceTags.Length; i++)
            {
                Gmsh.Model.Mesh.SetRecombine(2, surfaceTags[i]);
                reports[i] = new QMorphFaceReport() { Tag = surfaceTags[i], QMorph = false, Background = 8,
                                                      Reason = "Q-Morph is not available on macOS" };
            }
            return reports;
        }
    }
}
