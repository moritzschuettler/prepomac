# lib\highs.dll - license notice

## HiGHS 1.16.0

The unmodified `highs.dll` of the release archive `highs-1.16.0-x86_64-windows-mit.zip` published at
https://github.com/ERGO-Code/HiGHS/releases/tag/v1.16.0, licensed under the **MIT License**. The license
text is `LICENSE.txt` beside this notice, taken from `share/doc/HIGHS` of that archive.

## Third-party code it contains

`highs.exe --notice` of the same archive names two third-party components. CLI11 serves only the
command line of `highs.exe`, which is not shipped. The other one is compiled into the library:

| Component | Version | License |
|---|---|---|
| pdqsort | git b1ef26a | zlib License |

The notice of pdqsort, from `include/highs/pdqsort/pdqsort.h` of the archive:

```
pdqsort.h - Pattern-defeating quicksort.

Copyright (c) 2021 Orson Peters

This software is provided 'as-is', without any express or implied warranty. In no event will the
authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial
applications, and to alter it and redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the
   original software. If you use this software in a product, an acknowledgment in the product
   documentation would be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be misrepresented as
   being the original software.

3. This notice may not be removed or altered from any source distribution.
```

The MIT and zlib licenses are both compatible with PrePoMax's GPL-3.
