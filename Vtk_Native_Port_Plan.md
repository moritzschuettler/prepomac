# VTK native library: the full port

## Goal

Replace ActiViz 5.8 in `vtkControl` with `vtkNative`, the C++ library on VTK 9.7 behind a flat C API that the pilot
started. At the end `vtkControl` keeps its public surface (the 189 members PrePoMax calls), every one of them
backed by the native library; no Kitware .NET type is referenced anywhere; ActiViz is removed from the build, the
installer and the README. The work starts from branch `pilot/vtk-native` and lands on `master` only when it is done.

## What the pilot settled

Read `Pilot_Log.md` first; the final report is the basis of this plan.

- Groundwork exists: the C API conventions, the build (CMake inside the `vtkControl` build, the VTK runtime in
  `lib`), embedding, off screen rendering, two VTK versions in one process.
- The mirror is the test method: `vtkControl` sends every change to an off screen native view, a mode shows only
  what a slice ports, the same operations run on both paths, and layout, ids and cameras are compared exactly,
  pictures with a tolerance and side by side images. Every slice below reuses it.
- The VTK 9 rendering defaults found so far live once in the native view: polygon and line offsets, forced opaque
  edges with multisampling 8, the infinite viewer, 2D positions truncated to pixels, the text shift, CR widths,
  picking by ray instead of depth. New actor kinds are checked against them first.
- Formatting (number formats, culture, texts) stays in C#; only finished strings cross the API.
- Measured raw rate: about 45 - 50 lines of old C# per minute of agent time for geometry and widgets, review fixes
  included.

## Why it can be faster than the pilot estimate

The pilot report gave 11 - 21 h of agent time: the measured rates times a factor of 1.5 - 2.5 for the harder
geometry, plus 25 - 40 % integration, one review round per slice, all in sequence. This plan cuts that four ways:

1. **Parallel streams.** Geometry, widgets and interaction, and selection touch different files and different parts
   of the C API. Three sessions run them at the same time, each in its own worktree; the wall clock is the longest
   stream, not the sum.
2. **One harness, written once.** The pilot built a recorder and a comparison per slice. Step 0 turns them into one
   framework with scenes and checks that every slice extends with a few lines.
3. **Bigger slices, fewer reviews.** The bases (part actor, border and text widgets, widget set, picking) are ported,
   so the remaining code is mostly more of the same kind. Slices of 2,000 - 3,000 lines with one review per stream
   end, not per slice.
4. **No factor on the mechanical parts.** The factor stays only on the algorithmic code (section view caps and
   hatches, contours, animation, transformations) and on the camera feel.

Target: about **8 - 10 h of agent time** in total, **4 - 6 h of wall clock** with three streams, plus 4 review rounds.

## Structure of the native library

The pilot put everything into one `vtkNative.h` and one `vtkNativeApi.cpp`. Parallel streams would collide there,
so step 0 splits them by area before the streams start:

| Area | Header | API source | C++ sources |
|---|---|---|---|
| Core (library, view, camera, pictures, events) | `vtkNative.h` (includes the others) | `vtkNativeApi.cpp` | `vtkNativeView*.cpp`, `vtkNativeError`, ... |
| Geometry | `vtkNativeGeometry.h` | `vtkNativeApiGeometry.cpp` | `Geometry/` |
| Widgets | `vtkNativeWidgets.h` | `vtkNativeApiWidgets.cpp` | `Widgets/` |
| Selection | `vtkNativeSelection.h` | `vtkNativeApiSelection.cpp` | `Selection/` |

On the C# side the same split: `vtkControl/Native/Geometry`, `Widgets`, `Selection`, one `VtkNativeMethods` partial
class per area. `vtkControl.cs` is shared; each stream owns its regions (below) and touches other regions only
through one hook line each, which merges cleanly.

The API version becomes one number per area plus the core version, checked together at load, so the streams do not
fight over one constant.

## Step 0 - Preparation (one session, about 45 min)

1. Branch `port/vtk-native` from `pilot/vtk-native`.
2. Split the header, the API source and the C# methods as above; no behavior change. Slices A, B and C and the
   groundwork checks must still pass.
3. The harness framework, `VtkPort/Harness/`:
   - scenes: the solid, Joint_Hinge, a solved result (DISP and MISES), a shell model, a beam model, an assembly with
     contacts and symbols (BCs, loads, coordinate systems, constraints), a section view, an exploded view; each a
     model from `Tests/` or `APIs/Python/Test_Scripts/Models` and a few `pmx` lines;
   - checks: camera, picture (coverage and differing pixels, side by side), layout (rectangles, highlights), ids
     (picks); each takes the two paths' outputs from the mirror;
   - one runner, `Run_Port_Checks.sh --filter <name>`, that runs the checks of a stream or all of them, against a
     copy of the build folder, as `Run_Tests.py` does.
4. A switch in `vtkControl` per area (geometry, widgets, selection) and a target: the off screen mirror, where the
   checks compare, or the native view in the window, where the user reviews (models drawn, widgets dragged, parts
   and elements picked by clicking). ActiViz keeps running underneath either way: its parts still serve the old
   selection, symbols and widgets, so it is removed area by area only in the integration.

## Stream G - Geometry and data (about 11,800 lines; one session; about 4 - 5 h)

Owns: the regions Add geometry, Section view, Scalar fields, Animation, Clear, Settings (rendering part), Follower
view, Transformations, Highlight geometry, Hide/Show geometry, Exploded view; `vtkMaxActor` and `vtkMax/Actor`,
`Animation`, `ColorSpectrum`, `LookupTable`, `CoorSys` symbols, `Tools/SectionView*`.

| Slice | Content | Lines | Check scenes |
|---|---|---|---|
| G1 | The rest of `vtkMaxActor`: node, line and vertex actors, shells and beams, backface colors, actor representation, the overlay layer; Add geometry for those; Highlight, Hide/Show, Clear | ~3,500 | solid, shell, beam, assembly |
| G2 | Symbols: `AddOriented*` and the other symbol builders, coordinate system symbols, captions, the min/max annotation anchors | ~2,000 | assembly with symbols |
| G3 | Results: scalar fields, color spectrum and transfer functions, lookup tables, contours, the result coloring of parts, undeformed overlays | ~2,000 | result |
| G4 | Section view with caps and hatches, exploded view, follower view, transformations (symmetry, patterns) | ~2,300 | section, exploded, result with symmetry |
| G5 | Animation (frames, scale factors, the camera animation of the Views region), `RenderToPNG` with magnification and transparency | ~1,500 | result animation frames |

Each slice: port, extend the mirror to the new actor kind, add its scenes, pass the checks, commit.

## Stream W - Widgets and interaction (about 6,100 lines; one session; about 2 - 3 h)

Owns: the region Widgets, `vtkMax/Widget`, `vtkInteractorStyleControl.cs`, `Tools/PrecisionTouchpad.cs`,
`TrackpadGestureRecognizer.cs`, `TrackpadNavigation.cs`, the mouse handlers outside regions.

| Slice | Content | Lines | Checks |
|---|---|---|---|
| W1 | The remaining widgets on the ported bases: status block, scale bar, color bar, text with arrow (probe and min/max annotations), coordinate system triad with its targets | ~2,300 | layout and pictures as slice B; the triad clicks |
| W2 | The interaction style: rotate, pan, zoom, smooth zoom, rotation center, box selection rubber band, keys, the view change events | ~2,500 | camera after scripted event sequences on both paths (exact); embedded checks with Win32 messages |
| W3 | Touchpad and trackpad navigation, the Widgets region (settings, positions, visibility, context menus) | ~1,300 | scripted gestures where possible; the rest by the user |

The camera feel is the one thing no check replaces: W2 ends with a toggle script for the user, as slice B did.

## Stream S - Selection (about 1,500 lines; one session; about 1 - 1.5 h)

Owns: the region Selection.

| Slice | Content | Lines | Checks |
|---|---|---|---|
| S1 | The id resolution helpers: `GetGeometryPickProperties` (closest cell, cell face and edge nodes), `GetPointAndCellIdsInsideFrustum` (frustum extraction on the volume grids), the edge and surface angle helpers | ~600 | the slice C pick plan with ids from the native helpers only |
| S2 | Hover and selection highlights in the selection layer, the highlight branch of `PickByArea` | ~500 | pictures of hover and selection per mode, as phase 0 recorded them |
| S3 | The geometry selection modes (vertex, edge, surface, edge angle, surface angle, part) | ~400 | pick plan on the geometry view |

S2 draws through the part actor of stream G; it needs only what slice A already ported, so it does not wait for G.

## Step I - Integration (one session after the streams; about 2 h)

1. Merge the three streams into `port/vtk-native` (G, then W, then S); run all checks.
2. Turn the three switches on by default; run the checks with the mirror reversed (the native path in the window,
   ActiViz off screen) once, then remove the mirror and ActiViz: the `Kitware.VTK` references, the designer fields,
   the ActiViz installer part and the README section.
3. Move what is left of `vtkControl` into the thin C# layer: the public members call the native library; data types
   (`vtkMaxActorData`, `PartExchangeData`) stay as they are.
4. The Python API tests: `Run_Tests.py` in full (every group, the meshing ones included, since meshing draws), and
   the picture based tests against the pilot's phase 0 reference where the scenes overlap.
5. Packaging: the VTK runtime in the release folder, the license texts in `Licenses/` (VTK's BSD), the size of the
   release (about 217 MB of VTK DLLs in the pilot's runtime; strip the modules not loaded).
6. Update `CLAUDE.md`, `AGENTS.md` and the README for the new build (CMake, VTK_DIR, no ActiViz).

## Reviews by the user (4 rounds)

1. After G1 + G2: models and symbols in the native window (a toggle script per stream, as in slice B).
2. After W2: the camera feel and the widgets.
3. After G3 - G5 and S: results, section view, animation, selection and hover.
4. After the integration: a day of normal use on the release build.

Each round is a list of what to look at and a toggle script; findings go back to the stream that owns the code.

## Schedule

| When | Sessions |
|---|---|
| Hour 0 - 1 | Step 0 (one session) |
| Hour 1 - 5 | Streams G, W and S in parallel (three sessions, worktrees); S finishes first and takes G5 if G is behind |
| Review 1 and 2 during the streams | |
| Hour 5 - 7 | Integration (one session), review 3 |
| After | Review 4, fixes, merge to master |

## Measurement

`Port_Log.md`, as `Pilot_Log.md`: start and end per slice, plan usage, fix cycles, lines ported and written, the
user's review time, and the rendering defaults each slice found. The plan's estimate is checked after G1 and W1:
if a stream runs more than 50 % over, the remaining slices of that stream are split and the schedule redone.

## Risks

- **Rendering defaults of new actor kinds** (points and glyphs, translucency with depth peeling, contour lines,
  section caps). Each cost one fix cycle in the pilot; expect five to ten more.
- **The section view and the result coloring** have no pilot slice; G3 and G4 carry the factor of this plan.
- **Merge conflicts in `vtkControl.cs`**: mitigated by the region ownership and step 0's split; the integration
  merges in a fixed order.
- **Picking behavior changes** (border and tolerance picks, slice C): selections recorded with ActiViz may resolve
  to a neighboring element on regeneration. Decide once (accept, or emulate the old tolerance) before S1.
- **Interactive feel** cannot be checked automatically; the reviews are on the critical path.

## Done

- No `Kitware.VTK` reference in the solution; the build has no ActiViz.
- All port checks and all `Run_Tests.py` groups pass on the release build.
- The four review rounds are closed.
- `Port_Log.md` records the effort against this plan.
