// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeActorBatches.h"

#include <vtkAppendPolyData.h>
#include <vtkCellData.h>
#include <vtkCullerCollection.h>
#include <vtkInformation.h>
#include <vtkInformationIntegerKey.h>
#include <vtkMapper.h>
#include <vtkNew.h>
#include <vtkObjectFactory.h>
#include <vtkPlaneCollection.h>
#include <vtkPointData.h>
#include <vtkPolyData.h>
#include <vtkPolyDataMapper.h>
#include <vtkPropCollection.h>
#include <vtkProperty.h>
#include <vtkRenderWindow.h>
#include <vtkShaderProperty.h>
#include <vtkTransform.h>
#include <vtkTransformFilter.h>
#include <vtkTrivialProducer.h>

#include <map>
#include <sstream>
#include <unordered_map>

namespace vtkn
{
    namespace
    {
        vtkInformationIntegerKey* GetAllowedKey()
        {
            static vtkInformationIntegerKey* key = vtkInformationIntegerKey::MakeKey("BATCH_ALLOWED", "vtkNativeView");
            return key;
        }

        // Draws the batches in the place of their members: in what the renderer is about to draw, the first member of
        // a batch gives its place to the actor of the batch and the other members are left out. The batches are not
        // props of the renderer, so its order of drawing stays the order of its actors: of two faces that lie on each
        // other the one drawn later is seen
        class MemberCuller : public vtkCuller
        {
        public:
            static MemberCuller* New();
            vtkTypeMacro(MemberCuller, vtkCuller);

            // The actor of the batch for its first member, null for its other members
            std::unordered_map<vtkProp*, vtkProp*> BatchOfMember;

            double Cull(vtkRenderer*, vtkProp** propList, int& listLength, int& initialized) override
            {
                int kept = 0;
                double totalTime = 0;
                for (int i = 0; i < listLength; i++)
                {
                    vtkProp* prop = propList[i];
                    auto member = BatchOfMember.find(prop);
                    if (member != BatchOfMember.end())
                    {
                        if (member->second == nullptr) continue;
                        prop = member->second;
                    }
                    propList[kept++] = prop;
                    totalTime += initialized ? prop->GetRenderTimeMultiplier() : 1.0;
                }
                listLength = kept;
                return totalTime;
            }

        protected:
            MemberCuller() = default;
            ~MemberCuller() override = default;
        };

        vtkStandardNewMacro(MemberCuller);

        // The mesh an actor draws, when the actor was given it as it is: the output of a filter, such as the glyphs
        // of a symbol that follow the zoom, changes without the actor knowing
        vtkPolyData* GetMesh(vtkActor* actor)
        {
            vtkMapper* mapper = actor->GetMapper();
            if (mapper == nullptr || mapper->GetNumberOfInputConnections(0) != 1) return nullptr;
            if (vtkTrivialProducer::SafeDownCast(mapper->GetInputAlgorithm(0, 0)) == nullptr) return nullptr;
            return vtkPolyData::SafeDownCast(mapper->GetInputDataObject(0, 0));
        }

        void Mix(std::uint64_t& stamp, std::uint64_t value)
        {
            stamp ^= value + 0x9e3779b97f4a7c15ull + (stamp << 6) + (stamp >> 2);
        }

        // What an actor is drawn with, as a text: two actors of the same text look the same and can be drawn as one
        // mesh. An actor that must be drawn by itself has no text: one that is picked, turned, scaled or textured,
        // one colored by the values of its mesh, one with a color of its own for its back faces, and one with
        // translucent faces, which are blended by their depth among the others
        bool GetLook(vtkActor* actor, std::string& look)
        {
            vtkPolyData* mesh = GetMesh(actor);
            vtkMapper* mapper = actor->GetMapper();
            if (mesh == nullptr || mesh->GetNumberOfCells() == 0) return false;
            if (actor->GetPickable() || actor->GetTexture() != nullptr || actor->GetBackfaceProperty() != nullptr ||
                actor->GetUserMatrix() != nullptr)
                return false;
            const double* orientation = actor->GetOrientation();
            const double* scale = actor->GetScale();
            if (orientation[0] != 0 || orientation[1] != 0 || orientation[2] != 0 || scale[0] != 1 || scale[1] != 1 ||
                scale[2] != 1)
                return false;
            int cellFlag = 0;
            if (mapper->GetScalarVisibility() &&
                vtkAbstractMapper::GetAbstractScalars(mesh, mapper->GetScalarMode(), mapper->GetArrayAccessMode(),
                                                      mapper->GetArrayId(), mapper->GetArrayName(), cellFlag) != nullptr)
                return false;
            vtkProperty* property = actor->GetProperty();
            bool faces = mesh->GetNumberOfPolys() > 0 || mesh->GetNumberOfStrips() > 0;
            if (faces && property->GetOpacity() < 1) return false;
            //
            std::ostringstream text;
            text.precision(17);
            text << property->GetRepresentation() << ' ' << property->GetInterpolation() << ' '
                 << property->GetAmbient() << ' ' << property->GetDiffuse() << ' ' << property->GetSpecular() << ' '
                 << property->GetSpecularPower() << ' ' << property->GetOpacity() << ' ' << property->GetLineWidth()
                 << ' ' << property->GetPointSize() << ' ' << property->GetBackfaceCulling() << ' '
                 << property->GetFrontfaceCulling() << ' ' << property->GetLighting() << ' '
                 << property->GetEdgeVisibility() << ' ' << property->GetVertexVisibility() << ' '
                 << property->GetRenderPointsAsSpheres() << ' ' << property->GetRenderLinesAsTubes();
            for (const double* color : { property->GetAmbientColor(), property->GetDiffuseColor(),
                                         property->GetSpecularColor(), property->GetEdgeColor() })
                text << ' ' << color[0] << ' ' << color[1] << ' ' << color[2];
            double factor = 0;
            double units = 0;
            mapper->GetRelativeCoincidentTopologyPolygonOffsetParameters(factor, units);
            text << " offsets " << factor << ' ' << units;
            mapper->GetRelativeCoincidentTopologyLineOffsetParameters(factor, units);
            text << ' ' << factor << ' ' << units;
            mapper->GetRelativeCoincidentTopologyPointOffsetParameter(units);
            text << ' ' << units;
            // The point data of the appended mesh is what every member has
            text << " normals " << (mesh->GetPointData()->GetNormals() != nullptr) << ' '
                 << (mesh->GetCellData()->GetNormals() != nullptr);
            text << " planes";
            vtkPlaneCollection* planes = mapper->GetClippingPlanes();
            if (planes != nullptr)
            {
                vtkCollectionSimpleIterator iterator;
                planes->InitTraversal(iterator);
                for (vtkPlane* plane = planes->GetNextPlane(iterator); plane != nullptr;
                     plane = planes->GetNextPlane(iterator))
                    text << ' ' << static_cast<const void*>(plane);
            }
            vtkShaderProperty* shader = actor->GetShaderProperty();
            text << " shader " << shader->HasVertexShaderCode() << shader->HasFragmentShaderCode()
                 << shader->HasGeometryShaderCode();
            for (int i = 0; i < shader->GetNumberOfShaderReplacements(); i++)
            {
                std::string name;
                std::string value;
                bool replaceFirst = false;
                bool replaceAll = false;
                shader->GetNthShaderReplacement(i, name, replaceFirst, value, replaceAll);
                text << '|' << shader->GetNthShaderReplacementTypeAsString(i) << '|' << name << '|' << replaceFirst
                     << '|' << value << '|' << replaceAll;
            }
            look = text.str();
            return true;
        }
    }

    ActorBatches::ActorBatches()
    {
        Culler = vtkSmartPointer<MemberCuller>::New();
    }

    ActorBatches::~ActorBatches()
    {
        if (Renderer != nullptr) Renderer->RemoveCuller(Culler);
    }

    // The culler of the members runs before the cullers of the renderer, which measure the bounds of every prop
    void ActorBatches::SetRenderer(vtkRenderer* renderer)
    {
        Renderer = renderer;
        vtkCullerCollection* cullers = Renderer->GetCullers();
        std::vector<vtkSmartPointer<vtkCuller>> others;
        vtkCollectionSimpleIterator iterator;
        cullers->InitTraversal(iterator);
        for (vtkCuller* culler = cullers->GetNextCuller(iterator); culler != nullptr;
             culler = cullers->GetNextCuller(iterator))
            others.push_back(culler);
        cullers->RemoveAllItems();
        cullers->AddItem(Culler);
        for (const auto& culler : others) cullers->AddItem(culler);
    }

    void ActorBatches::Allow(vtkActor* actor)
    {
        if (actor == nullptr) return;
        if (actor->GetPropertyKeys() == nullptr)
        {
            vtkNew<vtkInformation> keys;
            actor->SetPropertyKeys(keys);
        }
        actor->GetPropertyKeys()->Set(GetAllowedKey(), 1);
    }

    // The visible allowed actors of the renderer in its order, each with a number that changes when the actor, its
    // property, its mapper or its mesh is modified
    std::uint64_t ActorBatches::GetAllowed(std::vector<Allowed>& allowed) const
    {
        std::uint64_t stamp = 0;
        vtkPropCollection* props = Renderer->GetViewProps();
        vtkCollectionSimpleIterator iterator;
        props->InitTraversal(iterator);
        for (vtkProp* prop = props->GetNextProp(iterator); prop != nullptr; prop = props->GetNextProp(iterator))
        {
            vtkInformation* keys = prop->GetPropertyKeys();
            if (keys == nullptr || !keys->Has(GetAllowedKey()) || !prop->GetVisibility()) continue;
            vtkActor* actor = vtkActor::SafeDownCast(prop);
            vtkPolyData* mesh = actor != nullptr ? GetMesh(actor) : nullptr;
            if (mesh == nullptr) continue;
            //
            Allowed entry = { actor, 0 };
            Mix(entry.Stamp, reinterpret_cast<std::uintptr_t>(actor));
            Mix(entry.Stamp, actor->GetMTime());
            Mix(entry.Stamp, actor->GetMapper()->GetMTime());
            Mix(entry.Stamp, mesh->GetMTime());
            Mix(entry.Stamp, actor->GetShaderProperty()->GetMTime());
            allowed.push_back(entry);
            Mix(stamp, entry.Stamp);
        }
        return stamp;
    }

    void ActorBatches::Update()
    {
        if (Renderer == nullptr || !Invalidated) return;
        Invalidated = false;
        std::vector<Allowed> allowed;
        std::uint64_t stamp = GetAllowed(allowed);
        if (stamp == Stamp) return;
        //
        Build(allowed);
        Stamp = stamp;
    }

    // Every allowed actor with a look is drawn by a batch, also one that shares its look with no other: drawn one by
    // one, the later of two faces that lie on each other was seen because their depths were the same to the bit, and
    // they are the same again only when the vertices of both are stored the same way, as the batches store them. A
    // batch whose members did not change is kept
    void ActorBatches::Build(const std::vector<Allowed>& allowed)
    {
        struct Group
        {
            std::string Look;
            std::vector<vtkActor*> Actors;
            std::uint64_t Stamp = 0;
        };
        std::vector<Group> groups;
        std::map<std::string, size_t> groupOfLook;
        std::string look;
        // The look of an actor is read again only when the actor changed: reading the looks of thousands of actors
        // takes longer than drawing them
        std::unordered_map<vtkProp*, Look> looks;
        for (const Allowed& entry : allowed)
        {
            Look& current = looks[entry.Actor];
            auto known = Looks.find(entry.Actor);
            if (known != Looks.end() && known->second.Stamp == entry.Stamp) current = std::move(known->second);
            else
            {
                current.Stamp = entry.Stamp;
                current.Batched = GetLook(entry.Actor, current.Text);
            }
            if (!current.Batched) continue;
            look = current.Text;
            // Drawn one by one, an actor is the only one of its group
            if (!Enabled) look += " actor " + std::to_string(groups.size());
            auto found = groupOfLook.find(look);
            if (found == groupOfLook.end())
            {
                found = groupOfLook.insert({ look, groups.size() }).first;
                groups.emplace_back();
                groups.back().Look = look;
            }
            Group& group = groups[found->second];
            group.Actors.push_back(entry.Actor);
            Mix(group.Stamp, entry.Stamp);
        }
        //
        Looks = std::move(looks);
        std::multimap<std::uint64_t, Batch> kept;
        for (Batch& batch : Batches) kept.insert({ batch.Stamp, batch });
        Batches.clear();
        MemberCuller* culler = static_cast<MemberCuller*>(Culler.Get());
        culler->BatchOfMember.clear();
        for (const Group& group : groups)
        {
            Batch batch;
            auto range = kept.equal_range(group.Stamp);
            for (auto entry = range.first; entry != range.second; ++entry)
            {
                if (entry->second.Look != group.Look) continue;
                batch = entry->second;
                kept.erase(entry);
                break;
            }
            if (batch.Actor == nullptr)
            {
                batch = CreateBatch(group.Actors);
                batch.Look = group.Look;
                batch.Stamp = group.Stamp;
            }
            for (vtkProp* member : batch.Members) culler->BatchOfMember[member] = nullptr;
            culler->BatchOfMember[batch.Members.front()] = batch.Actor;
            Batches.push_back(batch);
        }
        for (const auto& entry : kept) entry.second.Actor->ReleaseGraphicsResources(Renderer->GetRenderWindow());
    }

    ActorBatches::Batch ActorBatches::CreateBatch(const std::vector<vtkActor*>& actors) const
    {
        // The exploded view moves an actor by its position
        auto moved = [](vtkActor* actor) {
            const double* position = actor->GetPosition();
            return position[0] != 0 || position[1] != 0 || position[2] != 0;
        };
        Batch batch;
        batch.Actor = vtkSmartPointer<vtkActor>::New();
        vtkActor* first = actors.front();
        vtkMapper* firstMapper = first->GetMapper();
        vtkNew<vtkPolyDataMapper> mapper;
        if (actors.size() == 1 && !moved(first)) mapper->SetInputData(GetMesh(first));
        else
        {
            vtkNew<vtkAppendPolyData> append;
            for (vtkActor* actor : actors)
            {
                vtkPolyData* mesh = GetMesh(actor);
                if (!moved(actor))
                {
                    append->AddInputData(mesh);
                    continue;
                }
                vtkNew<vtkTransform> transform;
                transform->Translate(actor->GetPosition());
                vtkNew<vtkTransformFilter> movedMesh;
                movedMesh->SetInputData(mesh);
                movedMesh->SetTransform(transform);
                movedMesh->Update();
                append->AddInputData(vtkPolyData::SafeDownCast(movedMesh->GetOutput()));
            }
            append->Update();
            vtkNew<vtkPolyData> mesh;
            mesh->ShallowCopy(append->GetOutput());
            mapper->SetInputData(mesh);
        }
        mapper->ScalarVisibilityOff();
        mapper->SetClippingPlanes(firstMapper->GetClippingPlanes());
        // VTK shifts and scales the coordinates of a mesh by its own bounds before it stores them as floats, which
        // rounds the same vertex differently in two meshes. The batches store the coordinates as they are
        mapper->SetVBOShiftScaleMethod(vtkPolyDataMapper::DISABLE_SHIFT_SCALE);
        double factor = 0;
        double units = 0;
        firstMapper->GetRelativeCoincidentTopologyPolygonOffsetParameters(factor, units);
        mapper->SetRelativeCoincidentTopologyPolygonOffsetParameters(factor, units);
        firstMapper->GetRelativeCoincidentTopologyLineOffsetParameters(factor, units);
        mapper->SetRelativeCoincidentTopologyLineOffsetParameters(factor, units);
        firstMapper->GetRelativeCoincidentTopologyPointOffsetParameter(units);
        mapper->SetRelativeCoincidentTopologyPointOffsetParameter(units);
        // The property is the one of a member, so that what a view does with the properties of the renderer, such
        // as the line widths of a supersampled render, applies to the batch as to its members
        batch.Actor->SetMapper(mapper);
        batch.Actor->SetProperty(first->GetProperty());
        batch.Actor->SetShaderProperty(first->GetShaderProperty());
        batch.Actor->PickableOff();
        for (vtkActor* actor : actors) batch.Members.push_back(actor);
        return batch;
    }

    void ActorBatches::GetBatches(std::vector<std::pair<vtkProp*, vtkProp*>>& batches) const
    {
        for (const Batch& batch : Batches) batches.push_back({ batch.Actor.Get(), batch.Members.front() });
    }

    void ActorBatches::SetEnabled(bool enabled)
    {
        if (enabled == Enabled) return;
        Enabled = enabled;
        Stamp = 0;
        Invalidated = true;
    }

    void ActorBatches::GetCounts(int& batchCount, int& memberCount) const
    {
        batchCount = 0;
        memberCount = 0;
        for (const Batch& batch : Batches)
        {
            if (batch.Members.size() < 2) continue;
            batchCount++;
            memberCount += static_cast<int>(batch.Members.size());
        }
    }
}
