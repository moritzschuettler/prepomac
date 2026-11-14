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

- [ ] MessagePack-CSharp on net48 with the legacy csproj and `packages.config`: dynamic resolver works; check
      whether the source generator works with C# 7.3 projects (`PrePoMax`, `APIs` pin `LangVersion` 7.3) or only
      after the move to SDK style projects.
- [ ] ZstdSharp.Port on net48: speed and ratio against `Tools.Compress` on 3-4 real `.pmx` files.
- [ ] `System.Formats.Nrbf`: confirm it runs on net48 and decodes a real `.pmx` payload (`object[]` with
      `Controller`). Fallback if not: a net48 `PmxUpgrader.exe` that loads with BinaryFormatter and writes the new
      format (same pattern as `GmshCaller`).
- [ ] Graph audit: a walker over a loaded model that reports every object reached twice and every cycle. The list
      decides how much of decision 3 needs code changes.
- [ ] Types that BinaryFormatter tolerated only because they were null at save time (VTK objects, delegates,
      events): list them.
- [ ] Baseline timings: save, open and `DeepClone` of a large model with results.

Exit: a go/no-go note per item, and the measured baseline.

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
