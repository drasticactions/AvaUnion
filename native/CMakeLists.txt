cmake_minimum_required(VERSION 3.27)
project(avaunion-native LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_SKIP_BUILD_RPATH OFF)
set(CMAKE_BUILD_WITH_INSTALL_RPATH ON)
set(CMAKE_INSTALL_RPATH "")

include(cmake/UnionSource.cmake)


find_package(Qt6 6.9 REQUIRED COMPONENTS Core Gui DBus)
find_package(Union ${AVU_UNION_VERSION} EXACT REQUIRED)
find_package(KF6Config REQUIRED)
find_package(KF6IconThemes REQUIRED)
avu_union_private_includes()

set(AVU_LIBRARY_NAME avaunion-native-${AVU_UNION_VERSION})

add_library(avaunion_native SHARED
    src/avaunion.h
    src/avu_resolved.g.h
    src/Common.h
    src/Raster.h
    src/Resolved.h
    src/Leaves.g.h
    src/UnionSchemas.g.h
    src/Bootstrap.cpp
    src/Pump.cpp
    src/Events.cpp
    src/Elements.cpp
    src/Query.cpp
    src/Resolved.g.cpp
    src/Colors.cpp
    src/Icons.cpp
    src/Images.cpp
    src/Platform.cpp
    src/Util.cpp
    src/Export.cpp
)
set_target_properties(avaunion_native PROPERTIES
    OUTPUT_NAME ${AVU_LIBRARY_NAME}
    PREFIX lib
    NO_SONAME ON
)
target_compile_definitions(avaunion_native PRIVATE
    AVU_UNION_VERSION="${AVU_UNION_VERSION}"
    QT_NO_KEYWORDS
)
target_include_directories(avaunion_native BEFORE PRIVATE ${AVU_UNION_INCLUDE_DIRS})
target_include_directories(avaunion_native PRIVATE src)
target_link_libraries(avaunion_native PRIVATE Union::Union Qt6::Core Qt6::Gui Qt6::DBus KF6::ConfigCore KF6::IconThemes ${CMAKE_DL_LIBS})
target_link_options(avaunion_native PRIVATE -Wl,--no-undefined -Wl,--as-needed -Wl,--exclude-libs,ALL
    -Wl,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/src/exports.map)
set_target_properties(avaunion_native PROPERTIES LINK_DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/src/exports.map)

add_executable(union-export tools/union-export/main.cpp)
target_include_directories(union-export PRIVATE src)
target_link_libraries(union-export PRIVATE avaunion_native Qt6::Core)
set_target_properties(union-export PROPERTIES INSTALL_RPATH "$ORIGIN")
