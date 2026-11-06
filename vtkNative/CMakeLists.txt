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
    CommonTransforms
    InteractionStyle
    FiltersCore
    FiltersExtraction
    FiltersGeneral
    FiltersGeometry
    FiltersHybrid
    FiltersModeling
    FiltersSources
    IOImage
    RenderingCore
    RenderingFreeType
    RenderingOpenGL2
    RenderingUI)

add_library(vtkNative SHARED
    include/vtkNative.h
    include/vtkNativeCore.h
    include/vtkNativeGeometry.h
    include/vtkNativeSelection.h
    include/vtkNativeWidgets.h
    src/vtkNativeApi.cpp
    src/vtkNativeError.cpp
    src/vtkNativeError.h
    src/vtkNativeGuard.h
    src/vtkNativeOutputWindow.cpp
    src/vtkNativeOutputWindow.h
    src/vtkNativeRenderWindow.cpp
    src/vtkNativeRenderWindow.h
    src/vtkNativeView.cpp
    src/vtkNativeView.h
    src/vtkNativeViewCamera.cpp
    src/Geometry/vtkNativeApiGeometry.cpp
    src/Geometry/vtkNativeColorSpectrum.cpp
    src/Geometry/vtkNativeColorSpectrum.h
    src/Geometry/vtkNativePartActor.cpp
    src/Geometry/vtkNativePartActor.h
    src/Geometry/vtkNativeSectionView.cpp
    src/Geometry/vtkNativeSymbols.cpp
    src/Geometry/vtkNativeSymbols.h
    src/Geometry/vtkNativeTransformations.cpp
    src/Geometry/vtkNativeViewGeometry.cpp
    src/Geometry/vtkNativeViewPicture.cpp
    src/Selection/vtkNativeApiSelection.cpp
    src/Selection/vtkNativeViewPick.cpp
    src/Widgets/vtkNativeApiWidgets.cpp
    src/Widgets/vtkNativeArrowWidget.cpp
    src/Widgets/vtkNativeArrowWidget.h
    src/Widgets/vtkNativeBorderWidget.cpp
    src/Widgets/vtkNativeBorderWidget.h
    src/Widgets/vtkNativeColorBarWidget.cpp
    src/Widgets/vtkNativeColorBarWidget.h
    src/Widgets/vtkNativeCoorSysGeometry.cpp
    src/Widgets/vtkNativeCoorSysGeometry.h
    src/Widgets/vtkNativeCoorSysWidget.cpp
    src/Widgets/vtkNativeCoorSysWidget.h
    src/Widgets/vtkNativeInteractorStyle.cpp
    src/Widgets/vtkNativeInteractorStyle.h
    src/Widgets/vtkNativeRect.h
    src/Widgets/vtkNativeScalarBar.cpp
    src/Widgets/vtkNativeScalarBar.h
    src/Widgets/vtkNativeScalarBarWidget.cpp
    src/Widgets/vtkNativeScalarBarWidget.h
    src/Widgets/vtkNativeScaleBarWidget.cpp
    src/Widgets/vtkNativeScaleBarWidget.h
    src/Widgets/vtkNativeStatusBlockWidget.cpp
    src/Widgets/vtkNativeStatusBlockWidget.h
    src/Widgets/vtkNativeTextWidget.cpp
    src/Widgets/vtkNativeTextWidget.h
    src/Widgets/vtkNativeViewInteraction.cpp
    src/Widgets/vtkNativeViewWidgets.cpp
    src/Widgets/vtkNativeWidget.cpp
    src/Widgets/vtkNativeWidget.h
    src/Widgets/vtkNativeWidgetHighlight.cpp
    src/Widgets/vtkNativeWidgetHighlight.h
    src/Widgets/vtkNativeWidgetSet.cpp
    src/Widgets/vtkNativeWidgetSet.h)
target_include_directories(vtkNative PRIVATE include src src/Geometry src/Selection src/Widgets)
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
