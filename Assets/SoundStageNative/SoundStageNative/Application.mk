APP_OPTIM        := release
APP_ABI          := armeabi-v7a
APP_PLATFORM     := android-23
APP_BUILD_SCRIPT := Android.mk
# IMPORTANT: c++_static will only work if we have one and only one .so in the whole application.
# Otherwise, we should use c++_shared and make sure libc++.so is included in the application!
# More information: https://developer.android.com/ndk/guides/cpp-support
APP_STL := c++_static
APP_CPPFLAGS += -std=c++17 -static-libstdc++
