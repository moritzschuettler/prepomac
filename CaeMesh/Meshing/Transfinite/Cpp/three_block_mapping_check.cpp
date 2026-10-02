// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

// Checks that three_block_mapping.h gives the grids ThreeBlockMapping.cs wrote into three_block_vectors.txt: the
// same quads and folded quads, and the inner nodes within 1e-9 of the size of the face. Netgen and QuadWild build
// and run it against the copy of the header they include.
//
//   cl /std:c++17 /EHsc /O2 three_block_mapping_check.cpp
//   three_block_mapping_check three_block_vectors.txt
//
// The exit code is 0 when every case agrees.

#include "three_block_mapping.h"

#include <cmath>
#include <fstream>
#include <iostream>
#include <sstream>
#include <string>
#include <vector>

int main(int argc, char* argv[])
{
    const char* path = argc > 1 ? argv[1] : "three_block_vectors.txt";
    std::ifstream file(path);
    if (!file)
    {
        std::cerr << "Cannot open " << path << std::endl;
        return 2;
    }
    std::stringstream content;
    std::string line;
    while (std::getline(file, line))
        if (!line.empty() && line[0] != '#') content << line << '\n';
    int numOfCases = 0, numOfFailures = 0;
    std::string keyword;
    while (content >> keyword)
    {
        std::string name;
        content >> name;
        std::vector<three_block::Uv> sideUv[3];
        std::vector<three_block::Xyz> sideXyz[3];
        for (int k = 0; k < 3; k++)
        {
            int side, n;
            content >> keyword >> side >> n;
            for (int i = 0; i < n; i++)
            {
                three_block::Uv uv;
                three_block::Xyz xyz;
                content >> uv[0] >> uv[1] >> xyz[0] >> xyz[1] >> xyz[2];
                sideUv[k].push_back(uv);
                sideXyz[k].push_back(xyz);
            }
        }
        int n;
        content >> keyword >> n;
        std::vector<three_block::Uv> innerUv(n);
        for (auto& uv : innerUv) content >> uv[0] >> uv[1];
        content >> keyword >> n;
        std::vector<std::array<int, 4> > quads(n);
        for (auto& quad : quads) content >> quad[0] >> quad[1] >> quad[2] >> quad[3];
        int folded;
        content >> keyword >> folded >> keyword;
        //
        three_block::Grid grid = three_block::map(sideUv, sideXyz);
        double size = 0;
        for (int k = 0; k < 3; k++)
            for (const auto& uv : sideUv[k]) size = std::max(size, std::max(std::fabs(uv[0]), std::fabs(uv[1])));
        if (size == 0) size = 1;
        double deviation = 0;
        bool sameNodes = grid.innerUv.size() == innerUv.size();
        for (size_t i = 0; sameNodes && i < innerUv.size(); i++)
            for (int d = 0; d < 2; d++) deviation = std::max(deviation, std::fabs(grid.innerUv[i][d] - innerUv[i][d]));
        bool ok = sameNodes && grid.quads == quads && grid.numberOfFoldedQuads == folded && deviation <= 1E-9 * size;
        std::cout << (ok ? "ok      " : "FAILED  ") << name << "  quads " << grid.quads.size() << "  deviation "
                  << deviation << std::endl;
        numOfCases++;
        if (!ok) numOfFailures++;
    }
    std::cout << numOfCases - numOfFailures << " of " << numOfCases << " cases agree" << std::endl;
    return numOfFailures == 0 && numOfCases > 0 ? 0 : 1;
}
