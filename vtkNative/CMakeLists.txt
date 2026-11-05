# PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#
# vtkNative: the native rendering library PrePoMax reaches through a flat C API.
#
#   cmake -S vtkNative -B vtkNative/build -G "Visual Studio 17 2022" -A x64 -DVTK_DIR=<vtk build>/lib/cmake/vtk-9.7
#   cmake --build vtkNative/build --config Release
#
# The build writes vtkNative.dll and every VTK DLL it loads into vtkNative/build/runtime, the folder the
# PrePoMax post build event copies into the application's lib folder.
cmake_minimum_required(VERSION 3.21)
project(vtkNative LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(VTK 9.7 REQUIRED COMPONENTS
    CommonCore
    CommonDataModel
    InteractionStyle
    FiltersCore
    IOImage
    RenderingCore
    RenderingFreeType
    RenderingOpenGL2
    RenderingUI)

add_library(vtkNative SHARED
    include/vtkNative.h
    src/vtkNativeApi.cpp
    src/vtkNativeBorderWidget.cpp
    src/vtkNativeBorderWidget.h
    src/vtkNativeError.cpp
    src/vtkNativeError.h
    src/vtkNativeOutputWindow.cpp
    src/vtkNativeOutputWindow.h
    src/vtkNativePartActor.cpp
    src/vtkNativePartActor.h
    src/vtkNativeRect.h
    src/vtkNativeRenderWindow.cpp
    src/vtkNativeRenderWindow.h
    src/vtkNativeScalarBar.cpp
    src/vtkNativeScalarBar.h
    src/vtkNativeScalarBarWidget.cpp
    src/vtkNativeScalarBarWidget.h
    src/vtkNativeTextWidget.cpp
    src/vtkNativeTextWidget.h
    src/vtkNativeView.cpp
    src/vtkNativeView.h
    src/vtkNativeViewCamera.cpp
    src/vtkNativeWidgetHighlight.cpp
    src/vtkNativeWidgetHighlight.h
    src/vtkNativeWidgetSet.cpp
    src/vtkNativeWidgetSet.h)
target_include_directories(vtkNative PRIVATE include src)
target_compile_definitions(vtkNative PRIVATE VTKNATIVE_EXPORTS)
target_link_libraries(vtkNative PRIVATE ${VTK_LIBRARIES})
if(MSVC)
    target_compile_options(vtkNative PRIVATE /W4 /permissive- /utf-8)
endif()
vtk_module_autoinit(TARGETS vtkNative MODULES ${VTK_LIBRARIES})

set(VTKNATIVE_RUNTIME_DIR "${CMAKE_BINARY_DIR}/runtime")
get_target_property(VTK_COMMON_CORE_DLL VTK::CommonCore IMPORTED_LOCATION_RELEASE)
get_filename_component(VTK_RUNTIME_DIR "${VTK_COMMON_CORE_DLL}" DIRECTORY)
add_custom_command(TARGET vtkNative POST_BUILD
    COMMAND ${CMAKE_COMMAND}
            -DLIBRARY=$<TARGET_FILE:vtkNative>
            -DSEARCH_DIR=${VTK_RUNTIME_DIR}
            -DOUTPUT_DIR=${VTKNATIVE_RUNTIME_DIR}
            -P ${CMAKE_CURRENT_SOURCE_DIR}/CopyRuntime.cmake
    VERBATIM)
