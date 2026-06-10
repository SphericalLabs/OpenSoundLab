include $(CLEAR_VARS)

# override strip command to strip all symbols from output library; no need to ship with those..
# cmd-strip = $(TOOLCHAIN_PREFIX)strip $1

LOCAL_ARM_MODE  := arm
LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := libOSLNative
#LOCAL_CFLAGS    := -Werror
include $(LOCAL_PATH)/Build/native_sources.mk

LOCAL_C_INCLUDES  := $(OSL_NATIVE_INCLUDE_DIRS)
LOCAL_SRC_FILES := $(OSL_NATIVE_SOURCES)
LOCAL_LDLIBS    := -llog -lm
LOCAL_CFLAGS := -DTEST=1 -Wno-implicit-const-int-float-conversion -Wno-braced-scalar-init

# optional: print source files
# $(warning $(LOCAL_SRC_FILES))
# $(warning $(LOCAL_C_INCLUDES))

include $(BUILD_SHARED_LIBRARY)
