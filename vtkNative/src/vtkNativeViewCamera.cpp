// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The camera operations of vtkNative: the port of the Views region of vtkControl (standard, axis, vertical and
// normal views, zoom to fit, box zoom, the reset of the camera over the base and the overlay layer) and of the
// camera distance and clipping adjustment of vtkInteractorStyleControl. The camera animation is not ported.
#include "vtkNativeView.h"
#include "vtkNativeError.h"

#include <vtkInteractorObserver.h>
#include <vtkMath.h>

#include <algorithm>
#include <cmath>

namespace vtkn
{
    namespace
    {
        double Sign(double value)
        {
            return value > 0 ? 1.0 : (value < 0 ? -1.0 : 0.0);
        }

        // The angle between the directions, regardless of their sense
        double GetAngle(const double* v1, const double* v2)
        {
            double dot = v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
            double abs1 = std::sqrt(v1[0] * v1[0] + v1[1] * v1[1] + v1[2] * v1[2]);
            double abs2 = std::sqrt(v2[0] * v2[0] + v2[1] * v2[1] + v2[2] * v2[2]);
            if (abs1 == 0 || abs2 == 0) return 0;
            return std::acos(std::min(1.0, std::abs(dot / (abs1 * abs2))));
        }

        int GetClosestIsoDirectionQuadrant(const double* camera)
        {
            const double quadrants[4][3] = { { 1, 1, 1 }, { -1, 1, 1 }, { -1, -1, 1 }, { 1, -1, 1 } };
            int minId = -1;
            double min = 1e300;
            for (int i = 0; i < 4; i++)
            {
                double angle = GetAngle(camera, quadrants[i]);
                if (angle < min)
                {
                    min = angle;
                    minId = i + 1;
                }
            }
            if (minId == 1) return camera[0] >= 0 && camera[1] >= 0 && camera[2] >= 0 ? 1 : 7;
            if (minId == 2) return camera[0] <= 0 && camera[1] >= 0 && camera[2] >= 0 ? 2 : 8;
            if (minId == 3) return camera[0] <= 0 && camera[1] <= 0 && camera[2] >= 0 ? 3 : 5;
            return camera[0] >= 0 && camera[1] <= 0 && camera[2] >= 0 ? 4 : 6;
        }

        bool AreBoundsInitialized(const double* bounds)
        {
            return bounds[0] <= bounds[1] && bounds[2] <= bounds[3] && bounds[4] <= bounds[5];
        }

        void Cross(const double* a, const double* b, double* result)
        {
            result[0] = a[1] * b[2] - a[2] * b[1];
            result[1] = a[2] * b[0] - a[0] * b[2];
            result[2] = a[0] * b[1] - a[1] * b[0];
        }

        double Dot(const double* a, const double* b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }

        void Normalize(double* v)
        {
            double length = std::sqrt(Dot(v, v));
            if (length == 0) return;
            for (int i = 0; i < 3; i++) v[i] /= length;
        }
    }

    void View::SetViewParameters(const double* values10)
    {
        Require(values10 != nullptr, "The argument 'values' must not be null.");
        vtkCamera* camera = Renderer->GetActiveCamera();
        camera->SetPosition(values10[0], values10[1], values10[2]);
        camera->SetFocalPoint(values10[3], values10[4], values10[5]);
        camera->SetViewUp(values10[6], values10[7], values10[8]);
        camera->SetParallelScale(values10[9]);
        ResetClippingRange();
        FinishViewChange();
    }

    void View::SetStandardView(vtkn_orientation orientation, bool zoomToFit)
    {
        switch (orientation)
        {
            case VTKN_VIEW_FRONT: SetFrontBackView(true); break;
            case VTKN_VIEW_BACK: SetFrontBackView(false); break;
            case VTKN_VIEW_TOP: SetTopBottomView(true); break;
            case VTKN_VIEW_BOTTOM: SetTopBottomView(false); break;
            case VTKN_VIEW_LEFT: SetLeftRightView(true); break;
            case VTKN_VIEW_RIGHT: SetLeftRightView(false); break;
            case VTKN_VIEW_ISOMETRIC: SetIsometricView(true, zoomToFit); break;
            case VTKN_VIEW_ISOMETRIC_NEGATIVE: SetIsometricView(false, zoomToFit); break;
            case VTKN_VIEW_NORMAL: SetNormalView(); break;
            case VTKN_VIEW_VERTICAL: SetVerticalView(); break;
            default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The orientation is not one of vtkn_orientation.");
        }
    }

    void View::SetFrontBackView(bool front)
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        double delta = front ? 1 : -1;
        double* fPos = camera->GetFocalPoint();
        camera->SetPosition(fPos[0], fPos[1], fPos[2] + delta);
        camera->SetViewUp(0, 1, 0);
        ResetCameraToBounds();
        FinishViewChange();
    }

    void View::SetTopBottomView(bool top)
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        double delta = top ? 1 : -1;
        double* fPos = camera->GetFocalPoint();
        camera->SetPosition(fPos[0], fPos[1] + delta, fPos[2]);
        camera->SetViewUp(0, 0, -delta);
        ResetCameraToBounds();
        FinishViewChange();
    }

    void View::SetLeftRightView(bool left)
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        double delta = left ? -1 : 1;
        double* fPos = camera->GetFocalPoint();
        camera->SetPosition(fPos[0] + delta, fPos[1], fPos[2]);
        camera->SetViewUp(0, 1, 0);
        ResetCameraToBounds();
        FinishViewChange();
    }

    // The view turns to the octant closest to the current view direction and keeps the closest up axis, so the
    // result depends on the camera it starts from
    void View::SetIsometricView(bool positive, bool zoomToFit)
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        double delta = positive ? 1 : -1;
        //
        double pivot[3];
        double pivotDisplay[2];
        GetViewRotationPivot(pivot);
        WorldToDisplay(pivot, pivotDisplay);
        double distance = camera->GetDistance();
        //
        double closestUp[3];
        GetClosestUpView(closestUp);
        camera->SetViewUp(closestUp);
        //
        double fPos[3];
        camera->GetFocalPoint(fPos);
        int q = GetClosestIsoDirectionQuadrant(camera->GetViewPlaneNormal());
        const double offsets[8][3] = { { 1, 1, 1 }, { -1, 1, 1 }, { -1, -1, 1 }, { 1, -1, 1 },
                                       { 1, 1, -1 }, { -1, 1, -1 }, { -1, -1, -1 }, { 1, -1, -1 } };
        const double* offset = offsets[q - 1];
        camera->SetPosition(fPos[0] + delta * offset[0], fPos[1] + delta * offset[1], fPos[2] + delta * offset[2]);
        //
        camera->OrthogonalizeViewUp();
        double up[3];
        camera->GetViewUp(up);
        int maxId = 0;
        for (int i = 1; i < 3; i++)
        {
            if (std::abs(up[i]) > std::abs(up[maxId])) maxId = i;
        }
        double axisUp[3] = { 0, 0, 0 };
        axisUp[maxId] = Sign(up[maxId]);
        camera->SetViewUp(axisUp);
        //
        if (zoomToFit) ResetCameraToBounds();
        else KeepZoomAndRotationPivot(pivot, pivotDisplay, distance);
        FinishViewChange();
    }

    void View::SetNormalView()
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        double direction[3];
        camera->GetViewPlaneNormal(direction);
        int maxId = 0;
        for (int i = 1; i < 3; i++)
        {
            if (std::abs(direction[i]) > std::abs(direction[maxId])) maxId = i;
        }
        double delta[3] = { 0, 0, 0 };
        delta[maxId] = std::abs(direction[maxId]) == 1 ? -Sign(direction[maxId]) : Sign(direction[maxId]);
        double fPoint[3];
        camera->GetFocalPoint(fPoint);
        camera->SetPosition(fPoint[0] + delta[0], fPoint[1] + delta[1], fPoint[2] + delta[2]);
        ResetCameraToBounds();
        FinishViewChange();
    }

    void View::SetVerticalView()
    {
        double closestUp[3];
        GetClosestUpView(closestUp);
        Renderer->GetActiveCamera()->SetViewUp(closestUp);
        ResetCameraToBounds();
        FinishViewChange();
    }

    void View::SetAxisView(const double* axis, bool zoomToFit)
    {
        Require(axis != nullptr, "The argument 'axis' must not be null.");
        vtkCamera* camera = Renderer->GetActiveCamera();
        // Of the four global axes perpendicular to the new view direction the one that rotates the view the least
        double normal[3];
        camera->GetViewPlaneNormal(normal);
        Normalize(normal);
        double right[3];
        Cross(camera->GetViewUp(), normal, right);
        Normalize(right);
        double up[3];
        Cross(normal, right, up);
        double viewUp[3] = { 0, 0, 0 };
        bool found = false;
        double maxScore = 0;
        for (int axisId = 0; axisId < 3; axisId++)
        {
            for (int signId = 0; signId < 2; signId++)
            {
                double candidate[3] = { 0, 0, 0 };
                candidate[axisId] = signId == 0 ? 1 : -1;
                if (std::abs(Dot(candidate, axis)) > 0.5) continue;
                double candidateRight[3];
                Cross(candidate, axis, candidateRight);
                double score = Dot(candidateRight, right) + Dot(candidate, up);
                if (!found || score > maxScore)
                {
                    maxScore = score;
                    std::copy(candidate, candidate + 3, viewUp);
                    found = true;
                }
            }
        }
        //
        double pivot[3];
        double pivotDisplay[2];
        GetViewRotationPivot(pivot);
        WorldToDisplay(pivot, pivotDisplay);
        double distance = camera->GetDistance();
        //
        double fPos[3];
        camera->GetFocalPoint(fPos);
        camera->SetPosition(fPos[0] + axis[0], fPos[1] + axis[1], fPos[2] + axis[2]);
        camera->SetViewUp(viewUp);
        if (zoomToFit) ResetCameraToBounds();
        else KeepZoomAndRotationPivot(pivot, pivotDisplay, distance);
        FinishViewChange();
    }

    void View::SetVerticalAxisView(const double* axis, bool zoomToFit)
    {
        Require(axis != nullptr, "The argument 'axis' must not be null.");
        vtkCamera* camera = Renderer->GetActiveCamera();
        double pivot[3];
        double pivotDisplay[2];
        GetViewRotationPivot(pivot);
        WorldToDisplay(pivot, pivotDisplay);
        double distance = camera->GetDistance();
        // The axis cannot be vertical while the view looks along it: turn towards the vertical direction first
        double* normal = camera->GetViewPlaneNormal();
        if (std::abs(Dot(normal, axis)) > 0.999)
        {
            double direction[3];
            GetClosestUpView(direction);
            double fPos[3];
            camera->GetFocalPoint(fPos);
            camera->SetPosition(fPos[0] + direction[0], fPos[1] + direction[1], fPos[2] + direction[2]);
        }
        camera->SetViewUp(axis[0], axis[1], axis[2]);
        camera->OrthogonalizeViewUp();
        if (zoomToFit) ResetCameraToBounds();
        else KeepZoomAndRotationPivot(pivot, pivotDisplay, distance);
        FinishViewChange();
    }

    void View::ZoomToFit()
    {
        ResetCameraToBounds();
        FinishViewChange();
    }

    void View::SetZoomFactor(double parallelScale)
    {
        Require(parallelScale > 0, "The parallel scale must be positive.");
        Renderer->GetActiveCamera()->SetParallelScale(parallelScale);
    }

    void View::BoxZoom(int x1, int y1, int x2, int y2)
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        int* windowSize = RenderWindow->GetSize();
        double selectionCenter[2] = { static_cast<double>((x2 + x1) / 2), static_cast<double>((y2 + y1) / 2) };
        double windowCenter[2] = { static_cast<double>(windowSize[0] / 2), static_cast<double>(windowSize[1] / 2) };
        double selectionCenter3D[3];
        double windowCenter3D[3];
        DisplayToWorld(selectionCenter, selectionCenter3D);
        DisplayToWorld(windowCenter, windowCenter3D);
        //
        int selectionSize[2] = { std::abs(x2 - x1), std::abs(y2 - y1) };
        // The second condition is the one of vtkControl.SetBoxZoom, kept as it is
        if (selectionSize[0] == 0 || selectionSize[1] == 1) return;
        double zoomFactor = std::min(static_cast<double>(windowSize[0]) / selectionSize[0],
                                     static_cast<double>(windowSize[1]) / selectionSize[1]);
        camera->SetParallelScale(camera->GetParallelScale() / zoomFactor);
        double cPos[3];
        double fPos[3];
        camera->GetPosition(cPos);
        camera->GetFocalPoint(fPos);
        for (int i = 0; i < 3; i++)
        {
            double motion = selectionCenter3D[i] - windowCenter3D[i];
            cPos[i] += motion;
            fPos[i] += motion;
        }
        camera->SetPosition(cPos);
        camera->SetFocalPoint(fPos);
        FinishViewChange();
    }

    // Port of vtkControl.ResetCamera: the camera is reset to the base layer and to the overlay layer, and the
    // two framings are joined. The joining mixes world x and y with the screen axes; that is how it is in
    // vtkControl, and a port keeps it so that the two paths frame a view the same
    void View::ResetCameraToBounds()
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        Renderer->ResetCamera();
        double position1[3];
        double focalPoint1[3];
        camera->GetPosition(position1);
        camera->GetFocalPoint(focalPoint1);
        double scale1 = camera->GetParallelScale();
        //
        OverlayRenderer->ResetCamera();
        double focalPoint2[3];
        camera->GetFocalPoint(focalPoint2);
        double scale2 = camera->GetParallelScale();
        //
        int* size = RenderWindow->GetSize();
        double aspect = size[0] / static_cast<double>(size[1]);
        double yMax = std::max(focalPoint1[1] + scale1, focalPoint2[1] + scale2);
        double yMin = std::min(focalPoint1[1] - scale1, focalPoint2[1] - scale2);
        double xMax = std::max(focalPoint1[0] + scale1 * aspect, focalPoint2[0] + scale2 * aspect);
        double xMin = std::min(focalPoint1[0] - scale1 * aspect, focalPoint2[0] - scale2 * aspect);
        double centerX = (xMin + xMax) / 2.0;
        double centerY = (yMin + yMax) / 2.0;
        double newScale = std::max((yMax - yMin) / 2.0, (xMax - xMin) / (2.0 * aspect));
        double direction[3] = { position1[0] - focalPoint1[0], position1[1] - focalPoint1[1],
                                position1[2] - focalPoint1[2] };
        camera->SetFocalPoint(centerX, centerY, focalPoint1[2]);
        camera->SetPosition(centerX + direction[0], centerY + direction[1], focalPoint1[2] + direction[2]);
        camera->SetParallelScale(newScale);
        ResetClippingRange();
    }

    void View::FinishViewChange()
    {
        AdjustCameraDistanceAndClipping();
        Render();
    }

    void View::AdjustCameraDistanceAndClipping()
    {
        AdjustCameraDistance();
        ResetClippingRange();
    }

    // Port of vtkInteractorStyleControl.ResetClippingRange: the near and far planes cover every layer
    void View::ResetClippingRange()
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        double range[2];
        double overlayRange[2];
        Renderer->ResetCameraClippingRange();
        camera->GetClippingRange(range);
        OverlayRenderer->ResetCameraClippingRange();
        camera->GetClippingRange(overlayRange);
        camera->SetClippingRange(std::min(range[0], overlayRange[0]), std::max(range[1], overlayRange[1]));
    }

    // Port of vtkInteractorStyleControl.AdjustCameraDistance: the camera stands just outside the sphere around
    // the visible geometry, looking at a focal point one unit ahead
    void View::AdjustCameraDistance()
    {
        double b[6];
        if (!ComputeVisiblePropBounds(b)) return;
        //
        double r = 0.5 * std::sqrt((b[0] - b[1]) * (b[0] - b[1]) + (b[2] - b[3]) * (b[2] - b[3]) +
                                   (b[4] - b[5]) * (b[4] - b[5]));
        r *= 1.05;
        double center[3] = { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };
        //
        vtkCamera* camera = Renderer->GetActiveCamera();
        double pos[3];
        camera->GetPosition(pos);
        double delta[3] = { center[0] - pos[0], center[1] - pos[1], center[2] - pos[2] };
        double normal[3];
        camera->GetDirectionOfProjection(normal);
        double k = Dot(normal, delta) - r;
        double newPos[3] = { pos[0] + k * normal[0], pos[1] + k * normal[1], pos[2] + k * normal[2] };
        camera->SetPosition(newPos);
        camera->SetFocalPoint(newPos[0] + normal[0], newPos[1] + normal[1], newPos[2] + normal[2]);
    }

    bool View::ComputeVisiblePropBounds(double bounds[6])
    {
        bool found = false;
        vtkRenderer* renderers[2] = { Renderer, OverlayRenderer };
        for (vtkRenderer* renderer : renderers)
        {
            double rendererBounds[6];
            renderer->ComputeVisiblePropBounds(rendererBounds);
            if (!AreBoundsInitialized(rendererBounds)) continue;
            if (!found) std::copy(rendererBounds, rendererBounds + 6, bounds);
            else
            {
                for (int i = 0; i < 6; i += 2)
                {
                    bounds[i] = std::min(bounds[i], rendererBounds[i]);
                    bounds[i + 1] = std::max(bounds[i + 1], rendererBounds[i + 1]);
                }
            }
            found = true;
        }
        return found;
    }

    // The point under the center of the screen at the depth of the visible geometry
    void View::GetViewRotationPivot(double pivot[3])
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        double normal[3];
        double fPos[3];
        camera->GetViewPlaneNormal(normal);
        camera->GetFocalPoint(fPos);
        double bounds[6];
        Renderer->ComputeVisiblePropBounds(bounds);
        double center[3] = { (bounds[0] + bounds[1]) / 2, (bounds[2] + bounds[3]) / 2, (bounds[4] + bounds[5]) / 2 };
        double depth = normal[0] * (fPos[0] - center[0]) + normal[1] * (fPos[1] - center[1]) +
                       normal[2] * (fPos[2] - center[2]);
        for (int i = 0; i < 3; i++) pivot[i] = fPos[i] - depth * normal[i];
    }

    // Turns the view around the pivot, which stays under the screen position it was under, without zooming
    void View::KeepZoomAndRotationPivot(const double pivot[3], const double pivotDisplay[2], double distance)
    {
        vtkCamera* camera = Renderer->GetActiveCamera();
        camera->OrthogonalizeViewUp();
        ResetClippingRange();
        //
        double normal[3];
        double fPos[3];
        camera->GetViewPlaneNormal(normal);
        camera->GetFocalPoint(fPos);
        double target[3];
        DisplayToWorld(pivotDisplay, target);
        double targetOffset = normal[0] * (target[0] - pivot[0]) + normal[1] * (target[1] - pivot[1]) +
                              normal[2] * (target[2] - pivot[2]);
        double focalOffset = normal[0] * (fPos[0] - pivot[0]) + normal[1] * (fPos[1] - pivot[1]) +
                             normal[2] * (fPos[2] - pivot[2]);
        double offset = targetOffset - focalOffset;
        double newFPos[3];
        for (int i = 0; i < 3; i++) newFPos[i] = fPos[i] + pivot[i] - target[i] + offset * normal[i];
        camera->SetFocalPoint(newFPos);
        camera->SetPosition(newFPos[0] + distance * normal[0], newFPos[1] + distance * normal[1],
                            newFPos[2] + distance * normal[2]);
        ResetClippingRange();
    }

    void View::GetClosestUpView(double up[3])
    {
        double viewUp[3];
        Renderer->GetActiveCamera()->GetViewUp(viewUp);
        double a[3] = { std::abs(viewUp[0]), std::abs(viewUp[1]), std::abs(viewUp[2]) };
        double max = std::max(std::max(a[0], a[1]), std::max(a[1], a[2]));
        up[0] = up[1] = up[2] = 0;
        if (max == a[0]) up[0] = Sign(viewUp[0]);
        else if (max == a[1]) up[1] = Sign(viewUp[1]);
        else up[2] = Sign(viewUp[2]);
    }

    void View::WorldToDisplay(const double world[3], double display[2])
    {
        double result[3];
        vtkInteractorObserver::ComputeWorldToDisplay(Renderer, world[0], world[1], world[2], result);
        display[0] = result[0];
        display[1] = result[1];
    }

    void View::DisplayToWorld(const double display[2], double world[3])
    {
        double result[4];
        vtkInteractorObserver::ComputeDisplayToWorld(Renderer, display[0], display[1], 0, result);
        world[0] = result[0];
        world[1] = result[1];
        world[2] = result[2];
    }
}
