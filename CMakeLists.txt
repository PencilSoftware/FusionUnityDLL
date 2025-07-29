cmake_minimum_required(VERSION 3.15)
project(FusionUnity VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)

# 编译选项
if(MSVC)
    add_compile_options(/W3)
    add_compile_definitions(_CRT_SECURE_NO_WARNINGS)
else()
    add_compile_options(-Wall -Wextra -O3)
endif()

# Fusion源码目录
set(FUSION_DIR "${CMAKE_SOURCE_DIR}/external/Fusion/Fusion")

# 检查Fusion源码是否存在
if(NOT EXISTS ${FUSION_DIR})
    message(FATAL_ERROR "Fusion source not found at ${FUSION_DIR}")
endif()

# 收集源文件
file(GLOB FUSION_SOURCES "${FUSION_DIR}/*.c")
file(GLOB WRAPPER_SOURCES "${CMAKE_SOURCE_DIR}/wrapper/*.c")

# 创建共享库
add_library(FusionUnity SHARED 
    ${FUSION_SOURCES}
    ${WRAPPER_SOURCES}
)

# 包含目录
target_include_directories(FusionUnity PRIVATE 
    ${FUSION_DIR}
    ${CMAKE_SOURCE_DIR}/wrapper
)

# 设置输出属性
set_target_properties(FusionUnity PROPERTIES
    VERSION ${PROJECT_VERSION}
    SOVERSION 1
)

# 平台特定设置
if(WIN32)
    # Windows DLL
    set_target_properties(FusionUnity PROPERTIES
        OUTPUT_NAME "FusionUnity"
        SUFFIX ".dll"
        PREFIX ""
    )
elseif(APPLE)
    # macOS Bundle
    set_target_properties(FusionUnity PROPERTIES
        OUTPUT_NAME "FusionUnity"
        SUFFIX ".bundle"
        PREFIX ""
        BUNDLE TRUE
    )
elseif(ANDROID)
    # Android .so
    set_target_properties(FusionUnity PROPERTIES
        OUTPUT_NAME "FusionUnity"
        PREFIX "lib"
        SUFFIX ".so"
    )
else()
    # Linux .so
    set_target_properties(FusionUnity PROPERTIES
        OUTPUT_NAME "FusionUnity"
        PREFIX "lib"
        SUFFIX ".so"
    )
endif()

# 数学库链接（Linux和Android需要）
if(UNIX AND NOT APPLE)
    target_link_libraries(FusionUnity m)
endif() 