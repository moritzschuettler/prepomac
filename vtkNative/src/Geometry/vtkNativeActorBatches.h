// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// One mapper for the actors that look the same. vtkControl draws every surface of a constraint, an interaction or a
// load as an actor of its own, and VTK sets up a shader, its uniforms and a draw call for each one: the 2688 surfaces
// of 1344 ties took four fifths of a frame. The actors of a view that are allowed to and that have the same property,
// shader replacements, clipping planes and offsets are appended into one mesh, drawn by one actor in the place of the
// first of them, and are themselves left out of the render by a culler. They stay in the renderer as they are, so
// that everything a view does with an actor - showing, coloring, clipping, moving, measuring its bounds - goes on
// working on it; the batches are built again when one of them changes.
#pragma once

#include "vtkNative.h"

#include <vtkActor.h>
#include <vtkCuller.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

#include <cstdint>
#include <string>
#include <utility>
#include <vector>

namespace vtkn
{
    class ActorBatches
    {
    public:
        ActorBatches();
        ~ActorBatches();

        void SetRenderer(vtkRenderer* renderer);
        // An actor is drawn in a batch only when it was allowed to: one that nothing picks or animates by itself
        static void Allow(vtkActor* actor);
        // Looking through the allowed actors of a large model takes milliseconds, so a view says when something may
        // have changed them: after a call that changes its geometry, not after a move of its camera
        void Invalidate() { Invalidated = true; }
        // Before a render of the renderer: when invalidated, builds the batches again if an allowed actor, its
        // property, its mapper or its mesh changed since they were built
        void Update();
        // The actor of every batch with the first of its members; the actors of the batches are not props of the
        // renderer
        void GetBatches(std::vector<std::pair<vtkProp*, vtkProp*>>& batches) const;
        // Off draws every allowed actor through a mapper of its own, for comparing
        void SetEnabled(bool enabled);
        // The batches of more than one actor and the actors drawn by them, as of the last update
        void GetCounts(int& batchCount, int& memberCount) const;

    private:
        struct Allowed
        {
            vtkActor* Actor;
            std::uint64_t Stamp;
        };
        struct Batch
        {
            vtkSmartPointer<vtkActor> Actor;
            std::vector<vtkProp*> Members;
            std::string Look;
            std::uint64_t Stamp = 0;
        };

        std::uint64_t GetAllowed(std::vector<Allowed>& allowed) const;
        void Build(const std::vector<Allowed>& allowed);
        Batch CreateBatch(const std::vector<vtkActor*>& actors) const;

        vtkRenderer* Renderer = nullptr;
        vtkSmartPointer<vtkCuller> Culler;
        std::vector<Batch> Batches;
        std::uint64_t Stamp = 0;
        bool Invalidated = true;
        bool Enabled = true;
    };
}
