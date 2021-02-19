#!/bin/sh
echo ""
echo "Compiling native code..."

/c/Users/Ludwig/android-ndk-r21e-windows-x86_64/android-ndk-r21e/build/ndk-build.cmd NDK_PROJECT_PATH=. NDK_APPLICATION_MK=Application.mk $*
mv libs/armeabi-v7a/libnative.so ./armv7/Release/libSoundStageNative.so

echo ""
echo "Cleaning up / removing build folders..."  #optional..
rm -rf libs
rm -rf obj

echo ""
echo "Done!"


