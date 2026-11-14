// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The render passes of the base layer: the screen space ambient occlusion, after the ActiViz 9.7.1 test copy of
// vtkControl (SetAmbientOcclusion, CreateAmbientOcclusionPass, ApplySupersampledPixelSizes,
// PickWithoutAmbientOcclusionPass, UpdateAmbientOcclusionRadius), and the depth sorting of the translucent faces.
// Without these passes the translucent geometry is blended in the order of its actors, as VTK 5.8 blended it, so a
// part drawn later covers a part in front of it. Depth peeling blends it by depth, but into buffers of its own
// without multisampling, as the occlusion pass does, so the base layer is supersampled instead: a supersampling pass
// around the camera pass, with the occlusion on the opaque pass only and the peeling of the translucent geometry
// after it. The 2D actors of the base layer are drawn after the supersampling, at the size of the window. The window
// keeps its multisampling, which smooths the other layers. The base layer has a pass only with the ambient occlusion
// on or with a translucent face in the view, and peels only with such a face, so a view of opaque parts is drawn as
// before. Peeling draws its geometry several times, and the edge lines of the parts are translucent always, so the
// edge lines of the opaque parts are kept out of it: they lie on opaque faces and are blended once, before the
// peeling. The background is kept by the patch of vtkSSAOPass in the VTK source this library is built with, which
// discards the fragments at the far depth.
#include "vtkNativeView.h"
#include "vtkNativeError.h"

#include <vtkActorCollection.h>
#include <vtkCamera.h>
#include <vtkCameraPass.h>
#include <vtkDepthPeelingPass.h>
#include <vtkDualDepthPeelingPass.h>
#include <vtkInformation.h>
#include <vtkInformationIntegerKey.h>
#include <vtkLightsPass.h>
#include <vtkMapper.h>
#include <vtkMath.h>
#include <vtkObjectFactory.h>
#include <vtkOpaquePass.h>
#include <vtkOpenGLRenderer.h>
#include <vtkOverlayPass.h>
#include <vtkPolyData.h>
#include <vtkPropCollection.h>
#include <vtkProperty.h>
#include <vtkRenderPassCollection.h>
#include <vtkRenderState.h>
#include <vtkRendererCollection.h>
#include <vtkSSAAPass.h>
#include <vtkSSAOPass.h>
#include <vtkSequencePass.h>
#include <vtkTranslucentPass.h>

#include <algorithm>
#include <cmath>
#include <cstring>
#include <set>

namespace vtkn
{
    namespace
    {
        // The supersampling pass renders at sqrt(5) times the size of the window
        const double supersamplingScale = std::sqrt(5.0);
        // The pixels around the sampling radius that the blur of the occlusion and the supersampling filter reach
        const int tileGuardMargin = 8;
        // A dual peel takes the nearest and the farthest layer left, so this sorts eight layers of translucent
        // faces; what is between them is blended unsorted
        const int maximumNumberOfPeels = 4;

        // The props of the base layer carry one of these keys while the translucent faces are sorted
        vtkInformationIntegerKey* GetPlainBlendingKey()
        {
            static vtkInformationIntegerKey* key = vtkInformationIntegerKey::MakeKey("PLAIN_BLENDING", "vtkNativeView");
            return key;
        }

        vtkInformationIntegerKey* GetDepthSortingKey()
        {
            static vtkInformationIntegerKey* key = vtkInformationIntegerKey::MakeKey("DEPTH_SORTING", "vtkNativeView");
            return key;
        }

        // Renders its delegate with the props that carry a key only
        class KeyedPass : public vtkRenderPass
        {
        public:
            static KeyedPass* New();
            vtkTypeMacro(KeyedPass, vtkRenderPass);

            void SetDelegate(vtkRenderPass* delegate, vtkInformationIntegerKey* key)
            {
                Delegate = delegate;
                RequiredKeys->Set(key, 1);
            }

            void Render(const vtkRenderState* state) override
            {
                vtkRenderState filtered(state->GetRenderer());
                filtered.SetPropArrayAndCount(state->GetPropArray(), state->GetPropArrayCount());
                filtered.SetFrameBuffer(state->GetFrameBuffer());
                filtered.SetRequiredKeys(RequiredKeys);
                Delegate->Render(&filtered);
                this->NumberOfRenderedProps = Delegate->GetNumberOfRenderedProps();
            }

            void ReleaseGraphicsResources(vtkWindow* window) override
            {
                if (Delegate != nullptr) Delegate->ReleaseGraphicsResources(window);
            }

        protected:
            KeyedPass() = default;
            ~KeyedPass() override = default;

        private:
            vtkSmartPointer<vtkRenderPass> Delegate;
            vtkNew<vtkInformation> RequiredKeys;
        };

        vtkStandardNewMacro(KeyedPass);

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
        // The passes need the OpenGL context, which a window that has not rendered does not have yet
        if (RenderWindow->GetNeverRendered()) return;
        //
        RenderWindow->MakeCurrent();
        UpdateBasePass();
    }

    View::BasePass View::CreateBasePass(bool ambientOcclusion, bool depthSorting)
    {
        BasePass basePass;
        vtkNew<vtkOpaquePass> opaquePass;
        vtkSmartPointer<vtkRenderPass> opaqueStep = opaquePass;
        if (ambientOcclusion)
        {
            basePass.Occlusion = vtkSmartPointer<vtkSSAOPass>::New();
            basePass.Occlusion->SetDelegatePass(opaquePass);
            basePass.Occlusion->SetBlur(true);
            opaqueStep = basePass.Occlusion;
        }
        vtkNew<vtkLightsPass> lightsPass;
        vtkNew<vtkRenderPassCollection> steps;
        steps->AddItem(lightsPass);
        steps->AddItem(opaqueStep);
        vtkNew<vtkTranslucentPass> translucentPass;
        if (depthSorting)
        {
            vtkNew<KeyedPass> plainPass;
            plainPass->SetDelegate(translucentPass, GetPlainBlendingKey());
            steps->AddItem(plainPass);
            //
            vtkSmartPointer<vtkDepthPeelingPass> peelingPass;
            vtkOpenGLRenderer* openGLRenderer = vtkOpenGLRenderer::SafeDownCast(Renderer);
            if (openGLRenderer != nullptr && openGLRenderer->IsDualDepthPeelingSupported())
                peelingPass = vtkSmartPointer<vtkDualDepthPeelingPass>::New();
            else peelingPass = vtkSmartPointer<vtkDepthPeelingPass>::New();
            vtkNew<vtkTranslucentPass> peeledPass;
            peelingPass->SetTranslucentPass(peeledPass);
            peelingPass->SetMaximumNumberOfPeels(maximumNumberOfPeels);
            peelingPass->SetOcclusionRatio(0);
            vtkNew<KeyedPass> sortedPass;
            sortedPass->SetDelegate(peelingPass, GetDepthSortingKey());
            steps->AddItem(sortedPass);
        }
        else steps->AddItem(translucentPass);
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
        basePass.Pass = pass;
        return basePass;
    }

    // The edge lines of the parts are translucent always, and blended in the order of their actors they look the
    // same as sorted, so only a translucent face asks for the sorting
    bool View::HasTranslucentFaces()
    {
        vtkActorCollection* actors = Renderer->GetActors();
        vtkCollectionSimpleIterator iterator;
        actors->InitTraversal(iterator);
        for (vtkActor* actor = actors->GetNextActor(iterator); actor != nullptr;
             actor = actors->GetNextActor(iterator))
        {
            if (!actor->GetVisibility() || actor->GetMapper() == nullptr) continue;
            if (actor->GetProperty()->GetRepresentation() != VTK_SURFACE) continue;
            if (actor->GetProperty()->GetOpacity() >= 1) continue;
            //
            vtkPolyData* polyData = vtkPolyData::SafeDownCast(actor->GetMapper()->GetInputDataObject(0, 0));
            if (polyData == nullptr || polyData->GetNumberOfPolys() > 0 || polyData->GetNumberOfStrips() > 0)
                return true;
        }
        return false;
    }

    // The edge lines of an opaque part lie on its opaque faces, so they are blended before the translucent faces
    // are peeled and are not drawn again for every peel; everything else is sorted
    void View::MarkPropsForDepthSorting()
    {
        std::set<vtkProp*> plainProps;
        for (PartActor* actor : GetBaseActors())
        {
            vtkActor* geometry = actor->GetGeometry();
            if (geometry->GetVisibility() && geometry->GetProperty()->GetOpacity() < 1) continue;
            //
            for (vtkActor* edges : { actor->GetElementEdges(), actor->GetModelEdges(), actor->GetSectionViewHatch() })
                if (edges != nullptr) plainProps.insert(edges);
        }
        // A batch of actors that look the same is drawn in the place of its members
        std::vector<std::pair<vtkProp*, vtkProp*>> batches;
        Batches.GetBatches(batches);
        std::vector<vtkProp*> drawn;
        for (const auto& batch : batches)
        {
            if (plainProps.count(batch.second) > 0) plainProps.insert(batch.first);
            drawn.push_back(batch.first);
        }
        vtkPropCollection* props = Renderer->GetViewProps();
        vtkCollectionSimpleIterator iterator;
        props->InitTraversal(iterator);
        for (vtkProp* prop = props->GetNextProp(iterator); prop != nullptr; prop = props->GetNextProp(iterator))
            drawn.push_back(prop);
        for (vtkProp* prop : drawn)
        {
            bool plain = plainProps.count(prop) > 0;
            vtkInformationIntegerKey* key = plain ? GetPlainBlendingKey() : GetDepthSortingKey();
            vtkInformationIntegerKey* otherKey = plain ? GetDepthSortingKey() : GetPlainBlendingKey();
            if (prop->GetPropertyKeys() == nullptr)
            {
                vtkNew<vtkInformation> keys;
                prop->SetPropertyKeys(keys);
            }
            vtkInformation* keys = prop->GetPropertyKeys();
            if (!keys->Has(key)) keys->Set(key, 1);
            if (keys->Has(otherKey)) keys->Remove(otherKey);
        }
    }

    // The passes of the base layer by what it draws now, before every render of it. Their framebuffers are several
    // times the size of the window, so those of a pass that is no longer used are released
    void View::UpdateBasePass()
    {
        if (PickingWithoutBasePass) return;
        //
        vtkRenderPass* pass = nullptr;
        SsaoPass = nullptr;
        bool depthSorting = HasTranslucentFaces();
        if (depthSorting) MarkPropsForDepthSorting();
        if (AmbientOcclusion || depthSorting)
        {
            BasePass& basePass = BasePasses[AmbientOcclusion ? 1 : 0][depthSorting ? 1 : 0];
            if (basePass.Pass == nullptr) basePass = CreateBasePass(AmbientOcclusion, depthSorting);
            pass = basePass.Pass;
            SsaoPass = basePass.Occlusion;
        }
        if (SsaoPass != nullptr) SsaoPass->SetKernelSize(static_cast<unsigned int>(AmbientOcclusionKernelSize));
        vtkSmartPointer<vtkRenderPass> previousPass = Renderer->GetPass();
        if (previousPass != pass)
        {
            Renderer->SetPass(pass);
            if (previousPass != nullptr) previousPass->ReleaseGraphicsResources(RenderWindow);
        }
        // vtkSSAOPass clears its own framebuffer only when this flag is set
        Renderer->SetUseSSAO(AmbientOcclusion);
        UpdateAmbientOcclusionRadius();
        ApplySupersampledPixelSizes();
    }

    // A magnified picture is rendered in tiles of its own when the base layer has a pass, which is known only
    // once the pass follows the scene
    void View::UpdateBasePassForPicture()
    {
        if (RenderWindow->GetNeverRendered()) RenderWindow->Render();
        RenderWindow->MakeCurrent();
        UpdateBasePass();
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
        bool supersampled = Renderer->GetPass() != nullptr;
        double scale = supersampled ? supersamplingScale : 1;
        float minimum = supersampled ? 1.0f : 0.0f;
        std::map<vtkProperty*, SupersampledSizes> sizes;
        // The sizes are not a change of what an actor draws: one that is as it was added stays so (AddActor)
        std::vector<PartActor*> asAdded;
        bool looked = false;
        auto lookForAsAdded = [&]() {
            if (looked) return;
            looked = true;
            for (const auto& entry : BaseActors)
                if (entry.second->IsAsAdded()) asAdded.push_back(entry.second.get());
        };
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
            if (lineWidth != size.AppliedLineWidth || pointSize != size.AppliedPointSize) lookForAsAdded();
            if (lineWidth != size.AppliedLineWidth) property->SetLineWidth(size.AppliedLineWidth);
            if (pointSize != size.AppliedPointSize) property->SetPointSize(size.AppliedPointSize);
            sizes[property] = size;
        }
        for (PartActor* actor : asAdded) actor->MarkAsAdded();
        if (supersampled) SupersampledPixelSizes = sizes;
        else SupersampledPixelSizes.clear();
    }

    // Hardware picking renders the ids of the actors through the passes of the renderer. Supersampling would blend
    // and ambient occlusion would darken those ids, so a pick uses the plain passes. The parts are drawn opaque for
    // a pick, so the pass must not follow the scene meanwhile, and the line widths stay as they are: switching them
    // back and forth for every pick of a mouse move would rebuild the line shaders each time
    void View::PickWithoutBasePass(const std::function<void()>& pick)
    {
        vtkSmartPointer<vtkRenderPass> pass = Renderer->GetPass();
        bool useSsao = Renderer->GetUseSSAO();
        auto restore = [&]() {
            Renderer->SetPass(pass);
            Renderer->SetUseSSAO(useSsao);
            PickingWithoutBasePass = false;
        };
        PickingWithoutBasePass = true;
        Renderer->SetPass(nullptr);
        Renderer->SetUseSSAO(false);
        try
        {
            pick();
        }
        catch (...)
        {
            restore();
            throw;
        }
        restore();
    }

    // Screen space ambient occlusion sees each tile of a magnified picture on its own, so the tiles of
    // vtkWindowToImageFilter come out with darker or lighter borders. The tiles are rendered here the way that filter
    // renders them, by the tile viewport of the window and the window center and scale of the cameras, but
    // overlapping by the sampling radius, and only the part of a tile that the border cannot reach is kept. A
    // radius of more than a quarter of the window is cut to that, which may leave the borders of the tiles visible.
    // Without the occlusion the tiles overlap by the reach of the supersampling filter only
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
        int guard = tileGuardMargin;
        if (SsaoPass != nullptr) guard += static_cast<int>(std::ceil(SsaoPass->GetRadius() * pixelsPerUnit));
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
