// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Screen space ambient occlusion of the base layer, after the ActiViz 9.7.1 test copy of vtkControl
// (SetAmbientOcclusion, CreateAmbientOcclusionPass, ApplySupersampledPixelSizes, PickWithoutAmbientOcclusionPass,
// UpdateAmbientOcclusionRadius). The occlusion pass draws into a framebuffer of its own without multisampling, so the
// base layer is supersampled instead: a supersampling pass around the camera pass, with the occlusion on the opaque
// pass only, so that the translucent geometry is blended over it as before. The 2D actors of the base layer are drawn
// after the supersampling, at the size of the window. The window keeps its multisampling, which smooths the other
// layers. The background is kept by the patch of vtkSSAOPass in the VTK source this library is built with, which
// discards the fragments at the far depth.
#include "vtkNativeView.h"
#include "vtkNativeError.h"

#include <vtkActorCollection.h>
#include <vtkCamera.h>
#include <vtkCameraPass.h>
#include <vtkLightsPass.h>
#include <vtkMath.h>
#include <vtkOpaquePass.h>
#include <vtkOverlayPass.h>
#include <vtkProperty.h>
#include <vtkRenderPassCollection.h>
#include <vtkRendererCollection.h>
#include <vtkSSAAPass.h>
#include <vtkSSAOPass.h>
#include <vtkSequencePass.h>
#include <vtkTranslucentPass.h>

#include <algorithm>
#include <cmath>
#include <cstring>

namespace vtkn
{
    namespace
    {
        // The supersampling pass renders at sqrt(5) times the size of the window
        const double supersamplingScale = std::sqrt(5.0);
        // The pixels around the sampling radius that the blur of the occlusion and the supersampling filter reach
        const int tileGuardMargin = 8;

        // The origins of tiles of the given size that cover the length, each overlapping the next by two guards
        std::vector<int> GetTileOrigins(int length, int tileSize, int guard)
        {
            std::vector<int> origins = { 0 };
            while (origins.back() + tileSize < length)
                origins.push_back(std::min(origins.back() + tileSize - 2 * guard, length - tileSize));
            return origins;
        }
    }

    void View::SetAmbientOcclusion(bool enabled, double relativeRadius, int kernelSize)
    {
        AmbientOcclusion = enabled;
        AmbientOcclusionRelativeRadius = relativeRadius > 0 ? relativeRadius : 0.05;
        AmbientOcclusionKernelSize = std::max(8, kernelSize);
        if (enabled && AmbientOcclusionPass == nullptr) CreateAmbientOcclusionPass();
        if (SsaoPass != nullptr) SsaoPass->SetKernelSize(static_cast<unsigned int>(AmbientOcclusionKernelSize));
        Renderer->SetPass(enabled ? AmbientOcclusionPass.Get() : nullptr);
        // vtkSSAOPass clears its own framebuffer only when this flag is set
        Renderer->SetUseSSAO(enabled);
        // The framebuffers of the passes are several times the size of the window
        if (!enabled && AmbientOcclusionPass != nullptr && !RenderWindow->GetNeverRendered())
        {
            RenderWindow->MakeCurrent();
            AmbientOcclusionPass->ReleaseGraphicsResources(RenderWindow);
        }
        UpdateAmbientOcclusionRadius();
        ApplySupersampledPixelSizes();
    }

    void View::CreateAmbientOcclusionPass()
    {
        vtkNew<vtkOpaquePass> opaquePass;
        SsaoPass = vtkSmartPointer<vtkSSAOPass>::New();
        SsaoPass->SetDelegatePass(opaquePass);
        SsaoPass->SetBlur(true);
        vtkNew<vtkLightsPass> lightsPass;
        vtkNew<vtkTranslucentPass> translucentPass;
        vtkNew<vtkRenderPassCollection> steps;
        steps->AddItem(lightsPass);
        steps->AddItem(SsaoPass);
        steps->AddItem(translucentPass);
        vtkNew<vtkSequencePass> stepsPass;
        stepsPass->SetPasses(steps);
        vtkNew<vtkCameraPass> cameraPass;
        cameraPass->SetDelegatePass(stepsPass);
        vtkNew<vtkSSAAPass> supersamplingPass;
        supersamplingPass->SetDelegatePass(cameraPass);
        vtkNew<vtkOverlayPass> overlayPass;
        vtkNew<vtkRenderPassCollection> passes;
        passes->AddItem(supersamplingPass);
        passes->AddItem(overlayPass);
        vtkNew<vtkSequencePass> pass;
        pass->SetPasses(passes);
        AmbientOcclusionPass = pass;
    }

    // Before every render of the base layer. Hardware picking renders at the window size, but switching the sizes
    // back and forth for every pick of a mouse move would rebuild the line shaders each time
    void View::PrepareAmbientOcclusion()
    {
        if (!AmbientOcclusion) return;
        UpdateAmbientOcclusionRadius();
        if (Renderer->GetSelector() == nullptr) ApplySupersampledPixelSizes();
    }

    // The sampling radius is relative to the size of the visible model
    void View::UpdateAmbientOcclusionRadius()
    {
        if (!AmbientOcclusion || SsaoPass == nullptr) return;
        double bounds[6];
        Renderer->ComputeVisiblePropBounds(bounds);
        if (bounds[0] > bounds[1]) return;
        double dx = bounds[1] - bounds[0];
        double dy = bounds[3] - bounds[2];
        double dz = bounds[5] - bounds[4];
        double diagonal = std::sqrt(dx * dx + dy * dy + dz * dz);
        if (diagonal <= 0) return;
        //
        SsaoPass->SetRadius(AmbientOcclusionRelativeRadius * diagonal);
        SsaoPass->SetBias(0.001 * diagonal);
    }

    // The supersampling pass renders at sqrt(5) times the window size, so the lines and points of the base layer
    // would come out that much thinner. Each property keeps its own size and only what is drawn is scaled. Without
    // supersampling the GPU never draws them thinner than one pixel, so at least that is scaled
    void View::ApplySupersampledPixelSizes()
    {
        bool supersampled = AmbientOcclusion && Renderer->GetPass() != nullptr;
        double scale = supersampled ? supersamplingScale : 1;
        float minimum = supersampled ? 1.0f : 0.0f;
        std::map<vtkProperty*, SupersampledSizes> sizes;
        vtkActorCollection* actors = Renderer->GetActors();
        vtkCollectionSimpleIterator iterator;
        actors->InitTraversal(iterator);
        for (vtkActor* actor = actors->GetNextActor(iterator); actor != nullptr;
             actor = actors->GetNextActor(iterator))
        {
            vtkProperty* property = actor->GetProperty();
            if (sizes.count(property) > 0) continue;
            //
            float lineWidth = property->GetLineWidth();
            float pointSize = property->GetPointSize();
            SupersampledSizes size = { lineWidth, pointSize, lineWidth, pointSize };
            auto known = SupersampledPixelSizes.find(property);
            if (known != SupersampledPixelSizes.end()) size = known->second;
            if (lineWidth != size.AppliedLineWidth) size.LineWidth = lineWidth;
            if (pointSize != size.AppliedPointSize) size.PointSize = pointSize;
            size.AppliedLineWidth = static_cast<float>(std::max(size.LineWidth, minimum) * scale);
            size.AppliedPointSize = static_cast<float>(std::max(size.PointSize, minimum) * scale);
            if (lineWidth != size.AppliedLineWidth) property->SetLineWidth(size.AppliedLineWidth);
            if (pointSize != size.AppliedPointSize) property->SetPointSize(size.AppliedPointSize);
            sizes[property] = size;
        }
        if (supersampled) SupersampledPixelSizes = sizes;
        else SupersampledPixelSizes.clear();
    }

    // Hardware picking renders the ids of the actors through the passes of the renderer. Supersampling would blend
    // and ambient occlusion would darken those ids, so a pick uses the plain passes
    void View::PickWithoutAmbientOcclusionPass(const std::function<void()>& pick)
    {
        vtkSmartPointer<vtkRenderPass> pass = Renderer->GetPass();
        if (pass == nullptr)
        {
            pick();
            return;
        }
        Renderer->SetPass(nullptr);
        Renderer->SetUseSSAO(false);
        try
        {
            pick();
        }
        catch (...)
        {
            Renderer->SetPass(pass);
            Renderer->SetUseSSAO(true);
            throw;
        }
        Renderer->SetPass(pass);
        Renderer->SetUseSSAO(true);
    }

    // Screen space ambient occlusion sees each tile of a magnified picture on its own, so the tiles of
    // vtkWindowToImageFilter come out with darker or lighter borders. The tiles are rendered here the way that filter
    // renders them, by the tile viewport of the window and the window center and scale of the cameras, but
    // overlapping by the sampling radius, and only the part of a tile that the border cannot reach is kept. A
    // radius of more than a quarter of the window is cut to that, which may leave the borders of the tiles visible
    std::vector<uint8_t> View::ReadPixelsInOverlappingTiles(int magnification, int& width, int& height)
    {
        int* size = RenderWindow->GetSize();
        int tileWidth = size[0];
        int tileHeight = size[1];
        width = tileWidth * magnification;
        height = tileHeight * magnification;
        //
        UpdateAmbientOcclusionRadius();
        vtkCamera* viewCamera = Renderer->GetActiveCamera();
        double pixelsPerUnit = magnification * tileHeight / (2 * viewCamera->GetParallelScale());
        int guard = static_cast<int>(std::ceil(SsaoPass->GetRadius() * pixelsPerUnit)) + tileGuardMargin;
        guard = std::min(guard, std::min(tileWidth, tileHeight) / 4);
        std::vector<int> originsX = GetTileOrigins(width, tileWidth, guard);
        std::vector<int> originsY = GetTileOrigins(height, tileHeight, guard);
        // Every renderer draws the tiles with a copy of its camera, so the camera of the view is not modified
        std::vector<vtkRenderer*> renderers;
        std::vector<vtkSmartPointer<vtkCamera>> cameras;
        vtkRendererCollection* collection = RenderWindow->GetRenderers();
        vtkCollectionSimpleIterator iterator;
        collection->InitTraversal(iterator);
        for (vtkRenderer* renderer = collection->GetNextRenderer(iterator); renderer != nullptr;
             renderer = collection->GetNextRenderer(iterator))
        {
            renderers.push_back(renderer);
            cameras.push_back(renderer->GetActiveCamera());
            vtkSmartPointer<vtkCamera> copy = vtkSmartPointer<vtkCamera>::Take(cameras.back()->NewInstance());
            copy->ShallowCopy(cameras.back());
            renderer->SetActiveCamera(copy);
        }
        auto restore = [&]() {
            for (size_t i = 0; i < renderers.size(); i++) renderers[i]->SetActiveCamera(cameras[i]);
            RenderWindow->SetTileScale(1);
            RenderWindow->SetTileViewport(0, 0, 1, 1);
        };
        std::vector<uint8_t> pixels(static_cast<size_t>(width) * height * 3);
        try
        {
            RenderWindow->SetTileScale(magnification);
            for (int originY : originsY)
            {
                for (int originX : originsX)
                {
                    double viewport[4] = { originX / static_cast<double>(width), originY / static_cast<double>(height),
                                           (originX + tileWidth) / static_cast<double>(width),
                                           (originY + tileHeight) / static_cast<double>(height) };
                    RenderWindow->SetTileViewport(viewport);
                    double deltaX = (viewport[0] + viewport[2] - 1) * magnification;
                    double deltaY = (viewport[1] + viewport[3] - 1) * magnification;
                    for (size_t i = 0; i < renderers.size(); i++)
                    {
                        vtkCamera* camera = renderers[i]->GetActiveCamera();
                        double* center = cameras[i]->GetWindowCenter();
                        camera->SetWindowCenter(center[0] + deltaX, center[1] + deltaY);
                        double angle = vtkMath::RadiansFromDegrees(cameras[i]->GetViewAngle());
                        angle = 2 * std::atan(std::tan(angle / 2) / magnification);
                        camera->SetViewAngle(vtkMath::DegreesFromRadians(angle));
                        camera->SetParallelScale(cameras[i]->GetParallelScale() / magnification);
                    }
                    RenderWindow->Render();
                    RenderWindow->MakeCurrent();
                    unsigned char* tile = RenderWindow->GetPixelData(0, 0, tileWidth - 1, tileHeight - 1, 0);
                    if (tile == nullptr) throw Error(VTKN_ERROR_VTK, "The render window returned no RGB picture.");
                    // The guard is kept at the borders of the picture, where no other tile draws
                    int firstX = originX == 0 ? 0 : guard;
                    int lastX = originX + tileWidth == width ? tileWidth : tileWidth - guard;
                    int firstY = originY == 0 ? 0 : guard;
                    int lastY = originY + tileHeight == height ? tileHeight : tileHeight - guard;
                    for (int y = firstY; y < lastY; y++)
                    {
                        std::memcpy(pixels.data() + 3 * (static_cast<size_t>(originY + y) * width + originX + firstX),
                                    tile + 3 * (static_cast<size_t>(y) * tileWidth + firstX),
                                    3 * static_cast<size_t>(lastX - firstX));
                    }
                    delete[] tile;
                }
            }
        }
        catch (...)
        {
            restore();
            throw;
        }
        restore();
        return pixels;
    }
}
