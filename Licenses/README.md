# Third-party licenses

PrePoMax is distributed under the GNU General Public License v3 (see [LICENSE](../LICENSE) in the
repository root). The macOS build uses the third-party components below. Most are installed with
Homebrew and linked at run time; only Mmg is built by this repository and shipped inside the app.
`macOS/make-app.sh` copies this folder into `PrePoMax.app/Contents/Resources/Licenses`.

| Component | How the macOS build uses it | License | License text |
|---|---|---|---|
| PrePoMax | the application | GPL-3.0 | [LICENSE](../LICENSE) |
| NetGenMesher (macOS) | `Meshers/Netgen/NetGenMesher`, built from `macOS/Native/NetGenMesher` | GPL-3.0 (same as PrePoMax) | [LICENSE](../LICENSE) |
| Gmsh 4.15 | Homebrew `gmsh`, linked by NetGenMesher | GPL-2.0-or-later, with an exception permitting combination with OpenCASCADE | [gmsh/](gmsh/) |
| OpenCASCADE 7.9 | Homebrew `opencascade`, linked by NetGenMesher | LGPL-2.1 with the Open CASCADE exception | github.com/Open-Cascade-SAS/OCCT |
| Mmg 5.7.0 | `Meshers/Mmg/mmgs`, `Meshers/Mmg/mmg3d`, built from the upstream sources by `macOS/Native/Mmg/build.sh` | LGPL-3.0-or-later | [mmg/](mmg/) |
| lp_solve 5.5 | Homebrew `lp_solve` | LGPL-2.1 | [lpsolve/](lpsolve/) |
| Avalonia 11.3 | NuGet package, the user interface | MIT | github.com/AvaloniaUI/Avalonia |
| SkiaSharp | NuGet package, drawing | MIT | github.com/mono/SkiaSharp |
| CalculiX | installed separately (Homebrew tap `costerwi/calculix`), run as a separate program; the downloaded app carries it in `Contents/MacOS/Solver` | GPL-2.0 | [calculix/](calculix/) |
| Code_Aster 18.1 (optional) | conda-forge package in a Docker image built by `macOS/Native/CodeAster/build.sh`, run as a separate program | GPL-3.0 | code-aster.org |

Gmsh, Mmg and lp_solve are GPL/LGPL licenses compatible with PrePoMax's own GPL-3.0. Mmg's license
requires that both `COPYING.LESSER` (LGPL-3) and `COPYING` (GPL-3) travel with the binaries, which is
why all three files are in [mmg/](mmg/).

## The downloaded app

The stand-alone app of the releases (`make-app.sh --standalone`, see `macOS/bundle-deps.py`) also carries the
libraries above that Homebrew provides, together with the libraries they load (FLTK, cairo, FreeType, the X11
libraries, TBB, OpenBLAS, Open MPI, the GCC runtime and others), CalculiX, a Python 3.12 of
[python-build-standalone](https://github.com/astral-sh/python-build-standalone) with NumPy, and the .NET runtime
(MIT). The license files of every Homebrew formula it takes files from are copied into
`Contents/Resources/Licenses/Bundled`, with `VERSIONS.txt` naming the formula versions shipped; the Python keeps its
own license in `Contents/MacOS/Python/lib/python3.12/LICENSE.txt`.

## Corresponding source

GPL-3 section 6 and LGPL-3 require a route to the source for binaries that are conveyed. The app built
by `make-app.sh` contains PrePoMax, NetGenMesher and Mmg, all built from this repository and from the
upstream Mmg sources that `macOS/Native/Mmg/build.sh` downloads. A binary release should name the
exact versions shipped and keep the matching source archives, or carry a written offer.
