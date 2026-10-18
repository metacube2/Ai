cmake_minimum_required(VERSION 3.22)

project(NexusBridge VERSION 0.1.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_OSX_ARCHITECTURES "arm64" CACHE STRING "Zielarchitektur")
set(CMAKE_OSX_DEPLOYMENT_TARGET "11.0" CACHE STRING "Minimale macOS-Version")

# JUCE liegt bewusst ausserhalb des Workspace, damit kein Vendor-Code im
# Repository landet. Mit -DJUCE_PATH=... umstellbar.
set(JUCE_PATH "$ENV{HOME}/JUCE" CACHE PATH "Pfad zum JUCE-Framework")

if(NOT EXISTS "${JUCE_PATH}/CMakeLists.txt")
    message(FATAL_ERROR "JUCE nicht gefunden unter ${JUCE_PATH}. "
                        "git clone --depth 1 https://github.com/juce-framework/JUCE.git ${JUCE_PATH}")
endif()

add_subdirectory("${JUCE_PATH}" JUCE EXCLUDE_FROM_ALL)

juce_add_plugin(NexusBridge
    COMPANY_NAME            "Metacube"
    BUNDLE_ID               "com.metacube.nexusbridge"
    PLUGIN_MANUFACTURER_CODE Mtcb
    PLUGIN_CODE             Nxbr
    FORMATS                 VST3 Standalone
    PRODUCT_NAME            "Nexus Bridge"
    IS_SYNTH                FALSE
    NEEDS_MIDI_INPUT        FALSE
    NEEDS_MIDI_OUTPUT       FALSE
    IS_MIDI_EFFECT          FALSE
    EDITOR_WANTS_KEYBOARD_FOCUS TRUE
    COPY_PLUGIN_AFTER_BUILD TRUE
    VST3_CATEGORIES         "Tools" "Generator")

target_sources(NexusBridge PRIVATE
    Source/PluginProcessor.cpp
    Source/PluginEditor.cpp
    Source/ApiClient.cpp
    Source/NexusLookAndFeel.cpp
    Source/ReaderPanel.cpp
    Source/RestorePanel.cpp
    Source/PlatzhalterPanel.cpp)

target_compile_definitions(NexusBridge PUBLIC
    JUCE_WEB_BROWSER=0
    JUCE_USE_CURL=0
    JUCE_VST3_CAN_REPLACE_VST2=0
    JUCE_DISPLAY_SPLASH_SCREEN=0
    JUCE_REPORT_APP_USAGE=0)

target_link_libraries(NexusBridge PRIVATE
    juce::juce_audio_utils
    PUBLIC
    juce::juce_recommended_config_flags
    juce::juce_recommended_lto_flags
    juce::juce_recommended_warning_flags)
