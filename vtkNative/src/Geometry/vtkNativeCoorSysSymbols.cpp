// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of vtkMaxCoorSysSymbols and vtkMaxCoorSysSymbol.
#include "vtkNativeCoorSysSymbols.h"

#include <vtkCamera.h>
#include <vtkCellArray.h>
#include <vtkCellData.h>
#include <vtkCommand.h>
#include <vtkNew.h>
#include <vtkPoints.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty2D.h>
#include <vtkUnsignedCharArray.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    namespace
    {
        vtkSmartPointer<vtkActor2D> CreateColoredActor(vtkPolyData* polyData)
        {
            vtkNew<vtkPolyDataMapper2D> mapper;
            mapper->SetInputData(polyData);
            mapper->SetScalarModeToUseCellData();
            mapper->SetColorModeToDefault();
            mapper->ScalarVisibilityOn();
            vtkSmartPointer<vtkActor2D> actor = vtkSmartPointer<vtkActor2D>::New();
            actor->SetMapper(mapper);
            actor->UseBoundsOff();
            actor->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
            actor->GetPositionCoordinate()->SetValue(0, 0);
            return actor;
        }

        vtkSmartPointer<vtkPolyData> CreateColoredPolyData()
        {
            vtkSmartPointer<vtkPolyData> polyData = vtkSmartPointer<vtkPolyData>::New();
            vtkNew<vtkPoints> points;
            vtkNew<vtkUnsignedCharArray> colors;
            colors->SetNumberOfComponents(4);
            polyData->SetPoints(points);
            polyData->GetCellData()->SetScalars(colors);
            return polyData;
        }
    }

    CoorSysSymbolSet::CoorSysSymbolSet()
    {
        // The display position of a symbol origin is computed from its world position
        OriginCoordinate = vtkSmartPointer<vtkCoordinate>::New();
        OriginCoordinate->SetCoordinateSystemToWorld();
        ShaftPolyData = CreateColoredPolyData();
        vtkNew<vtkCellArray> lines;
        ShaftPolyData->SetLines(lines);
        ShaftActor = CreateColoredActor(ShaftPolyData);
        ShaftActor->GetProperty()->SetLineWidth(2.0f);
        EndPolyData = CreateColoredPolyData();
        vtkNew<vtkCellArray> polygons;
        EndPolyData->SetPolys(polygons);
        EndActor = CreateColoredActor(EndPolyData);
        // The camera is not modified through the widgets during the mouse interaction, so the symbols are projected
        // again right before the renderer draws them
        StartCommand = vtkSmartPointer<vtkCallbackCommand>::New();
        StartCommand->SetClientData(this);
        StartCommand->SetCallback([](vtkObject*, unsigned long, void* clientData, void*) {
            static_cast<CoorSysSymbolSet*>(clientData)->GenerateGeometry(false);
        });
    }

    CoorSysSymbolSet::~CoorSysSymbolSet()
    {
        SetRenderer(nullptr);
    }

    void CoorSysSymbolSet::SetRenderer(vtkRenderer* renderer)
    {
        if (Renderer == renderer) return;
        if (Renderer != nullptr)
        {
            Renderer->RemoveObserver(StartObserver);
            for (const auto& symbol : Symbols)
            {
                for (const auto& actor : symbol->LabelActors) Renderer->RemoveViewProp(actor);
            }
            Renderer->RemoveViewProp(EndActor);
            Renderer->RemoveViewProp(ShaftActor);
        }
        Renderer = renderer;
        if (Renderer != nullptr)
        {
            Renderer->AddViewProp(ShaftActor);
            Renderer->AddViewProp(EndActor);
            for (const auto& symbol : Symbols)
            {
                for (const auto& actor : symbol->LabelActors) Renderer->AddViewProp(actor);
            }
            StartObserver = Renderer->AddObserver(vtkCommand::StartEvent, StartCommand);
        }
        GeometryGenerated = false;
    }

    void CoorSysSymbolSet::SetOverrideColor(const double* color)
    {
        HasOverrideColor = color != nullptr;
        if (color != nullptr) std::copy(color, color + 3, OverrideColor);
        for (const auto& symbol : Symbols) symbol->Geometry.SetOverrideColor(HasOverrideColor ? OverrideColor : nullptr);
        GeometryGenerated = false;
    }

    // The labels of all the symbols share one text property, sized by the shaft of the last added symbol
    void CoorSysSymbolSet::UpdateLabelTextProperty(double shaftLength)
    {
        int labelFontSize = CoorSysGeometry::GetLabelFontSize(shaftLength);
        if (LabelTextProperty != nullptr && LabelFontSize == labelFontSize) return;
        LabelFontSize = labelFontSize;
        LabelSizes.clear();
        // CreateNewTextProperty of vtkControl at the label size
        LabelTextProperty = vtkSmartPointer<vtkTextProperty>::New();
        LabelTextProperty->SetColor(0, 0, 0);
        LabelTextProperty->SetFontFamilyToArial();
        LabelTextProperty->SetLineSpacing(1.5);
        LabelTextProperty->SetFontSize(LabelFontSize);
        LabelTextProperty->SetLineOffset(-std::round(LabelFontSize / 7.0));
        for (const auto& symbol : Symbols)
        {
            for (const auto& mapper : symbol->LabelMappers) mapper->SetTextProperty(LabelTextProperty);
        }
    }

    void CoorSysSymbolSet::AddSymbol(const double origin[3], const double axisDirections[3][3],
                                     const std::array<std::string, 3>& labels, double symbolSize)
    {
        double shaftLength = CoorSysGeometry::GetShaftLengthOfSymbol(symbolSize);
        UpdateLabelTextProperty(shaftLength);
        std::unique_ptr<Symbol> symbol(new Symbol());
        std::copy(origin, origin + 3, symbol->Origin);
        for (int i = 0; i < 3; i++) std::copy(axisDirections[i], axisDirections[i] + 3, symbol->AxisDirections[i]);
        symbol->Labels = labels;
        symbol->Geometry.SetShaftLength(shaftLength);
        symbol->Geometry.SetOverrideColor(HasOverrideColor ? OverrideColor : nullptr);
        for (int i = 0; i < 3; i++)
        {
            symbol->LabelMappers[i] = vtkSmartPointer<vtkTextMapper>::New();
            symbol->LabelMappers[i]->SetInput(labels[i].c_str());
            symbol->LabelMappers[i]->SetTextProperty(LabelTextProperty);
            symbol->LabelActors[i] = vtkSmartPointer<vtkActor2D>::New();
            symbol->LabelActors[i]->SetMapper(symbol->LabelMappers[i]);
            symbol->LabelActors[i]->GetPositionCoordinate()->SetCoordinateSystemToDisplay();
            symbol->LabelActors[i]->GetPositionCoordinate()->SetValue(0, 0);
            symbol->LabelActors[i]->UseBoundsOff();
            if (Renderer != nullptr) Renderer->AddViewProp(symbol->LabelActors[i]);
        }
        Symbols.push_back(std::move(symbol));
        GeometryGenerated = false;
    }

    void CoorSysSymbolSet::Clear()
    {
        for (const auto& symbol : Symbols)
        {
            for (const auto& actor : symbol->LabelActors)
            {
                if (Renderer != nullptr) Renderer->RemoveViewProp(actor);
            }
        }
        Symbols.clear();
        // The shared poly data is emptied so that the removed symbols stop being drawn
        GenerateShaftsAndEnds();
        GeometryGenerated = false;
    }

    void CoorSysSymbolSet::GenerateGeometry(bool force)
    {
        if (Renderer == nullptr || Symbols.empty()) return;
        int* size = Renderer->GetSize();
        if (size[0] == 0 && size[1] == 0) return;
        // The projection depends on the camera and on the renderer size
        vtkCamera* camera = Renderer->GetActiveCamera();
        vtkMTimeType cameraModifiedTime = camera == nullptr ? 0 : camera->GetMTime();
        if (!force && GeometryGenerated && cameraModifiedTime == CameraModifiedTime && size[0] == RendererSize[0] &&
            size[1] == RendererSize[1]) return;
        CameraModifiedTime = cameraModifiedTime;
        RendererSize[0] = size[0];
        RendererSize[1] = size[1];
        //
        double right[3];
        double up[3];
        double viewPlaneNormal[3];
        CoorSysGeometry::GetCameraBasis(camera, right, up, viewPlaneNormal);
        for (const auto& symbol : Symbols)
        {
            OriginCoordinate->SetValue(symbol->Origin[0], symbol->Origin[1], symbol->Origin[2]);
            double* center = OriginCoordinate->GetComputedDoubleDisplayValue(Renderer);
            const double center2[2] = { center[0], center[1] };
            // Only the relative depth of the origins matters when the symbols are sorted
            symbol->CenterDepth = symbol->Origin[0] * viewPlaneNormal[0] + symbol->Origin[1] * viewPlaneNormal[1] +
                                  symbol->Origin[2] * viewPlaneNormal[2];
            symbol->Geometry.Update(center2, symbol->CenterDepth, symbol->AxisDirections, right, up, viewPlaneNormal);
            for (int axisId = 0; axisId < 3; axisId++)
            {
                std::array<double, 2> labelPosition = symbol->Geometry.GetLabelPosition(axisId,
                                                                                        GetLabelSize(*symbol, axisId));
                symbol->LabelActors[axisId]->GetPositionCoordinate()->SetValue(labelPosition[0], labelPosition[1]);
            }
        }
        GenerateShaftsAndEnds();
        GeometryGenerated = true;
    }

    // The symbols and the ends closer to the viewer are drawn over the ones behind them
    void CoorSysSymbolSet::GenerateShaftsAndEnds()
    {
        vtkPoints* shaftPoints = ShaftPolyData->GetPoints();
        vtkCellArray* shaftLines = ShaftPolyData->GetLines();
        vtkUnsignedCharArray* shaftColors = vtkUnsignedCharArray::SafeDownCast(ShaftPolyData->GetCellData()->GetScalars());
        vtkPoints* endPoints = EndPolyData->GetPoints();
        vtkCellArray* endPolygons = EndPolyData->GetPolys();
        vtkUnsignedCharArray* endColors = vtkUnsignedCharArray::SafeDownCast(EndPolyData->GetCellData()->GetScalars());
        shaftPoints->Reset();
        shaftLines->Reset();
        shaftColors->Reset();
        endPoints->Reset();
        endPolygons->Reset();
        endColors->Reset();
        for (Symbol* symbol : GetSymbolsSortedByDepth())
        {
            for (int endId : symbol->Geometry.GetEndIdsSortedByDepth())
            {
                if (symbol->Geometry.IsEndVisible(endId))
                    symbol->Geometry.AppendShaft(shaftPoints, shaftLines, shaftColors, endId);
            }
        }
        for (Symbol* symbol : GetSymbolsSortedByDepth())
        {
            symbol->Geometry.AppendOrigin(endPoints, endPolygons, endColors);
            for (int endId : symbol->Geometry.GetEndIdsSortedByDepth())
            {
                if (symbol->Geometry.IsEndVisible(endId))
                    symbol->Geometry.AppendEnd(endPoints, endPolygons, endColors, endId);
            }
        }
        for (vtkObject* changed : std::initializer_list<vtkObject*>{ shaftPoints, shaftLines, shaftColors, endPoints,
                                                                      endPolygons, endColors, ShaftPolyData, EndPolyData })
            changed->Modified();
    }

    std::vector<CoorSysSymbolSet::Symbol*> CoorSysSymbolSet::GetSymbolsSortedByDepth()
    {
        std::vector<Symbol*> symbols;
        for (const auto& symbol : Symbols) symbols.push_back(symbol.get());
        std::stable_sort(symbols.begin(), symbols.end(),
                         [](const Symbol* a, const Symbol* b) { return a->CenterDepth < b->CenterDepth; });
        return symbols;
    }

    // vtkMaxWidgetTools.GetTextSize, kept per label text as vtkMaxCoorSysSymbols keeps it
    const int* CoorSysSymbolSet::GetLabelSize(Symbol& symbol, int axisId)
    {
        auto entry = LabelSizes.find(symbol.Labels[axisId]);
        if (entry == LabelSizes.end())
        {
            int size[2] = { 0, 0 };
            symbol.LabelMappers[axisId]->GetSize(Renderer, size);
            entry = LabelSizes.emplace(symbol.Labels[axisId], std::array<int, 2>{ size[0], size[1] }).first;
        }
        return entry->second.data();
    }
}
