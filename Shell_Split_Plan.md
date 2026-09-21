# Shell split plan

A plan for a second splitting tool set whose region is **surfaces** instead of parts. The four
splitting operations stay exactly as they are; only what is picked changes, and with it what the
result is allowed to be: a shell split imprints the faces it is given, and the part it works on is
replaced in place - same kind of body, same name, same ids.

Implemented on 2026-09-21: the kernel in the NetgenMesher repository, the rest here.

## 1. What the present tool does, measured

Run against `Tests/Joints/Joint_Plates.step` (two surface bodies) and `Tests/Joints/Joint_Links.step`
(two solids) with the shipped `ThirdParty/netgen/NetGenMesher.exe`:

| operation | input | result |
|---|---|---|
| Plane, shell part | 1 shell, 1 face | 1 shell, 2 faces - the part keeps its name |
| Plane, solid part | 1 solid, 7 faces | **2 solids**, 14 faces - the part becomes `Compound-1` |
| Extrude edge, shell part, direction in the surface | 1 shell, 1 face | 1 shell, 2 faces - the tool face lies in the surface and divides it anyway |
| Split a face using two points, solid part | 1 solid, 7 faces | 1 solid, 8 faces |

Three things follow.

A surface body already splits, and the C# side already copes with it: the visualization file reports
`Number of solids: 0`, so `GetNumberOfSplitGeometrySolids` sends the result down the replace in place
branch and the part keeps its name, id, colour, element types and geometry selections.

A solid is always cut through its volume, because `BRepAlgoAPI_Splitter` is given a face as the tool.
There is no way to ask for the faces only, and the part that comes back is a different part.

One tool shape does refuse, and it is not about surfaces: sweeping a **closed** edge within its own
plane - a hole circle swept across the plate it sits in - builds a prism that runs over itself, and no
boolean makes sense of it. It fails the same way on a solid, and on the new path as on the old one, so
it is a limit of that tool shape rather than anything this plan touches.

The last row is the mechanism the new tool needs: `SplitAFaceUsingTwoPoints` general fuses the shape
with an **edge**, and an edge cannot cut a volume - it can only subdivide the faces it lies on.

## 2. What the new tool must do

- The region selection takes **surfaces**, one or many, from any CAD part. Nothing else is pickable.
- Both a surface part's faces and a solid part's faces may be picked, in the same operation.
- All four splitting operations are offered, with the splitter and direction selections unchanged, and
  a fifth that only a surface region can have: **Line by two points**, first in the list.
- **No new part is ever produced.** Every part in the region is replaced in place: a shell part comes
  back a shell part with split faces, a solid part a solid with split faces, a compound a compound
  with split faces. The name, the part id, and a compound's sub part names and ids are retained.
- There is no compound part of surfaces, and none is ever created.

Nothing is added to the body and nothing is taken away, so a part cannot come apart: the pieces of a
split face still meet along the edge the split made. A result that does not match the body it is to
replace is therefore a failure of the operation, not a second part - see section 4.3.

### 2.1 Line by two points

A tool that draws its line **on** the surface instead of building a shape in space, because the tools
that build one in space cannot cut a flat shell: every point that can be picked on it lies in its own
plane, so a plane through three of them is the plane of the shell.

It also replaces `BREP_SPLIT_A_FACE_USING_TWO_POINTS`, which projects a straight edge onto the face
with `BRepAlgo_NormalProjection` and **segmentation faults** when the chord between the two points
passes through the axis of the surface - measured on the cylindrical face of `Joint_Links.step` with
two points facing each other across the axis, where the boolean returns a shape that cannot be
written. There is no projection here, so there is no direction to be undefined:

1. the two points are located in the parameter space of each target face with
   `ShapeAnalysis_Surface::ValueOfUV`, and a point that is not on that surface leaves the face out;
2. a periodic direction is walked the short way round;
3. a `Geom2d_Line` through the two parameter points is clipped to the parameter box of the face, so
   the line reaches the boundary and divides it, without running round a periodic surface and cutting
   it twice;
4. `BRepBuilderAPI_MakeEdge(line2d, surface, first, last)` and `BRepLib::BuildCurves3d` give an edge
   that lies on the surface by construction, and it is imprinted like any other.

On a plane this is the straight chord. On a cylinder it is the arc or the generatrix. It is not a
geodesic: on a sphere it is not a great circle, and on an unevenly parametrised freeform face it can
bow. It never fails, which is the trade against a projection that sometimes takes the process down.

## 3. Where the faces are cut - the kernel

The splitting itself is in `C:\Pc\Programiranje\NetgenMesher`, not in this repository.

### 3.1 The tool definition gains a target

`GetSplitGeometryToolDefinition` writes a key and value text file whose parser ignores keys it does
not know, so the format number goes to 2 and three keys are added:

```
Split target: Shell
Target solid: <n>                  opens a group; absent or 0 means the shape itself
Target face ids: <id> <id> ...
```

The face ids are kernel ids, read through `GetSplitGeometryKernelId` from
`Visualization.FaceKernelIds`, exactly as the extend face tool already reads its source face ids.

A compound part is split as a whole, while what the user picks are its sub parts, whose face ids mean
nothing in the compound's brep. The ids are therefore grouped by `Target solid`, and the kernel
resolves them against the faces of that solid in exploration order. This is the same convention the
existing `Solid ids` key relies on - a compound carries its sub parts in the order they were written
out of its brep - and it holds because a sub part's brep was written by exploring that solid.

### 3.2 The face split itself

A new `SplitFacesWithTool(occgeom, tool, fuzzyValue, targetFaces)` beside `SplitWithTool` in
`libsrc/occ/occgeom.cpp`:

1. resolve the target faces from the ids;
2. `BOPAlgo_Builder` with every target face and the tool shape as **arguments**, not as argument and
   tool;
3. for each target face, `Modified(face)` gives its pieces;
4. `ShapeBuild_ReShape::Replace(face, <compound of the pieces>)` for every face that was split, then
   `Apply(occgeom->shape)`;
5. `BuildFMap`, `changed = 1`.

Step 2 is a general fuse rather than `BRepAlgoAPI_Splitter` on purpose. The splitter does not split a
face with a same domain tool face, which is precisely the coplanar prism the extrude edge tool builds
on a flat surface body; a general fuse splits same domain faces into their common and exclusive
parts, so the same call covers both a tool that crosses the faces and a tool that lies in them.

Step 4 is what keeps the body the body: the shape is not rebuilt from its faces, the faces are
replaced inside it, so the shell, the solid and the compound above them survive with their other
faces untouched. This is the whole reason the part can be replaced in place afterwards.

If replacing a face by a compound of faces turns out to need repair, the fallback is the idiom already
proven in this codebase: take the edges of the pieces that are not edges of the original shape, and
general fuse the **whole shape** with those edges, which is what `SplitAFaceUsingTwoPoints` does. It
costs a second pass and is known to leave a solid whole.

The existing "the tool did not split the geometry" check on the face count stays as it is.

## 4. What comes back, and what PrePoMax makes of it

### 4.1 One rule

Every part is replaced in place. The kind of the part decides which of the two existing replacement
paths is taken, and the part is known before the run, so the result does not have to be classified by
counting solids in a visualization file:

| the picked faces belong to | the result brep | how it is put back |
|---|---|---|
| a surface part | one shell, more faces | `ReplacePartGeometryFromFile` |
| a solid part | one solid, more faces | `ReplacePartGeometryFromFile` |
| a compound part | the same solids, more faces | `ImportBrepCompoundPart` followed by `SwapCompoundParts` |

`ReplacePartGeometryFromFile` keeps the name, the part id, the colour and the element types, and
takes `keepGeometrySelections`. `SwapCompoundParts` keeps the compound name, its id and colour, and
the names, ids and visibility of its sub parts; it requires the sub part count to be unchanged, which
an imprint guarantees because it neither adds nor removes a solid.

So a load, a surface, a mesh setup item or a boundary condition that refers to the part still refers
to it after the split. That is the point of the tool: today the same cut renames the part to
`Compound-1` and everything attached to it is lost.

### 4.2 No compound of surfaces

Unreachable through any row of the table, and `CompoundGeometryPart` refuses a sub part that is not a
solid, so the rule needs no new guard.

### 4.3 A result that does not fit is a refusal

An imprint cannot separate a body, so the result should always match. If it does not - a different
solid count, a shell that came apart, a free face - the part is **left as it was** and the output
window says which part and why. It is not imported as several parts, and no part is created.

The check is made before anything is replaced. `ReplaceSplitGeometryPart` already builds a
visualization file for the result to count its solids; the surface path reads the shell and free face
counts from the same file and compares all three with the body it is replacing. This also removes the
one place in the present code that can leave a stray part behind: `ReplacePartGeometryFromFile` does
nothing but a redraw when an import yields more than one part, and leaves the old part in the model
beside the new ones. That branch becomes unreachable, and is turned into an exception so it stays so.

## 5. The C# side

### 5.1 CaeMesh

- `SplitTypeEnum { Solid, Shell }` in `CaeMesh/Enums/`.
- `GeometryFeature` gains `_splitType`, serialized in `GetObjectData` and read in the
  `SerializationInfo` constructor. A file written before this exists comes back as `Parts`, which is
  what every command in it meant.

The four operation classes are not touched. They describe a splitting tool, and the tool is the same
whether it cuts a part or imprints a face.

### 5.2 Controller

`SplitGeometry` keeps its shape - resolve the selections, check validity, size one tool for the whole
region, then one pass per part - and branches on the region type in three places:

- `GetSplitGeometryRegionPartNames`: for `Surfaces`, the parts that own the picked faces, with a
  compound sub part reported as its compound, as now.
- a new `GetSplitGeometryTargetFaceIds(feature, partName)` beside `GetSplitGeometrySolidIds`, giving
  the kernel face ids grouped by solid number for a compound and ungrouped otherwise.
- a new `ReplaceSplitSurfacePart` beside `ReplaceSplitGeometryPart`: the body count check of section
  4.3, then one of the two replacement paths, and a refusal for the part otherwise.

`GetSplitGeometryRegionExtent` is fed the owning parts and needs no change: the tool has to reach past
the bodies the faces belong to, not past the faces.

The report is the existing one. The messages name the part they are about, as the rest of the geometry
operations do, and a part the tool missed or refused does not stop the parts it did not.

### 5.3 The form

A new `FrmSplitShell` in `PrePoMax/Forms/03_GeometryFeature/`, deriving from `FrmSplitSolid`,
which means making the members it has to reach `protected` and `SetSelectItem` `virtual`. It differs
in four points and in nothing else:

- the window text is `Split Shell` and the form name follows it;
- the region arms `SetSelectItemToGeometrySurface()` instead of `SetSelectItemToPart()`;
- `PickCompoundSubPartsSeparately` stays false - a picked face already names one sub part;
- the feature it builds carries `SplitTypeEnum.Shell`.

The region row is one property on `ViewGeometryFeature` shared by both forms, renamed at runtime
through the descriptor the view already installs:

```csharp
public void SetRegionSelectionText(string displayName, string description)
{
    CustomPropertyDescriptor cpd = _dctd.GetProperty(nameof(RegionSelection));
    cpd.SetDisplayName(displayName);
    cpd.SetDescription(description);
}
```

`SetDisplayName`, `SetDescription` and `SetIsBrowsable` are all present on the descriptor and are
already used elsewhere in the forms.

### 5.4 Command and menu

One command still: `CSplitGeometry` takes the name it shows in the history from the feature, so a
shell split reads `Split shell: Plane by three points` and a part split keeps reading
`Split solid: ...`. A second command class would say the same thing twice.

`tsmiSplitShell` is declared in `FrmMain.Designer.cs` in the generated style, next to
`tsmiSplitSolid` in the CAD Part menu, with its handler, the `_frmSplitShell` field, the
`AddFormToAllForms` call and the entry in the `SelectionChanged` dispatch in `FrmMain.cs`.

The Python API does not carry the splitting operations, and `ToPython` keeps saying so.

## 6. Staging

1. **The kernel.** `Split target`, `Target solid`, `Target face ids`, and `SplitFacesWithTool`.
   Verified from the command line before any C# is written, against a shell part, a solid part, a
   compound and a coplanar extruded edge. `NetGenMesher.exe` and `nglib.dll` are then copied into
   `ThirdParty/netgen` as the matched pair its README describes.
2. **The model and the controller.** The enum, the serialized field, the tool definition, the three
   branches and the replacement rules of section 4, driven from the existing dialog with the region
   type forced, before the second form exists.
3. **The form and the menu.**

## 7. Verification

There is no test project, so building the touched projects and running the application is the check.
The kernel steps are checked on their own from the command line, which is where the four measurements
of section 1 came from:

- a solid whose face is imprinted stays one solid and gains faces;
- a surface body gains faces and stays one shell;
- an edge extruded in the plane of a surface body divides only the surfaces that were picked;
- a compound keeps its solid count, and therefore its sub parts.

In the application, the check is that the model tree is unchanged apart from the faces: the same part
names, in the same places, with everything that referred to them still referring to them.
