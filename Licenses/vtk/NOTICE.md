# VTK 9.7.1 and the libraries bundled in it

PrePoMax draws its 3D view with `lib\vtkNative.dll`, built from `vtkNative/` in this repository, which links the
Visualization Toolkit (VTK) 9.7.1. The VTK libraries ship as `lib\vtk*-9.7.dll`.

- **VTK** is licensed under the BSD 3-Clause license: [Copyright.txt](Copyright.txt).
- **The VTK build is patched.** `Rendering/OpenGL2/vtkSSAOPass.cxx` keeps the background where nothing is drawn
  when the ambient occlusion is on; the patch is `vtkNative/patches/vtk-9.7.1-ssao-background.patch` in this
  repository. Everything else is the VTK 9.7.1 release (vtk.org), built shared, x64, Release, with the StandAlone,
  Rendering and Imaging module groups and without FFMPEG.
- **Libraries bundled in VTK** and shipped among the `vtk*` DLLs, each under its own license, in
  [ThirdParty/](ThirdParty/):

| DLL | Library | License |
|---|---|---|
| `vtksys` | KWSys | BSD 3-Clause (`kwsys-Copyright.txt`) |
| `vtkDICOMParser` | DICOMParser | BSD 3-Clause (`DICOMParser-Copyright.txt`) |
| `vtkParallelDIY` | DIY2 | BSD 3-Clause-style (`diy2-LICENSE.txt`) |
| `vtkexpat` | Expat | MIT (`expat-COPYING`) |
| `vtkfmt` | {fmt} | MIT (`fmt-LICENSE`) |
| `vtkfreetype` | FreeType 2.14.3 | the FreeType License (`freetype-FTL.TXT`; `freetype-LICENSE.TXT` explains the choice of FTL or GPLv2) |
| `vtkglad` | glad | MIT (`glad-LICENSE`) |
| `vtkjpeg` | libjpeg-turbo | IJG, BSD 3-Clause and zlib (`jpeg-LICENSE.md`) |
| `vtkjsoncpp` | JsonCpp | MIT or public domain (`jsoncpp-LICENSE`) |
| `vtkkissfft` | KISS FFT | BSD 3-Clause (`kissfft-COPYING`) |
| `vtkloguru` | Loguru | public domain (`loguru-LICENSE`) |
| `vtklz4` | LZ4 | BSD 2-Clause (`lz4-LICENSE`) |
| `vtklzma` | XZ Utils liblzma | public domain / 0BSD (`lzma-COPYING`) |
| `vtkmetaio` | MetaIO | BSD 3-Clause (`metaio-License.txt`) |
| `vtkpng` | libpng | PNG Reference Library License (`png-LICENSE`) |
| `vtkpugixml` | pugixml | MIT (`pugixml-LICENSE.md`) |
| `vtkscn` | scnlib | Apache-2.0 (`scn-LICENSE`, `scn-LICENSE.nanorange`) |
| `vtktiff` | libtiff | libtiff license (`tiff-LICENSE.md`) |
| `vtktoken` | token | BSD 3-Clause (`token-license.md`) |
| `vtkverdict` | Verdict | BSD 3-Clause (`verdict-LICENSE`) |
| `vtkzlib` | zlib | zlib License (`zlib-LICENSE`) |

Portions of this software are copyright (C) 1996-2026 The FreeType Project (www.freetype.org). All rights reserved.
