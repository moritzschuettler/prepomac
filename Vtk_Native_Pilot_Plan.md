# VTK native C library pilot

## Goal

Measure how long porting `vtkControl` from ActiViz 5.8 to a native C++ library with a flat C API takes when the work is
done by Claude agents. The pilot ports three representative slices, checks each against the current rendering, and
records the effort so the whole port can be estimated. The deliverable is the measurement and the estimate, not
production code. Nothing from the pilot goes to `master`.

## Background

- `vtkControl` is about 23.5k lines: `vtkControl.cs` (11k, 14 `#region`s), `vtkMax/Actor/vtkMaxActor.cs` (2.6k),
  `vtkInteractorStyleControl.cs` (1.6k), about 3.6k in `vtkMax/Widget/`, plus tools and enums.
- Kitware types appear only inside `vtkControl` (30 files). `PrePoMax` uses about 166 of its members, and the geometry
  calls take `vtkMaxActorData`, which is plain data (arrays, colours, ids). The public surface of `vtkControl` is the
  boundary the C library sits behind.
- The native library must not depend on the PrePoMax .NET version: the app is on .NET Framework 4.8 now and moves to
  .NET 10 later. P/Invoke is the same on both.
- Only data crosses the C boundary: opaque handles, numbers, arrays, UTF-8 strings, enums, callbacks. No C++ objects,
  no STL types, no exceptions.

## Inputs

| What | Where |
|---|---|
| PrePoMax sources | a branch `pilot/vtk-native` of `C:\Pc\Programiranje\PrePoMax\PrePoMax v3.0.n dev` |
| VTK 9.7.1 build (shared, x64, Release, VS 2022) | `C:\Pc\Programiranje\KitwareVTK\vtk-9.7.1-build-x64` |
| VTK CMake package | `VTK_DIR=C:\Pc\Programiranje\KitwareVTK\vtk-9.7.1-build-x64\lib\cmake\vtk-9.7` |
| VTK runtime DLLs | `C:\Pc\Programiranje\KitwareVTK\vtk-9.7.1-build-x64\bin\Release` (versioned names, `*-9.7.dll`) |
| CMake | `C:\Program Files\CMake\bin\cmake.exe` |
| MSBuild | `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe` |

The VTK source in `C:\Pc\Programiranje\KitwareVTK\vtk-9.7.1-source` carries local patches (see
`PrePoMax-v2.6-ActiViz-9.7.1-test\VTK97-INTEGRATION.md` there). Only Release VTK is built; a Debug PrePoMax calling
a Release native library is fine because no C++ runtime objects cross the C API.

Follow `AGENTS.md` and `CLAUDE.md` of the PrePoMax repository for C# code, building and testing.

## Phases

### 0. Reference

Record reference output from the current `vtkControl` (ActiViz 5.8) for a few models: a solid part, `Joint_Hinge`,
and a result with the scalar bar. Use fixed window sizes and the standard views (isometric, front, top, zoom to fit).

- Images through `vtkControl.RenderToPNG`.
- Pick output for slice C (see there).

### 1. Groundwork (one-off cost, measured separately)

- A native C++ library (`vtkNative`, CMake) linked against VTK 9.7.1.
- C API conventions: opaque handles, error codes with a last-error message, array and string passing, callbacks from
  C++ to C#.
- C# P/Invoke layer.
- Embedding the render window in the existing WinForms control with `SetParentId(Handle)`.
- Off-screen rendering to PNG.
- Running ActiViz 5.8 and VTK 9.7 in one process (their DLL names differ). Confirm it works: it would let the real
  port go region by region while the old code keeps running.
- Build integration: native DLLs and VTK runtime copied next to `PrePoMax.exe`.

### 2. Slice A: geometry and views (data-heavy code, about 5k lines)

- `AddCells` and `AddActor` with `vtkMaxActor` for mesh parts: surfaces, edges, colours.
- The `Views` region: camera presets, zoom to fit.
- `RenderToPNG`.

### 3. Slice B: scalar bar widget (event-heavy code, about 900 lines)

- `vtkMax/Widget/03_vtkMaxScalarBarWidget.cs` with its mouse interaction (drag, resize, value editing callbacks).

### 4. Slice C: selection and picking (about 2.1k lines, the `Selection` region)

- `vtkControl.Pick(x1, y1, boxSelection, x2, y2)` reports picked point, direction, frustum planes and part names in
  `OnMouseLeftButtonUpSelection`. `Controller` turns those into ids without VTK.
- Port the screen-to-3D step only. Keep the id resolution in C#.

## Checking

- Old and new paths render the same scene in one process; images are compared automatically. VTK 5.8 and 9.7
  defaults differ, so compare with a tolerance and on shape coverage, not exact pixels, and save side-by-side images
  for visual review.
- Slice C: a grid of pixel picks per select mode (node, element, edge, surface, part) and a few boxes (inside and
  crossing). Resolved ids must match exactly, points within a tolerance. Picks that land on a border between two
  elements are reported, not failed. Hover highlight is checked with `RenderToPNG`.
- Interaction (camera feel, widget dragging, hover, rubber band) is checked by the user.
- Pickers that read back the rendered frame need an OpenGL context; an off-screen render window provides it on
  Windows.

## Measurement

Keep `Pilot_Log.md` in the pilot folder, updated as work goes:

- Start and end time of each phase and slice.
- Agent turns and token cost per phase (from the session usage).
- Number of fix cycles before each slice passed its checks.
- Lines of C# replaced and lines of C++ and C API written.
- The user's own review and checking time (the user notes it).
- Problems that would recur across the full port, and problems that were one-off.

## Result

A short report at the end of `Pilot_Log.md`:

1. Measured effort per phase and slice.
2. The remaining `vtkControl` code sorted into the three kinds (geometry and data, widgets and interaction, selection),
   with line counts per kind.
3. Estimate for the full port: groundwork once plus the measured rate per kind, with a range.
4. The C API conventions that worked, as a starting point for the real port.
