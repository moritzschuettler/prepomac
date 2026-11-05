// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// An actor as vtkMaxActor draws it, in any layer: the surface cells PrePoMax extracted, triangulated with smooth
// point normals, the element edges and the model edges as their own actors, the colors and the visibility rules of
// the edges mode; or the nodes of a geometry as vertices; or the output of a symbol pipeline. Port of vtkMaxActor
// (CreatePolyFromData, CreateNodalActor, AddCellDataToPoly, UpdateColor) and of the ApplyEdgeVisibility* and
// ApplySelectionFormattingToActor methods of vtkControl.
#pragma once

#include "vtkNative.h"

#include <vtkActor.h>
#include <vtkAlgorithmOutput.h>
#include <vtkCellLocator.h>
#include <vtkFloatArray.h>
#include <vtkLookupTable.h>
#include <vtkPolyData.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

#include <array>
#include <string>
#include <vector>

namespace vtkn
{
    // The colors vtkControl formats the selection layer with
    struct HighlightColors
    {
        double Primary[3] = { 1, 0, 0 };
        double Secondary[3] = { 1, 0, 0 };
    };

    class PartActor
    {
    public:
        PartActor(const vtkn_part_style& style, const vtkn_mesh& geometry, const vtkn_mesh* modelEdges,
                  const vtkn_mesh* modelEdgesToDraw);
        // An actor drawing a poly data or the output of a pipeline, as vtkMaxActor(data, mapper)
        PartActor(const vtkn_part_style& style, vtkPolyData* polyData);
        PartActor(const vtkn_part_style& style, vtkAlgorithmOutput* output);

        void AddTo(vtkRenderer* renderer);
        void RemoveFrom(vtkRenderer* renderer);
        void SetGlobalLighting(double ambient, double diffuse);
        void ApplyEdgesVisibility(vtkn_edges_visibility edgesVisibility);
        // ApplyEdgeVisibilityAndBackfaceCullingToActor and ...ToActorEdges for the given layer
        void ApplyEdgesVisibility(vtkn_edges_visibility edgesVisibility, vtkn_layer layer, bool sectionViewActive);
        void ApplySelectionFormatting(const HighlightColors& colors);

        vtkActor* GetGeometry() { return Geometry; }
        vtkActor* GetElementEdges() { return ElementEdges; }
        vtkActor* GetModelEdges() { return ModelEdges; }
        // The input of the geometry mapper
        vtkPolyData* GetPolyData();
        // The model edges a part is highlighted by: the ones it owns, also when it draws fewer
        vtkPolyData* GetModelEdgesForHighlight();
        bool Owns(vtkProp* prop) const;
        // The locator of the surface cells, whose global ids are the element ids; null for a part that cannot be
        // picked, as vtkMaxActor.CellLocator
        vtkCellLocator* GetCellLocator() { return CellLocator; }
        // GetPickPoint picks every part opaque; UpdateColor puts the opacity back
        void SetPickOpacity();
        void UpdateColor();

        const vtkn_part_style& GetStyle() const { return Style; }
        vtkn_layer GetLayer() const { return static_cast<vtkn_layer>(Style.layer); }
        bool IsAPart() const { return Style.is_a_part != 0; }
        bool IsVisible() const { return Style.visible != 0; }
        // vtkMaxActor.VtkMaxActorVisible
        void SetVisible(bool visible);
        const std::string& GetParentName() const { return ParentName; }
        void SetParentName(const std::string& parentName) { ParentName = parentName; }
        // Results (G3): the point values and extreme nodes, the cell values with their color table, the coloring by
        // the lookup table of the results
        void SetPointScalars(const float* values, int count, const vtkn_extreme_nodes* extremes);
        bool HasExtremes() const { return HasExtremeNodes; }
        const vtkn_extreme_nodes& GetExtremes() const { return Extremes; }
        bool HasPointScalars() const { return PointScalars != nullptr; }
        void SetCellColors(const float* values, int count, const double* rgb, int rgbCount);
        bool GetColorContours() const { return Style.color_contours != 0; }
        void SetColorContours(bool colorContours) { Style.color_contours = colorContours ? 1 : 0; }
        // The part of UpdateScalarFormatting for one actor: the scalars are shown with color contours, else hidden
        void ApplyScalarVisibility();
        void SetResultLookupTable(vtkLookupTable* lookupTable, const double range[2]);
        bool GetGeometryHiddenBySectionView() const { return GeometryHiddenBySectionView; }
        void SetGeometryHiddenBySectionView(bool hidden) { GeometryHiddenBySectionView = hidden; }

    private:
        void CreateFromMesh(const vtkn_mesh& geometry, const vtkn_mesh* modelEdges, const vtkn_mesh* modelEdgesToDraw);
        void CreateNodal(const vtkn_mesh& geometry);
        void CreateFromMapper(vtkMapper* mapper);
        void ApplyStyleSizes();

        vtkn_part_style Style;
        std::string ParentName;
        std::vector<int> SourceCells;
        vtkSmartPointer<vtkFloatArray> PointScalars;
        vtkSmartPointer<vtkFloatArray> CellScalars;
        std::vector<std::array<double, 3>> ColorTable;
        vtkn_extreme_nodes Extremes = {};
        bool HasExtremeNodes = false;
        bool GeometryHiddenBySectionView = false;
        vtkSmartPointer<vtkActor> Geometry;
        vtkSmartPointer<vtkActor> ElementEdges;
        vtkSmartPointer<vtkActor> ModelEdges;
        vtkSmartPointer<vtkPolyData> ModelEdgesForHighlight;
        vtkSmartPointer<vtkCellLocator> CellLocator;
    };

    // The legacy cell layout of a vtkn_mesh checked whole: every cell's node count and point indices
    void CheckMesh(const vtkn_mesh& mesh, const char* meshName);
    // Port of AddNodeAndCellDataToPoly: triangles for the surface cells, lines for the edges, vertices, and when
    // asked the unique element edges
    void BuildPolyData(const vtkn_mesh& mesh, bool extractEdges, vtkSmartPointer<vtkPolyData>& polyActor,
                       vtkSmartPointer<vtkPolyData>& polyEdges, std::vector<int>* sourceCells = nullptr);
    // The style vtkMaxActor has before any data sets it, as its default constructor leaves it
    vtkn_part_style DefaultStyle();
}
