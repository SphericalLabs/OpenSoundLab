OSL_NATIVE_CORE_DIR ?= Core
OSL_NATIVE_ADDONS_DIR ?= Addons
OSL_NATIVE_PACKAGE_ADDONS_DIR ?= ../Packages/*/OSLNativeAddons~ ../Packages/*/OSLNativeAddons

OSL_NATIVE_CORE_INCLUDE_DIRS := \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Abi \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Shared/Buffers \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Shared/Math \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Drum \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Envelope \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Filter \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/FilterTwo \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Keyboard \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/LinkPhaseGenerator \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Maraca \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Noise \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Oscillator \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/ClipPlayer \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Utilities \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/Devices/Xylophone \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/FreeVerb/dfx-library \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/FreeVerb/freeverb/components \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/UnityAudioPlugin \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/pcg-cpp/include \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/link/include \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/link/modules/asio-standalone/asio/include \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/AprilTag \
    $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/AprilTag/common

OSL_NATIVE_COMMON_SOURCES := \
    $(OSL_NATIVE_CORE_DIR)/Shared/Math/AudioMath.cpp \
    $(OSL_NATIVE_CORE_DIR)/Shared/Buffers/CompressedRingBuffer.cpp \
    $(OSL_NATIVE_CORE_DIR)/Shared/Buffers/CRingBuffer.cpp \
    $(OSL_NATIVE_CORE_DIR)/Shared/Buffers/RingBuffer.cpp \
    $(OSL_NATIVE_CORE_DIR)/Shared/Math/Biquad.cpp \
    $(OSL_NATIVE_CORE_DIR)/Shared/Math/Resample.cpp \
    $(OSL_NATIVE_CORE_DIR)/Shared/Math/Util.c \
    $(OSL_NATIVE_CORE_DIR)/Devices/ClipPlayer/ClipPlayerApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Drum/DrumApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Envelope/EnvelopeApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Envelope/EnvelopeMath.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Filter/Filter.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Filter/CombFilterApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/FilterTwo/FilterTwo.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Keyboard/KeyboardApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/LinkPhaseGenerator/LinkPhaseGeneratorApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Maraca/MaracaApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Noise/NoiseApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Oscillator/OscillatorApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Oscillator/Oscillator.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Utilities/BufferApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Utilities/DiagnosticApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Utilities/FaderApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Utilities/GateApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Utilities/MicApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Utilities/AprilTagApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Utilities/WaveTextureApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/Devices/Xylophone/XylophoneApi.cpp \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/apriltag.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/apriltag_pose.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/apriltag_quad_thresh.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/tagStandard41h12.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/g2d.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/homography.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/image_u8.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/image_u8_parallel.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/image_u8x3.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/matd.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/pnm.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/pthreads_cross.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/string_util.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/svd22.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/time_util.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/unionfind.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/workerpool.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/zarray.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/zhash.c \
    $(OSL_NATIVE_CORE_DIR)/External/AprilTag/common/zmaxheap.c

OSL_NATIVE_GLOB_SOURCES := \
    $(wildcard $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/FreeVerb/freeverb/components/*.cpp) \
    $(wildcard $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/FreeVerb/dfx-library/*.cpp) \
    $(wildcard $(LOCAL_PATH)/$(OSL_NATIVE_CORE_DIR)/External/UnityAudioPlugin/*.cpp)

OSL_NATIVE_ADDON_INCLUDE_DIRS :=
OSL_NATIVE_ADDON_SOURCES :=
OSL_NATIVE_REPO_ADDON_MANIFESTS := $(wildcard $(LOCAL_PATH)/$(OSL_NATIVE_ADDONS_DIR)/*/addon_sources.mk)
OSL_NATIVE_PACKAGE_ADDON_MANIFESTS := $(foreach addonRoot,$(OSL_NATIVE_PACKAGE_ADDONS_DIR),$(wildcard $(LOCAL_PATH)/$(addonRoot)/*/addon_sources.mk))
OSL_NATIVE_DISCOVERED_ADDON_IDS :=
OSL_NATIVE_ADDON_MANIFESTS :=

define OSL_NATIVE_DISCOVER_ADDON_MANIFEST
OSL_NATIVE_DISCOVERED_ADDON_ID := $(notdir $(patsubst %/,%,$(dir $(1))))
ifeq ($(filter $(OSL_NATIVE_DISCOVERED_ADDON_ID),$(OSL_NATIVE_DISCOVERED_ADDON_IDS)),)
OSL_NATIVE_DISCOVERED_ADDON_IDS += $(OSL_NATIVE_DISCOVERED_ADDON_ID)
OSL_NATIVE_ADDON_MANIFESTS += $(1)
endif
endef

$(foreach addonManifest,$(OSL_NATIVE_REPO_ADDON_MANIFESTS) $(OSL_NATIVE_PACKAGE_ADDON_MANIFESTS),$(eval $(call OSL_NATIVE_DISCOVER_ADDON_MANIFEST,$(addonManifest))))
OSL_NATIVE_ADDON_IDS := $(notdir $(patsubst %/,%,$(dir $(OSL_NATIVE_ADDON_MANIFESTS))))

define OSL_NATIVE_INCLUDE_ADDON_MANIFEST
OSL_NATIVE_ADDON_MANIFEST := $(1)
OSL_NATIVE_ADDON_DIR := $(patsubst $(LOCAL_PATH)/%,%,$(patsubst %/,%,$(dir $(1))))
include $(1)
endef

ifneq ($(strip $(OSL_NATIVE_ADDON_MANIFESTS)),)
$(info OSLNative native addons: $(OSL_NATIVE_ADDON_IDS))
$(foreach addonManifest,$(OSL_NATIVE_ADDON_MANIFESTS),$(eval $(call OSL_NATIVE_INCLUDE_ADDON_MANIFEST,$(addonManifest))))
else
$(info OSLNative native addons: none)
endif

OSL_NATIVE_INCLUDE_DIRS := \
    $(OSL_NATIVE_CORE_INCLUDE_DIRS) \
    $(OSL_NATIVE_ADDON_INCLUDE_DIRS)

OSL_NATIVE_SOURCES := \
    $(OSL_NATIVE_COMMON_SOURCES) \
    $(OSL_NATIVE_GLOB_SOURCES:$(LOCAL_PATH)/%=%) \
    $(OSL_NATIVE_ADDON_SOURCES)
