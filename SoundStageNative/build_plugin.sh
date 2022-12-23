#!/bin/sh

if [[ $USER == "hannes.barfuss@fhnw.ch" ]]
then
	NDK_PATH="/Users/hannes.barfuss@fhnw.ch/Library/Android/sdk/ndk/23.1.7779620/ndk-build"
	PLUGIN_PATH="/Users/hannes.barfuss@fhnw.ch/Documents/dev/soundstage/soundstagevr-quest/Assets/SoundStageNative/SoundStageNative"
	SOURCE_PATH="/Users/hannes.barfuss@fhnw.ch/Documents/dev/soundstage/soundstagevr-quest/SoundStageNative"
	cd $SOURCE_PATH
	echo ""
	echo "Compiling native code..."

	arch -x86_64 $NDK_PATH NDK_DEBUG=1 NDK_PROJECT_PATH=. NDK_APPLICATION_MK=./Application.mk $*

	mv libs/arm64-v8a/libSoundStageNative.so ../Assets/SoundStageNative/arm64/Release/libSoundStageNative.so
else

	NDK_PATH="/c/Users/Ludwig/android-ndk-r21e-windows-x86_64/android-ndk-r21e/build/ndk-build.cmd"
	echo ""
	echo "Compiling native code..."

	$NDK_PATH NDK_PROJECT_PATH=. NDK_APPLICATION_MK=./Application.mk $*
	mv libs/arm64-v8a/libSoundStageNative.so ../Assets/SoundStageNative/arm64/Release/libSoundStageNative.so
fi

echo ""
echo "Cleaning up / removing build folders..."  #optional..

rm -rf libs
rm -rf obj

echo ""
echo "Done!"
