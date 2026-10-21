cmake_minimum_required(VERSION 3.27)
project(svg-reference LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(Qt6 6.9 REQUIRED COMPONENTS Core Gui Svg)

add_executable(svg-reference main.cpp)
target_link_libraries(svg-reference PRIVATE Qt6::Core Qt6::Gui Qt6::Svg)
