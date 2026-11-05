// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// A part of the model as vtkMaxActor draws it: the surface cells PrePoMax extracted, triangulated with smooth point
// normals, the element edges and the model edges as their own actors, the part colors and the visibility rules of
// the edges mode. Port of the part path of vtkMaxActor (CreatePolyFromData, AddCellDataToPoly, UpdateColor) and of
// the ApplyEdgeVisibilityAndBackfaceCulling* methods of vtkControl.
#pragma once

#include "vtkNative.h"

#include <vtkActor.h>
#include <vtkCellLocator.h>
#include <vtkPolyData.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

namespace vtkn
{
    class PartActor
    {
    public:
        PartActor(const vtkn_part_style& style, const vtkn_mesh& geometry, const vtkn_mesh* modelEdges,
                  const vtkn_mesh* modelEdgesToDraw);

        void AddTo(vtkRenderer* renderer);
        void RemoveFrom(vtkRenderer* renderer);
        void SetGlobalLighting(double ambient, double diffuse);
        void ApplyEdgesVisibility(vtkn_edges_visibility edgesVisibility);

        vtkActor* GetGeometry() { return Geometry; }
        bool Owns(vtkProp* prop) const;
        // The locator of the surface cells, whose global ids are the element ids; null for a part that cannot be
        // picked, as vtkMaxActor.CellLocator
        vtkCellLocator* GetCellLocator() { return CellLocator; }
        vtkActor* GetModelEdges() { return ModelEdges; }
        // GetPickPoint picks every part opaque; UpdateColor puts the opacity back
        void SetPickOpacity();
        void UpdateColor();

    private:
        vtkn_part_style Style;
        vtkSmartPointer<vtkActor> Geometry;
        vtkSmartPointer<vtkActor> ElementEdges;
        vtkSmartPointer<vtkActor> ModelEdges;
        vtkSmartPointer<vtkCellLocator> CellLocator;
    };

    // The legacy cell layout of a vtkn_mesh checked whole: every cell's node count and point indices
    void CheckMesh(const vtkn_mesh& mesh, const char* meshName);
    // Port of AddNodeAndCellDataToPoly: triangles for the surface cells, lines for the edges, vertices, and when
    // asked the unique element edges
    void BuildPolyData(const vtkn_mesh& mesh, bool extractEdges, vtkSmartPointer<vtkPolyData>& polyActor,
                       vtkSmartPointer<vtkPolyData>& polyEdges);
}
