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

1. **Explicit schema in code with stable numeric keys.** Persisted types get `[MessagePackObject]`, members get
   `[Key(n)]`, abstract bases get `[Union(id, typeof(...))]`. Files depend only on these numbers, never on class,
   field, namespace or assembly names, so refactoring cannot break a file. Fields are tagged, so an older PrePoMax
   skips unknown fields and a newer one defaults missing fields; most changes need no version check.
2. **Serializer: MessagePack-CSharp.** netstandard2.0 (runs on net48) and modern .NET, among the fastest .NET
   serializers, source generated (AOT friendly) on modern .NET. Alternative considered: protobuf-net (similar
   robustness, slower, weaker polymorphism).
3. **Persisted data is a tree, not a graph.** No shared references, no cycles. Cross references are by name, which
   PrePoMax mostly does already (a load names its surface, a section its material).
4. **`DeepClone` is a round trip through the same serializer.** One set of rules: what is cloned is exactly what is
   saved. Expected 10-50x faster than BinaryFormatter.
5. **`Controller` is no longer persisted.** A new root `PmxDocument { Model, Results, Commands, Annotations, Jobs,
   UiState }` is what is saved.
6. **Container: chunked file, not ZIP.**
   ```
   header       magic "PMX2", format version, writer program version
   chunk table  name, offset, length, codec, CRC32 (per chunk)
   chunks       model, history, annotations, jobs, ui, geometry, mesh, results/<n>, ...
   ```
   - Zstd compression (ZstdSharp.Port, pure managed), chunks compressed in parallel.
   - CRC32 per chunk: corruption is detected and located; a damaged results chunk still lets the model open.
   - Results chunks are loaded lazily, when first shown.
   - Mesh and results stay hand-written binary, as raw little-endian arrays (bulk readable with
     `MemoryMarshal.Cast` on modern .NET).
   - A `pmx-dump` tool prints any chunk as JSON for debugging.
7. **History is an append-only journal.** One frame per command: length + CRC32 + MessagePack payload. Saving a
   command appends one frame instead of rewriting the whole history; a crash loses at most the last command.
8. **Schema snapshot test.** A generated text file lists every union id and every `(type, key, member type)`. A check
   fails when an existing entry changes meaning; new entries are allowed. This is the long run safeguard.
9. **Legacy files in their own project.** `PrePoMax.LegacyImport` reads old `.pmx`/`.pmh` files and holds all of
   today's compatibility code. Once it works, the `SerializationInfo` constructors and `_Compatibility` shims are
   deleted from the domain classes.
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

The whole save and open of the application is timed per corpus file by `Tests/Storage/Run_Corpus.py` (the `--report`
of each run), against which Phases 3 and 4 are measured.

The serializer and type ids follow decision 1 with two changes recorded here: the stable numbers are written as
`[PmxType(id)]` on every stored type and `[PmxKey(n)]` on every stored field (attributes of `CaeGlobals`, so the domain
projects do not reference MessagePack), and polymorphism uses the type number of the value instead of `[Union]` lists on
the bases - every stored class has one number, which also names it in an `object` or interface slot.

---

## 4. Phase 1 - One seam for all serialization

- [ ] `IObjectSerializer` in `CaeGlobals` with `Clone`, `Serialize(Stream)`, `Deserialize(Stream)`.
- [ ] Route `DeepClone`, `DumpToFile`, `DumpToStream`, `LoadFromFile`, `Tools.LoadDumpFromFile` through it. The
      BinaryFormatter implementation stays the default; nothing changes for the user.
- [ ] Add `PmxDocument`; `SaveToPmx` and `OpenPmx` build and read it instead of `object[] { this, ... }`. The file
      is still BinaryFormatter at this point (a new internal layout, reader keeps old layouts).

Exit: all Python test groups pass; old files open; files saved by this build open in this build.

---

## 5. Phase 2 - Schema

- [ ] One-off Roslyn script: for every `[Serializable]` type, add `[MessagePackObject]` and `[Key(n)]` to the members
      BinaryFormatter writes today (fields not `[NonSerialized]`), keys in declaration order; `[IgnoreMember]` on
      the rest; `[Union]` on every abstract base with ids per numbered command folder / model area (id ranges
      documented in this file).
- [ ] Fix the graph audit findings (shared references → names; cycles → back references rebuilt after load).
- [ ] Constructors/setters MessagePack needs (`[SerializationConstructor]` or `internal` setters).
- [ ] Generate `Schema_Snapshot.txt` and the check that compares against it.
- [ ] Debug-only dual mode for `DeepClone`: clone with both serializers and compare the results.

Exit: dual mode reports no differences across all Python test groups.

---

## 6. Phase 3 - Switch DeepClone

- [ ] MessagePack implementation of `IObjectSerializer.Clone` becomes the default.
- [ ] Measure against the Phase 0 baseline.

Exit: all Python test groups pass; Regenerate History reproduces the same model on the reference files.

---

## 7. Phase 4 - New container and journal

- [ ] Chunked container writer and reader (`CaeGlobals/FileInOut` or a new `CaeStorage` project), with CRC32 and
      Zstd.
- [ ] `SaveToPmx` writes the new format; `OpenPmx` detects the magic and reads new or old.
- [ ] Results chunks loaded lazily.
- [ ] History journal replaces `.pmh`; the old `.pmh` still reads.
- [ ] Settings to JSON (old binary settings still read once, then rewritten).
- [ ] `pmx-dump` tool.

Exit: round trip of every reference file (old → open → save new → open → Regenerate History) gives identical models
and results; save and open faster than baseline.

---

## 8. Phase 5 - Legacy import and removal of BinaryFormatter

- [ ] `PrePoMax.LegacyImport` (NRBF reader, or the upgrader executable from Phase 0): old `.pmx`/`.pmh` → the new
      domain objects, carrying the logic of today's `TryReadCompressedPmx*` readers and `_Compatibility` shims.
- [ ] Delete BinaryFormatter, `RenamedTypeSerializationBinder`, the `SerializationInfo` constructors and
      `GetObjectData` from the domain projects.
- [ ] Drop `[Serializable]` where nothing else needs it (clipboard, remoting) - check first.

Exit: no `BinaryFormatter` reference outside `PrePoMax.LegacyImport`; the oldest reference files (v1.3.x) still
open.

---

## 9. Verification

- Python API scripts: `Run_Tests.py --filter Test_Job Test_Result Test_Api Test_Rpc Test_Model_Constraints Audit_`
  after each phase, plus the groups the phase's code reaches.
- A reference corpus of `.pmx` and `.pmh` files from every released version since v1.3.5, with and without
  results, kept outside the repository; a script that opens each, saves it, reopens it and regenerates the history.
- Schema snapshot check on every build.
- Timings against the Phase 0 baseline: save, open (with and without results), `DeepClone`, history append.

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
