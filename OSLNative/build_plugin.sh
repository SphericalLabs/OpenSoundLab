#!/bin/sh

NDK_PATH="$HOME/android-ndk-r26b/build/ndk-build.cmd"
echo ""
echo "Compiling native code..."

$NDK_PATH NDK_PROJECT_PATH=. NDK_APPLICATION_MK=./Application.mk $*
mv libs/arm64-v8a/libOSLNative.so ../Assets/OSLNative/arm64/Release/libOSLNative.so

echo ""
echo "Cleaning up / removing build folders..."  #optional..

rm -rf libs
rm -rf obj

echo ""
echo "Done!"
