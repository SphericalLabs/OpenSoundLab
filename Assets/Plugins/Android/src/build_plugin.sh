#!/bin/sh
echo ""
echo "Compiling NativeCode.c..."

C:\Users\Jim\AppData\Local\Android\Sdk\ndk\20.1.5948944\build\ndk-build.cmd NDK_PROJECT_PATH=. NDK_APPLICATION_MK=Application.mk $*
mv libs/armeabi-v7a/libnative.so ..

echo ""
echo "Cleaning up / removing build folders..."  #optional..
rm -rf libs
rm -rf obj

echo ""
echo "Done!"

sleep  30
'libs/armeabi/libnative.so':