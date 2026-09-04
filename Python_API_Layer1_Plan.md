# Layer 1 - the automation API

Working-level plan for the Layer 1 step of [Python_Integration_Plan.md](Python_Integration_Plan.md).
Layer 0 is implemented; this layer gives an unattended run something to say.

## Baseline

Facts established by reading the current code. They are the reason this layer is high volume but low
risk.

- **The command surface is uniform.** Almost every area exposes the same four verbs:
  `Add<X>Command(X)`, `Replace<X>Command(oldName, X)`, `Duplicate<X>sCommand(string[])`,
  `Remove<X>sCommand(string[])`. Step-scoped areas prefix a `stepName` argument and add
  `Propagate`, `Hide` and `Show`. One generic collection class can drive all of them.
- **The selection machinery is already model-aware.** `SelectionService.CreateByIds` accepts the
  `geometry` and `femodel` views today, and `IPythonSelectionGateway.GetMesh(view)` resolves their
  meshes. Only `SelectionService.Compile(...)` is private behind the results-only
  `CompileForResult`; it needs a model sibling, not a rewrite.
- **Selection-built regions regenerate for free, and the controller creates the sets.** `FeNodeSet`
  and `FeSurface` carry `Selection CreationData`, and `Controller.AddNodeSet` calls
  `ReselectNodeSet(nodeSet)` before storing. Region-bearing items go further: with
  `RegionType = RegionTypeEnum.Selection`, `Controller.AddLoad` and its siblings call
  `ConvertSelectionBased*` to create an `Internal` set named after the item. The API supplies the
  compiled selection and creates nothing.
- **Unit-aware values need no new machinery.** Model constructors already build their
  `EquationContainer`s with the right converter type, e.g.
  `F1 = new EquationContainer(typeof(StringForceConverter), f1, null, constant)`. The service passes
  plain doubles into the existing constructor and assigns `.Equation` afterwards for expressions.
- **`APIs.csproj` references only `CaeGlobals`, `CaeMesh`, `CaeResults`.** `CaeModel` and `CaeJob`
  are missing, and `PrePoMax` must stay unreferenced.

### The real type count

The 150 figure in the strategic plan counted `View*` wrappers. The creatable **model** types are
fewer, which is what makes a typed method per type affordable:

| Area | Creatable types | Names |
|---|---|---|
| Material properties | 8 | Density, Elastic, ElasticWithDensity, Plastic, SlipWear, SpecificHeat, ThermalConductivity, ThermalExpansion |
| Sections | 6 | Solid, Shell, Membrane, Mass, PointMass, DistributedMass |
| Constraints | 9 | CompressionOnly, Coupling, CyclicSymmetry, DistributingCoupling, KinematicCoupling, PointSpring, RigidBody, SurfaceSpring, Tie |
| Surface interaction | 4 | SurfaceInteraction + Friction, GapConductance, SurfaceBehavior |
| Distributions | 3 | FromEquation, FromFile, Mapped |
| Amplitudes | 1 | Tabular |
| Initial conditions | 3 | AngularVelocity, Temperature, TranslationalVelocity |
| Steps | 12 | Static, Frequency, Buckle, ComplexFrequency, Dynamic, ModalDynamics, SteadyStateDynamics, HeatTransfer, CoupledTempDisp, UncoupledTempDisp, SlipWear, BoundaryDisplacement |
| Boundary conditions | 4 | DisplacementRotation, FixedBC, SubmodelBC, TemperatureBC |
| Loads | 17 | BodyFlux, CentrifLoad, CFlux, CLoad, DFlux, DLoad, FilmHeatTransfer, GravityLoad, HydrostaticPressure, ImportedPressure, ImportedSTLoad, MomentLoad, PreTensionLoad, RadiationHeatTransfer, ShellEdgeLoad, STLoad, VariablePressure |
| Defined fields | 1 | DefinedTemperature |
| Field / history outputs | 7 | Nodal, Element, Contact (+ Section history) |
| Mesh setup items | 6 | MeshingParameters, LocalMeshSize, ThickenShellMesh, GmshSetupItem, ShellQuadWild, FeMeshRefinement |
| Regions and misc | ~8 | NodeSet, ElementSet, Surface, ReferencePoint, CoordinateSystem, ContactPair, Job, Parameter |

**About 88 `add_*` methods plus ~17 `pmx.create` part makers, across ~20 collection classes and ~9
services.** There are no draft classes: a model type contributes a method, not a class.

## Architecture

### Folder layout

Mirrors the existing `APIs/Python/Results` structure.

Namespaces follow the folders: everything under `Model/` is `APIs.Python.Model`, the rest is
`APIs.Python`.

```
APIs/Python/
  Commands/          IPythonCommandGateway (results, exists)
                     IPythonModelGateway, IPythonMeshGateway, IPythonJobGateway   (new)
  Common/            IMeshResolver                                    (new, view-agnostic)
                     MeshResolver - composed next to PrePoMaxAPI      (new)
  Selection/         SelectionService, SelectionAPI, SelectionFactoryAPI
                     (exists; decoupled from ResultResolver in 1.0)
  Create/            CreateAPI - the ~17 part makers, one flat namespace       (new)
  Model/
    Common/          ModelItemsAPI, StepItemsAPI, ModelResolver, RegionAPI
    Document/        DocumentAPI, ParametersAPI, ModelPropertiesAPI
    Geometry/        PartsAPI, PartAPI, GeometryService
    Meshing/         MeshSetupItemsAPI, MeshingService
    Regions/         NodeSetsAPI, ElementSetsAPI, SurfacesAPI, ReferencePointsAPI,
                     CoordinateSystemsAPI, RegionService
    Materials/       MaterialsAPI, MaterialService
    Sections/        SectionsAPI, SectionService
    Fields/          AmplitudesAPI, DistributionsAPI, InitialConditionsAPI
    Interactions/    ConstraintsAPI, SurfaceInteractionsAPI, ContactPairsAPI
    Steps/           StepsAPI, StepAPI
      Loads/         LoadsAPI       - 17 add_* / replace_* pairs
      Bcs/           BoundaryConditionsAPI
      Outputs/       FieldOutputsAPI, HistoryOutputsAPI, DefinedFieldsAPI
  Jobs/              JobsAPI, JobAPI, JobService
```

### Gateways

`IPythonCommandGateway` stays as the results gateway. Three new interfaces keep any one of them
readable, each implemented next to the existing `ControllerPythonCommandGateway`:

- `IPythonModelGateway` - document, parameters, model properties, regions, materials, sections,
  fields, interactions, steps and step content. The bulk.
- `IPythonMeshGateway` - geometry parts, mesh setup items, meshing, mesh tools.
- `IPythonJobGateway` - job create/replace/remove/run/kill and result opening.

### Generic collections

`ModelItemsAPI<TItem>` supplies the verbs that do not depend on the item type:

```
get_names()  contains(name)  get(name)  first()  last()
__len__()    __getitem__(name)  __iter__()
duplicate(names)   remove(names)   rename(old, new)
```

`StepItemsAPI<TItem>` extends it with the step-scoped variants and `propagate(name)`, `hide(names)`,
`show(names)`.

### `add_*` commits an item, `pmx.create` makes a part

**There is no draft object.** A model item whose arguments are all plain values is built, validated
and added in one call:

```python
step.loads.add_cload('Load-1', selection, f1=0, f2=-150, f3=0)
```

But some model items are **composed of other objects**. Those parts are arguments, so they must be
cheap to write - a part maker never sits behind the collection that happens to consume it. All of
them live in one flat, short namespace, exactly as a selection already does:

| Verb | Meaning | Records a command? |
|---|---|---|
| `<collection>.add_<type>(...)` / `.add(...)` | builds a model item and puts it in the model | Yes |
| `pmx.create.<part>(...)` | builds a detached part to pass as an argument | No |

**The parts.** About 17 makers in total - small enough for one flat namespace, and every name is
unambiguous on its own:

| Part | Consumed by | `pmx.create` members |
|---|---|---|
| Selection | almost every region-bearing item | `selection_by_ids` (mirrors the existing `pmx.selection.create_by_ids`) |
| `MaterialProperty` | `Material.Properties` | 8 - `elastic`, `density`, `plastic`, `thermal_conductivity`, ... |
| `SurfaceInteractionProperty` | `SurfaceInteraction.Properties` | 3 - `friction`, `gap_conductance`, `surface_behavior` |
| `StepControlParameter` | `StepControls.Parameters`, set by `ReplaceStepControlsCommand` | 4 - `contact_control`, `field_control`, `reset_control`, `time_incrementation_control` |
| `ModalDamping` | `ModalDynamicsStep.ModalDamping` | 1, carrying a list of ratio/range rows |

```python
c = pmx.create

pmx.model.materials.add('Steel', [c.elastic(210000, 0.3), c.density(7.85e-9)])
pmx.model.surface_interactions.add('SI-1', [c.friction(0.2),
                                            c.surface_behavior(pressure_overclosure='hard')])
```

`pmx.selection` keeps `create_by_ids`, `highlight` and `clear_highlight` - highlighting is not
creation and belongs there - so the existing published name goes on working.

Note the difference from a factory tree for *everything*, which was considered and rejected:
`pmx.create` holds only the ~17 parts, never the ~88 model items. A model item is committed by
`add_*` on its collection, so there is exactly one way to reach it.

Building a composed item in one `add` matters for more than looks: adding the parts one at a time
would record an Add plus one Replace per part in the command history.

Remaining rules:

- The method is `add_<type>` where the area has several creatable types, and plain `add` where it has
  exactly one - jobs, amplitudes, node sets, element sets, surfaces, reference points, coordinate
  systems, parameters, defined fields, materials, surface interactions.
- `replace_<type>(old_name, ...)` mirrors `add_<type>` argument for argument and shares the same
  private builder in the service, so the two cannot drift apart.
- Anything that is not a part is a **plain value**: a number, a string, a bool, or a list of rows for
  a tabular property such as an amplitude table or a plastic curve.

This drops the ~88 draft classes and the `CreateBase()` indirection; a model type now contributes an
`add_*`/`replace_*` pair plus one builder in the service, and a part type contributes one `pmx.create`
member returning a light immutable wrapper. The trade is that collections are hand-written per area
rather than generic instantiations, which is the smaller cost of the two.

Reading back is separate from editing. `step.loads['Load-1']` returns a read-only item handle for
inspection; changing an item goes through `replace_<type>`, because a replacement is what the command
history records.

### Selection is not part of results

Selection is a shared service used by results, regions and every region-bearing model item, so it
must not depend on the results side. Today `SelectionService` holds a `ResultResolver` and uses it in
exactly one place - `ResolveMesh`, for the `results` view. The `geometry` and `femodel` views already
go through `IPythonSelectionGateway.GetMesh(view)`, and `Compile(...)` itself is view-agnostic.

The dependency is inverted with one small interface:

```
IMeshResolver          FeMesh GetMesh(string view, string resultName)
MeshResolver           holds ResultResolver and the selection gateway; dispatches by view
SelectionService       takes IMeshResolver, never ResultResolver
```

`MeshResolver` is composed in `PrePoMaxAPI`, which is the level that legitimately knows about both
results and the model. `SelectionService` then exposes one public entry per direction:

```
CompileForView(selection, view, target)        model and geometry
CompileForResult(selection, resultName, target)   kept as a thin wrapper, existing callers unchanged
```

Do this **before** adding the model compile path, not after; otherwise the model side inherits the
results dependency and it never gets removed.

### Regions - a selection is the native form

`RegionTypeEnum.Selection` is a first-class region type, and **the controller already converts it**.
`Controller.AddLoad` calls `ConvertSelectionBasedLoad(load)`, which creates the backing set itself:

```csharp
name = FeMesh.GetNextFreeSelectionName(_model.Mesh.NodeSets, load.Name);
FeNodeSet nodeSet = new FeNodeSet(name, load.CreationIds);
nodeSet.CreationData = load.CreationData.DeepCopy(true, false);
nodeSet.Internal = true;
nodeSet.ParentMultiRegion = load;
```

The set is named after the item, flagged `Internal` so it never reaches the model tree, and linked
back through `ParentMultiRegion`. Sections, constraints, contact pairs and boundary conditions have
the same `ConvertSelectionBased*` path.

So the API sets three fields and hands the item to the existing command:

```
RegionType   = RegionTypeEnum.Selection
CreationData = compilation.Selection
CreationIds  = compilation.Ids
```

**The API must never create the internal set itself** - that would duplicate `ConvertSelectionBased*`
and drift from it. Passing a selection is the normal path; passing a named set or surface is the
special case, and `RegionAPI` supports both:

```python
top = pmx.selection.create_by_ids('femodel', 'surfaces', 'surfaces', {1: [3]})
step.loads.add_dload('P-1', top, 2.5)                                  # selection - normal
step.loads.add_dload('P-1', pmx.model.region('surface', 'Top'), 2.5)   # named region - special
```

### Values, equations and units

One rule, applied through a single helper:

- a Python **float/int** is a value in the current model units;
- a Python **str** is an equation, assigned to the `EquationContainer.Equation` after construction;
- an enum is a **string**, matching the `StandardValue` text used by the property grid.

### Namespaces - one per tree branch

The results side already occupies the plain names in `APIs.Python`: `MeshAPI`, `PartAPI`, `PartsAPI`,
`NodesAPI`, `ElementAPI`, `ElementsAPI`, `NodeDataAPI`, `ElementDataAPI` and the whole `Field*` family
carry no `Result` prefix. The model tree wants most of those names for its own types, and three
collisions appeared in the first three steps.

**Each branch of the tree gets its own namespace.** The C# namespace is invisible to Python - a user
never names a type, only reaches it through `pmx` - so this costs nothing at the API surface:

```
APIs.Python          BaseAPI, the converters, the gateways, the selection, the root object, pmx.create
APIs.Python.Model    the model tree
APIs.Python.Results  the results tree - moved at 1.10, not before
```

A nested namespace resolves its parent without a `using` directive, so a model file needs no extra
directive to reach `BaseAPI` or a converter. Only `PrePoMaxAPI`, which lives in the parent, needs
`using APIs.Python.Model;`.

With that in place the model types carry their natural names, and the three collision-driven names are
gone: `PartItemAPI` is `PartAPI`, `PartItemsAPI<T>` is `PartsAPI<T>`, `ModelMeshAPI` is `MeshAPI`, and
`CreatePartsAPI` is `CreateAPI`. A later step may add `NodesAPI` or `FieldOutputsAPI` without
consulting the results side at all.

### Naming

| C# | Python |
|---|---|
| `AddLoad(step, CLoad)` | `add_cload(...)` on the owning collection |
| `ReplaceLoad(step, old, CLoad)` | `replace_cload(old_name, ...)` |
| `RemoveLoads(step, names)` | `remove(names)` from the generic base |
| `FeNodeSet`, `CLoad`, `StaticStep` | `node_set`, `cload`, `static_step` |
| `RegionTypeEnum.NodeSetName` | `'node_set'` |
| property `MaxIncrementSize` | `max_increment_size` |

### Errors

Convert, validate, then call the gateway - never the reverse. A rejected call must leave nothing in
the command history. Domain failures raise `PythonAPIException`; argument failures raise it too, with
the Python argument name in the message.

---

## Step 1.0 - Foundations

**Gate. Nothing else starts until this is settled, because every later step repeats its decisions ~88
times.**

Deliverables:

1. Add `CaeModel` and `CaeJob` project references to `APIs.csproj`. Confirm no path to `PrePoMax`.
2. `ModelResolver` - the model-side counterpart of `ResultResolver`: resolves the model, mesh,
   geometry, step and named items, and raises consistent "does not exist" errors.
3. `ModelItemsAPI<TItem>` and `StepItemsAPI<TItem>` with the type-independent verb set above.
4. The service-side builder convention shared by `add_<type>` and `replace_<type>`, so no model type
   is constructed in two places.
5. The three gateway interfaces, with the document and region methods implemented; the rest are added
   per step.
6. **Decouple selection from results.** Introduce `IMeshResolver`/`MeshResolver`, change
   `SelectionService` to take the interface, and expose `CompileForView` alongside the existing
   `CompileForResult`. Existing result history-output callers must keep working unchanged. This lands
   before any model compile path exists.
7. `RegionAPI` on top of `CompileForView`.
8. The value/equation/unit helper and the enum-string helper.
9. `pmx.model` root wired into `PrePoMaxAPI`, and `PythonEngineManager.SetPrePoMaxAPI` extended to
   pass the model gateways. `PrePoMaxAPI` is re-rooted here: it currently takes a `ResultsCollection`,
   which makes the whole API a child of results, and it must take the gateways instead.

**Acceptance.** `PrePoMax.sln` builds. `Test_Result_History_Outputs.py` still passes, proving the
selection move broke nothing. A script creates a `femodel` selection, adds a node set from it, reads
the names back, renames it, and removes it - and the saved `.pmx` regenerates to the same model.

### Status - done

Implemented and verified against the built application with
`-f cube.inp -u MM_TON_S_C --script Test_Model_Node_Sets.py -g No -x Yes`, which passes with exit
code 0. Node sets were carried through as the reference vertical slice, so the generic base is proven
by a real consumer rather than written blind.

Delivered: `CaeModel` project reference; `IMeshResolver`/`MeshResolver` with `SelectionService` taking
the interface and exposing `CompileForView`; `ModelResolver`; `ModelItemsAPI<TItem, TItemAPI>`;
`IPythonModelGateway` and `ControllerPythonModelGateway`; `NodeSetService`, `NodeSetsAPI`,
`NodeSetAPI`; `ModelAPI` as `pmx.model`; `CreatePartsAPI` as `pmx.create`; `PrePoMaxAPI` re-rooted to
take the gateways.

Deliberate deviations, each because the deliverable had no consumer yet and would have shipped
untested:

- **`StepItemsAPI`, `RegionAPI`, the value/equation helper, `IPythonMeshGateway` and
  `IPythonJobGateway` are deferred** to the first step that consumes them - 1.8, 1.5, 1.5, 1.3 and
  1.9 respectively. Node sets take a selection directly and carry no `RegionType` or equation, so
  none of them could have been exercised here.
- **`CaeJob` is referenced at 1.9**, not now, for the same reason.

Two findings that changed the code:

- `len()` and iteration in this codebase come from the .NET collection interface, not from a method
  named `__len__` - `StringListAPI` implements `IReadOnlyList<string>`. `ModelItemsAPI` now implements
  `IReadOnlyList<TItemAPI>`, which also gives positional indexing and `for item in collection`.
- `Controller.RenameCommand` read `_currentView`, so a scripted rename depended on the view the user
  happened to leave behind. An overload taking the view explicitly was added, and the model gateway
  passes `ViewGeometryModelResults.Model`.

**Not verified: replay of a script-recorded command.** `CommandsCollection.WriteToFile` skips writing
while `BatchRegenerationMode` is set, so a `--script` run persists no `history.pmh` - correct
isolation, but it means the only route to a replayable file is a saved `.pmx`, which arrives with
step 1.1. The in-memory history is correct: the run report lists `Run Python script` among the
executed commands. Re-check this at 1.1.

Fixed in passing, completing Layer 0 step 0.5: `RunBatchScriptAsync` enabled batch mode only *after*
the `-f` file was opened, so the open commands were still written to the shared `history.pmh`. The
work directory is now set before the first command, and a script run leaves the interactive user's
recovery file untouched.

---

## Steps 1.1 - 1.9

Each step delivers a collection with its `add_*`/`replace_*` pairs, gateway methods, `[PythonAPI]`
metadata, a `Python_API_Overview.md` section and a `Test_Scripts` entry. Order follows how a model is built, so
each step is usable alone and the test scripts accumulate into one end-to-end workflow.

### 1.1 Document, parameters, model properties

Controller entry points: `OpenFileCommand`, `ImportFileCommand`, `SaveToPmxCommand`, the eight
`Export*Command`s, `SetNewModelPropertiesCommand(modelSpace, unitSystemType)`,
`ReplaceModelPropertiesCommand`, and the five parameter commands.

```python
pmx.new(model_space='three_d', unit_system='MM_TON_S_C')
pmx.open(r'C:\work\model.pmx')
pmx.import_file(r'C:\work\part.step')
pmx.model.properties.name = 'Bracket'
pmx.model.parameters.add('t', 2.5)
pmx.save(r'C:\work\model.pmx')
pmx.export_to_calculix(r'C:\work\model.inp')
```

**Why first:** without open/save nothing else can be tested end to end, and `--script` becomes
immediately useful.

### Status - done

Implemented and verified with `Test_Model_Document.py`, which passes with exit code 0. Delivered:
`pmx.new_model`, `pmx.open`, `pmx.import_file`, `pmx.save` and 8 `export_*` methods on the root;
`pmx.model.properties` (name, model space, unit system); `pmx.model.parameters` on the generic
collection base; `PythonValueConverter` as the shared number-or-equation helper.

Three things the code forced:

- **`open` and `new_model` raise inside a recorded script.** `Controller.OpenPmx` calls
  `InitializeNewCommands`, and `Controller.New` calls `_commands.Clear()` - both replace the command
  collection that the running script command belongs to. Scripts open their file with `-f`; the two
  methods stay available in the console.
- **A string argument is an equation and gets a leading `=`**, matching what
  `ResultFieldOutputService` already does. `PythonValueConverter` applies it once for the whole API.
- **Parameters have no duplicate command**, so `DuplicateItems` became virtual on `ModelItemsAPI`
  with a "not supported" default, and `rename` is carried out as a replacement. A renamed parameter
  is not rewritten inside equations that reference it - the same as in the dialogs.

**Replay is now verified**, closing the item left open at 1.0. `pmx.save` from inside a script cannot
produce a replayable file, because a script command is added to the history only after it completes.
A `--save <file.pmx>` switch was therefore added to the Layer 0 option set: it saves after the script
command is recorded. A scripted build then round-trips - `-r0 built.pmx` regenerates with
`Run Python script` in the history, re-executes it, and rebuilds the parameter and the node set.

Deferred: `export_to_3mf`, because `Export3mfFileProperties` lives in the `PrePoMax` project and needs
its fields passed as primitives through the gateway.

### 1.2 Geometry and parts

`Geometry part`, `Geometry CAD part`, `Geometry Stl part` and `Model part` regions - 31 commands,
the largest command count outside steps. Covers import, transform, split, defeature, part properties
and part removal.

```python
pmx.model.geometry.parts.get_names()
part = pmx.model.geometry.parts['Solid-1']
part.transform(translate=[0, 0, 10])
```

**Depends on:** 1.0, 1.1.

### Status - done

Implemented and verified with `Test_Model_Parts.py`, which passes with exit code 0. Delivered:
`pmx.model.geometry.parts` and `pmx.model.parts` over a shared `PartItemsAPI<T>`/`PartItemAPI` pair,
`IPythonMeshGateway` with all 31 commands, and `PythonColorConverter`.

The two collections share appearance, visibility, scale, rename and remove, and differ only where the
commands differ: geometry adds compound, convert, swap, defeature, face flip, face split and the STL
operations; model adds translate, rotate, mirror and merge.

Three things the code forced:

- **`PartAPI` and `PartsAPI` already exist** on the results side (`APIs/Python/Mesh/Parts`). Worked
  around with `PartItemAPI`/`PartItemsAPI<T>` at the time, then resolved properly by the namespace
  split - see *Namespaces* above. The types now carry their natural names again.
- **pythonnet cannot resolve two indexer overloads.** With both `this[string]` and `this[int]` public,
  `collection[0]` failed with "No method matches given arguments". The positional indexer is now an
  explicit `IReadOnlyList<T>` implementation, so Python sees one indexer, and `len()` and iteration
  still work. Positional access from Python goes through iteration or `get_names()`.
- **`PythonIntListConverter` rejects zero**, because it converts entity IDs. Colors are read with the
  double converter instead.

Also added: `PythonStringListConverter.ToStringArrayOrSingle`, so every model collection accepts a
single name as well as a list. The results API still takes lists only - another 1.10 item.

### 1.3 Meshing

`CreateMeshesCommand`, the four mesh-setup-item commands, and the six setup-item types. Long
running - route it through the Layer 0 blocking behaviour, not a new mechanism.

```python
pmx.model.mesh.setup_items.add_meshing_parameters('MP-1', selection, max_size=5.0)
pmx.model.mesh.create(['Solid-1'])
```

**Depends on:** 1.2.

### Status - done

Implemented and verified with `Test_Model_Meshing.py`, run against an imported STL cube, which passes
with exit code 0 and produces a real mesh - `ModelPart(name='Solid_part-1', type=Solid, nodes=8961,
elements=5293)`. Delivered: `pmx.model.mesh.create(...)` and `pmx.model.mesh.setup_items` with nine
typed `add_*` methods.

Corrections to the plan as written:

- **There are 10 creatable setup item types, not 6.** `GmshSetupItem` is a base with six subclasses -
  `TetrahedralGmsh`, `ShellGmsh`, `TransfiniteMesh`, `ExtrudeMesh`, `RevolveMesh` and `SweepMesh` -
  and `FeMeshRefinement`, which the plan counted, lives in `CaeMesh/_Compatibility` and is not
  creatable.
- **`SweepMesh` is deferred.** Its `SideSurfaceIds` and `LayerGroupEdgeIds` are topology arrays the
  sweep dialog computes from picked geometry; exposing them as plain arguments would be guesswork.
  The other nine are implemented.
- **A setup item without a selection is rejected by the controller**, so the selection is a required
  argument rather than an optional one. `Controller.AddMeshSetupItem` throws when `CreationData` is
  null.
- **Meshing parameters start from the application default.** The gateway exposes
  `Controller.GetDefaultMeshingParameters(name)` and the API overrides only the values the script
  gives, so a script does not have to restate twenty parameters to change one.

`MeshAPI` collided with the existing results-side `MeshAPI` - the third clash in three steps. Rather
than rename case by case again, the naming convention was settled before 1.4: the model tree moved to
the `APIs.Python.Model` namespace and every collision-driven name was reverted to its natural form.
See *Namespaces* above.

### 1.4 Regions

Node sets, element sets, surfaces, reference points, coordinate systems. **This step must precede
1.5-1.8**, because sections, constraints, boundary conditions and loads all reference regions by
name.

```python
pmx.model.node_sets.add('Fixed', selection)
pmx.model.surfaces.add('Top', selection)
pmx.model.reference_points.add('RP-1', [0, 0, 100])
```

Trap: `AddSurfaceCommand(surface, update = true)` carries an extra argument the generic collection
must not lose.

### Status - done

Implemented and verified with `Test_Model_Regions.py`, which passes with exit code 0. Node sets came
with 1.0; this step added element sets, surfaces, reference points and coordinate systems, and pulled
the five services onto a shared `RegionService` base.

- **`RegionService`** carries what the five share: the model mesh, the model-view selection compile,
  the shared name-space validation and the command guard. `NodeSetService` was refactored onto it, so
  the duplication introduced at 1.0 is gone.
- **`RegionServices`** bundles the five services, so the model root takes one constructor argument
  for the area instead of five.
- **A region name is unique across every mesh entity**, not just within its own collection -
  `Controller.GetAllMeshEntityNames` is the authority, and the test covers it.
- **Surfaces have two creation paths.** `add(name, selection)` and `add_from_node_set(name,
  node_set_name)`, matching `FeSurfaceCreatedFrom`. The third value, `Faces`, is internal to the
  application and is not exposed.
- **Coordinate systems are defined by three points** - origin, a point on the local X axis and a
  point in the local XY plane - written into the nine `EquationContainer`s the type carries.

Deferred: the reference point creation modes other than coordinates (`OnPoint`, `BetweenTwoPoints`,
`CircleCenter`, `CenterOfGravity`, `BoundingBoxCenter`) and the selection-based coordinate system
origin. Both are computed by their dialogs from picked geometry, and the value they store is the same
coordinate triple the API already accepts.

### 1.5 Materials and sections

6 section `add_*` methods, plus the 8 material property makers in `pmx.create`. Sections reference a
material name and a region.

```python
pmx.model.materials.add('Steel', [pmx.create.elastic(210000, 0.3),
                                  pmx.create.density(7.85e-9)])
pmx.model.sections.add_solid('Sec-1', 'Steel', selection)
```

**Material is the first composed item**, so it establishes the `pmx.create` part convention that 1.7
and 1.8 reuse. `Material.Properties` is a `List<MaterialProperty>` and `AddProperty` is the only way
in, so the parts are genuinely objects, not keyword values. A tabular property such as plastic still
takes a plain list of rows.

### Status - done

Implemented and verified with `Test_Model_Materials_Sections.py`, which passes with exit code 0.
Delivered: `pmx.model.materials`, `pmx.model.sections` with 5 `add_*` methods, and the 10 material
property makers in `pmx.create`.

Corrections to the plan:

- **There are 10 material property types, not 8.** `ElasticEngineeringConstants` and `ElasticLamina`
  were missing from the count. Both are orthotropic and only accept the table form.
- **There are 5 creatable section types, not 6.** `MassSection` is the shared base of
  `PointMassSection` and `DistributedMassSection`, not a section a user creates.

Three things the code forced:

- **A Python list arrives as a `PyObject`, not as an `IEnumerable`.** The first run failed on
  `materials.add('Steel', [elastic, density])`. `MaterialService` now unwraps a `PyList`/`PyTuple`
  through the GIL, the same way `PythonStringListConverter` does. Any later area that takes a list of
  parts - surface interactions at 1.7, step controls at 1.8 - needs the same handling.
- **A tabular property accepts one row or many.** `pmx.create.density(7.85e-9)` and
  `pmx.create.density([[7.8e-9, 0], [7.7e-9, 400]])` both work; the last value of a row is always the
  temperature. `plastic` and the two orthotropic properties require the list form, because one row
  cannot describe them.
- **The elastic-property rule lives in the model.** `Material.AddProperty` rejects a second elastic
  property, so the API adds the parts in order and lets the domain object refuse - no duplicated
  validation.

Sections confirm the region design end to end: the API passes a selection with
`RegionType = Selection`, and `ConvertSelectionBasedSection` turns it into an internal element or node
set named after the section. The test asserts that `region_type` reads back as `ElementSetName`.

### 1.6 Amplitudes, distributions, initial conditions

3 + 1 + 3 types. Small, and unblocks the amplitude and distribution references used by loads in 1.8.

### Status - done

Implemented and verified with `Test_Model_Fields.py`, which passes with exit code 0. Delivered:
`pmx.model.amplitudes`, `pmx.model.distributions` and `pmx.model.initial_conditions`, over a shared
`FieldItemService` base and a `FieldServices` bundle, following the shape 1.4 established.

- **`MappedDistribution` is deferred.** It carries a point cloud plus scale and translate vectors that
  the dialog builds from an imported file; the two distributions a script can state directly - from an
  equation and from a file - are implemented.
- **Initial conditions are region bearing**, so they take a selection and the controller converts it.
  The test asserts `region_type` reads back as `NodeSetName`.
- The angular velocity axis is a point plus a direction, matching the model constructor rather than
  the dialog's own presentation.

### 1.7 Constraints, surface interactions, contact pairs

8 constraint types - not 9; `Coupling` is a concrete class but the dialog only offers its two
subclasses - contact pairs, and 3 more `pmx.create` members for the surface interaction properties.
`SurfaceInteraction.Properties` is a `List<SurfaceInteractionProperty>`, the same shape as `Material`.
Master/slave region pairs are the other new shape here: two selections per item.

#### Change set 1 - surface interactions and contact pairs: done

`pmx.model.surface_interactions` and `pmx.model.contact_pairs`, with `friction`, `gap_conductance`
and `surface_behavior` on `pmx.create`. Verified by `Test_Model_Interactions.py` (exit code 0) and by
a `--save` / `-r0` replay round trip.

- `MasterSlaveRegionApplier` adapts one side of an `IMasterSlaveMultiRegion` at a time and reuses
  `RegionApplier`, so a master and a slave region accept a `Selection` or a `Region` exactly the way
  a single region does. It is what the 8 constraints of change set 2 need as well.
- `PythonTableConverter` handles the tabular arguments that are not material properties. The material
  ones keep their own converter, because their last column is always the temperature and their single
  row form takes the values as separate arguments.
- **This closes the 1.8 loose end**: `history_outputs.add_contact` now has a real contact pair to
  point at, and the test asserts `region_type` reads back as `ContactPair`.

Two model behaviours the API now refuses rather than passes on silently:

- **`SmallSliding` is cleared by `Method`.** `ContactPair.SmallSliding`'s setter re-applies `Method`,
  and `Method`'s setter clears small sliding whenever the method is `SurfaceToSurface`. So
  `small_sliding=True` on a surface to surface pair was accepted and then quietly dropped. The API
  now rejects that combination and names the two methods that support it.
- **`ReplaceContactPair` deletes and re-converts the internal sets unconditionally**, and so does
  `ReplaceConstraint`. `ReplaceBoundaryCondition` and `ReplaceLoad` are guarded by
  `MultiRegionChanged` instead, and that difference is deliberate rather than an inconsistency: a
  boundary condition and a load are **step keywords**, so a propagated copy in another step can share
  the same internal set, and deleting it whenever the item is replaced would break those other steps.
  A constraint and a contact pair are model level and own their sets exclusively, so the unguarded
  delete is correct for them.

  The consequence for the API is that these two commands expect the regions to be supplied afresh,
  the way the dialog always supplies them. A clone that already carried the converted surface name
  lost its surfaces and left the writer with a dangling reference -
  `The given key 'Internal_Selection-1_..._Master' was not present in the dictionary`. So
  `CloneForReplace` resets a side back to `Selection` when it carries `CreationData`, which makes the
  command recreate it; a side that names a region of the model is left alone.

#### Change set 2 - constraints: done

`pmx.model.constraints` with the 8 types, verified by `Test_Model_Constraints.py` (exit code 0) and
by a `--save` / `-r0` replay round trip. **1.7 is complete.**

The three region shapes the constraints use are worth writing down, because the class hierarchy hides
them:

| Shape | Types | Master slot | Slave slot |
|---|---|---|---|
| Master and slave | `Tie`, `CyclicSymmetry` | Master surface | Slave surface |
| Reference point and region | `RigidBody`, `KinematicCoupling`, `DistributingCoupling` | The **reference point** | The region, which their `IMultiRegion` aliases |
| Single region | `PointSpring`, `SurfaceSpring`, `CompressionOnly` | The region, which their `IMultiRegion` aliases | Unused, `RegionTypeEnum.None` |

So `RegionApplier` serves the last two shapes directly through `IMultiRegion`, and only the tie and
the cyclic symmetry need `MasterSlaveRegionApplier`. A reference point reads back as the master side
with `master_region_type` of `ReferencePointName`, which the test asserts.

`CyclicSymmetry` is the awkward one: it derives from `Tie`, so it has the master and slave surfaces,
**and** its own third region, which is neither of them.

- That third region is the **result expansion region**, and
  `ConvertSelectionBasedConstraint` only converts it when `NumberOfGraphSectors > 1`, discarding it
  otherwise. Giving it with the default of one graph sector was therefore silently dropped, so the
  API rejects that combination and says what it needs.
- `SectorAngleDeg` is derived as `360 / NumberOfSectors`, so it is read only and reaches Python
  through `get_value('sector_angle_deg')` rather than as an argument.

### 1.8 Steps and step content

The largest slice: 12 step types, 4 boundary conditions, 15 loads, 1 defined field, 3 field outputs,
4 history outputs, plus step controls. **Split into four change sets** - steps, then boundary
conditions, then loads, then outputs - or the review becomes unmanageable. **All four are done.**

```python
step = pmx.model.steps.add_static('Step-1', time_period=1.0)
step.bcs.add_fixed('Fix-1', fixed_selection)
step.loads.add_dload('Pressure-1', top_selection, 2.5)
step.field_outputs.add_nodal('F-Output-1', ['U', 'RF'])
```

Steps are composed too. `StepControls.Parameters` is a `List<StepControlParameter>` set through its
own `ReplaceStepControlsCommand`, so the 4 control parameter types become `pmx.create` members fed to
a `step.set_controls([...])` call rather than being step constructor arguments. `ModalDynamicsStep`
takes a `pmx.create.modal_damping(...)` part the same way.

Traps: `AddStepCommand(step, copyBCsAndLoads)` has a second argument; step content is step-scoped and
adds `propagate`, `hide` and `show`.

#### Change set 1 - steps: done

`pmx.model.steps` with 12 `add_*` and 12 `replace_*` methods, verified by `Test_Model_Steps.py`
(exit code 0) and by a `--save` / `-r0` replay round trip over all of its commands.

- **Every value is an optional named argument and a value that is not given is not written.** A new
  step therefore keeps the application default and a replaced step keeps what it already carries, so
  `replace_static('Step-1', nlgeom=False)` changes one value instead of resetting the rest. It is the
  `add_meshing_parameters` convention from 1.3, applied to a much wider surface.
- `replace_*` clones the existing step, so the step type cannot change - the same restriction the
  dialog has, because it opens the type it was given. Changing the type is remove plus add. A
  `name=` argument renames in the same call.
- The step type's own values are read back with `step.get_value(name)` and listed by
  `get_value_names()`, driven by reflection over the properties the concrete type declares. Writing
  stays fully named-argument based; only reading is reflective, so the API surface is not stringly
  typed. `Damping` and `ModalDamping` are flattened into `damping_type`, `damping_alpha`,
  `damping_beta`, `damping_viscous_damping_ratio` and `damping_ratio_count`.
- `perturbation=True` is rejected by `Step.IsPerturbationSupported`, so the model does the domain
  check rather than the API repeating a type list.
- A new step gets the settings default solver type through a new gateway method
  `GetDefaultSolverType()`, matching what `FrmStep` applies. An explicit `solver_type` wins.
- 7 new `pmx.create` members: `damping`, `modal_damping`, `damping_ratio`,
  `time_incrementation_control`, `field_control`, `contact_control`, `reset_control`. The step
  controls are replaced as a whole list through `step.set_controls([...])` / `clear_controls()`,
  because `ReplaceStepControlsCommand` replaces the whole `StepControls`.
- `PythonObjectListConverter.ToList<T>` generalises the `PyObject` list unwrapping that
  `MaterialService` had inlined; `MaterialService` now uses it. This is the 1.5 note cleared, and it
  is what 1.7 needs for the surface interaction properties.

Three things found on the way, none of them fixed:

- **`InitialStep` is dead code.** Nothing constructs it - `CalculixFileWriter` is the only place that
  names it. It is not exposed, so 11 step types are addable, matching `FrmStep`.
- **`DampingRatioAndRange` is declared in namespace `CaeJob`** inside the `CaeModel` assembly. It is
  a serialized type, so the namespace cannot be corrected without a serialization binder.
- **`move` takes a zero based index**, while `CMoveSteps.GetCommandString` prints it one based.

#### Change set 2 - boundary conditions: done

`pmx.model.steps['Step-1'].bcs` with the 4 types, verified by `Test_Model_BCs.py` (exit code 0) and
by a `--save` / `-r0` replay round trip.

```python
step = pmx.model.steps.add_static('Step-1')
step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))
step.bcs.add_displacement_rotation('BC-1', selection, u1=0, u2=0, u3=1.5)
```

- **`RegionAPI` and `pmx.create.region(region_type, name)` are in.** This closes the gap flagged
  after 1.6: an item can now point at an existing node set, element set, surface, reference point or
  part instead of carrying its own selection. Every add and replace method takes `region`, which is
  either a `Selection` or a `Region`; `RegionApplier` is the single place that maps both onto
  `IMultiRegion`. It was pulled forward rather than left to 1.10 because deferring it meant touching
  all 21 boundary condition and load signatures a second time.
- The named region is checked when it is **used**, not when it is created - `pmx.create.region` stays
  a detached description, like every other `pmx.create` member.
- A selection based region does not read back as `Selection`. The controller's
  `ConvertSelectionBasedBoundaryCondition` turns it into an internal node set at add time, so
  `region_type` is `NodeSetName` either way and `region_name` names a set the user did not create.
  This is the behaviour to expect for every selection based item in the API.
- **`Step.IsBoundaryConditionSupported` is checked before the command.**
  `Controller.AddBoundaryCondition` silently does nothing for an unsupported type, while the command
  is already recorded - so without the pre-check a script would record a no-op. A temperature
  boundary condition in a static step is rejected with the step type named.
- The `complex` flag is **derived**, not an argument: `FrmBC` computes it as
  `step is SteadyStateDynamicsStep`, so the service derives it the same way. `phase_deg` stays an
  argument, since it is the user's input.
- A degree of freedom that is not given is left unconstrained (`NaN`), and `get_value` reads a `NaN`
  back as `None`. `replace_displacement_rotation` therefore needs `free_dofs=['U3']` to unconstrain
  one, because a missing argument means "keep".
- The reflection reader moved out of `StepAPI` into `ModelItemValueReader`, shared by steps and
  boundary conditions and ready for loads and outputs. It flattens an `EquationContainer` into its
  value and the damping objects into `damping_*` keys.
- `StepItemsAPI` carries the step name for a step scoped collection; `StepContentServices` bundles
  the step scoped services so that adding loads and outputs does not change `ModelAPI` again.

Naming debt this creates: the new methods call the argument `region`, while sections, initial
conditions and mesh setup items still call it `selection`. Rename the older ones to `region` at 1.10
and widen them to accept a `Region` as well.

#### Change set 3 - loads: done

`pmx.model.steps['Step-1'].loads` with 15 `add_*` and 15 `replace_*` methods, verified by
`Test_Model_Loads.py` (exit code 0) and by a `--save` / `-r0` replay round trip.

```python
step.loads.add_concentrated_force('Force-1', selection, f3=-1000)
step.loads.add_pressure('Pressure-1', pmx.create.region('SurfaceName', 'Top'), 2.5)
step.loads.add_gravity('Gravity-1', part_selection, f3=-9810)
```

- 15 types, not the 17 the plan guessed. `VariablePressure` is abstract, and `ImportedSTLoad` lives
  in `_Compatibility/` and is not offered by `FrmLoad`, so it is deserialized but never created -
  the same case as `InitialStep`. `ImportedPressure` **is** offered by the dialog, so it is exposed.
- The selection target follows `Controller.ConvertSelectionBasedLoad`: nodes for the concentrated
  force, moment and flux; elements for gravity, centrifugal and body flux; surfaces for the other
  nine. Every one reads back as its converted region type, never as `Selection`.
- Derived, not arguments: `complex` (the step type) and `axisymmetric` (the model space). The
  pre-tension load also inherits the dialog's `!twoD` gate, which `IsLoadSupported` does not cover,
  so the service checks it separately.
- **The surface traction has two independent component groups.** `f1`-`f3` are the total force and
  `p1`-`p3` the force per area; `ViewSTLoad` browses one or the other depending on whether a
  distribution is set, and the writer follows the same rule. They are not linked by the surface
  area, so the API exposes both and the distribution decides which one the solver sees.
- `StepItemService<TItem>` now carries what every step scoped item needs - step lookup, region,
  referenced names, per step name uniqueness - and `BoundaryConditionService` was moved onto it.
  Only the commands stay per type. `LoadService` is 90 lines because of it, and the same base will
  carry the outputs in change set 4.
- The reflection reader's snake case rule was sharpened: a run of capitals stays together unless its
  last letter starts a word, so `FMagnitude` reads as `f_magnitude` while `UR1` stays `ur1`.

Two model behaviours the API now documents rather than hides:

- `RadiationHeatTransfer.CavityName` is **truncated to three characters** by its own setter, a
  solver limit. The test asserts the truncation.
- `ImportedPressure` is read from its results file on **every model check**
  (`CalImportedPressureLoad` calls `ImportPressure`), and the writer throws on a null
  `PressureTime` or `PressureVariableName`. So both are required arguments, and the test covers only
  the argument checks - the accepting path needs a real `.frd` and is still untested. Worth
  revisiting at 1.9, where a solved job produces one.

One dialog bug found and left alone: `FrmLoad.PopulateListOfLoads` gates the centrifugal load with
`step.IsLoadSupported(gravityLoad)` instead of the centrifugal one. Harmless today, because every
step supports both or neither.

#### Change set 4 - outputs and defined fields: done

`step.field_outputs`, `step.history_outputs` and `step.defined_fields`, verified by
`Test_Model_Outputs.py` (exit code 0) and by a `--save` / `-r0` replay round trip. **1.8 is complete.**

```python
step.field_outputs.add_nodal('NF-Output-1', ['U', 'RF'])
step.history_outputs.add_nodal('H-Output-1', selection, ['U', 'RF'], totals_type='Only')
step.defined_fields.add_temperature('DF-1', selection, 100)
```

- **The output variable enums are used as flags without carrying `FlagsAttribute`**, so a combined
  value is neither defined nor printable by `Enum.ToString` - it prints as a number.
  `PythonValueConverter.ToFlagsEnum` builds one from a name or a list of names, `FromFlagsEnum`
  decomposes one back, and `ModelItemValueReader` now falls back to the decomposed names whenever
  `Enum.IsDefined` says the value has no name of its own. `get_variables()` returns them as a list.
- A field output covers the whole model and carries no region. A history output does: nodes for the
  nodal one, elements for the element one, surfaces for the section one. A **contact** history
  output names a contact pair instead, checked against `model.ContactPairs` - the contact pair
  collection itself is 1.7, so today the test can only assert the rejection.
- A new static step already carries `NF-Output-1` and `EF-Output-1` from its own constructor, so the
  collection is not empty on a fresh step. The test asserts that.
- `DefinedTemperature` has two forms, `ByValue` and `FromFile`, and they became two add methods -
  `add_temperature` and `add_temperature_from_file` - so the type is derived from the one that is
  used rather than passed as an argument. The from file form covers the whole model and takes no
  region, matching `ConvertSelectionBasedDefinedField`, which skips it.
- Field and history outputs have no hide and show, because they draw no symbols; defined fields do.

Same gap as the imported pressure: `add_temperature_from_file` is covered only by its rejection
paths, because the accepting path needs a real `.frd`. Both are worth revisiting at 1.9.

#### Change set 4 - field outputs, history outputs and defined fields: done

`step.field_outputs`, `step.history_outputs` and `step.defined_fields`, verified by
`Test_Model_Outputs.py` (exit code 0) and by a `--save` / `-r0` replay round trip. **1.8 is complete.**

```python
step.field_outputs.add_nodal('NF-Output-1', ['U', 'RF'])
step.history_outputs.add_nodal('H-Output-1', selection, ['U', 'RF'], totals_type='Only')
step.defined_fields.add_temperature('DF-1', selection, 100)
```

- **The output variables are flag enums without a `FlagsAttribute`.** `NodalFieldVariable` and its
  five siblings start at 1 and are combined with `|=`, but carry no attribute, so `Enum.IsDefined`
  rejects a combined value and `ToString` prints the number rather than the names.
  `PythonValueConverter.ToFlagsEnum<T>` parses a name or a list of names and ORs them, reporting an
  unknown name against the full list; `FromFlagsEnum` decomposes a combined value back into names.
  `ModelItemValueReader` uses the same decomposition, so `get_value('variables')` reads `"RF, U"`
  instead of `"3"`, and `get_variables()` returns them as a list.
- A field output covers the whole model, so it carries no region. A history output does: nodes for
  the nodal one, elements for the element one, surfaces for the section one.
- **A contact history output names a contact pair, not a region.** Contact pairs are 1.7, so the
  name is validated directly against `model.ContactPairs` for now; the check moves to the contact
  pair collection when 1.7 lands.
- A defined temperature has two forms - by value on a region, and read from a results file for the
  whole model - so it becomes two add methods, `add_temperature` and `add_temperature_from_file`,
  and the `DefinedTemperatureTypeEnum` is derived from which one is used.
  `replace_temperature_from_file` refuses a defined field that is given by value, because the two
  forms take different arguments.
- `Step.IsDefinedFieldSupported` is checked before the command, the same as for boundary conditions
  and loads. An uncoupled temperature displacement step refuses a defined temperature, and the test
  asserts it.

Build note found here: `PrePoMax.csproj`'s `AfterBuild` target **moves** every `*.dll`, `*.pdb` and
`*.xml` from the output root into `lib`, and `Move` cannot overwrite. An interrupted build that
leaves reference copies in the output root therefore makes every later build fail with `MSB3677` for
each duplicated file, even though the compile itself succeeded. Deleting the output root copies that
also exist in `lib` restores a clean build - they are the same files, not a newer version.

### 1.9 Analysis

Job create/replace/duplicate/remove, `PrepareAndRunJobCommand(jobName, onlyCheckModel)` which returns
a `bool`, kill, and result opening.

```python
job = pmx.jobs.add('Job-1')
job.run(wait=True, timeout=600)
print(job.status)          # OK | Failed | Killed | TimedOut | FailedWithResults
pmx.jobs.open_results('Job-1')
```

**This closes the Layer 0 deferral.** `job.run(wait=True)` reuses the existing synchronous path
`Controller.PrepareAndRunJob(..., asynchronous: false)`; it does not add a mechanism. The `--timeout`
watchdog stays the outer safety net.

#### 1.9 - done

`pmx.jobs` with the collection verbs plus `add`, `replace`, `open_results`, and a `Job` that carries
`run`, `check_model`, `kill`, `get_output` and `status`. Verified by `Test_Analysis.py` (exit code 0),
which builds a material, a section, a step, a boundary condition and a load on the cube, solves it,
opens the results and reads `U3` back - the first end to end proof that the API can drive a full
analysis. The `--save` / `-r0` replay of that script re-solves and passes too.

- The synchronous run needed **no new mechanism**, only a reachable one:
  `Controller.PrepareAndRunJobCommand(jobName, onlyCheckModel, executeSynchronous)` routes through the
  existing `AddAndExecute(command, executeSynchronous)` and `CPrepareAndRunJob.ExecuteSynchronous`,
  which the regeneration path already used.
- `run` returns the **job status as a string** rather than a bool, because the controller's bool only
  says whether the run started. `job.status` reads the same value later.
- `timeout` is a watchdog `Timer` that kills the solver, because the synchronous submit blocks the
  calling thread. `AnalysisJob.Kill` sets `Killed`, so the API reports `TimedOut` itself when it was
  the one that fired. A timeout without `wait` is rejected rather than silently ignored.
- A new analysis takes the solver executable and the work directory from the application settings,
  the way `FrmAnalysis.PrepareForm` does, and its `argument` defaults to the analysis name because
  that names the input file.
- The `APIs` project now references `CaeJob`. It never did before - `DampingRatioAndRange` only made
  it look that way, because that type is declared in namespace `CaeJob` inside the `CaeModel`
  assembly.

**A Layer 0 defect this finally exposed.** Exit code 5 had never been reachable, so it had never been
verified. It was wrong twice over:

- `Program.GetExitCode` walked the exception chain testing `PromptRefusedException` **and**
  `PythonScriptException` in the same pass, so the outer `PythonScriptException` always won and a
  refusal reported `ScriptError`. The chain is now searched for the refusal first.
- A prompt refused **inside a Python script** does not survive as a typed exception at all, because
  the Python runtime carries the traceback as text. `BatchRunReport` already recorded the refusal, so
  `HasRefusedPrompt()` is now the fallback. It is only consulted on the failure path, where the
  question is which code to report rather than whether the run failed.

`Test_Prompt_Refused.py` runs an analysis whose elements have no section assignment, which raises the
application's own OK or Cancel question, and asserts process exit code **5** with `"refused": true`
in the report.

---

## The exposability rule

Every command must eventually emit a Python string for Layer 2 journaling, so no command can stay
dialog-only. The test for whether a command is exposable is:

> **A command is exposable when its arguments are the user's primary inputs. Anything derived must be
> derived at or below the controller, never in the form.**

Applying it re-audited the deferred list and **five of six deferrals turned out to be premature** - they
were judged from the data members rather than from the code that fills them:

| Item | Where the derivation lives | Outcome |
|---|---|---|
| `SweepMesh` | `FeModel.IsSweepMeshProperlyDefined` fills `Direction`, `SweepCenter`, `SideSurfaceIds`, `LayerGroupEdgeIds` from `CreationIds`, on the validity pass the add already triggers | Exposed. Input is the source surface selection |
| Reference point modes | `FeMesh.UpdateReferencePoint` computes the coordinates for every `CreatedFrom` | Exposed as `add_from_selection` and `add_from_region` |
| Coordinate system points | `ReselectModelCoordinateSystem` plus `FeMesh.UpdateCoordinateSystem` derive all three points | Exposed as `add_from_selections` |
| `MappedDistribution` | It is `abstract`; `DistributionFromFile` derives from it | Phantom - already implemented. Its mapped properties are now exposed on `add_from_file` |
| `export_to_3mf` | Nothing derived; the type is simply in the `PrePoMax` project | Exposed, the gateway takes primitives |
| `SplitPartMeshUsingSurface` | Nothing derived - `SplitPartMeshData` is pure user input | Exposed |

### Two classes of selection-bearing command

The audit made a distinction explicit that the rest of the layer depends on:

- **Tree items** - node sets, surfaces, sections, initial conditions, loads. They carry `CreationData`
  and are **reselected** when the mesh changes, so the selection must be stored as a `Selection`
  description and the ids are derived.
- **One-shot operations** - split part mesh, boundary layer, remesh, merge coincident nodes,
  defeature, flip faces, delete STL faces. They execute once and are **not** recreated on a mesh
  change; they re-run only during regeneration.

A one-shot operation still cannot replay from stored ids. Regeneration is not guaranteed to rebuild
the same mesh - a parameter change, an edited meshing parameter or a new PrePoMax version's mesher
shifts every id - so an operation that replays from ids silently operates on different entities. So
**both kinds must store the `Selection` and re-resolve the ids at execution time**; the difference is
only *when* they re-resolve, not *whether* they do.

An audit of all eight one-shot operations found six already re-resolving from `CreationData`
(`Defeature`, `FlipFaceOrientations`, `DeleteStlPartFaces`, `SplitAFaceUsingTwoPoints`,
`MergeCoincidentNodes`, `RemeshElements`) and two that did not:

- **`CCreateBoundaryLayer`** stored `int[] _geometryIds` only. It now carries a `GeometrySelection`
  and implements `ISerializable` with a `case "_geometryIds":` shim, so a history file written by
  v2.6.2 or older keeps replaying from its stored ids while a new command re-resolves.
  `Controller.CreateBoundaryLayer` reselects from `CreationData` when it is present - unlike
  `Defeature` it must not throw when it is null, because an old command legitimately has none.
- **`SplitPartMeshUsingSurface`** never re-resolved either of its two regions.
  `Controller.SplitPartMeshUsingSurface` now calls `ReselectSplitPartMeshData`, which re-resolves
  `BasePartCreationIds` and `SplitterSurfaceCreationIds` from their `*CreationData`. No serialized
  shape changed - `SplitPartMeshData` already had both slots - so no shim was needed.

## Not assigned to any step

Command regions the step list above never covered, found while implementing 1.2 - 1.6. They need a
home before Layer 1 can be called complete.

| Area | Commands | Note |
|---|---|---|
| Tools | `CreateBoundaryLayer`, `RemeshElements`, `ThickenShellMesh`, `SplitPartMeshUsingSurface`, `UpdateNodalCoordinatesFromFile` | 5 mesh tools. `FindEdgesByAngleForModelParts` landed in 1.2 |
| Node menu | `RenumberNodes`, `MergeCoincidentNodes` | |
| Element menu | `RenumberElements`, `CreateElementSetFromElementQuality` | |
| Activate / deactivate | `ActivateDeactivateMultiple` | The strategic plan listed `activate`/`deactivate` in the collection verb set; the base never got them |
| Part element types | `PartProperties` carries 14 element type fields | The part collections expose name and colour only |

### Status - cleared before 1.8

Implemented and verified with `Test_Model_Mesh_Tools.py`, which passes with exit code 0.

- **`activate` / `deactivate` on `ModelItemsAPI`**, plus `ResolveItems`, calling
  `ActivateDeactivateMultipleCommand`. It is **opt-in per area**, not universal:
  `Controller.ActivateDeactivate` throws `NotImplementedException` outside a fixed type list, so only
  materials, initial conditions, mesh setup items and model parts override the hook. Everything else
  reports "not supported", the way `duplicate` already does for parameters. The test asserts both
  sides of that.
- **Mesh tools on `pmx.model.mesh`** - `create_boundary_layer`, `remesh_elements`,
  `thicken_shell_mesh`, `update_nodal_coordinates_from_file`.
- **`pmx.model.nodes`** - `count`, `renumber`, `merge_coincident`.
- **`pmx.model.elements`** - `count`, `renumber`, `create_element_set_from_quality`.
- **Part element types** - `get_element_type_names()`, `get_element_types(part_name)` and
  `set_element_type(...)`, driven by reflection over the 14 enum fields of `PartProperties` with
  `LinearTetraType` mapping to `linear_hexa` style names. `PartProperties` is a struct, so the setter
  writes to a boxed copy and unboxes it back before the replace command.

`NodesAPI` and `ElementsAPI` exist on the results side too; the namespace split absorbed that without
a rename.

**`SplitPartMeshUsingSurface`** is exposed as well. `SplitPartMeshData` carries two independent
region/selection pairs plus offset, maxH, minH, hausdorff and an exact flag; none of them is derived,
so by the exposability rule they are all plain arguments.

Nothing in this section is deferred any more, and the full script suite plus the `--save` / `-r0`
replay round trip passes after the two replay fixes.

## Step 1.10 - Reconcile the existing results API

`results`, `mesh` and `selection` were written before the model surface. Align them to the naming
convention, the region abstraction and the collection base.

The concrete debt is the two-call form: the results API uses `field_outputs.create_equation(...)`
followed by `add(draft)`, which the single-call decision supersedes. Add `add_equation(...)`,
`add_from_field(...)` and the rest, and keep the existing `create_*` plus `add(draft)` pair working.
Existing user scripts must not break.

The result history outputs are the one place where the draft form still earns its keep - a history
output draft carries a lot of state - so they are kept alongside the new methods, not removed.

#### 1.10 - done

- **The single call forms are in.** 4 `add_*` and 4 `replace_*` on the result field outputs, 7 and 7
  on the result history outputs, each taking the arguments of its `create_*` plus the two optional
  filters. Every `create_*` and the `add(draft)` pair still work unchanged, so existing scripts keep
  running; `Test_Analysis.py` asserts both forms side by side.
- **A results replace re-specifies the definition**, unlike a model replace where a value that is not
  given is kept. A result draft is built whole rather than cloned from the existing definition, so
  there is nothing to keep. The signatures say so: `replace_equation(old_name, name, equation, ...)`
  takes the same required arguments as `add_equation`.
- **`region` now means the same thing everywhere it can.** `sections` and `initial_conditions` are
  the two older areas whose items are `IMultiRegion`, so their `selection` argument became `region`
  and now goes through `RegionApplier` - a `Selection` or a `Region`, exactly like a boundary
  condition or a load.

  The rename stops there deliberately. Node sets, element sets and surfaces are **created from** a
  selection, so pointing them at an existing named region would be circular; the geometry part
  operations and the mesh setup items take a geometry selection, which a model region cannot be. All
  of those keep `selection`, because that is what they actually accept.
- The two `ResultHistoryOutputFromFieldAPI` members that the documentation extractor could not
  resolve were `step_id` and `increment_id`, declared as `int?`. That was a gap in the extractor's
  regular expression, not in the API. Fixed, and the extractor now resolves every member.

## Step 1.11 - Documentation and end-to-end tests

- `Python_API_Overview.md` completed and checked against the `[PythonAPI]` members.
- `Test_Scripts` suite that imports geometry, meshes, applies material, section, boundary conditions
  and loads, solves, and extracts a result value - runnable through Layer 0 with a report assertion.

#### 1.11 - done

**Layer 1 is complete.** `Run_Tests.py` runs 20 scripts and all 20 pass, in about 80 seconds.

- The runner reads each script's **own header** for the model file and the expected exit code, rather
  than keeping a second list that would drift. It asserts the process exit code, that the run report
  agrees with it, and that a passing script printed `TEST PASSED`. The model fixtures moved into
  `Test_Scripts/Models`, so the suite no longer depends on files that happened to be in a scratch
  directory.
- The three **results side scripts had never been run in batch** - they predate the batch contract and
  had no header. Giving them one and running them found a real defect, below. `Test_Analysis.py`
  leaves `PyTest_Job.frd` in the work directory, so the result tests now have a solved model to read
  and the runner orders them last.

**The defect the suite found: `len()` was broken on every results side collection.** Fifteen classes
carried a `__len__` method and nothing else, which is not enough - pythonnet needs a real .NET
collection interface, the same lesson that made `ModelItemsAPI` implement `IReadOnlyList<TItemAPI>`
back in 1.0. The results side was written before that and never caught up, so `len(components)` threw
`TypeError: object of type 'ResultHistoryOutputComponentsAPI' has no len()` and iteration did not work
either. All fifteen now implement `IReadOnlyList<T>` with the positional indexer explicit, so `len()`,
iteration and name indexing all work and behave the way the model side does.

Documentation corrections in the same pass: the collection conventions no longer hedge with "usually"
and state the by name rule, exit code 6 (`LauncherError`) was missing from the table, and the
`create_` convention now covers both the detached parts and the result drafts.

---

## Sizing

| Step | add_* / pmx.create | Collections | Gateway methods | Relative size |
|---|---|---|---|---|
| 1.0 foundations | - | 2 base | - | Medium, highest leverage |
| 1.1 document | 2 | 2 | ~16 | Small |
| 1.2 geometry | 4 | 2 | ~31 | Large |
| 1.3 meshing | 6 | 1 | ~6 | Medium |
| 1.4 regions | 5 | 5 | ~20 | Medium |
| 1.5 materials, sections | 7 + 8 parts | 2 | ~8 | Medium |
| 1.6 fields | 7 | 3 | ~12 | Small |
| 1.7 interactions | 11 + 3 parts | 3 | ~20 | Medium |
| 1.8 steps and content | 46 + 7 parts | 6 | ~37 | Very large - split in four, all four done |
| 1.9 analysis | 1 | 1 | ~6 | Small |

## Decisions taken

1. **Two creation verbs, chosen by the argument.** `<collection>.add_<type>(...)` builds a model
   item and records a command. `pmx.create.<part>(...)` builds a detached part - a selection, a
   material property, a surface interaction property, a step control parameter - to pass as an
   argument; parts are arguments, so they get a short flat path rather than sitting behind the
   collection that consumes them. Everything that is not a part is a plain value. Still typed per
   model type (decision D2), so autocomplete and `help()` show the real arguments, but as method
   pairs rather than draft classes.
2. **A selection is the normal region form.** No API-side decision exists: `RegionTypeEnum.Selection`
   plus `CreationData`/`CreationIds` is handed to the existing command, and the controller's
   `ConvertSelectionBased*` creates the internal set exactly as it does for the dialogs.
3. **`pmx.model` mirrors the model tree.** `pmx.model.steps['Step-1'].loads`, not a flat `pmx.steps`.
   `results`, `jobs`, `selection` and `create` sit beside `model`, not inside it.

Still open, and cheap to defer: whether `add` should also accept the constructor arguments directly
(`step.loads.add_cload(...)`) as a shorthand for the one-liner case. It duplicates surface area, so
it should only be added if real scripts prove the two-call form tedious.

## Risks

| Risk | Mitigation |
|---|---|
| 1.8 stalls the effort | Split into four change sets; steps and loads alone cover most real scripts |
| The API diverges from the UI | Every builder calls the same model constructor the `Frm*` dialog calls, never a parallel one |
| Regeneration breaks on selection-built regions | Store the compiled `Selection` as `CreationData`; never resolve to ids in the API |
| Command history compatibility | The API adds no new `C*` commands - it reuses the existing ones |
| Current-view dependence | Model operations must not depend on `Controller.CurrentView`; check each gateway method during 1.4 |
