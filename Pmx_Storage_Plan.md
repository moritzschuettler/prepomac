# New .pmx storage - plan

Goal: replace every use of `BinaryFormatter` (the `.pmx` file, the `.pmh` history, the settings file and
`DeepClone`) with an explicit, versioned, schema based format that is fast, survives code refactoring for the long
run, and runs unchanged on .NET Framework 4.8 and on modern .NET (8+). When this is done, file storage no longer
blocks moving PrePoMax to modern .NET.

---

## 1. Starting point

### Where BinaryFormatter is used

| Use | Code | What is serialized |
|---|---|---|
| `.pmx` save/open | `Controller.SaveToPmx`, `Controller.OpenPmx` and the `TryReadCompressedPmx*` readers | `object[] { Controller, _jobs, treeStates, sidePanelsState }`. Through `Controller` this pulls in `_model` (without mesh), `_allResults` (without field data), `_annotations`, `_commands`, and `_results` (compatibility v1.3.3). |
| `.pmh` history | `CommandsCollection.WriteToFile` / `ReadFromFile` | The whole `List<Command>`, rewritten after **every** command. |
| Settings | `SettingsContainer.SaveToFile` / `LoadFromFile` | `ToDictionary().DumpToFile(...)` |
| Deep copies | `ExtensionMethods.DeepClone` (`CaeGlobals/Extensions.cs`) | About 2,200 call sites; every command clones its arguments with it. |
| Helpers | `DumpToFile`, `DumpToStream`, `LoadFromFile`, `Tools.LoadDumpFromFile` | All go through `RenamedTypeSerializationBinder`. |

### Already outside BinaryFormatter (kept)

- Mesh and geometry: `FeModel.WriteToBinaryWriter` / `ReadFromBinaryReader` → `FeMesh.WriteToBinaryWriter`.
- Results field data: `ResultsCollection.WriteToFileStream` / `ReadFromBinaryReader`.

Both are hand-written, versioned binary and are the right tool for bulk numeric data.

### Size of the serialized surface

| Project | `[Serializable]` files | with `ISerializable` |
|---|---|---|
| CaeGlobals | 30 | 8 |
| CaeMesh | 120 | 29 |
| CaeModel | 254 | 99 |
| CaeResults | 49 | 7 |
| CaeJob | 3 | 1 |
| PrePoMax (mostly commands) | 517 | 8 |
| vtkControl, UserControls | 28 | 0 |

Compatibility shims: `PrePoMax/Commands/_Compatibility/` (v1.3.5 ... v2.6.4) plus the `SerializationInfo`
constructors with `case "_name":` fallbacks.

---

## 2. Design decisions

1. **Explicit schema in code with stable numeric keys.** Every stored type carries `[PmxType(id)]` and every stored
   field `[PmxKey(n)]` (attributes of `CaeGlobals`); a field is stored under the number of the class that declares it
   combined with its key, and a value in an `object`, interface or base class slot is written with the number of its
   type. Files depend only on these numbers, never on class, field, namespace or assembly names, so refactoring cannot
   break a file. Fields are tagged, so a newer PrePoMax reads an older file and a field the file lacks starts at zero
   or null unless an `[OnDeserialized]` method sets it. An older PrePoMax reading a newer file skips fields it does
   not know, and refuses the file, naming the reason, when a skipped field held an object referenced elsewhere or a
   type it does not have. *(Changed in Phase 0 from `[MessagePackObject]`/`[Key]`/`[Union]`; see the Phase 0 results.)*
2. **Serializer: MessagePack wire format, PrePoMax's own resolver.** `MessagePackWriter`/`MessagePackReader` of
   MessagePack-CSharp (netstandard2.0, runs on net48 and modern .NET) with formatters in `CaeGlobals/Serialization`
   that keep the binary formatter's semantics: objects created without a constructor, read only fields set through
   emitted methods, dictionary comparers kept, deserialization callbacks run. The source generator can replace the
   reflection after the SDK style move. Alternative considered: protobuf-net (similar robustness, slower, weaker
   polymorphism).
3. **Persisted data keeps its references.** Each payload numbers the objects it writes and writes an object met again
   as a reference to its number, so the back references of the model (a surface to its constraint) and shared
   arrays come back as they were, without code changes. Cross references between items are by name, as before.
   *(Changed in Phase 0 from "a tree, not a graph" after the graph audit.)*
4. **`DeepClone` is a round trip through the same serializer.** One set of rules: what is cloned is exactly what is
   saved. Expected 10-50x faster than BinaryFormatter; measured 3-9x on large models (Phase 3).
5. **`Controller` is no longer persisted.** A new root `PmxDocument { Model, Results, Commands, Annotations, Jobs,
   UiState }` is what is saved.
6. **Container: chunked file, not ZIP.**
   ```
   header       magic "PMX2", format version, writer program version
   chunk table  name, offset, length, codec, CRC32 (per chunk)
   chunks       document (model without mesh, history, annotations, jobs, ui), mesh, results/<name>
   ```
   - Zstd compression (ZstdSharp.Port, pure managed), chunks and 1 MB frames within them compressed in parallel.
   - CRC32 per chunk: corruption is detected and located; a damaged results chunk still lets the model open.
   - Results chunks are loaded lazily, when first shown.
   - Mesh and results stay hand-written binary (`FeModel.WriteToBinaryWriter`, `FeResults.WriteToFileStream`).
   - A `pmx-dump` tool prints any chunk as JSON for debugging.
7. **History is an append-only journal.** One frame per command: length + CRC32 + MessagePack payload. Saving a
   command appends one frame instead of rewriting the whole history; a crash loses at most the last command.
8. **Schema snapshot test.** `Schema_Snapshot.txt` lists every type number, every `(type, key, member type)` and
   every enumeration member value. `PmxTool schema --check` runs after every build and fails when an existing entry
   changes meaning or disappears; new entries are allowed, removed ones are retired and never reused. This is the
   long run safeguard.
9. **Legacy files in their own project.** `PrePoMax.LegacyImport` reads old `.pmx`/`.pmh`/settings files with an
   NRBF decoder, without the binary formatter. Old files name the domain types, so the `SerializationInfo`
   constructors and the `_Compatibility` shim types stay in the domain assemblies and the import calls them;
   `GetObjectData`, `ISerializable` and `[Serializable]` are removed. *(Changed in Phase 5; see there.)*
10. **Settings are plain JSON.** Small, human editable, independent of everything else.

---

## 3. Phase 0 - Spike (verify before committing)

- [x] MessagePack-CSharp on net48 with the legacy csproj and `packages.config`: dynamic resolver works; check
      whether the source generator works with C# 7.3 projects (`PrePoMax`, `APIs` pin `LangVersion` 7.3) or only
      after the move to SDK style projects.
- [x] ZstdSharp.Port on net48: speed and ratio against `Tools.Compress` on 3-4 real `.pmx` files.
- [x] `System.Formats.Nrbf`: confirm it runs on net48 and decodes a real `.pmx` payload (`object[]` with
      `Controller`). Fallback if not: a net48 `PmxUpgrader.exe` that loads with BinaryFormatter and writes the new
      format (same pattern as `GmshCaller`).
- [x] Graph audit: a walker over a loaded model that reports every object reached twice and every cycle. The list
      decides how much of decision 3 needs code changes.
- [x] Types that BinaryFormatter tolerated only because they were null at save time (VTK objects, delegates,
      events): list them.
- [x] Baseline timings: save, open and `DeepClone` of a large model with results.

Exit: a go/no-go note per item, and the measured baseline.

### Phase 0 results (2026-11-15)

Measured with throw-away spikes (net48 console programs against the v3.0.n Debug build) on files from `C:\Temp`; the
corpus used for every round trip is described under Verification.

**MessagePack-CSharp - GO, with a hand-written resolver instead of the dynamic resolver and the generator.**
MessagePack 3.1.12 runs on net48 from `packages.config`; its dependencies are the ones `CaeGlobals` already has
(System.Memory 4.5.5, Unsafe 6.0, Immutable 8.0, Bcl.AsyncInterfaces 8.0) plus `Microsoft.NET.StringTools`. The source
generator is not usable: the legacy csproj files do not run analyzers from `packages.config`, and `PrePoMax`/`APIs` pin
C# 7.3. The plan's fallback - a resolver that works by reflection - is taken, but written for PrePoMax rather than
using `DynamicObjectResolver`, because the stock resolver cannot keep what BinaryFormatter guarantees today and the
2,200 `DeepClone` call sites rely on:
- objects are created without running a constructor (most domain types have no parameterless one) and read only
  fields are set (emitted IL, as BinaryFormatter does);
- shared references and cycles survive (see the graph audit);
- the comparer of a dictionary or hash set survives (model collections are case insensitive);
- `[OnDeserialized]`/`IDeserializationCallback` run, as `OrderedDictionary` and `GeometryMetadata` need.
MessagePack is used for its wire format and its `MessagePackWriter`/`MessagePackReader`; the formatters live in
`CaeGlobals/Serialization`. The source generator can replace the reflection part after the SDK style move.

**ZstdSharp.Port - GO, level 3.** ZstdSharp.Port 0.8.5 (0.8.8 needs System.Memory 4.6, which would need binding
redirects across the solution). The model payload (BinaryFormatter dump + mesh), compressed:

| File | Raw | Deflate Optimal (today) | Zstd 3 | Zstd 9 |
|---|---|---|---|---|
| Model.pmx (v2.5.1) | 2.89 MB | 1.18 MB, 128 ms / 6 ms | 0.97 MB, 11 ms / 2 ms | 0.88 MB, 55 ms / 2 ms |
| Gripper.pmx (v2.6.3) | 13.2 MB | 5.07 MB, 497 ms / 29 ms | 4.53 MB, 49 ms / 17 ms | 4.23 MB, 245 ms / 17 ms |
| impeller.pmx (v2.6.0) | 51.8 MB | 25.9 MB, 2069 ms / 118 ms | 25.4 MB, 173 ms / 67 ms | 23.2 MB, 654 ms / 69 ms |

(times are compress / decompress). Zstd 3 is smaller than today's Deflate and compresses 10-12x faster.

**System.Formats.Nrbf - GO.** System.Formats.Nrbf 9.0.11 runs on net48 and decodes the `object[]` payload of every
version tried (v1.3.5, v1.4.0, v2.0.6, v2.5.1, v2.6.0, v2.6.1, v2.6.3, v3.0.1): Gripper.pmx 176,660 records in 87 ms.
The fallback `PmxUpgrader.exe` is not needed. It brings System.Reflection.Metadata 9 and System.Collections.Immutable 9,
so it stays inside `PrePoMax.LegacyImport`.

**Graph audit - decision 3 amended: references are preserved by the serializer.** Objects reached twice in real
files:
- model items through back references: `FeSurface._parentMasterMultiRegion`/`_parentSlaveMultiRegion`/
  `_parentMultiRegion` and `FeGroup.ParentMultiRegion` point at the constraint, contact pair, BC, load, section or
  initial condition that also sits in its model collection;
- `ResultsCollection._currentResult` is also an entry of `_results`;
- `GeometryMetadata._kindIdEntity` indexes the entities of `_entities`;
- `CalculixUserKeywords` keeps the same `int[]` key in its list and its dictionary;
- `SelectionNodeMouse._partOffsets` arrays shared between selection nodes;
- `CReplaceModelProperties._newModelProperties` holding the live `ModelProperties` of the model;
- comparers, `Type` objects.
The only cycle is `CommandsCollection._controller -> Controller`, which exists only because `Controller` is the root of
today's file. Rewriting the back references as names would touch the selection and region code everywhere, so the
serializer numbers every object it writes and writes an object met again as a reference to its number - the same
graph BinaryFormatter keeps. Persisted data is still written as a tree of tagged values; only the reference marker
(an extension value) makes it a graph. The cycle goes away with `PmxDocument` (Phase 1).

**Types tolerated only because they were empty at save time.** Delegates: `AnalysisJob.DataOutputEvent`,
`JobStatusChanged`, `PreRun`, `PostRun`, `LastRunCompleted`, `ExecutableJob.AppendOutput`, `PanelPage.SetViewEvent`, and
`CommandsCollection.EnableDisableUndoRedo` (not empty: BinaryFormatter wrote the whole `Controller` it points to).
Non-serializable field types: `DynamicCustomTypeDescriptor _dctd` in about 40 `View*` wrappers, `ItemSetData` fields of
the `View*` wrappers and transform parameters, `SearchContactPair._masterSlaveItem`. Types that need an explicit
formatter: `System.Drawing.Color` (48 fields), `Size`, `System.Type` (5), `FileInfo` (3). The new serializer never
stores a delegate, stores a type without a schema only while it is null (as BinaryFormatter did), and has formatters for
the drawing types, `Type` and `FileInfo`.

**Baseline timings (BinaryFormatter, model part of the file only).**

| File | Dump | Deserialize | Serialize | `DeepClone(model)` | `DeepClone(commands)` |
|---|---|---|---|---|---|
| Model.pmx | 0.90 MB | 22 ms | 10 ms | 9 ms | 25 ms |
| Gripper.pmx | 5.99 MB | 581 ms | 26 ms | 46 ms | 93 ms |
| impeller.pmx | 24.8 MB | 55 ms | 41 ms | 70 ms | 88 ms |

Phases 3 and 4 are measured against this binary formatter baseline with the same files: `DeepClone` of the model,
the history and the results, and the storage part of a save and an open (the model dump with its mesh and the
results, compressed as the file stores them).

### Review of the major decisions (Fable 5.2, 2026-11-15)

The user asked for each major decision to be put to Fable 5.2 with the code, the options and the measurements. Its
recommendation and the decision taken:

| Decision | Recommendation | Decision |
|---|---|---|
| Resolver | Hand-written on MessagePackWriter/Reader; the stock resolver needs constructor and readonly changes on ~1,100 types and still loses references and comparers | As recommended |
| Type and member identity | One `[PmxType(id)]` per type and `[PmxKey(n)]` per declaring class; enumeration members in the snapshot; ids append only after the first assignment; a removed type gives an honest message | As recommended: enumeration members are snapshot entries (`E`), the unknown-type message names a removed type as a cause, retired numbers stay in the snapshot |
| Shared references and cycles | The serializer keeps references; fix `CReplaceModelProperties`, which stored the live `ModelProperties`; an older reader must not silently turn a reference into a skipped field into null | As recommended: the command now executes with a clone, and a reference to an object the reader skipped fails with "written by a newer version" instead of reading as null |
| Container | Chunked file, not ZIP; model, annotations, jobs and UI in one document; history as its own chunk in journal frames | Container as recommended, but the history stays in the document: `CommandsCollection` carries its position and view state with the commands, one payload keeps one reference space, and the CRC of the document already locates damage. Saving loads every result first and goes through a temporary file, as Fable required |
| Journal | Design right; consider a durable flush, a Debug check of commands changed after recording, surfacing a cut-off journal | Kept without `Flush(true)` (the history was never flushed to disk before either, and one fsync per command would slow scripted sessions); a cut-off journal reads up to the last intact frame |
| Legacy import | NRBF reader in `PrePoMax.LegacyImport`, the only option that survives modern .NET; the Immutable 9 redirect is process wide | As recommended; the redirect is in `PrePoMax.exe.config`, `PmxTool.exe.config` and `GmshCaller.exe.config` |
| Phase 5 scope | Keep the `SerializationInfo` constructors and `_Compatibility` shims (old files name those types); delete `GetObjectData` and `ISerializable`, the legacy reader then detects the constructor; drop `[Serializable]` last, after checking clipboard and `.resx` | As recommended (the clipboard carries only text and images, the `.resx` objects are only `System.Drawing.Color`) |
| Objects of removed types (after the Phase 5 commit) | Drop the null entries in `OrderedDictionary.OnDeserialization` only when the legacy reader passes its report as the sender and dropped something, not in the reader (it would edit BCL payloads) and not always (legitimate nulls); remove at import the commands that held a dropped object and the load commands naming only dropped loads, by an explicit command table, not reflection over string fields (false positives) and not by tolerating missing names in the Controller during replay (permanent, and hides real bugs in undo too); warn in the output, not as replay errors | As recommended, with the load commands marked by `ILoadNamesCommand`; added the numbered sibling rule for a removed name no kept command made, which `tmp_massLoad.pmx` needs |
| Equation callbacks after a read (found by the Phase 3/4 test runs) | The owner wires them in an `[OnDeserialized]` method calling its own setters; not the legacy constructors on every read, not a back reference from the container; plus a check that a new owner cannot forget it; audit the other `[NonSerialized]` state the legacy constructors set | As recommended: `RestoreEquationCallbacks` in 51 classes, `PmxTool schema --check` fails a type with an equation setter and no `[OnDeserialized]` method; the audit found `SelectionNode._hash`, reset to -1 in `[OnDeserializing]` |
| Settings JSON | Yes; per-section fallback; invariant numbers; atomic write; import `settings.bin` only when there is no `settings.json` | Per-section fallback, invariant numbers and the temporary-file write are in. `settings.bin` is also imported when it is newer than `settings.json`, because the documented upgrade is to copy it into a new folder after its first start; the folders are portable, so two versions never share one |

Fable also flagged that a field missing from an older file comes back zero or null rather than at its initializer
value (an `[OnDeserializing]`/`[OnDeserialized]` method sets such a default, as the binary formatter required too),
and that the `SerializationInfo` constructors no longer run for new files; the dual mode of Phase 2 compares exactly
that difference over the whole test suite.

The serializer and type ids follow decision 1 with two changes recorded here: the stable numbers are written as
`[PmxType(id)]` on every stored type and `[PmxKey(n)]` on every stored field (attributes of `CaeGlobals`, so the domain
projects do not reference MessagePack), and polymorphism uses the type number of the value instead of `[Union]` lists on
the bases - every stored class has one number, which also names it in an `object` or interface slot.

---

## 4. Phase 1 - One seam for all serialization

- [x] `IObjectSerializer` in `CaeGlobals` with `Clone`, `Serialize(Stream)`, `Deserialize(Stream)`.
- [x] Route `DeepClone`, `DumpToFile`, `DumpToStream`, `LoadFromFile`, `Tools.LoadDumpFromFile` through it. The
      BinaryFormatter implementation stays the default; nothing changes for the user.
- [x] Add `PmxDocument`; `SaveToPmx` and `OpenPmx` build and read it instead of `object[] { this, ... }`. The file
      is still BinaryFormatter at this point (a new internal layout, reader keeps old layouts).

Exit: all Python test groups pass; old files open; files saved by this build open in this build.

**Done.** `CaeGlobals/Serialization`: `IObjectSerializer`, `BinaryObjectSerializer` and the static
`ObjectSerializer.Default`. `PmxDocument { Model, AllResults, Commands, Annotations, Jobs, TreeStates,
SidePanelsState }` is what `SaveToPmx` dumps; the readers return a `PmxDocument` for the new layout and build one from
the `object[]` of the old layouts (`CreatePmxDocumentFromLegacyData`), so `OpenPmx` has one path.
`CommandsCollection._controller` and its undo/redo event are `[NonSerialized]`, which removes the only cycle of the
graph audit; old files still carry them, which the binary formatter skips. Verified with the test groups of the
Verification section plus `Test_Model_Document`, `Test_Record_And_Run` and `Test_Batch`, and the corpus run.

---

## 5. Phase 2 - Schema

- [x] One-off Roslyn script: for every `[Serializable]` type, add `[MessagePackObject]` and `[Key(n)]` to the members
      BinaryFormatter writes today (fields not `[NonSerialized]`), keys in declaration order; `[IgnoreMember]` on
      the rest; `[Union]` on every abstract base with ids per numbered command folder / model area (id ranges
      documented in this file).
- [x] Fix the graph audit findings (shared references → names; cycles → back references rebuilt after load).
- [x] Constructors/setters MessagePack needs (`[SerializationConstructor]` or `internal` setters).
- [x] Generate `Schema_Snapshot.txt` and the check that compares against it.
- [x] Debug-only dual mode for `DeepClone`: clone with both serializers and compare the results.

Exit: dual mode reports no differences across all Python test groups.

**Done, with the attributes of the Phase 0 decision.** A Roslyn script (kept out of the repository, it ran once) gave
the 1,117 `[Serializable]` types of the compiled sources `[PmxType(id)]` and every field the binary formatter writes
`[PmxKey(n)]` in declaration order - an auto-property `[field: PmxKey(n)]`, a field marked `[NonSerialized]`
nothing, so it stays out. `Controller` keeps no keys, since it is no longer stored. The id ranges, in sorted file
order within each project:

| Project | Ids |
|---|---|
| CaeGlobals | 1000 - 1999 |
| CaeMesh | 2000 - 2999 |
| CaeModel | 3000 - 4999 |
| CaeResults | 5000 - 5999 |
| CaeJob | 6000 - 6099 |
| vtkControl | 6100 - 6199 |
| UserControls | 6200 - 6499 |
| PrePoMax (commands in numbered folder order) | 10000 - 19999 |

A field is stored under `(id of the declaring type << 10) | key`, so a type and its bases number their fields
independently and a key never collides across a hierarchy.

- The graph audit needs no code change: the serializer keeps shared references and cycles (Phase 0), and the one
  cycle went away with `PmxDocument`.
- No constructors or setters were needed: objects are created uninitialized and fields, read only ones included, are
  set through emitted dynamic methods, as the binary formatter does.
- `Schema_Snapshot.txt` lists every type number with its kind and every `(type, key)` with the stored type of the
  field, by number where the field type is a stored type. `PmxTool schema --check` runs after every build of
  `PrePoMax` (its `PostBuildEvent`) and fails the build when an entry changed its meaning or disappeared;
  `PmxTool schema --update` adds new entries and keeps removed ones as retired (`R`), so a number is never reused.
- Dual mode: a Debug build started with `PREPOMAX_DUAL_SERIALIZER=1` clones with both serializers, keeps the binary
  formatter's clone, compares the two graphs field by field (`PmxGraphComparer`) and logs every difference to
  `%TEMP%\PrePoMax\pmx_dual_serializer.log`. The whole Python test suite ran in dual mode with an empty log.
  Before that, every file of the corpus was cloned with the new serializer and compared with the binary formatter
  graph: no differences.
- `CReplaceModelProperties` executed with the `ModelProperties` it stores, so the history and the model shared one
  object (graph audit); it now executes with a clone, as every other command does.

---

## 6. Phase 3 - Switch DeepClone

- [x] MessagePack implementation of `IObjectSerializer.Clone` becomes the default.
- [x] Measure against the Phase 0 baseline.

Exit: all Python test groups pass; Regenerate History reproduces the same model on the reference files.

**Done.** `ObjectSerializer.Default` is `PmxObjectSerializer`, for `DeepClone` and for the dump helpers, so a material
library, the Gmsh data handed to `GmshCaller` and a history file are written as storage payloads too. A stream that is
not a payload goes to `ObjectSerializer.LegacyReader`, here still the binary formatter.

Measuring showed one type the plan had not foreseen: `GeometryMetadata` gives the binary formatter its entities as
one block of bytes through `GetObjectData`, because stored one object at a time the faces and edges of an assembly
are hundreds of thousands of objects. The new serializer stored its fields and was slower on such models, so the
type now keeps the same block in a stored field filled by `[OnSerializing]` and read back by `[OnDeserialized]`; its
two dictionary fields are retired in the schema snapshot (the first use of the retire mechanism).

`DeepClone`, median of 7 after a warm-up, BinaryFormatter / new:

| File | Model | History | Results |
|---|---|---|---|
| Model.pmx | 6 / 1 ms | 10 / 3 ms | 0 / 0 ms |
| Gripper.pmx | 65 / 24 ms | 18 / 2 ms | 17 / 5 ms |
| impeller.pmx | 85 / 20 ms | 8 / 2 ms | 6 / 4 ms |
| sweep_14_2.pmx | 5 / 1 ms | 1 / 0 ms | 0 / 0 ms |

The payloads are 5-35 % smaller than the binary formatter's. The first clone of a session also builds the plans of
the types it meets (reflection and emitted accessors), about 100 ms in all.

**Found later.** The full test runs of the Phase 3 and Phase 4 commits, which finished during Phase 5, failed 20 of
81 scripts on two faults of this phase, both fixed in the Phase 5 commit:
- `GmshCaller.exe` reads its data as a storage payload since this phase, and MessagePack needs the binding redirects
  of `GmshCaller.exe.config`, which the post-build step never copied into `lib` (17 meshing scripts).
- The check and changed callbacks of `EquationContainer` are `[NonSerialized]` delegates that the `SerializationInfo`
  constructors wired on every binary formatter clone; the new clone left them null, so a replaced load kept its old
  magnitude and a zero value was no longer refused (3 scripts). The dual mode of Phase 2 compared fields, not
  delegates. Each owner now wires them in `RestoreEquationCallbacks`, an `[OnDeserialized]` method calling its
  setters, and `PmxTool schema --check` fails a type with an equation setter and no such method. An audit of the
  other `[NonSerialized]` state those constructors set found `SelectionNode._hash`, which must start at -1 (a clone got
  0, a key the selection cache shares between nodes).

---

## 7. Phase 4 - New container and journal

- [x] Chunked container writer and reader (`CaeGlobals/FileInOut` or a new `CaeStorage` project), with CRC32 and
      Zstd.
- [x] `SaveToPmx` writes the new format; `OpenPmx` detects the magic and reads new or old.
- [x] Results chunks loaded lazily.
- [x] History journal replaces `.pmh`; the old `.pmh` still reads.
- [x] Settings to JSON (old binary settings still read once, then rewritten).
- [x] `pmx-dump` tool.

Exit: round trip of every reference file (old → open → save new → open → Regenerate History) gives identical models
and results; save and open faster than baseline.

**Done.** In `CaeGlobals/Storage`:
- `PmxContainer` - the layout of decision 6: magic `PMX2`, container version, the program name and version at
  offset 8 (so `GetPmxFileVersion` and the backup of an older file keep working), and a table of contents with name,
  codec, offset, lengths and CRC-32 of each chunk, itself guarded by a CRC-32. `PmxChunk` compresses a chunk with Zstd
  level 3 (Optimal), 1 (Fastest) or not at all, following the compression setting, as independent 1 MB frames that
  are compressed and decompressed in parallel. `PmxCrc32` is the zlib CRC-32, eight bytes at a time.
- A `.pmx` holds the chunks `document` (the `PmxDocument` payload), `mesh` (`FeModel.WriteToBinaryWriter`) and
  `results/<name>` (one per result, `FeResults.WriteToFileStream` with its blocks left uncompressed, since the chunk
  is compressed as a whole). `OpenPmx` reads a container by its magic and every older file as before. A damaged
  result chunk drops that result with a warning and the model still opens; a damaged document or mesh names the
  part that is damaged. Saving over a file in the older layout keeps a backup of it even when it was written by the
  same version, which could not read the new one.
- Results are read lazily: `ResultsCollection.SetPendingLoad` keeps the chunk of each result, which is read the
  first time the result is used (`CurrentResult`, `GetResult`); a save writes every result.
- `PmxJournal` is the history: `PMH2`, then one frame per command (length, CRC-32, payload). `CommandsCollection`
  appends the commands recorded since its last write and writes again from the first command that changed; the last
  command written is serialized once more on the next write, since a save command stores the hash of its file after it
  was recorded. A reader stops at the first damaged frame. A `.pmh` of an older version is still read.
- Settings are `settings.json` (`PmxJsonSerializer`: the stored fields by name, enumerations by member name, colors
  by name or #AARRGGBB, a field the file does not name keeps its default). A `settings.bin` is read when there is no
  `settings.json` or it is newer - an older version's settings copied in after the first start - and rewritten as
  `settings.json`. README, `Python_API_Overview.md` and the automation settings page name the new file.
- `PmxTool dump <file> [chunk] [--all]` prints the table of contents of a container with the state of every CRC and
  the document as JSON, with type and field names resolved from their numbers; it also reads a journal frame by frame
  and a single payload.

Storage part of a save and an open (same files as Phase 0, median of 7, the old layout written with Deflate
Optimal):

| File | Old save | New save | Old open | New open, current result | New open, all results | Old / new size |
|---|---|---|---|---|---|---|
| Gripper.pmx | 1855 ms | 452 ms | 686 ms | 583 ms | 592 ms | 41.8 / 41.8 MB |
| impeller.pmx | 3600 ms | 483 ms | 1104 ms | 923 ms | 959 ms | 49.8 / 51.4 MB |
| Model.pmx | 224 ms | 21 ms | 60 ms | 42 ms | 45 ms | 1.9 / 1.6 MB |
| Tree_res.pmx | 1366 ms | 388 ms | 544 ms | 472 ms | 479 ms | 27.9 / 28.0 MB |
| bug_sdv.pmx | 1915 ms | 592 ms | 743 ms | 107 ms | 655 ms | 50.5 / 50.1 MB |

Saving is 3-10 times faster, opening 10-30 % faster, and a file with several results opens with only the current one
read (bug_sdv.pmx: 7 times faster). A history append writes one frame instead of the whole history.

---

## 8. Phase 5 - Legacy import and removal of BinaryFormatter

- [x] `PrePoMax.LegacyImport` (NRBF reader, or the upgrader executable from Phase 0): old `.pmx`/`.pmh` → the new
      domain objects, carrying the logic of today's `TryReadCompressedPmx*` readers and `_Compatibility` shims.
- [x] Delete BinaryFormatter, `RenamedTypeSerializationBinder`, the `SerializationInfo` constructors and
      `GetObjectData` from the domain projects. *(The constructors stay - see below.)*
- [x] Drop `[Serializable]` where nothing else needs it (clipboard, remoting) - check first.

Exit: no `BinaryFormatter` reference outside `PrePoMax.LegacyImport`; the oldest reference files (v1.3.x) still
open.

**Done.** `PrePoMax.LegacyImport` (net48, x64) holds `LegacyObjectReader`, `LegacyReader` and
`RenamedTypeSerializationBinder`, moved there from `CaeGlobals`. `Program` hands `LegacyReader.Read` to
`ObjectSerializer.LegacyReader`, so `CaeGlobals` no longer knows how an old stream is read, and the readers of old files
in `Controller`, `CommandsCollection`, the settings and the material library are unchanged: they still receive the
same objects. `BinaryObjectSerializer` and `DualObjectSerializer` are deleted. No source file names the binary
formatter any more; the remaining mentions are the standard header comment the WinForms designer writes into every
`.resx`.

`LegacyObjectReader` decodes the records with `System.Formats.Nrbf` and builds the graph the way the binary formatter
did:
- an object is created without a constructor and its fields are set by the member names of the record (a field of a
  base type is `Base+_field`), converting a primitive where the field type changed;
- a type with a `(SerializationInfo, StreamingContext)` constructor is given its members through it. These
  constructors run after the graph is read, in the order the binary formatter ran them: once the record of the object
  and the records of its members were read and every constructor reached from its members - through objects without
  one and arrays - had run, the one completed last first. Both halves matter. `UnitSystem` sets the units of the
  string converters and the materials of a v1.3 file build their `EquationContainer`s from them (run depth first,
  `error.pmx` got a Young's modulus of 0.21 instead of 210000); `FeMesh` converts the `FeMeshRefinement`s it finds in
  its dictionary of mesh setup items (run in plain stream order, those were still empty and ten v2.x files did not
  open);
- `[OnDeserializing]` runs before the fields are set; `[OnDeserialized]` and `IDeserializationCallback` run once the
  graph is complete (the collections of the base library rebuild themselves there);
- a `System.Type` (`UnitySerializationHolder`) is resolved through the binder, a delegate is read as null, and a type
  that no longer exists is read as null where the binary formatter threw, so the file opens instead of being refused.
  The reader reports each such object, with the name it carried and the object that held it, in a
  `LegacyReadReport` it adds to `ObjectSerializer.LegacyReadReport` (per thread, taken by whoever opens the file) and
  hands to the deserialization callbacks as their sender. An `OrderedDictionary` given the report drops the entries
  left null, keeping its order and item layout. On opening a `.pmx`, and on reading an old `.pmh`, the history
  commands that held such an object are removed, and with them the load commands (`ILoadNamesCommand`: remove, hide,
  show, duplicate, propagate, edit) that name only the loads those commands made, or a numbered sibling of one that
  no kept command made (`tmp_massLoad.pmx` removes `Mass_Under_Gravity-1` and holds only `Mass_Under_Gravity-2`);
  a list naming some of them loses those names. Every dropped item and removed command is written to the output as a
  warning, and the Controller stays strict: replay does not tolerate a missing name anywhere else.
- the old `InitialVelocity` (v2.2.2 and earlier) held by `CAddInitialCondition` and `CReplaceInitialCondition`
  becomes an `InitialTranslationalVelocity` in their `[OnDeserialized]` method, by the conversion the model already
  used when opened (`InitialVelocity.ToInitialTranslationalVelocity`), so its history replays.

Departure from the plan: the `SerializationInfo` constructors are **not** deleted. Old files name the domain types and
their old member names, and the constructors with the `case "Command+_name":` fallbacks and the `_Compatibility`
types are where that knowledge lives; moving it into the import project would copy ~150 constructors into a second
place for no gain. What is gone is the writing side: `GetObjectData` and `ISerializable` from 152 files and
`[Serializable]` from 992, since nothing writes that format any more. The clipboard carries only text and images and
nothing uses remoting, so no type needs the attribute. The static classes that carried `[PmxType]` (file readers and
writers, `AdvisorCreator`) lost it; their ids are retired in the snapshot. This is what Fable 5.2 recommended for the
Phase 5 scope (see the review table).

System.Formats.Nrbf brings System.Collections.Immutable 9.0.0.11, whose binding redirect is in `PrePoMax.exe.config`,
`PmxTool.exe.config` and `GmshCaller.exe.config`.

---

## 9. Verification

- Python API scripts: `Run_Tests.py --filter Test_Job Test_Result Test_Api Test_Rpc Test_Model_Constraints Audit_`
  after each phase, plus the groups the phase's code reaches.
- A reference corpus of `.pmx` and `.pmh` files from every released version since v1.3.5, with and without
  results, kept outside the repository; a script that opens each, saves it, reopens it and regenerates the history.
- Schema snapshot check on every build.
- Timings against the Phase 0 baseline: save, open (with and without results), `DeepClone`, history append.

**The corpus used.** 151 `.pmx` files: up to two per PrePoMax version found in a local folder of users' files
(v1.3.0 to v3.0.1, read from copies, never modified), two per folder of the repository's `Tests`, and the three
largest models with results; plus 6 `.pmh` histories. The list names local files and stays outside the repository.
`PmxTool/Corpus/Run_Corpus.py` runs each file through `Corpus_Fingerprint.py`: open the copy (A), save it, open the
saved file (B), regenerate its history into a new file (R) and open that (C); A, B and C are fingerprints of the model
(counts, names, the CalculiX deck) and of the results. A file fails when B differs from A, when the history does not
regenerate, or when A or C differ from the same run with the build before the migration (the baseline, `master` at
the start of the branch).

The judging is relative to the baseline because the baseline itself does not pass every file: 75 of the 151 fail
there. 7 do not open (the four files from v1.3.0 to v1.3.4, older than the oldest version the reader supports, two
saved by v2.4.x development builds and one holding a type that no longer exists), 9 reopen differently (some decks list `*Dload` lines and some nodes in no fixed
order) and 66 do not regenerate, 60 of them because the history imports a geometry file that is not in the copy. What
the baseline fails in the same way is reported as a note, not a failure. `sweep_test.pmx` regenerates to a different
mesh on every run, the baseline included (4330 or 4250 elements), and is marked nondeterministic: its regenerated deck
is not compared. The random model hash in the deck heading is ignored.

**Results at the end of Phase 5.** All 81 Python API scripts pass. The corpus: 150 of the 151 `.pmx` files and all 6
`.pmh` histories pass, and the new build fails fewer files of its own than the baseline (72 against 75: 6 do not open,
4 reopen differently, 64 do not regenerate, each also failed by the baseline except as below; `Plosca_MB_3.pmx` and
`palica_NDT.pmx`, which the baseline reopened differently, now pass). Of the 14 files from
v1.3.x the 10 the baseline opens still open, `error.pmx` among them with its units and material values as before, and
the 4 from v1.3.0 to v1.3.4 are refused as before. Two files are read further than the baseline read them, and
their histories then stop at commands the current version cannot replay:

- `tmp_massLoad.pmx` (v2.0.3) is the one reported failure. It holds a `MassUnderGravityLoad`, a type that no longer
  exists. The baseline refuses the whole file; the new reader reads the load as null (see Phase 5), so the model
  opens with an error from the validity check, and the history fails at the commands that add, edit and remove that
  load.
- `clamp.pmx` (v1.3.x): the baseline cannot read its history; the new build reads it, and the old initial velocity
  commands then fail in `ConvertSelectionBasedInitialCondition`.

Both are follow-ups outside this migration. A null left in a model collection by a missing type could be dropped
when the collection is read, and the old initial velocity commands need a conversion that works on replay.

---

## 10. Risks

| Risk | Mitigation |
|---|---|
| Shared references the code relies on without knowing | Phase 0 graph audit; Debug dual mode in Phase 2. |
| Source generator unusable on C# 7.3 legacy projects | Dynamic resolver on net48 (Reflection.Emit), generator after the SDK style move. |
| A legacy file version nobody tested | Reference corpus covering every release; the legacy importer keeps the existing readers' logic verbatim. |
| Key numbering mistakes later | Schema snapshot check fails the build. |
| Size of the attribute pass (~1,000 types) | Generated by script, reviewed per project, one project per commit. |

---

## 11. Out of scope

The rest of a modern .NET port: SDK style projects, `GmshCommon` C++/CLI for .NET, ActiViz/VTK (covered by the VTK
native port), WinForms differences.
