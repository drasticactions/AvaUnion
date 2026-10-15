cmake_minimum_required(VERSION 3.27)
project(union-reference LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)

find_package(Qt6 6.9 REQUIRED COMPONENTS Core Gui Quick QuickControls2)

add_executable(union-reference main.cpp)
target_link_libraries(union-reference PRIVATE Qt6::Core Qt6::Gui Qt6::Quick Qt6::QuickControls2)
