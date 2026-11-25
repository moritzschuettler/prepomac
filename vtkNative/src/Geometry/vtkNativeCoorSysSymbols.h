// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxCoorSysSymbols and vtkMaxCoorSysSymbol: the coordinate systems of the model drawn as 2D triads at
// the display positions of their origins, sorted by depth, with the projection of the triad widget (W).
#pragma once

#include "vtkNativeCoorSysGeometry.h"

#include <vtkActor2D.h>
#include <vtkCallbackCommand.h>
#include <vtkCoordinate.h>
#include <vtkPolyData.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>
#include <vtkTextMapper.h>
#include <vtkTextProperty.h>
#include <vtkType.h>

#include <array>
#include <map>
#include <memory>
#include <string>
#include <vector>

namespace vtkn
{
    class CoorSysSymbolSet
    {
    public:
        CoorSysSymbolSet();
        ~CoorSysSymbolSet();
        CoorSysSymbolSet(const CoorSysSymbolSet&) = delete;
        CoorSysSymbolSet& operator=(const CoorSysSymbolSet&) = delete;

        void SetRenderer(vtkRenderer* renderer);
        // A null color restores the per axis colors
        void SetOverrideColor(const double* color);
        void AddSymbol(const double origin[3], const double axisDirections[3][3],
                       const std::array<std::string, 3>& labels, double symbolSize);
        void Clear();
        bool IsEmpty() const { return Symbols.empty(); }

    private:
        struct Symbol
        {
            CoorSysGeometry Geometry;
            double Origin[3];
            double AxisDirections[3][3];
            std::array<std::string, 3> Labels;
            std::array<vtkSmartPointer<vtkTextMapper>, 3> LabelMappers;
            std::array<vtkSmartPointer<vtkActor2D>, 3> LabelActors;
            double CenterDepth = 0;
        };
        void GenerateGeometry(bool force);
        void GenerateShaftsAndEnds();
        void UpdateLabelTextProperty(double shaftLength);
        std::vector<Symbol*> GetSymbolsSortedByDepth();
        const int* GetLabelSize(Symbol& symbol, int axisId);

        vtkRenderer* Renderer = nullptr;
        std::vector<std::unique_ptr<Symbol>> Symbols;
        std::map<std::string, std::array<int, 2>> LabelSizes;
        vtkSmartPointer<vtkTextProperty> LabelTextProperty;
        int LabelFontSize = 0;
        unsigned int LabelFontGeneration = 0;
        bool HasOverrideColor = false;
        double OverrideColor[3] = { 0, 0, 0 };
        vtkSmartPointer<vtkCoordinate> OriginCoordinate;
        vtkSmartPointer<vtkCallbackCommand> StartCommand;
        unsigned long StartObserver = 0;
        vtkMTimeType CameraModifiedTime = 0;
        int RendererSize[2] = { 0, 0 };
        bool GeometryGenerated = false;
        vtkSmartPointer<vtkPolyData> ShaftPolyData;
        vtkSmartPointer<vtkActor2D> ShaftActor;
        vtkSmartPointer<vtkPolyData> EndPolyData;
        vtkSmartPointer<vtkActor2D> EndActor;
    };
}
