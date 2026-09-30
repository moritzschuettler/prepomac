#pragma once

// Managed wrapper of the QuadMesher Q-Morph quad mesher (ThirdParty\QuadMesher).
// It works on the current Gmsh model, like the Gmsh wrappers in GmshCommon.h.
//
// Full-quad surface meshing with boundary rows (C#):
//
//   Gmsh.Option.SetNumber("Mesh.RecombineAll", 0);
//   Gmsh.Model.Mesh.Generate(1);                       // curves, even counts per surface
//   QMorph.SetBackgroundAlgorithms(surfaceIds);        // per surface, before the 2D mesh
//   Gmsh.Model.Mesh.Generate(2);                       // triangles
//   QMorphFaceReport[] report = QMorph.QuadMeshSurfaces(surfaceIds);
//   if (report.Any(r => !r.QMorph))                    // surfaces left to Gmsh
//   {
//       Gmsh.Option.SetNumber("Mesh.RecombinationAlgorithm", 1);   // Blossom
//       Gmsh.Model.Mesh.Recombine();
//   }
//
// The surfaces are meshed in one Generate(2) pass: QuadMeshSurfaces re-meshes
// only regular surfaces that miss the quality gate, and never a periodic one.

#include "qmorph/qmorph_gmsh.h"

namespace GmshCommon {

    public ref class QMorphFaceReport
    {
    public:
        property int Tag;                  // Gmsh surface tag
        property bool QMorph;              // quads written by Q-Morph; false: triangles kept, SetRecombine set
        property System::String^ Reason;   // why the surface was left to Gmsh
        property int Background;           // Gmsh 2D algorithm of the triangulation used
        property int Triangles;            // background triangles
        property int Quads;
        property double P95Condition;      // physical Q1 condition number, 95th percentile
        property double MaxCondition;
        property double MinAngle;          // quad corner angles in degrees
        property double MaxAngle;
        property double Seconds;
        property bool Periodic;
        property bool Singular;            // has a pole or apex
    };

    public ref class QMorph
    {
    public:
        // Even boundary node count on every surface (pairs odd surfaces through shared curves and sets
        // transfinite counts); returns the number of changed curves, after which the caller regenerates
        // the 1D mesh. PrePoMax evens the counts itself, so this is only needed without that.
        static int MakeFaceNodeCountsEven(array<int>^ surfaceTags)
        {
            try { return qmg::makeFaceNodeCountsEven(ToNative(surfaceTags)); }
            catch (const std::exception& e) { throw gcnew System::Exception(gcnew System::String(e.what())); }
        }
        // Frontal-Delaunay for quads (8) on regular surfaces, Frontal-Delaunay (6) on periodic or singular ones
        static void SetBackgroundAlgorithms(array<int>^ surfaceTags)
        {
            try { qmg::setBackgroundAlgorithms(ToNative(surfaceTags)); }
            catch (const std::exception& e) { throw gcnew System::Exception(gcnew System::String(e.what())); }
        }
        // Replaces the triangles of the surfaces by Q-Morph quads (defaults: automatic transitions,
        // clean-up, retries with the Gmsh algorithms 6 and 5)
        static array<QMorphFaceReport^>^ QuadMeshSurfaces(array<int>^ surfaceTags)
        {
            return QuadMeshSurfaces(surfaceTags, true, gcnew array<int>{ 6, 5 });
        }
        // cleanup: topological clean-up and smoothing; alternatives: Gmsh 2D algorithms to retry
        // regular surfaces with that miss the quality gate (empty: no retries)
        static array<QMorphFaceReport^>^ QuadMeshSurfaces(array<int>^ surfaceTags, bool cleanup, array<int>^ alternatives)
        {
            qm::Options options;
            options.cleanup = cleanup;
            std::vector<qmg::FaceReport> reports;
            try { reports = qmg::quadMeshSurfaces(ToNative(surfaceTags), options, ToNative(alternatives)); }
            catch (const std::exception& e) { throw gcnew System::Exception(gcnew System::String(e.what())); }
            array<QMorphFaceReport^>^ result = gcnew array<QMorphFaceReport^>((int)reports.size());
            for (int i = 0; i < (int)reports.size(); i++)
            {
                const qmg::FaceReport& r = reports[i];
                QMorphFaceReport^ m = gcnew QMorphFaceReport();
                m->Tag = r.tag;
                m->QMorph = r.qmorph;
                m->Reason = gcnew System::String(r.reason.c_str());
                m->Background = r.background;
                m->Triangles = r.triangles;
                m->Quads = r.quads;
                m->P95Condition = r.p95;
                m->MaxCondition = r.maxCondition;
                m->MinAngle = r.minAngle;
                m->MaxAngle = r.maxAngle;
                m->Seconds = r.seconds;
                m->Periodic = r.periodic;
                m->Singular = r.singular;
                result[i] = m;
            }
            return result;
        }

    private:
        static std::vector<int> ToNative(array<int>^ values)
        {
            std::vector<int> v;
            if (values == nullptr) return v;
            v.reserve(values->Length);
            for (int i = 0; i < values->Length; i++) v.push_back(values[i]);
            return v;
        }
    };
}
