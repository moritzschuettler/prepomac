# Composite support - integration plan

Scope: the four items agreed with the customer (orthotropic material + failure limits, material orientation, composite
layup, results post-processing). Budget: 2-3 months, fixed price, so the plan front-loads the solver questions that
decide the post-processing design and names what is cut first if time runs short.

---

## 1. Starting point

### Already in the dev tree

| Area | State |
|---|---|
| Orthotropic elasticity | **Done end to end.** `ElasticEngineeringConstants` (9 constants) and `ElasticLamina` (E1, E2, nu12, G12, G13, G23) in `CaeModel/Materials`, with temperature tables, stability check, dialog (`Forms/41_Material`), writers `CalElasticEngineeringConstants` / `CalElasticLamina`, `.inp` reader and Python API (`elastic_engineering_constants`, `elastic_lamina`). |
| Coordinate systems | `CaeMesh/CoordinateSystem.cs`, rectangular and cylindrical, with `DirectionX/Y/Z(point)`, stored in `FeMesh.CoordinateSystems`, with an API. Used only for BCs (`*TRANSFORM`) and result transforms so far. |
| Shell normals | `FeMesh.GetElementFaceCenterAndNormal` (S2 = positive face), `GetNodeNormals`, flip-orientation command. No per-element local axes. |
| Parameters | Numeric only (`EquationParameter`, `.pmp`, `-p` in batch). `EquationContainer.Value` is evaluated lazily, so any new `EquationContainer` field (layer thickness, angle) can be driven by parameters at no extra cost. |
| Symbols | `Controller.DrawArrowSymbols` (batched glyphs), `DrawDLoadSymbols` as a per-face template, `SpatialPointSampler` for thinning. |
| Thickness colouring | `AnnotateWithColorEnum.SectionThicknesses` through `FeModel.GetSectionThickness`. |
| Result derived fields | `ResultFieldOutput*` (Equation, Limit, Envelope, CoordinateSystemTransform) evaluated lazily in `FeResults.ComputeFieldFromResultFieldOutput`. This is where the new shell and composite outputs plug in. |

### Missing

- `*ORIENTATION` / `*DISTRIBUTION` output, any link between a section and an orientation.
- Composite shell section, layer data, element type checks.
- Material strength (failure limit) data.
- Any notion of layers, plies, or the top and bottom of a shell in results. All fields are **nodal**; the `.frd` reader
  rebuilds an expanded shell part as a plain solid part.

### The earlier composites fork (`..\pre-po-max-composites-master`, based on v2.4.5)

Pre-processing only, with no post-processing at all. Use it as a reference, not as code to merge:
- Reusable ideas: the layer-grid dialog (`FrmCompositeLayup`), `CalCompositeShellSection` data line format, CSV import
  of layers, the colour-by-composite annotation.
- Not reusable as is: orientation is one `*ORIENTATION` per group of elements with similar normals (not per element);
  the ply rotation axis is hard-coded to local 1 instead of the normal; the shell offset is written as ±t instead of
  ±0.5; the CS/normal options are stored but ignored; its material writers emit `TYPE=ORTHO` with engineering constants
  (wrong).
- Draping (`KinDrape`) and ply drop-offs are out of the agreed scope (see section 8).

---

## 2. Design decisions (confirmed)

1. **The composite layup is a new section type, `CompositeShellSection`**, not a separate collection. CalculiX maps
   1:1 (`*SHELL SECTION, COMPOSITE`), and it reuses the section region machinery, the `0063_Section` commands,
   colour by section, `sections.add_*` in the API, and the validity checks. Different layups on different regions are
   simply different sections.
2. **Material orientation is a new model item, `MaterialOrientation`, assigned to a region** (like Abaqus's "assign
   material orientation"), in a new `FeModel.MaterialOrientations` collection with its own tree node "Orientations"
   (after Sections). The user does not have to split sections to match orientation regions.
3. **PrePoMax computes the ply axes of every element itself, and writes them as rectangular `*ORIENTATION`s per
   group of elements with equal axes.** *Revised after Phase 0:* `*DISTRIBUTION` cannot be used with
   `*SHELL SECTION` in ccx 2.22 (the solve returns NaN). ccx projects the x axis of each orientation onto every shell
   element itself, with axis 3 = shell normal. So a single orientation is exact for the base direction, but a layer
   angle given by the orientation's rotation line (`3, θ`) turns about the orientation's z axis, *not* about the shell
   normal. On a curved or inclined shell that distorts the ply angle. PrePoMax therefore computes e1(θ) for every
   element and layer, and groups elements whose axes agree within a tolerance (0.1°). Each group gets its own
   orientations and its own `*SHELL SECTION` card. A flat region collapses to one group; a doubly curved one goes
   towards one group per element. The preview, the writer and the results all use the same calculator.
4. **Layer angles are applied by PrePoMax**, through decision 3: one orientation per (group, distinct evaluated layer
   angle). A parameter change in batch mode just produces different vectors.
5. **Failure limits are a material property that the `.inp` writer skips.** Failure is evaluated in PrePoMax
   post-processing, because CalculiX does not compute composite failure.
6. **Per-layer material change through parameters stays out of scope**, as stated in the offer (3b). Changing the
   material property values through parameters already works.
7. **`CaeResults` does not reference `CaeModel`** (it is the other way round). The controller hands results a plain
   data object (`CompositeResultData`: per shell element its axes and its layers' thickness, angle, and strength
   limits), built from the model when results are opened. No model types leak into `CaeResults`.

---

## 3. Phase 0 - Solver behaviour spike (done)

Run on the solver PrePoMax ships (**CalculiX 2.22**, `Published/PrePoMax v3.0.0 RC1/Solver/ccx_dynamic.exe`), with
19 generated models. The plate is 100 × 50, 4 × 2 elements, and each ply is 0.5 thick (E1 140000, E2 = E3 10000,
nu12 = nu13 0.3, nu23 0.5, G12 = G13 5000). Loading is a prescribed axial strain of 0.001, with uz = 0 at all nodes.
The ccx 2.22 manual sections quoted are 6.2.14 (shells), 7.42 `*DISTRIBUTION`, 7.102 `*ORIENTATION` and
7.117 `*SHELL SECTION`. The generator and analysis scripts are kept with the models; see "Reference models" below.

### ccx facts

| # | Fact | Evidence | Consequence |
|---|---|---|---|
| F1 | `COMPOSITE` accepts **S8R and S6 only**. S4R and S8 stop with "*can only be used for S8R or S6 shell elements*". | c07 (S6) runs, c08 (S4R) and c09 (S8) error | Element type check: S8R for quads, S6 for triangles. |
| F2 | **`*DISTRIBUTION` works only with `*SOLID SECTION`** (manual 7.42). Used on a shell section, the solve produces NaN without any input error. | c04, c11, c12, c15: "Solution contains NaN" | Decision 3 revised: per-group rectangular orientations for shells. `*DISTRIBUTION` stays usable for solid sections (decision 4 of section 12). |
| F3 | ccx makes **its own orientation for every shell element** (`OR0_shell_0000000001` in the `.dat`). It projects the orientation's x axis onto the shell at ξ = η = 0, and **axis 3 = shell normal**. If x is within 0.1° of the normal, it projects the orientation's z axis instead. Without any orientation it projects **global x** (global z as the fallback). | c18: global x projected onto a plate inclined in space, giving a fibre angle of −16.60° in the plate. The `.dat` ply stresses (42.88 / 3.81 / 12.78) match the hand calculation (42.99 / 3.82 / 12.82) within 0.3 %. c10: no orientation means 0° in every layer. | The PrePoMax calculator must use exactly this rule, including the 0.1° fallback to z, so that the preview equals the solver. |
| F4 | The rotation line `3, 90.` on a rectangular orientation equals an explicit 90° orientation, but it turns about the **orientation's** z axis (manual 7.102). It is allowed only for rectangular systems. | c19 = c01 to all digits | It is exact only when the orientation's z is the shell normal; hence decision 3. |
| F5 | **Layer 1 is on the negative-normal side** (bottom) and the list stacks along the normal. Reversing the element node order (normal −z) reverses the stack. The normal follows the right-hand rule on the node order. | c01 vs c06 | Matches section 12, decision 3. No reordering in the writer. |
| F6 | `OFFSET` is a **fraction of the total thickness**: `OFFSET=0.5` puts the reference surface on top, so the layers lie in [−t, 0]. | c05 | The fork's ±t offset is wrong; write the fraction. |
| F7 | `*SHELL SECTION, MATERIAL=…, ORIENTATION=…` on a plain (non composite) shell applies the orientation as well. | c14: 90° orthotropic shell, σxx = E2·ε = 10.0 | Orthotropic homogeneous shells come with Phase 2 at no extra cost. |
| F8 | In the `.frd`, **each layer is its own 20-node brick** (he20, frd type 4; S6 gives 15-node wedges, type 5) **with its own nodes**. Interface nodes are not shared, so **ply stresses are not averaged across plies**. The interface jumps (135.2 below / 9.77 above at z = 0 in c01). | c01: 8 shells → 16 elements, 178 nodes = 2 layers × 89 | No node splitting or `.dat` fallback is needed for clean ply values. |
| F9 | Composite layers get **new element ids from (max element id in the model) + 1**, assigned element after element in ascending shell id order, layer 1 first. Plain shells keep their own id. The element's **frd material number is the layer material** (the order of `*MATERIAL` in the input). | c20: ids 100 + 7k, shells 107/114/135/142 → 157-164, layer materials 1 / 2, plain shells 121… keep their ids | The mapping (shell id, layer) is computed by the rule and checked by geometry (centroid → nearest shell element, signed distance along the normal). |
| F10 | Composites are **always written 3D**: `OUTPUT=2D` and the default give the same elements. With `OUTPUT=2D` the `.frd` also lists the original shell nodes, unused by any element and with zero values (215 vs 178 nodes). The default for shells is 3D anyway (manual 7.51 `*EL FILE`, 7.96 `*NODE FILE`). | c01 / c02 / c03 identical | No need to force `Output=3D`. The reader must tolerate unused nodes. |
| F11 | **`.frd` stresses are in global axes. `.dat` (`*EL PRINT`) stresses are in the ply axes**, with 8 integration points per layer for S8R (2 × 2 in plane × 2 through the layer) and 6 for S6, under the **original shell id**. | c01 90° ply: frd σxx = 9.4 (= E2·ε), dat s11 = −2.6 / s22 = 10.1 | PrePoMax rotates the frd tensor into e1(θ), e2(θ), n per element. The `.dat` serves as a cross-check in the tests. |
| F12 | Against classical lamination theory ([0/90]s): **fibre stress within 0.14 %** (140.59 vs 140.79; 90° ply 10.15 vs 9.94). The transverse stress is 2.41 vs 2.62 (8 %), because the expanded solid carries a small σzz ≈ ±0.4 instead of the plane-stress σzz = 0. | c13 | Test tolerances. Phase 4 should evaluate plane-stress ply stresses from the in-plane strains (Q̄·ε), as offer item 4a assumes, as an option next to the 3D stresses. |
| F13 | The same laminate **rotated in space** (local `*TRANSFORM` for the BCs, rotated orientations) gives the flat ply stresses within 1.1 %, with σ12 = 0. | c17 vs c13 | Invariance benchmark for Phase 2. |
| F14 | Composite restrictions (manual 6.2.14): no `RF` in the `.frd`, no `*NODAL THICKNESS`, no error estimators. | manual | Validity checks / warnings for field outputs. |
| F15 | *(Phase 2 spike)* The shell normal ccx projects onto is taken at **ξ = η = 0: the centre of an S8R but the first node of an S6**. On a coarse curved cylinder (8 elements around) the S6 fibre angle at node 1 matched within 0.8°, against up to 26° off at the centroid; for S8R the centre matched exactly. | `scripts/cyl.py`: isotropic cylinder in axial tension, orientation x = (1, 0, 1), angle recovered from the `.dat` stresses in the element axes | `MaterialOrientationCalculator` evaluates the normal and the coordinate system at that point. |

### Reference models

The models and scripts are in [Composite_Phase0](Composite_Phase0) (untracked): `models/cNN_*/*.inp`, plus
`scripts/gen.py` and `gen2.py` to regenerate them and `frd.py`, `analyze.py` and `analyze2.py` to read the results.
To rerun: `py -I scripts/gen.py <dir>` and `py -I scripts/gen2.py <dir>`, run `ccx_dynamic.exe -i <case>` in each
case folder, then `py -I scripts/analyze.py <dir> <cases…>` and `py -I scripts/analyze2.py <dir>`.

c01 (order), c13 (flat [0/90]s), c17 (rotated), c18 (projection) and c20 (id mapping), together with `frd.py`, move
into `APIs/Python/Test_Scripts/Models` with the first test script that solves them (Phase 2 for c17 and c18). Phase 1
needs no solver run, so they stay in `Composite_Phase0` until then. A cylinder model is still to be added in Phase 2,
for the cylindrical coordinate system.

---

## 4. Phase 1 - Material: orthotropic + failure limits (implemented)

**Orthotropic (item 1):** already supported. Done:
- `CalElasticLamina` completes the lamina as transversely isotropic: E3 = E2, nu13 = nu12 and nu23 = E2/(2·G23) − 1,
  through `ElasticLamina.GetPoissonsRatio23`, instead of nu23 = nu12. Existing lamina models give slightly different
  results; **this goes in the release notes.** `ElasticLamina.CheckStability` now checks the completed 3 × 3 normal
  compliance block (Cholesky, like `ElasticEngineeringConstants`), so a G23 that implies an unstable nu23 is refused,
  naming v23. The G23 description in the dialog and `Python_API_Overview.md` state the completion.
- Not done: orthotropic thermal expansion (`*EXPANSION, TYPE=ORTHO`). It was optional, and is the first cut.

**Failure limits (1a):** `CaeModel/Materials/CompositeStrength.cs`.
- Values: Xt, Xc, Yt, Yc, S12, S23 (all > 0, compressive ones positive) and the Tsai-Wu F12* (−1 < F12* < 1, default
  −0.5, `CompositeStrength.DefaultTsaiWuCoefficient`). All are `EquationContainer`s, so they can be parameters.
  S23 is required because Hashin's matrix compression mode uses it. Zt, Zc and S13 are left out: the criteria of
  Phase 4 are in-plane (plane stress) ones, and 3D criteria are out of scope (section 8). The property has no
  temperature.
- `Material.ContainsEquation` and `CalculixFileWriter.AppendMaterials` (the property is skipped; nothing is written).
- Dialog: a "Strength" group with a "Composite Strength" node, declared in `FrmMaterial.Designer.cs`, and
  `ViewCompositeStrength` (categories Fiber direction / Transverse direction / Shear / Interaction).
- API: `pmx.create.composite_strength(tensile_strength_1, compressive_strength_1, tensile_strength_2,
  compressive_strength_2, shear_strength_12, shear_strength_23, tsai_wu_coefficient=None)`, the `MaterialPropertyAPI`
  type name and `get_values`, the journal maker in `PythonMaterialEmitter`, and `Python_API_Overview.md`.
- Test: `test_composite_strength` in `Test_Model_Materials_Sections.py` covers read-back, the maker and replace round
  trip, refusals naming the argument, a temperature dependent material, the `.inp` (no strength keyword, nu23 from
  G23) and the unstable G23 refusal.
- Not verified yet: the dialog itself, interactively (adding, editing and saving a strength in `FrmMaterial`), and
  the material library round trip. Both are a manual check in the GUI.

---

## 5. Phase 2 - Material orientation (implemented)

**As built** (differences from the design below are marked):
- Model: `CaeModel/Orientations/MaterialOrientation.cs` with region, coordinate system, `ReferenceAxis`
  (`CaeMesh/Enums/MaterialOrientationAxisEnum.cs`) and `RotationAngle`; there is no `FlipNormal`, because ccx always
  takes axis 3 along the normal given by the node order. `FeModel.MaterialOrientations` with the `.pmx`
  compatibility default; `CheckValidity` marks an orientation invalid when its region or coordinate system is
  missing or its angle does not evaluate. Overlapping regions make nothing invalid: the later active orientation applies,
  as a later section does. Selecting an orientation, in the tree or in its dialog, shows its own axes on its whole
  region.
- Calculator: `CaeMesh/MaterialOrientationCalculator.cs`, with the normal at the centre of a quad and at node 1 of a
  triangle (F15).
- Writer: `ShellOrientationGroup` groups the elements of a shell section by quantized axis 1 and normal (step 0.001,
  so within about 0.1°). Each group gets an internal `*Elset`, a `CalOrientation` and its own `*Shell section ...,
  Orientation=` card; uncovered elements get a card without orientation. A section with no covered element is
  written as before.
- UI: commands in `Commands/0062_MaterialOrientation`, a controller region, the tree node "Material Orientations"
  after Sections (it reuses the coordinate system icon), the menu Property → Material Orientation, and
  `Forms/42_MaterialOrientation` (`FrmMaterialOrientation`, `ViewMaterialOrientation`). Orientations can be
  deactivated, renamed and duplicated.
- Visualization: the region is highlighted by its outline only. The material axes are drawn at the shell
  evaluation points with the coordinate system symbol, labelled 1, 2, 3, axes of equal length and in the axis
  colours. A second set of that symbol on the selection layer does not repaint in the highlight colour and is
  cleared with the highlight (`vtkControl.AddMaterialAxesSymbol`). Symbols are shown on the "Symbol density" percentage of the visible region elements (Appearance, the last group; default 50 %; 100 % is the full density of a spatial resolution that does not depend on the mesh size, and a lower percentage an evenly spread share of it, named like the vector plot density), spatially distributed by `SpatialPointSampler`,
  and the label is 1* where the reference axis was replaced. They are drawn while the dialog is open and when
  the orientation is selected in the tree. On triangles the symbol sits at node 1, the point the solver uses
  (F15). *Not done:* show/hide per item, and a preview on the geometry before meshing.
- API: `pmx.model.material_orientations` (`add`, `replace`, and the shared verbs including `activate`/`deactivate`),
  `MaterialOrientation` read-back, the journal emitter, the gateway, the tree item type `material_orientations`, the
  MCP model listing, `PythonEnumVocabulary` (`reference_axis`) and `Python_API_Overview.md`.
- Tests: `Test_Model_Material_Orientations.py` on `Models/composite_shells.inp` (flat plate, inclined plate with a
  local system and 30°, cylinder with a cylindrical system → 8 groups, refusals, overlap, deactivation).
- Solver check: a deck exported by PrePoMax (`scripts/export_orientation_deck.py`) solved by ccx
  (`scripts/solve_orientation_deck.py`). The inclined plate reads s11 / s22 / |s12| = 149.9 / 50.0 / 86.5 in the
  element axes, against 150 / 50 / 86.6 expected; the cylinder gives the expected hoop and axial values exactly.
- *Not done:* solid sections (section 12, decision 4).
- *Not verified:* the dialog and the arrows in the GUI by eye.

Original design:

### Model (`CaeModel/Orientations/MaterialOrientation.cs`)

- `NamedClass, IMultiRegion, IContainsEquations`; region = part, element set, or selection (geometry faces or
  elements), converted to an internal element set the way `Controller.ConvertSelectionBasedSection` does for sections.
- `CoordinateSystemName` ("Global" or a local rectangular or cylindrical CS), `ReferenceAxis` (X/Y/Z of that CS) = the
  major material direction, `RotationAngle` (`EquationContainer`, degrees, about the normal), `FlipNormal` (bool).
- Projection rule per element, matching ccx (F3): n = element normal at ξ = η = 0 (right-hand rule on the node
  order); e1 = normalize(a − (a·n)n), where a is the reference axis at that point (point-dependent for a cylindrical
  CS); if a is within 0.1° of n, use the CS z axis instead and report it; rotate by the angle about n;
  e2 = n × e1; e3 = n.
- The calculator lives in `CaeMesh` (`MaterialOrientationCalculator.GetElementAxes(FeMesh, elementIds, cs, ...)`),
  so the writer, the preview, and results all use the same code. For S8/S6 elements, take the normal from the
  parametric centre, not from the first three nodes.
- `FeModel`: the new collection in the constructor, copy constructor, deserialization switch, `GetObjectData`, and
  `CheckValidity` (overlapping regions = error, CS missing = invalid item). It must work with old `.pmx` files that
  have no such field.

### Writer

- `CalculixFileWriter.GetModelKeywords`: a new "Orientations" block before "Sections", with a `CalOrientation` keyword
  class (rectangular, `a` = e1 and `b` = e2 as points, no rotation line).
- `ShellSection`: the elements are grouped by their computed axes (0.1° tolerance, decision 3). Each group gets an
  internal element set, one orientation and its own `*SHELL SECTION, MATERIAL=…, ORIENTATION=…` card (F7). Elements
  that no orientation covers stay in a group without `ORIENTATION=`, so ccx projects global x (F3), and the writer
  issues a warning.
- `SolidSection`, if it comes along (section 12, decision 4): one `*DISTRIBUTION` per section plus one orientation
  (F2: allowed there).

### UI

- Commands `PrePoMax/Commands/0063_Orientation/` (`CAdd`, `CReplace`, `CDuplicate`, `CRemove`, plus show/hide),
  each with `ToPython`.
- Controller `#region Orientation menu` with the `XxxCommand` / `Xxx` pair; tree node in `UserControls/ModelTree.cs` and
  `ModelTree.Designer.cs` (Distributions is the template); `FrmMain` create/edit/delete dispatch and menu items in
  the Designer.
- `Forms/43_Orientation/FrmMaterialOrientation` + `ViewMaterialOrientation` (`IFormItemSetDataParent`,
  `IFormHighlight`).

### Visualization (2b)

- Preview while the dialog is open, and show/hide for an item from the tree: arrows at element centroids for e1 (and
  optionally e2 and n in other colours), batched through `DrawArrowSymbols` and thinned by `SpatialPointSampler`.
- Geometry view (before meshing): the same arrows at the centroids of the geometry visualization triangles of the
  selected faces, using the same calculator.
- Elements where the reference axis was nearly normal (fallback used) are highlighted.

### API

- `model.orientations.add(name, region, coordinate_system_name, reference_axis, rotation_angle_deg, flip_normal)`,
  `replace(...)`, and the shared verbs, with an `orientations` collection on `ModelAPI`
  (`Model/Fields/Distribution*API` as the template). Gateway methods; `PythonEnumVocabulary` entry for
  `reference_axis`; journal emitter.

**Done when:** the rotated-plate model and the cylinder model give e1 along the intended direction on every element
in the preview and in the `.inp`. ccx's `.dat` ply stresses for the rotated plate and for c18 (projection) match the
values the calculator predicts.

---

## 6. Phase 3 - Composite layup (implemented)

**As built** (differences from the design below are marked):
- Model: `CaeModel/Sections/CompositeShellSection.cs`, derived from `ShellSection`, so it shares the shell region
  handling, the selection conversion, the element type checks and the offset. It holds a list of
  `CompositeLayer`s (material, thickness, angle) and `Symmetric`.
  - The section material is the one of layer 1, which drives colour by material.
  - The base thickness follows the total, and `GetSolverLayers()` adds the mirror image of a symmetric layup.
  - *No layer name* (layers are numbered).
  - The mass weighs every solver layer with the density of its own material (the section density is the
    thickness-weighted mean); a layer material without a density is named and excludes the elements. The mass is
    readable as `ModelPart.get_mass()` and `get_mass_warnings()`.
- Validity: every layer material must exist and be active. The elements must be S8R or S6
  (`FeModel.GetCompositeShellElementTypeError` names the part and the element type to set). Thickness > 0 and finite
  angles are required.
- Writer: `CalCompositeShellSection`. The elements are grouped by axes as in Phase 2, and each group gets one
  orientation per distinct evaluated layer angle, turned about the normal. Elements no orientation covers start
  from the solver default (projected global x) through a default `MaterialOrientation`, so layer angles apply
  everywhere.
- UI: `FrmSection` now derives from `FrmPropertyDataListView`. The new type "Composite Shell Section" has a Layers
  tab (a material combo column, thickness and angle accepting expressions and parameters, and a new row copying the
  last one). The Data group holds Total thickness (read only), Offset and Symmetric. Selecting a layer row shows that
  layer's material axes (coordinate system symbols at 50 %) over the region outline (3c). Under the table a layup
  diagram draws every solver layer as a bar as high as it is thick, layer 1 at the bottom (−n side), labelled with
  material, thickness and angle; the mirrored half of a symmetric layup is faded and the selected row outlined.
  The thickness and angle columns carry their units in the header, as the material tables do, so a pasted or
  typed value may come with or without a unit. A wrong element type on OK asks Yes (set the parts to S8R/S6),
  No (save the section invalid) or Cancel. *Not done:* a per-layer view from the tree; CSV import is covered by
  pasting rows.
- API: `sections.add_composite_shell(name, region, layers, offset=0, symmetric=False)` and
  `replace_composite_shell(...)`, with `layers = [[thickness, material_name, angle_deg], ...]`. Read-back adds
  `layers`, `symmetric` and the total `thickness`. Also the journal emitter and `Python_API_Overview.md`.
- Tests: `Test_Model_Composite_Sections.py` covers the S8 refusal, layers and symmetry in the deck, two shared
  angle orientations, layer angles over a local orientation, an angle driven by a parameter that changes with the
  parameter, the cylinder without an orientation, refusals and the replace type check.
- Solver check: `scripts/export_composite_deck.py` and `solve_composite_deck.py` solve a symmetric [0/90] laminate
  written by PrePoMax.
  - Flat plate: s11 / s22 = 140.60 / 2.41 (0°) and −2.41 / 10.15 (90°), exactly c13 of Phase 0.
  - Inclined plate through its orientation: 139.6 / 2.41 and −2.41 / 9.98, the c17 deviation.
- *Batch parameters (3a):* `Test_Model_Composite_Parameters.py` saves a laminate whose first layer takes its
  thickness and angle from parameters, then regenerates the `.pmx` with `-r ... -p layup.pmp` and with
  `-p "T=0.3; A=-45"`; each regenerated solver input carries the new thickness and the new layer axes.

Original design:

### Model

- `CaeModel/Sections/CompositeShellSection.cs : Section` with `List<CompositeLayer>`. A layer holds `Name`,
  `MaterialName`, `Thickness` and `Angle` (both `EquationContainer`, item 3a), plus a section-level `Offset` and
  `Symmetric` (mirror at write time).
- The base `Thickness` is the sum of the layers (read-only), so `SectionThicknesses` colouring works unchanged.
- Places that assume one material per section: `FeModel.CheckValidity` section switch, `GetSectionAssignments` (colour
  by material), `MaterialNamesUsedInActiveSections` (all layer materials must be written), material rename/delete
  propagation.
- Checks: at least one layer, all thicknesses > 0, materials exist, and the part is a shell meshed with **S8R
  (parabolic quads) or S6 (parabolic triangles)** (F1). Otherwise a clear message names the element type to mesh with.
  The parabolic quad type must be S8R, not S8.
- Field output checks (F14): a warning that `RF` is not written for composite elements.

### Writer

- `CalCompositeShellSection`: `*Shell section, Composite, Elset=..[, Offset=..]`, with per layer
  `thickness, , material, orientation`. The offset is written as a fraction of the total thickness (F6). Elements are
  grouped by axes as in Phase 2, one card per group, and each layer names the group's orientation for that layer's
  evaluated angle (decisions 3-4). Without an orientation item, the layer lines carry no orientation, so ccx uses
  projected global x (F3), and the writer issues a warning.
- `Output=` needs no change: composites are always written 3D (F10).

### UI

- `FrmSection`: a new type "Composite shell" with a layer table tab (DataGridView, the same pattern as the material
  data points in `FrmMaterial`): add, remove, move up/down, material combo, thickness and angle accepting
  `=expressions` and parameters. Layer 1 = bottom (S1 side); the convention is shown in the dialog.
- Optional: CSV import of a layer table (idea from the fork). Cut if short of time.

### Visualization (3c)

- A layer selector in the dialog (and "show layer" from the tree): highlight the section region, draw the fibre
  direction of the selected layer (base orientation rotated by the layer angle), and show its thickness in the
  legend/status.
- A small layup stack diagram in the dialog (bars proportional to thickness, labelled with material and angle), as a
  Designer-declared panel with a paint handler.
- Total thickness through the existing colour-by-section-thickness mode.

### Parameters (3a)

- Nothing new is needed beyond `EquationContainer`. Verify that a `.pmp` overriding layer thickness and angle in batch
  (`-p`) changes the written `.inp` and the orientation vectors. Add this to a test script.

### API

- `model.sections.add_composite_shell(name, region, layers, orientation?, offset, symmetric)` with
  `layers = [[thickness, material, angle_deg], ...]` (plain list, `PythonListConverter`), plus
  `replace_composite_shell`. `PythonSectionEmitter`, docs, audit.

**Done when:** the [0/90]s plate and the cylinder are built in the GUI and from Python, solve in ccx, and a batch run
with a `.pmp` changes a ply angle.

---

## 7. Phase 4 - Results post-processing (implemented)

**As built so far** (differences from the design below are marked):
- Mapping: `Controller.GetShellResultData`, called when results are opened, builds `CaeResults/Composite/ShellResultData`
  (`ShellResultElement` per result element: shell element, section, layer, material and the axes) and stores it in
  `FeResults` (serialized with the results).
  - Plain shells keep their element ids. The layers of composite shells are the last result ids the model does not
    have, which must be contiguous and as many as the solver layers (F9). Each is checked to lie on its shell element
    (centroid distance within the shell size plus the offset thickness). *The frd material number is not checked.*
  - A failed check writes a message to the output and leaves the composite layers out; the results still open.
  - The result element sets `<section>_LAYER-<n>` are added.
  - The axes come from the governing material orientation (later one wins) or the solver default, turned by the
    layer angle, so they are exactly the ones written to the deck.
  - `PlyStrengths` carries the `CompositeStrength` of every material.
- New result field output types in `CaeResults/FieldOutput`, all computed per node with the mean axes of the result
  elements around it, NaN off the shells:
  - `ResultFieldOutputMaterialAxes`: any tensor field rotated into the material axes (R·T·Rᵀ) with its invariants.
  - `ResultFieldOutputShellPlaneStress` (4a): S11, S22, S12, PRINCIPAL_MAX, PRINCIPAL_MIN, SHEAR_MAX and ANGLE (°).
  - `ResultFieldOutputCompositeFailure` (4c): MaxStress, TsaiHill, TsaiWu (reserve factor from the quadratic) and
    Hashin 1980 plane stress, giving FAILURE_INDEX, RESERVE_FACTOR and MODE (1-5 for MaxStress and Hashin, 0 for the
    interactive ones). A material without strength gives NaN. `CompositeFailureCalculator` holds the formulas.
  - A source must be a tensor in the global axes with all components; an output in material axes is refused as a
    source, so nothing is rotated twice.
- UI: the three types are appended to the result field output dialog ("Material Axes", "Shell Plane Stress",
  "Composite Failure"), with tensor fields only in the field list and an explanation when the results carry no shell
  data.
- API: `add_/replace_/create_field_output_` `material_axes`, `shell_plane_stress` and `composite_failure`, the read-back
  classes, the journal emitter and `Python_API_Overview.md`.
- Test: `Test_Result_Composite.py` solves the [0/90]s plate (c13 loads) with a job, and checks the ply stresses of
  all four layers against Phase 0 (σ11 within 1 %, σ22 within 3 %), the plane-stress principal value and angle,
  MaxStress (index and mode for both ply angles), Tsai-Wu and Hashin against the formulas in the script, NaN on an
  isotropic shell, and the refusals.
- Added after the first review:
  - Units by component (ANGLE in °, the failure values unitless), and a node takes the mean of the tensor rotated
    into the axes of each of its elements, since the projected x axis turns over between neighbouring elements of
    a cylinder (no NaN there any more).
  - ANGLE is exactly 0 or 90 when the shear is below 0.1 % of half the normal difference, and NaN without
    in-plane stress.
  - Each layer is a result part `<model part>_Layer-<n>`, gathered in a results tree group named after the part.
  - Tensor plot: Material Axes along the node axes, Shell Plane Stress S11/S22 along axes 1/2 and the in-plane
    principal values along their directions.
  - **Plane-stress ply stresses** (`stress_source='Strain'` on Shell Plane Stress and Composite Failure):
    σ = Q̄·ε from the in-plane strains with E1, E2, ν12, G12 of the ply material. The [0/90]s plate matches CLT
    within 0.5 % of each ply's largest stress (0° ply 140.78 / 2.61, 90° ply −2.59 / 9.94); the 90° ply σ11,
    which the lateral contraction alone gives, is 0.9 % off because the plate edges set εy.
  - **Maximum over layers:** MAX_FAILURE_INDEX and CRITICAL_LAYER on Composite Failure.
  - **Rotated plate:** INCLINED with a local orientation and supports in the local system. The rotation of the
    tensors is exact (hand rotation within 0.1 %). Against the flat plate, ccx itself solves the turned plate
    differently: fibre stress −0.65 %, 90° ply σ22 −2.1 % (3D) and −1.7 % (from the strains), so the test allows
    2.5 % of each ply's largest stress. This refines F13.
- *Not done:* a GUI check of the tensor plots.

Original design:

Phase 0 made this phase simpler: the `.frd` already holds every layer as separate elements with their own nodes
(F8), so there is no averaging across plies to undo, and no node splitting or `.dat` reading is needed.

### Mapping (4b)

- When results are opened against a model with composite sections, the controller builds `CompositeResultData` and
  hands it to `FeResults`.
- Every expanded element is mapped to (shell element id, layer index) by the ccx numbering rule (F9): new ids from
  max id + 1, ascending shell id, layer 1 first, with the section's layer count. The mapping is then checked by
  geometry (centroid → nearest shell element, signed distance along the normal / layer thicknesses) and by the frd
  material number. If the check disagrees, the results are refused with a message rather than shown with a wrong
  mapping.
- The expanded part keeps being rebuilt as a solid result part (`PartMappingEnum.Rebuild`). Result element sets per
  layer (`<section>_LAYER-1` … `-n`) are added to it. The unused original shell nodes the `.frd` lists with `Output=2D`
  are dropped (F10).

### Ply stresses

- Rotate S (and E) from the global frame (F11) into the ply axes (e1(θ), e2(θ), n) → S11, S22, S12, S13, S23 in
  material axes. Nodes are shared only within one layer, so a node is rotated with the mean axes of that layer's
  elements around it. Those agree within the grouping tolerance, except at an orientation boundary, where the
  element-wise values are used.
- Two variants, selectable: the **3D stresses** as ccx computes them, and **plane-stress ply stresses** from the
  in-plane strains (Q̄·ε, σ33 = 0). The second matches classical lamination theory exactly and is what the strength
  values of a ply are meant for. ccx's 3D σ22 differs by about 8 % in c13 (F12).
- The `.dat` ply-axis integration point stresses (F11) serve only as a cross-check in the test scripts.

### Shell in-plane principal stresses (4a)

- A new result field output type, `ResultFieldOutputShellPlaneStress`: project the stress tensor onto the tangent
  plane at each node (normal from the shell mesh for 2D output, from the mapped shell element for expanded output).
  It outputs σ11, σ22, τ12 in the local plane, plus σ_P1, σ_P2, τ_max (in-plane) and the principal angle, assuming
  plane stress (σ33 neglected).
- Works for plain shells as well as for composite plies.

### Failure criteria (4c)

- A new result field output type, `ResultFieldOutputCompositeFailure`: the criterion, and either one layer or all
  layers (envelope = max over layers).
- Criteria: max stress, Tsai-Hill, Tsai-Wu, Hashin (fibre/matrix, tension/compression).
- Outputs: failure index and reserve factor (strength ratio; quadratic solve for Tsai-Wu), plus the governing mode for
  max stress and Hashin.
- Limits come from the layer material's `CompositeStrength`. A layer without limits gives NaN plus a clear message,
  not a silent 0.
- Display "one layer at a time": a layer selector that shows only that layer's elements (the layer element set) with
  its failure field.
  Legends, min/max, and probing work as for any other field.
- Wire into `FeResults.ComputeFieldFromResultFieldOutput`, the result field output dialog (`Forms/...ResultFieldOutput`),
  commands under `0110_Results`, and the API (`add_shell_plane_stress`, `add_composite_failure` on the result field
  outputs), keeping the API rule that a result replace re-specifies the whole definition.

**Done when:** for the [0/90]s plate in tension, the ply stresses in material axes match classical lamination theory
(computed in the test script): the plane-stress variant within 0.5 %, and the 3D variant within 0.5 % for σ11 and
10 % for σ22 (F12). The rotated plate gives the same ply results within 1.5 % (F13), and failure indices
match a hand calculation; and per-layer display works in the GUI.

---

## 8. Out of scope (as offered) and possible follow-ups

- Different material per layer through parameters (string parameters or material IDs), per offer item 3b.
- Ply drop-offs (plies covering only part of a section). The fork's ITA approach; a follow-up could add a region per
  layer.
- Draping simulation (the fork's KinDrape port).
- Continuum shells and layered solid sections (`*SOLID SECTION` with orientation is cheap after Phase 2 and can be
  added if there is time).
- Progressive damage, Puck/LaRC criteria, interlaminar (delamination) criteria.
- `.inp` import of `COMPOSITE` / `*ORIENTATION` / `*DISTRIBUTION` (nice for round trips; cut first).

---

## 9. Verification

- Build per touched project; full `PrePoMax.sln` at the end of each phase.
- New scripts in `APIs/Python/Test_Scripts`:
  - `Test_Model_Composite_Material.py` (strength property, library, journal).
  - `Test_Model_Orientation.py` (axes on plate, rotated plate, cylinder; fallback reporting; `.inp` content).
  - `Test_Model_Composite_Section.py` (layers, checks, S4R/S8 refusal, S6 accepted, `.pmp` batch override of thickness and
    angle, orientation grouping on a flat vs a curved part).
  - `Test_Result_Composite.py` (CLT comparison, rotated-plate invariance, failure indices, in-plane principal stresses).
  - `Test_Result_Composite_Lamination.py` (free plates of an unsymmetric, unbalanced 5 ply laminate under N_x, N_x with a
    30 degree orientation, and M_x: mid-plane strains and curvatures, layer heights, ply stresses and the four criteria
    with their mode against lamination theory evaluated in the script).
- Regenerate History on a GUI-built composite model, and journal replay (`Audit_API_Coverage.py` requires `ToPython`).
- `Python_API_Overview.md` updated with each API change (`Audit_API_Documentation.py`); `PythonEnumVocabulary` for
  every new option string.
- End of change: `Run_Tests.py --filter Test_Model Test_Job Test_Result Test_Api Test_Rpc Audit_` against a copy of
  the Debug folder.

---

## 10. Schedule (≈ 10-11 weeks plus buffer)

| Week | Work | Customer checkpoint |
|---|---|---|
| 1 | Phase 0 spike, design confirmed (**done**, section 3) | ccx facts + design note |
| 1-2 | Phase 1 material + strength (**implemented**, section 4) | - |
| 2-4 | Phase 2 orientation (model, writer, preview, API) (**implemented**, section 5) | **Build 1:** orientations + materials |
| 4-6 | Phase 3 composite section (model, writer, dialog, layer preview, batch params, API) (**implemented**, section 6) | **Build 2:** layups solve in ccx |
| 6-9 | Phase 4 mapping, ply stresses, in-plane principal stresses, failure criteria, per-layer display | - |
| 9-10 | Benchmarks, documentation, full test run, fixes | **Build 3:** release candidate |
| 11-12 | Buffer / customer feedback | Final delivery |

Ask the customer early for one of their real models (geometry + layup + strength values) to use as the acceptance case
alongside the benchmarks.

---

## 11. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| ~~ccx output not as assumed~~ | - | Settled by Phase 0 (F8-F11). The id rule is still checked by geometry on every load, in case a later ccx changes it. |
| ~~Interface averaging~~ | - | None: layers have their own nodes (F8). |
| Many orientation groups on doubly curved parts (up to one per element and angle) | Large `.inp`, slow ccx input | Measure on the customer's model in Phase 2; tolerance as a setting; elements of a flat region always share one group |
| The PrePoMax projection differs from ccx's (F3) | The preview does not show what the solver used | One calculator copying ccx's rule (ξ = η = 0, 0.1° fallback to z); c18 and the `.dat` cross-check in the tests |
| Composite restricted to S8R / S6 (F1) | Users with S4/S3 or S8 meshes | Clear check message naming the element type; meshing advice in the dialog |
| ccx's 3D ply σ22 differs from plane stress (F12) | Disagreement with hand calculations | Plane-stress ply stress variant; both documented |
| Visualisation effort (item named in the offer) | Schedule | Batched glyphs only; the stack diagram is simple; cut the geometry-view preview before the mesh view |
| Fixed budget | Overrun | Cut order: `.inp` import → CSV import → orthotropic expansion → geometry-view preview → Hashin |

## 12. Decisions taken

1. **Model design:** composite is a shell section type (`CompositeShellSection`), and orientation is a separate model
   item assigned to a region (decisions 1-2).
2. **Lamina nu23:** fixed to nu23 = E2/(2·G23) − 1 (Phase 1). Existing lamina results change slightly; this goes in the
   release notes.
3. **Layer order:** layer 1 is at the bottom (the S1, negative-normal side), and layers stack along the normal.
   Phase 0 confirmed that this is ccx's own order (F5), so the writer keeps the list as it is.
4. **Solid sections:** the Phase 2 orientation model and calculator are designed so they also work for solids. The
   `*SOLID SECTION, ORIENTATION=` link is added only if the schedule has room after Phase 4; otherwise it is a
   follow-up (section 8).
5. **Orientation output for shells** (decided after Phase 0, F2-F4): rectangular orientations per group of elements
   with equal computed axes, not `*DISTRIBUTION` (not supported for shells) and not ccx's rotation line (it turns
   about the wrong axis on curved parts).
