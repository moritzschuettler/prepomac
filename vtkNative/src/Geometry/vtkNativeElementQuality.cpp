// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The element quality metrics of the Element Quality dialog, computed by Verdict (Sandia) where it defines them. The
// angles and the warpage of a solid element are taken over its faces, as the commercial preprocessors report them.
#include "vtkNativeError.h"
#include "vtkNativeGuard.h"

#include <vtkCellType.h>
#include <vtk_verdict.h>

#include <algorithm>
#include <cmath>
#include <limits>
#include <vector>

using namespace vtkn;

namespace
{
    typedef double Point[3];

    struct CellShape
    {
        int CornerCount;
        std::vector<std::vector<int>> Edges;
        std::vector<std::vector<int>> Faces;
    };

    const double NotDefined = std::numeric_limits<double>::quiet_NaN();
    const double RadiansToDegrees = 180.0 / 3.14159265358979323846;

    const CellShape& GetCellShape(int cellType)
    {
        static const CellShape triangle = { 3, { { 0, 1 }, { 1, 2 }, { 2, 0 } }, { { 0, 1, 2 } } };
        static const CellShape quad = { 4, { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 } }, { { 0, 1, 2, 3 } } };
        static const CellShape tetra = { 4, { { 0, 1 }, { 1, 2 }, { 2, 0 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                                         { { 0, 1, 3 }, { 1, 2, 3 }, { 2, 0, 3 }, { 0, 2, 1 } } };
        static const CellShape hexahedron = { 8, { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 },
                                                   { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } },
                                              { { 0, 3, 2, 1 }, { 4, 5, 6, 7 }, { 0, 1, 5, 4 }, { 1, 2, 6, 5 },
                                                { 2, 3, 7, 6 }, { 3, 0, 4, 7 } } };
        static const CellShape wedge = { 6, { { 0, 1 }, { 1, 2 }, { 2, 0 }, { 3, 4 }, { 4, 5 }, { 5, 3 },
                                              { 0, 3 }, { 1, 4 }, { 2, 5 } },
                                         { { 0, 1, 2 }, { 3, 5, 4 }, { 0, 3, 4, 1 }, { 1, 4, 5, 2 },
                                           { 2, 5, 3, 0 } } };
        static const CellShape pyramid = { 5, { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 0, 4 }, { 1, 4 },
                                                { 2, 4 }, { 3, 4 } },
                                           { { 0, 3, 2, 1 }, { 0, 1, 4 }, { 1, 2, 4 }, { 2, 3, 4 },
                                             { 3, 0, 4 } } };
        switch (cellType)
        {
            case VTK_TRIANGLE: return triangle;
            case VTK_QUAD: return quad;
            case VTK_TETRA: return tetra;
            case VTK_HEXAHEDRON: return hexahedron;
            case VTK_WEDGE: return wedge;
            case VTK_PYRAMID: return pyramid;
            default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The cell type is not a linear triangle, quad, tetra, "
                                                              "hexahedron, wedge or pyramid.");
        }
    }

    double GetDistance(const Point& a, const Point& b)
    {
        double dx = b[0] - a[0];
        double dy = b[1] - a[1];
        double dz = b[2] - a[2];
        return std::sqrt(dx * dx + dy * dy + dz * dz);
    }

    void GetTriangleNormal(const Point& a, const Point& b, const Point& c, double* normal)
    {
        double u[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
        double v[3] = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
        normal[0] = u[1] * v[2] - u[2] * v[1];
        normal[1] = u[2] * v[0] - u[0] * v[2];
        normal[2] = u[0] * v[1] - u[1] * v[0];
    }

    double GetAngleBetweenTriangles(const Point& a, const Point& b, const Point& c, const Point& d, const Point& e,
                                    const Point& f)
    {
        double first[3];
        double second[3];
        GetTriangleNormal(a, b, c, first);
        GetTriangleNormal(d, e, f, second);
        double lengths = std::sqrt(first[0] * first[0] + first[1] * first[1] + first[2] * first[2]) *
                         std::sqrt(second[0] * second[0] + second[1] * second[1] + second[2] * second[2]);
        if (lengths == 0) return 180.0;
        double cosine = (first[0] * second[0] + first[1] * second[1] + first[2] * second[2]) / lengths;
        return std::acos(std::max(-1.0, std::min(1.0, cosine))) * RadiansToDegrees;
    }

    double GetQuadWarpage(const Point (&p)[4])
    {
        double firstDiagonal = GetAngleBetweenTriangles(p[0], p[1], p[2], p[0], p[2], p[3]);
        double secondDiagonal = GetAngleBetweenTriangles(p[0], p[1], p[3], p[1], p[2], p[3]);
        return std::max(firstDiagonal, secondDiagonal);
    }

    double GetEdgeRatio(const CellShape& shape, const Point* corners)
    {
        double shortest = std::numeric_limits<double>::max();
        double longest = 0;
        for (const std::vector<int>& edge : shape.Edges)
        {
            double length = GetDistance(corners[edge[0]], corners[edge[1]]);
            shortest = std::min(shortest, length);
            longest = std::max(longest, length);
        }
        if (shortest == 0) return std::numeric_limits<double>::max();
        return longest / shortest;
    }

    // The face metrics: the minimum or maximum corner angle, or the warpage, over the faces of the element
    double GetFaceMetric(int32_t metric, const CellShape& shape, const Point* corners)
    {
        bool found = false;
        double result = metric == VTKN_QUALITY_MINIMUM_ANGLE ? std::numeric_limits<double>::max() : 0;
        for (const std::vector<int>& face : shape.Faces)
        {
            Point facePoints[4];
            for (size_t i = 0; i < face.size(); i++) std::copy(corners[face[i]], corners[face[i]] + 3, facePoints[i]);
            //
            double value;
            if (metric == VTKN_QUALITY_WARPAGE)
            {
                if (face.size() != 4) continue;
                value = GetQuadWarpage(facePoints);
            }
            else if (face.size() == 3)
            {
                value = metric == VTKN_QUALITY_MINIMUM_ANGLE ? vtkverdict::tri_minimum_angle(3, facePoints)
                                                             : vtkverdict::tri_maximum_angle(3, facePoints);
            }
            else
            {
                value = metric == VTKN_QUALITY_MINIMUM_ANGLE ? vtkverdict::quad_minimum_angle(4, facePoints)
                                                             : vtkverdict::quad_maximum_angle(4, facePoints);
            }
            found = true;
            result = metric == VTKN_QUALITY_MINIMUM_ANGLE ? std::min(result, value) : std::max(result, value);
        }
        return found ? result : NotDefined;
    }

    // Verdict numbers a wedge as Exodus does, with the triangles wound the other way than in VTK, so an ideal VTK
    // wedge would measure -1
    double GetWedgeScaledJacobian(const Point* corners)
    {
        const int exodusOrder[6] = { 0, 2, 1, 3, 5, 4 };
        Point exodusCorners[6];
        for (int i = 0; i < 6; i++) std::copy(corners[exodusOrder[i]], corners[exodusOrder[i]] + 3, exodusCorners[i]);
        return vtkverdict::wedge_scaled_jacobian(6, exodusCorners);
    }

    double GetVerdictMetric(int32_t metric, int cellType, const Point* corners)
    {
        switch (metric)
        {
            case VTKN_QUALITY_SCALED_JACOBIAN:
                switch (cellType)
                {
                    case VTK_TRIANGLE: return vtkverdict::tri_scaled_jacobian(3, corners);
                    case VTK_QUAD: return vtkverdict::quad_scaled_jacobian(4, corners);
                    case VTK_TETRA: return vtkverdict::tet_scaled_jacobian(4, corners);
                    case VTK_HEXAHEDRON: return vtkverdict::hex_scaled_jacobian(8, corners);
                    case VTK_WEDGE: return GetWedgeScaledJacobian(corners);
                    case VTK_PYRAMID: return vtkverdict::pyramid_scaled_jacobian(5, corners);
                    default: return NotDefined;
                }
            case VTKN_QUALITY_ASPECT_RATIO:
                switch (cellType)
                {
                    case VTK_TRIANGLE: return vtkverdict::tri_aspect_ratio(3, corners);
                    case VTK_QUAD: return vtkverdict::quad_aspect_ratio(4, corners);
                    case VTK_TETRA: return vtkverdict::tet_aspect_ratio(4, corners);
                    default: return NotDefined;
                }
            case VTKN_QUALITY_SKEW:
                switch (cellType)
                {
                    case VTK_QUAD: return vtkverdict::quad_skew(4, corners);
                    case VTK_HEXAHEDRON: return vtkverdict::hex_skew(8, corners);
                    default: return NotDefined;
                }
            case VTKN_QUALITY_TAPER:
                switch (cellType)
                {
                    case VTK_QUAD: return vtkverdict::quad_taper(4, corners);
                    case VTK_HEXAHEDRON: return vtkverdict::hex_taper(8, corners);
                    default: return NotDefined;
                }
            default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The metric is not one of vtkn_quality_metric.");
        }
    }

    double GetElementQuality(int32_t metric, int cellType, const CellShape& shape, const Point* corners)
    {
        if (metric == VTKN_QUALITY_EDGE_RATIO) return GetEdgeRatio(shape, corners);
        if (metric == VTKN_QUALITY_MINIMUM_ANGLE || metric == VTKN_QUALITY_MAXIMUM_ANGLE ||
            metric == VTKN_QUALITY_WARPAGE)
            return GetFaceMetric(metric, shape, corners);
        return GetVerdictMetric(metric, cellType, corners);
    }
}

extern "C"
{
    vtkn_status VTKN_CALL vtkn_compute_element_quality(int32_t metric, int32_t cell_type, const double* points,
                                                       int32_t element_count, double* values)
    {
        return Guard([&]() {
            Require(metric >= VTKN_QUALITY_SCALED_JACOBIAN && metric <= VTKN_QUALITY_TAPER,
                    "The metric is not one of vtkn_quality_metric.");
            Require(element_count >= 0, "The element count must not be negative.");
            Require(element_count == 0 || (points != nullptr && values != nullptr), "An argument must not be null.");
            const CellShape& shape = GetCellShape(cell_type);
            //
            const Point* corners = reinterpret_cast<const Point*>(points);
            for (int32_t i = 0; i < element_count; i++)
            {
                values[i] = GetElementQuality(metric, cell_type, shape, corners);
                corners += shape.CornerCount;
            }
            return VTKN_OK;
        });
    }
}
