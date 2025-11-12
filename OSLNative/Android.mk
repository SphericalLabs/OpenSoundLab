include $(CLEAR_VARS)

# override strip command to strip all symbols from output library; no need to ship with those..
# cmd-strip = $(TOOLCHAIN_PREFIX)strip $1

LOCAL_ARM_MODE  := arm
LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := libOSLNative
#LOCAL_CFLAGS    := -Werror
LOCAL_C_INCLUDES  := $(LOCAL_PATH)/FreeVerb/dfx-library
LOCAL_C_INCLUDES  += $(LOCAL_PATH)/FreeVerb/freeverb/components
FREEVERB_SOURCES := $(wildcard $(LOCAL_PATH)/FreeVerb/freeverb/components/*.cpp)
FREEVERB_SOURCES += $(wildcard $(LOCAL_PATH)/FreeVerb/dfx-library/*.cpp)
MASTERBUSRECORDER_SOURCES := $(wildcard $(LOCAL_PATH)/MasterBusRecorder/*.cpp)
LOCAL_SRC_FILES := main.cpp util.c Filter.cpp Compressor.cpp RingBuffer.cpp CRingBuffer.cpp Delay.cpp Freeverb.cpp resample.cpp Artefact.cpp $(MASTERBUSRECORDER_SOURCES) $(FREEVERB_SOURCES:$(LOCAL_PATH)/%=%)
LOCAL_LDLIBS    := -llog
LOCAL_CFLAGS := -Wno-implicit-const-int-float-conversion -Wno-braced-scalar-init

# optional: print source files
# $(warning $(LOCAL_SRC_FILES))
# $(warning $(LOCAL_C_INCLUDES))

include $(BUILD_SHARED_LIBRARY)
