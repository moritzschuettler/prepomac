# PrePoMax for macOS - NetGenMesher, built against the OpenCASCADE of Homebrew (brew install opencascade) and the patched
# gmsh 4.13 of Native/Gmsh (build.sh there), the gmsh PrePoMax ships on Windows. The app carries gmsh next to its
# assemblies and OpenCASCADE in Contents/Frameworks. On Linux
# (experimental) the libraries of the distribution are used: OpenCASCADE headers in /usr/include/opencascade, gmsh in
# the default paths or in GMSH_DIR (the gmsh SDK).
cmake_minimum_required(VERSION 3.20)
project(NetGenMesher CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

if(APPLE)
    set(HOMEBREW_PREFIX "/opt/homebrew" CACHE PATH "The Homebrew prefix with OpenCASCADE and gmsh")
    set(OCC_INCLUDE_DIR "${HOMEBREW_PREFIX}/include/opencascade")
    set(LIBRARY_PREFIX "${HOMEBREW_PREFIX}")
else()
    set(OCC_INCLUDE_DIR "/usr/include/opencascade" CACHE PATH "The OpenCASCADE headers")
    set(GMSH_DIR "/usr" CACHE PATH "The folder with include/gmsh.h and lib/libgmsh.so")
    set(LIBRARY_PREFIX "${GMSH_DIR}")
endif()
set(OCC_LIBRARIES
    TKernel TKMath TKG2d TKG3d TKGeomBase TKGeomAlgo TKBRep TKTopAlgo TKPrim TKBO TKBool TKFillet TKOffset
    TKShHealing TKMesh TKFeat TKHLR TKXSBase TKDE TKDESTEP TKDEIGES TKCDF TKLCAF TKCAF TKXCAF)

add_executable(NetGenMesher main.cpp Common.cpp Visualization.cpp Geometry.cpp Mesh.cpp Operations.cpp Faces.cpp Edit.cpp)
if(APPLE)
    set(GMSH_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../Gmsh")
    target_include_directories(NetGenMesher PRIVATE "${GMSH_ROOT}/src/api")
    target_link_directories(NetGenMesher PRIVATE "${GMSH_ROOT}/build")
endif()
target_include_directories(NetGenMesher PRIVATE "${OCC_INCLUDE_DIR}" "${LIBRARY_PREFIX}/include")
target_link_directories(NetGenMesher PRIVATE "${LIBRARY_PREFIX}/lib")
target_link_libraries(NetGenMesher PRIVATE ${OCC_LIBRARIES} gmsh)
if(APPLE)
    # gmsh is found in the main folder of the application (Meshers/Netgen is two folders below it), else in the build
    # of Native/Gmsh; OpenCASCADE in Homebrew
    set_target_properties(NetGenMesher PROPERTIES
        BUILD_RPATH "@executable_path/../..;${GMSH_ROOT}/build;${HOMEBREW_PREFIX}/lib"
        INSTALL_RPATH "@executable_path/../../Frameworks;@executable_path;${HOMEBREW_PREFIX}/lib")
else()
    set_target_properties(NetGenMesher PROPERTIES BUILD_RPATH "${LIBRARY_PREFIX}/lib")
endif()
