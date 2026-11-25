// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The id resolution of the Selection region of vtkControl, the step after a pick: the surface cell closest to a
// point with its nodes (GetGeometryPickProperties, GetNodeIdOnCellFaceClosestToPoint), the closest cells on named
// parts (GetClosestCellsOnActors), and the nodes and elements of a part's volume grid inside a frustum
// (GetPointAndCellIdsInsideFrustum). The volume grids come from the caller, as vtkMaxActor takes them from the data
// its provider returns; which nodes, edges or surfaces the ids mean is decided by the caller.
#include "vtkNativeView.h"
#include "vtkNativeError.h"

#include <vtkCell.h>
#include <vtkCellData.h>
#include <vtkDataArray.h>
#include <vtkDoubleArray.h>
#include <vtkFrustumSelector.h>
#include <vtkIdTypeArray.h>
#include <vtkNew.h>
#include <vtkObjectFactory.h>
#include <vtkPlanes.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkSTLWriter.h>
#include <vtkSignedCharArray.h>
#include <vtkWindowedSincPolyDataFilter.h>

#include <algorithm>
#include <cfloat>
#include <cmath>

namespace vtkn
{
    namespace
    {
        // vtkExtractSelectedFrustum, which vtkControl uses, is gone in VTK 9.7; vtkFrustumSelector holds its point
        // and cell tests (a point inside every plane; a cell inside or crossing the frustum) behind protected methods
        class FrustumTest : public vtkFrustumSelector
        {
        public:
            static FrustumTest* New();
            vtkTypeMacro(FrustumTest, vtkFrustumSelector);
            void SelectPoints(vtkDataSet* input, vtkSignedCharArray* inside) { ComputeSelectedPoints(input, inside); }
            void SelectCells(vtkDataSet* input, vtkSignedCharArray* inside) { ComputeSelectedCells(input, inside); }

        protected:
            FrustumTest() = default;
        };
        vtkStandardNewMacro(FrustumTest);

        vtkSmartPointer<vtkIdTypeArray> ToIdArray(const int32_t* values, int count)
        {
            vtkSmartPointer<vtkIdTypeArray> array = vtkSmartPointer<vtkIdTypeArray>::New();
            array->SetNumberOfValues(count);
            for (int i = 0; i < count; i++) array->SetValue(i, values[i]);
            return array;
        }

        // Port of GetCellNormal: the normal of the first three points
        void GetCellNormal(vtkCell* cell, double normal[3])
        {
            normal[0] = normal[1] = normal[2] = 0;
            vtkPoints* points = cell->GetPoints();
            if (points == nullptr || points->GetNumberOfPoints() < 3) return;
            double p0[3];
            double p1[3];
            double p2[3];
            points->GetPoint(0, p0);
            points->GetPoint(1, p1);
            points->GetPoint(2, p2);
            double a[3] = { p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2] };
            double b[3] = { p2[0] - p0[0], p2[1] - p0[1], p2[2] - p0[2] };
            double n[3] = { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
            double length = std::sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            if (length == 0) return;
            for (int i = 0; i < 3; i++) normal[i] = n[i] / length;
        }
    }

    // Port of the GetGlobalCellIdClosestTo3DPoint overload over all visible parts, with the cell it found: the
    // node ids of the surface cell are the global point ids of its points (GetCellFaceNodeIds) and their
    // coordinates those of GetCellFaceNodeCoor
    void View::GetClosestCell(double point[3], double maxDistance, const std::set<std::string>* selectableParts,
                              vtkn_closest_cell& cell)
    {
        cell.element_id = -1;
        cell.node_count = 0;
        PartActor* closestPart = nullptr;
        vtkIdType closestCellId = -1;
        double minDistance = DBL_MAX;
        double pointOut[3] = { 0, 0, 0 };
        for (const auto& entry : Parts)
        {
            PartActor* part = entry.second.get();
            bool visible = selectableParts != nullptr ? selectableParts->count(entry.first) > 0
                                                      : part->GetGeometry()->GetVisibility() != 0;
            vtkCellLocator* locator = part->GetCellLocator();
            if (visible && locator != nullptr)
            {
                double closestPoint[3];
                vtkIdType localCellId = -1;
                int subId = -1;
                double distance2 = -1;
                locator->FindClosestPoint(point, closestPoint, localCellId, subId, distance2);
                if (distance2 >= 0 && distance2 < minDistance)
                {
                    minDistance = distance2;
                    closestPart = part;
                    closestCellId = localCellId;
                    std::copy(closestPoint, closestPoint + 3, pointOut);
                }
            }
            if (minDistance == 0) break;
        }
        if (closestPart == nullptr) return;
        // The cell is reported also beyond the distance, with no element id and the point where it was
        vtkDataSet* dataSet = closestPart->GetCellLocator()->GetDataSet();
        vtkCell* surfaceCell = dataSet->GetCell(closestCellId);
        vtkDataArray* pointIds = dataSet->GetPointData()->GetGlobalIds();
        int count = static_cast<int>(surfaceCell->GetNumberOfPoints());
        Require(count <= 8, "A surface cell has more than eight nodes.");
        Require(pointIds != nullptr, "The surface of a part carries no node ids.");
        cell.node_count = count;
        for (int i = 0; i < count; i++)
        {
            cell.node_ids[i] = static_cast<int32_t>(pointIds->GetTuple1(surfaceCell->GetPointId(i)));
            surfaceCell->GetPoints()->GetPoint(i, cell.node_coordinates + 3 * i);
        }
        std::copy(point, point + 3, cell.point);
        if (maxDistance >= 0 && minDistance > maxDistance * maxDistance) return;
        cell.element_id = static_cast<int32_t>(dataSet->GetCellData()->GetGlobalIds()->GetTuple1(closestCellId));
        std::copy(pointOut, pointOut + 3, point);
        std::copy(pointOut, pointOut + 3, cell.point);
    }

    // Port of GetClosestCellsOnActors
    void View::GetClosestCellsOnParts(const double point[3], const std::vector<std::string>& names, double* distances,
                                      double* normals)
    {
        double x[3] = { point[0], point[1], point[2] };
        for (size_t i = 0; i < names.size(); i++)
        {
            distances[i] = -1;
            std::fill(normals + 3 * i, normals + 3 * i + 3, 0.0);
            auto found = Parts.find(names[i]);
            if (found == Parts.end() || found->second->GetCellLocator() == nullptr) continue;
            vtkCellLocator* locator = found->second->GetCellLocator();
            double closestPoint[3];
            vtkIdType localCellId = -1;
            int subId = -1;
            double distance2 = -1;
            locator->FindClosestPoint(x, closestPoint, localCellId, subId, distance2);
            if (distance2 < 0 || localCellId < 0) continue;
            distances[i] = std::sqrt(distance2);
            GetCellNormal(locator->GetDataSet()->GetCell(localCellId), normals + 3 * i);
        }
    }

    double View::GetSelectionPrecisionInModel(int selectionDistance)
    {
        return GetSelectionPrecision(selectionDistance);
    }

    void View::GetPartBounds(const std::string& name, double bounds[6])
    {
        auto found = Parts.find(name);
        if (found == Parts.end()) throw Error(VTKN_ERROR_NOT_FOUND, "No part is named '" + name + "'.");
        found->second->GetGeometry()->GetBounds(bounds);
    }

    // Port of vtkMaxActor.Smooth: the surface of the part smoothed by a windowed sinc filter, written as STL, which
    // SmoothGeometryPart of the Controller imports again
    void View::SmoothPartToStl(const std::string& name, const std::string& fileName)
    {
        auto found = Parts.find(name);
        if (found == Parts.end()) throw Error(VTKN_ERROR_NOT_FOUND, "No part is named '" + name + "'.");
        vtkNew<vtkWindowedSincPolyDataFilter> smoother;
        smoother->SetInputData(found->second->GetPolyData());
        smoother->NormalizeCoordinatesOn();
        smoother->SetNumberOfIterations(20);
        smoother->BoundarySmoothingOn();
        smoother->FeatureEdgeSmoothingOn();
        smoother->SetFeatureAngle(360.0);
        smoother->SetPassBand(0.1);
        smoother->NonManifoldSmoothingOn();
        vtkNew<vtkSTLWriter> writer;
        writer->SetFileName(fileName.c_str());
        writer->SetInputConnection(smoother->GetOutputPort());
        if (writer->Write() != 1)
            throw Error(VTKN_ERROR_VTK, "The smoothed part could not be written to '" + fileName + "'.");
    }

    // Port of AddNodeAndCellDataToGrid for the volume grid: the cells as they are, with the node and element ids
    void View::SetVolumeGrid(const std::string& name, const vtkn_mesh* grid)
    {
        if (grid == nullptr)
        {
            VolumeGrids.erase(name);
            return;
        }
        CheckMesh(*grid, "volume grid");
        Require(grid->point_ids != nullptr && grid->cell_ids != nullptr, "A volume grid needs its node and element ids.");
        vtkNew<vtkPoints> points;
        points->SetNumberOfPoints(grid->point_count);
        for (int i = 0; i < grid->point_count; i++) points->SetPoint(i, grid->coordinates + 3 * i);
        vtkSmartPointer<vtkUnstructuredGrid> volumeGrid = vtkSmartPointer<vtkUnstructuredGrid>::New();
        volumeGrid->SetPoints(points);
        volumeGrid->Allocate(grid->cell_count);
        std::vector<vtkIdType> ids;
        for (int i = 0, cell = 0; i < grid->cells_length; cell++)
        {
            int count = grid->cells[i];
            ids.assign(grid->cells + i + 1, grid->cells + i + 1 + count);
            volumeGrid->InsertNextCell(grid->cell_types[cell], count, ids.data());
            i += count + 1;
        }
        volumeGrid->GetPointData()->SetGlobalIds(ToIdArray(grid->point_ids, grid->point_count));
        volumeGrid->GetCellData()->SetGlobalIds(ToIdArray(grid->cell_ids, grid->cell_count));
        VolumeGrids[name] = volumeGrid;
    }

    // The extraction of GetPointAndCellIdsInsideFrustum: the points inside every plane, and the cells inside or
    // crossing the frustum
    void View::GetIdsInsideFrustum(const std::string& name, const double* planes, int planeCount,
                                   std::vector<int>& pointIds, std::vector<int>& cellIds)
    {
        auto found = VolumeGrids.find(name);
        if (found == VolumeGrids.end()) throw Error(VTKN_ERROR_NOT_FOUND, "No volume grid is set for '" + name + "'.");
        Require(planeCount == 6, "A frustum has six planes.");
        vtkUnstructuredGrid* grid = found->second;
        //
        vtkNew<vtkPoints> origins;
        vtkNew<vtkDoubleArray> normals;
        normals->SetNumberOfComponents(3);
        for (int i = 0; i < planeCount; i++)
        {
            origins->InsertNextPoint(planes + 6 * i);
            normals->InsertNextTuple(planes + 6 * i + 3);
        }
        vtkNew<vtkPlanes> frustum;
        frustum->SetPoints(origins);
        frustum->SetNormals(normals);
        vtkNew<FrustumTest> test;
        test->SetFrustum(frustum);
        //
        pointIds.clear();
        cellIds.clear();
        vtkNew<vtkSignedCharArray> insidePoints;
        insidePoints->SetNumberOfValues(grid->GetNumberOfPoints());
        test->SelectPoints(grid, insidePoints);
        vtkDataArray* globalPointIds = grid->GetPointData()->GetGlobalIds();
        for (vtkIdType i = 0; i < grid->GetNumberOfPoints(); i++)
        {
            if (insidePoints->GetValue(i) == 1) pointIds.push_back(static_cast<int>(globalPointIds->GetTuple1(i)));
        }
        vtkNew<vtkSignedCharArray> insideCells;
        insideCells->SetNumberOfValues(grid->GetNumberOfCells());
        test->SelectCells(grid, insideCells);
        vtkDataArray* globalCellIds = grid->GetCellData()->GetGlobalIds();
        for (vtkIdType i = 0; i < grid->GetNumberOfCells(); i++)
        {
            if (insideCells->GetValue(i) == 1) cellIds.push_back(static_cast<int>(globalCellIds->GetTuple1(i)));
        }
    }
}
