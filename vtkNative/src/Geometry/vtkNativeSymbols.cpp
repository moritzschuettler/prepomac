// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Port of the symbol sources of the Add geometry region of vtkControl: AddSphereActor, the AddOriented* methods,
// CreateSpringSource, SmoothLinePoints and the joint parts AddCylinderAlongX, AddSocketAlongX, AddCube,
// AddSquareSleeveAlongX and AddWeldFillet.
#include "vtkNativeSymbols.h"
#include "vtkNativeError.h"

#include <vtkAppendPolyData.h>
#include <vtkArrowSource.h>
#include <vtkCellArray.h>
#include <vtkConeSource.h>
#include <vtkCubeSource.h>
#include <vtkCylinderSource.h>
#include <vtkLineSource.h>
#include <vtkLinearExtrusionFilter.h>
#include <vtkNew.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkPolyDataNormals.h>
#include <vtkPolyLine.h>
#include <vtkRotationalExtrusionFilter.h>
#include <vtkSphereSource.h>
#include <vtkTransform.h>
#include <vtkTransformPolyDataFilter.h>
#include <vtkTriangleFilter.h>
#include <vtkTubeFilter.h>

#include <array>
#include <cmath>
#include <vector>

namespace vtkn
{
    namespace
    {
        const double Pi = 3.14159265358979323846;
        const int ArrowTipResolution = 21;
        const int ArrowShaftResolution = 15;

        vtkSmartPointer<vtkPolyData> Triangulate(vtkAlgorithmOutput* output)
        {
            vtkNew<vtkTriangleFilter> triangleFilter;
            triangleFilter->SetInputConnection(output);
            triangleFilter->Update();
            vtkSmartPointer<vtkPolyData> result = vtkSmartPointer<vtkPolyData>::New();
            result->DeepCopy(triangleFilter->GetOutput());
            return result;
        }

        // The normals some sources set on their triangulation for smooth shading
        void SetComputedNormals(vtkPolyData* polyData)
        {
            vtkNew<vtkPolyDataNormals> normals;
            normals->SetInputData(polyData);
            normals->Update();
            polyData->GetPointData()->SetNormals(normals->GetOutput()->GetPointData()->GetNormals());
        }

        vtkSmartPointer<vtkPolyData> Transform(vtkPolyData* polyData, vtkTransform* transform)
        {
            vtkNew<vtkTransformPolyDataFilter> transformFilter;
            transformFilter->SetInputData(polyData);
            transformFilter->SetTransform(transform);
            transformFilter->Update();
            vtkSmartPointer<vtkPolyData> result = vtkSmartPointer<vtkPolyData>::New();
            result->DeepCopy(transformFilter->GetOutput());
            return result;
        }

        vtkSmartPointer<vtkPolyData> TranslateX(vtkPolyData* polyData, double x)
        {
            vtkNew<vtkTransform> transform;
            transform->Translate(x, 0, 0);
            return Transform(polyData, transform);
        }

        void AddCylinderAlongX(vtkAppendPolyData* append, double centerX, double length, double radius)
        {
            vtkNew<vtkCylinderSource> cylinder;
            cylinder->SetCenter(0, 0, 0);
            cylinder->SetHeight(length);
            cylinder->SetRadius(radius);
            cylinder->SetResolution(16);
            cylinder->CappingOn();
            cylinder->Update();
            vtkNew<vtkTransform> transform;
            transform->Translate(centerX, 0, 0);
            transform->RotateZ(-90);
            append->AddInputData(Transform(cylinder->GetOutput(), transform));
        }

        void AddSocketAlongX(vtkAppendPolyData* append, double innerRadius, double outerRadius, double startAngleDeg,
                             double endAngleDeg, int armCount, double armAngleDeg)
        {
            const int arcPoints = 12;
            vtkNew<vtkPoints> points;
            for (int i = 0; i <= arcPoints; i++)
            {
                double angle = (startAngleDeg + (endAngleDeg - startAngleDeg) * i / arcPoints) * Pi / 180;
                points->InsertNextPoint(outerRadius * std::sin(angle), 0, outerRadius * std::cos(angle));
            }
            for (int i = arcPoints; i >= 0; i--)
            {
                double angle = (startAngleDeg + (endAngleDeg - startAngleDeg) * i / arcPoints) * Pi / 180;
                points->InsertNextPoint(innerRadius * std::sin(angle), 0, innerRadius * std::cos(angle));
            }
            const int count = 2 * (arcPoints + 1);
            vtkNew<vtkCellArray> polygons;
            polygons->InsertNextCell(count);
            for (int i = 0; i < count; i++) polygons->InsertCellPoint(i);
            vtkNew<vtkPolyData> profile;
            profile->SetPoints(points);
            profile->SetPolys(polygons);
            vtkNew<vtkRotationalExtrusionFilter> extrusion;
            extrusion->SetInputData(profile);
            extrusion->SetResolution(6);
            extrusion->SetAngle(armAngleDeg);
            extrusion->CappingOn();
            extrusion->Update();
            for (int i = 0; i < armCount; i++)
            {
                vtkNew<vtkTransform> transform;
                transform->RotateY(90);
                transform->RotateZ(360.0 * i / armCount - armAngleDeg / 2);
                append->AddInputData(Transform(extrusion->GetOutput(), transform));
            }
        }

        void AddCube(vtkAppendPolyData* append, double centerX, double centerY, double centerZ, double lengthX,
                     double lengthY, double lengthZ)
        {
            vtkNew<vtkCubeSource> cube;
            cube->SetCenter(centerX, centerY, centerZ);
            cube->SetXLength(lengthX);
            cube->SetYLength(lengthY);
            cube->SetZLength(lengthZ);
            cube->Update();
            vtkSmartPointer<vtkPolyData> output = vtkSmartPointer<vtkPolyData>::New();
            output->DeepCopy(cube->GetOutput());
            append->AddInputData(output);
        }

        void AddSquareSleeveAlongX(vtkAppendPolyData* append, double length, double outerWidth, double innerWidth)
        {
            vtkNew<vtkPoints> points;
            const double widths[2] = { outerWidth, innerWidth };
            const int ySigns[4] = { -1, 1, 1, -1 };
            const int zSigns[4] = { -1, -1, 1, 1 };
            for (int shell = 0; shell < 2; shell++)
            {
                for (int end = 0; end < 2; end++)
                {
                    for (int corner = 0; corner < 4; corner++)
                        points->InsertNextPoint((end == 0 ? -1 : 1) * length / 2, ySigns[corner] * widths[shell] / 2,
                                                zSigns[corner] * widths[shell] / 2);
                }
            }
            vtkNew<vtkCellArray> polygons;
            for (int corner = 0; corner < 4; corner++)
            {
                int next = (corner + 1) % 4;
                const int faces[4][4] = { { corner, next, 4 + next, 4 + corner },
                                          { 8 + corner, 12 + corner, 12 + next, 8 + next },
                                          { 4 + corner, 4 + next, 12 + next, 12 + corner },
                                          { corner, 8 + corner, 8 + next, next } };
                for (const auto& face : faces)
                {
                    vtkIdType ids[4] = { face[0], face[1], face[2], face[3] };
                    polygons->InsertNextCell(4, ids);
                }
            }
            vtkNew<vtkPolyData> sleeve;
            sleeve->SetPoints(points);
            sleeve->SetPolys(polygons);
            append->AddInputData(sleeve);
        }

        void AddWeldFillet(vtkAppendPolyData* append, int side)
        {
            vtkNew<vtkPoints> points;
            points->InsertNextPoint(0, side * 0.075, -0.5);
            points->InsertNextPoint(0, side * 0.375, -0.5);
            points->InsertNextPoint(0.3, side * 0.075, -0.5);
            vtkNew<vtkCellArray> polygons;
            vtkIdType ids[3] = { 0, 1, 2 };
            polygons->InsertNextCell(3, ids);
            vtkNew<vtkPolyData> triangle;
            triangle->SetPoints(points);
            triangle->SetPolys(polygons);
            vtkNew<vtkLinearExtrusionFilter> extrusion;
            extrusion->SetInputData(triangle);
            extrusion->SetExtrusionTypeToVectorExtrusion();
            extrusion->SetVector(0, 0, 1);
            extrusion->SetScaleFactor(1.0);
            extrusion->CappingOn();
            extrusion->Update();
            vtkSmartPointer<vtkPolyData> output = vtkSmartPointer<vtkPolyData>::New();
            output->DeepCopy(extrusion->GetOutput());
            append->AddInputData(output);
        }

        std::vector<std::array<double, 3>> SmoothLinePoints(const std::vector<std::array<double, 3>>& points,
                                                            int numOfPoints)
        {
            int half = numOfPoints / 2;
            int evenNumOfPoints = half * 2 + 1;
            std::vector<std::array<double, 3>> smooth(points.size());
            int count = static_cast<int>(points.size());
            for (int i = 0; i < count; i++)
            {
                if (i < half || i >= count - half) smooth[i] = points[i];
                else
                {
                    std::array<double, 3> newPoint = { 0, 0, 0 };
                    for (int j = -half; j <= half; j++)
                        for (int k = 0; k < 3; k++) newPoint[k] += points[i + j][k];
                    for (int k = 0; k < 3; k++) newPoint[k] /= evenNumOfPoints;
                    smooth[i] = newPoint;
                }
            }
            return smooth;
        }

        vtkSmartPointer<vtkPolyData> CreateSpringSource(double diameter, double height, int numOfTurns,
                                                        int radialResolution)
        {
            double r = diameter / 2;
            double phi = 0;
            double phiDelta = Pi / radialResolution;
            double linPart = diameter;
            double hDelta = (height - 2 * linPart) / (numOfTurns * radialResolution);
            const int linResolution = 3;
            std::vector<std::array<double, 3>> points;
            double delta = linPart / linResolution;
            for (int i = 0; i < linResolution; i++) points.push_back({ delta * i, 0, 0 });
            delta = r / linResolution;
            for (int i = 0; i < linResolution; i++) points.push_back({ linPart, delta * i, 0 });
            for (int i = 0; i <= numOfTurns * radialResolution; i++)
            {
                points.push_back({ linPart + hDelta * i, r * std::cos(phi), r * std::sin(phi) });
                phi += phiDelta;
            }
            delta = r / linResolution;
            for (int i = 0; i < linResolution; i++) points.push_back({ height - linPart, r - delta * (i + 1), 0 });
            delta = linPart / linResolution;
            for (int i = 0; i < linResolution; i++) points.push_back({ height - linPart + delta * (i + 1), 0, 0 });
            points = SmoothLinePoints(points, 3);
            //
            vtkNew<vtkPoints> vtkPoints;
            for (const auto& point : points) vtkPoints->InsertNextPoint(point[0], point[1], point[2]);
            vtkNew<vtkCellArray> cells;
            cells->InsertNextCell(static_cast<vtkIdType>(points.size()));
            for (size_t i = 0; i < points.size(); i++) cells->InsertCellPoint(static_cast<vtkIdType>(i));
            vtkNew<vtkPolyData> lineData;
            lineData->SetPoints(vtkPoints);
            lineData->SetLines(cells);
            vtkNew<vtkTubeFilter> tubeFilter;
            tubeFilter->SetInputData(lineData);
            tubeFilter->SetRadius(0.03);
            tubeFilter->SetNumberOfSides(10);
            tubeFilter->CappingOn();
            tubeFilter->UseDefaultNormalOff();
            return Triangulate(tubeFilter->GetOutputPort());
        }

        vtkSmartPointer<vtkArrowSource> CreateArrow(double relativeSize)
        {
            vtkSmartPointer<vtkArrowSource> arrow = vtkSmartPointer<vtkArrowSource>::New();
            arrow->SetTipResolution(ArrowTipResolution);
            arrow->SetTipLength(0.3 / relativeSize);
            arrow->SetTipRadius(0.1 / relativeSize);
            arrow->SetShaftResolution(ArrowShaftResolution);
            arrow->SetShaftRadius(0.03 / relativeSize);
            return arrow;
        }
    }

    SymbolSource CreateSymbolSource(const vtkn_symbol_parameters& parameters)
    {
        SymbolSource symbol;
        double shift = parameters.invert ? -1.05 : 0.05;
        switch (parameters.kind)
        {
            case VTKN_SYMBOL_SPHERE:
            {
                vtkNew<vtkSphereSource> sphere;
                sphere->SetRadius(0.2);
                sphere->SetPhiResolution(13);
                sphere->SetThetaResolution(13);
                symbol.Source = Triangulate(sphere->GetOutputPort());
                SetComputedNormals(symbol.Source);
                break;
            }
            case VTKN_SYMBOL_DISPLACEMENT_CONE:
            {
                vtkNew<vtkConeSource> cone;
                cone->SetCenter(-0.5, 0, 0);
                cone->SetResolution(31);
                symbol.Source = Triangulate(cone->GetOutputPort());
                symbol.ScaleFactor = 0.3;
                break;
            }
            case VTKN_SYMBOL_CONE:
            {
                vtkNew<vtkConeSource> cone;
                cone->SetCenter(-0.5, 0, 0);
                cone->SetRadius(0.35);
                cone->CappingOn();
                cone->SetResolution(11);
                symbol.Source = Triangulate(cone->GetOutputPort());
                symbol.ScaleFactor = 0.3;
                break;
            }
            case VTKN_SYMBOL_ROTATIONAL_CONSTRAINT:
            {
                vtkNew<vtkCubeSource> cube;
                cube->SetCenter(-1.55, 0.0, 0.0);
                cube->SetXLength(0.4);
                cube->SetYLength(0.9);
                cube->SetZLength(0.9);
                vtkNew<vtkLineSource> line;
                line->SetPoint1(0, 0, 0);
                line->SetPoint2(-1.3, 0, 0);
                vtkNew<vtkAppendPolyData> append;
                append->AddInputConnection(cube->GetOutputPort());
                append->AddInputConnection(line->GetOutputPort());
                symbol.Source = Triangulate(append->GetOutputPort());
                symbol.ScaleFactor = 0.3;
                symbol.Flat = true;
                break;
            }
            case VTKN_SYMBOL_JOINT:
            {
                vtkNew<vtkAppendPolyData> append;
                if (parameters.joint == VTKN_JOINT_HINGE)
                {
                    AddCylinderAlongX(append, 0, 2.4, 0.12);
                    AddCylinderAlongX(append, 0, 0.9, 0.35);
                    AddCylinderAlongX(append, -0.49, 0.08, 0.5);
                    AddCylinderAlongX(append, 0.49, 0.08, 0.5);
                }
                else if (parameters.joint == VTKN_JOINT_CYLINDRICAL)
                {
                    AddCylinderAlongX(append, 0, 2.4, 0.12);
                    AddCylinderAlongX(append, 0, 0.9, 0.35);
                }
                else if (parameters.joint == VTKN_JOINT_SLIDER)
                {
                    AddCube(append, 0, 0, 0, 2.4, 0.24, 0.24);
                    AddSquareSleeveAlongX(append, 0.9, 0.7, 0.34);
                }
                else if (parameters.joint == VTKN_JOINT_BALL)
                {
                    vtkNew<vtkSphereSource> sphere;
                    sphere->SetRadius(0.45);
                    sphere->SetPhiResolution(16);
                    sphere->SetThetaResolution(16);
                    append->AddInputConnection(sphere->GetOutputPort());
                    AddSocketAlongX(append, 0.6, 0.65, 75, 178, 3, 50);
                }
                else if (parameters.joint == VTKN_JOINT_WELD)
                {
                    AddCube(append, -0.075, 0, 0, 0.15, 1.6, 1.0);
                    AddCube(append, 0.5, 0, 0, 1.0, 0.15, 1.0);
                    AddWeldFillet(append, 1);
                    AddWeldFillet(append, -1);
                }
                else throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The joint symbol is not one of vtkn_joint.");
                vtkNew<vtkPolyDataNormals> polyDataNormals;
                polyDataNormals->SetInputConnection(append->GetOutputPort());
                polyDataNormals->ConsistencyOn();
                polyDataNormals->AutoOrientNormalsOn();
                symbol.Source = Triangulate(polyDataNormals->GetOutputPort());
                symbol.ScaleFactor = 0.36;
                symbol.Flat = true;
                break;
            }
            case VTKN_SYMBOL_ARROW:
            {
                vtkSmartPointer<vtkArrowSource> arrow = CreateArrow(parameters.relative_size);
                symbol.Source = TranslateX(Triangulate(arrow->GetOutputPort()), shift);
                break;
            }
            case VTKN_SYMBOL_DOUBLE_ARROW:
            {
                vtkSmartPointer<vtkArrowSource> arrow = CreateArrow(1);
                vtkNew<vtkConeSource> cone;
                cone->SetResolution(21);
                cone->SetHeight(0.3);
                cone->SetRadius(0.1);
                cone->SetCenter(0.65, 0, 0);
                vtkNew<vtkAppendPolyData> append;
                append->AddInputConnection(arrow->GetOutputPort());
                append->AddInputConnection(cone->GetOutputPort());
                symbol.Source = TranslateX(Triangulate(append->GetOutputPort()), 0.05);
                break;
            }
            case VTKN_SYMBOL_SPRING:
                symbol.Source = TranslateX(CreateSpringSource(0.2, 1, 8, 10), -1.05);
                break;
            case VTKN_SYMBOL_THERMO:
            {
                double rs = 0.2;
                double rc = 0.1;
                vtkNew<vtkSphereSource> sphere;
                sphere->SetRadius(rs);
                sphere->SetPhiResolution(11);
                sphere->SetThetaResolution(11);
                sphere->SetCenter(rs, 0, 0);
                sphere->Update();
                vtkNew<vtkCylinderSource> cylinder;
                cylinder->SetRadius(rc);
                cylinder->SetHeight(1 - rs);
                cylinder->SetResolution(7);
                cylinder->SetCenter(0, 0.5 + rs, 0);
                cylinder->Update();
                vtkNew<vtkTransform> transform;
                transform->RotateZ(-90);
                vtkNew<vtkAppendPolyData> append;
                append->AddInputData(sphere->GetOutput());
                append->AddInputData(Transform(cylinder->GetOutput(), transform));
                vtkSmartPointer<vtkPolyData> triangles = Triangulate(append->GetOutputPort());
                SetComputedNormals(triangles);
                symbol.Source = TranslateX(triangles, shift);
                symbol.ScaleFactor = 0.5;
                break;
            }
            case VTKN_SYMBOL_FLUX:
            {
                double rs = 0.2;
                vtkNew<vtkSphereSource> sphere;
                sphere->SetRadius(rs);
                sphere->SetPhiResolution(9);
                sphere->SetThetaResolution(11);
                sphere->SetCenter(parameters.center ? 0 : rs, 0, 0);
                symbol.Source = TranslateX(Triangulate(sphere->GetOutputPort()), shift);
                symbol.ScaleFactor = 0.5;
                break;
            }
            default:
                throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The symbol is not one of vtkn_symbol.");
        }
        return symbol;
    }
}
